using Microsoft.EntityFrameworkCore;
namespace JustAuth.Services.Auth
{
    using Data;
    using Utils;
    using JustAuth.Services.Validation;

    public class UserManager<TUser>:IUserManager<TUser> where TUser: AppUser, new()
    {
        const int VERIFICATION_TOKENS_HOURS = 24;
        private readonly IAuthDbMain<TUser> _context;
        private readonly ILogger<UserManager<TUser>> _logger;
        private readonly IEmailValidator _emailValidator;
        private readonly IPasswordValidator _pwdValidator;
        private readonly IUsernameValidator _uNameValidator;
        public UserManager( IAuthDbMain<TUser> context,
                            ILogger<UserManager<TUser>> logger,
                            IEmailValidator emailValidator,
                            IPasswordValidator pwdValidator,
                            IUsernameValidator uNameValidator
                            )  {
            _context =context;
            _logger = logger;
            _emailValidator = emailValidator;
            _pwdValidator = pwdValidator;
            _uNameValidator = uNameValidator;
        } 
#region USER
    public async Task<IServiceResult<TUser>> CreateUserAsync(string email, string username, string password) {
            TUser newUser = new ();
            IServiceResult opResult;
            opResult = await SetEmailAsync(newUser, email);
            if (opResult.IsError)
                return ServiceResult<TUser>.Fail(opResult);
            opResult = await SetUsernameAsync(newUser, username);
            if (opResult.IsError)
                return ServiceResult<TUser>.Fail(opResult);
            opResult = SetPassword(newUser, password);
            if (opResult.IsError)
                return ServiceResult<TUser>.Fail(opResult);
            opResult = await SetEmailVerificationAsync(newUser);
            if (opResult.IsError)
                return ServiceResult<TUser>.Fail(opResult);
            _context.Users.Add(newUser);
            return ServiceResult<TUser>.Success(newUser);
        }
    public async Task<IServiceResult<TUser>> GetUserAsync(int id) {
            try {
                var user = await _context.Users.Include(_=>_.JwtRefreshToken).FirstOrDefaultAsync(_=>_.Id==id);
                return ServiceResult<TUser>.FromResultObject(user, nullCaseError: "Requested user does not exist");
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult<TUser>.FailInternal();
            }
        }
    public async Task<IServiceResult<TUser>> GetUserByEmailAsync(string email) {
            try {
                var user = await _context.Users.Include(_=>_.JwtRefreshToken).FirstOrDefaultAsync(_=>_.Email==email);
                return ServiceResult<TUser>.FromResultObject(user, nullCaseError: "Requested user does not exist");
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult<TUser>.FailInternal();
            }
            
        }
    public async Task<IServiceResult<TUser>> GetUserByUsernameAsync(string username) {
        try {
            var user = await _context.Users.Include(_=>_.JwtRefreshToken).FirstOrDefaultAsync(_=>_.Email==username);
            return ServiceResult<TUser>.FromResultObject(user, nullCaseError: "Requested user does not exist");
        }
        catch (Exception e) {
            _logger.LogError("{e}", e.ToString());
            return ServiceResult<TUser>.FailInternal();
        }
    }
    public async Task<IServiceResult<TUser>> GetUserByCredentialAsync(string credential) {
        try {
            var user = await _context.Users.Include(_=>_.JwtRefreshToken).FirstOrDefaultAsync(_=>_.Email==credential || _.Username==credential);
            return ServiceResult<TUser>.FromResultObject(user, nullCaseError: "Requested user does not exist");
        }
        catch (Exception e) {
            _logger.LogError("{e}", e.ToString());
            return ServiceResult<TUser>.FailInternal();
        }
    }
#endregion
#region  PASSWORD
    public IServiceResult SetPassword(TUser user, string password) {
            try {
                //validation
                var valid = _pwdValidator.Validate(password);
                if(valid.IsError)
                    return valid;
                //action
                user.PasswordHash = Cryptography.HashPassword(password);
                return ServiceResult.Success();
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult.FailInternal();
            }
        }
    public async Task<IServiceResult> SetPasswordResetAsync(TUser user) {
        try {
            //validation
            if (!user.IsEmailVerified) 
                return ServiceResult.Fail(403, "Verify your current email before changing password.");
            var token = Cryptography.GetRandomToken();
            //Make sure we never have dublicates
            //Guid chances are VERY small but still
            while (await _context.Users.AnyAsync(_=>_.PasswordResetToken == token)) {
                token = Cryptography.GetRandomToken();
            }
            //action
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(VERIFICATION_TOKENS_HOURS);
            return ServiceResult.Success(); 
        }
        catch (Exception e) {
            _logger.LogError("{e}", e.ToString());
            return ServiceResult.FailInternal();
        }
        
    }
    public IServiceResult<TUser> VerifyPassword(TUser user, string token, string newPassword) {
        try {
            //validation
            if(token is null || user is null || user.PasswordResetToken != token)
                return ServiceResult<TUser>.Fail(403, "Forbidden.");
            if(DateTime.UtcNow > user.PasswordResetTokenExpiration )
                return ServiceResult<TUser>.Fail(401, "Verification link has expired.");
            var result = SetPassword(user, newPassword);
            if(result.IsError) 
                return ServiceResult<TUser>.Fail(result);
            //action
            ClearPasswordReset(user);
            
            return ServiceResult<TUser>.Success(user);
        }
        catch (Exception e) {
            _logger.LogError("{e}", e.ToString());
            return ServiceResult<TUser>.FailInternal();
        }
        

    }
    public void ClearPasswordReset(TUser user) {
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiration = null;
    }

#endregion
#region EMAIL
    public async Task<IServiceResult> SetEmailAsync(TUser user, string newEmail) {
            try {
                var valid = _emailValidator.Validate(newEmail);
                if(valid.IsError)
                    return valid;
                var available = await CheckEmailAvailableAsync(newEmail);
                if(!available) 
                    return ServiceResult.Fail(409, "Email already occupied. Please, choose another.");
                user.Email = newEmail;
                return ServiceResult.Success();
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult.FailInternal();
            }
        }
    public async Task<IServiceResult> SetEmailChangeAsync(TUser user, string newEmail) {
            try {
                if (!user.IsEmailVerified) 
                    return ServiceResult.Fail(403, "Verify your current email before changing it.");
                var valid = _emailValidator.Validate(newEmail);
                if(valid.IsError)
                    return valid;
                var available = await CheckEmailAvailableAsync(newEmail);
                if(!available) 
                    return ServiceResult.Fail(409, "Email already occupied. Please, choose another.");
                var token = Cryptography.GetRandomToken();
                //Make sure we never have dublicates
                while (await _context.Users.AnyAsync(_=>_.EmailVrfToken == token)) {
                    token = Cryptography.GetRandomToken();
                }
                user.EmailVrfToken = token;
                user.EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(VERIFICATION_TOKENS_HOURS);
                user.NewEmail  = newEmail;
                return ServiceResult.Success();
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult.FailInternal();
            }
        }
    public async Task<IServiceResult> SetEmailVerificationAsync(TUser user) {
            try {
                if(user.IsEmailVerified)
                    return ServiceResult.Fail(403, "Forbidden.");
                var token = Cryptography.GetRandomToken();
                //Make sure we never have dublicates
                //Guid chances are VERY small but still
                while (await _context.Users.AnyAsync(_=>_.EmailVrfToken == token)) {
                    token = Cryptography.GetRandomToken();
                }
                user.IsEmailVerified = false;
                user.EmailVrfToken = token;
                user.EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(VERIFICATION_TOKENS_HOURS);
                return ServiceResult.Success();
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult.FailInternal();
            }
        }
    public async Task<IServiceResult<TUser>> VerifyEmailAsync(string token) {
        try {
            TUser user = await _context.Users.FirstOrDefaultAsync(_=>_.EmailVrfToken==token);
            if(token is null || user is null)
                return ServiceResult<TUser>.Fail(403, "Forbidden.");
            if(DateTime.UtcNow > user.EmailVrfTokenExpiration)
                return ServiceResult<TUser>.Fail(401, "Verification link has expired.");
            if(user.NewEmail is not null) {
                var result = await SetEmailAsync(user, user.NewEmail);
                if(result.IsError)
                    return ServiceResult<TUser>.Fail(result);
            }
                
            user.IsEmailVerified = true;
            ClearEmailVerification(user);
            return ServiceResult<TUser>.Success(user);
        }
        catch (Exception e) {
            _logger.LogError("{e}", e.ToString());
            return ServiceResult<TUser>.FailInternal();
        }
        
    }
    public void ClearEmailVerification(TUser user) {
        user.EmailVrfToken = null;
        user.EmailVrfTokenExpiration = null;
        user.NewEmail = null;
    }
    public async Task<bool> CheckEmailAvailableAsync(string email) {
            var exists = await _context.Users.AnyAsync(_=>_.Email==email);
            return !exists;
        }
#endregion
#region USERNAME
    public async Task<IServiceResult> SetUsernameAsync(TUser user, string newUsername) {
            try {
                //validation
                var valid = _uNameValidator.Validate(newUsername);
                if(valid.IsError)
                    return valid;
                var available = await CheckUsernameAvailableAsync(newUsername);
                if(!available) 
                    return ServiceResult.Fail(409, "Username already occupied. Please, choose another.");
                //action
                user.Username = newUsername;
                return ServiceResult.Success();
            }
            catch (Exception e) {
                _logger.LogError("{e}", e.ToString());
                return ServiceResult.FailInternal();
            }
        }
    public async Task<bool> CheckUsernameAvailableAsync(string username) {
            var exists = await _context.Users.AnyAsync(_=>_.Username==username);
            return !exists;
        }
#endregion
        

        
    }
}