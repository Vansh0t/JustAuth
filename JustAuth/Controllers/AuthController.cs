using System.Reflection;
using JustAuth.Data;
using JustAuth.Services.Auth;
using JustAuth.Services.Emailing;
using JustAuth.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JustAuth.Controllers
{
    [RequireHttps]
    [ApiController]
    [Route("auth")]
    public class AuthController<TUser>:ControllerBase where TUser:AppUser, new()
    {
        
        
        private readonly IUserManager<TUser> _userManager;
        private readonly IEmailService _emailing;
        private readonly ILogger<AuthController<TUser>> _logger;
        private readonly IJwtProvider _jwt;
        private readonly AuthDbMain<TUser> _context;
        private readonly MappingOptions _map;
        public AuthController(ILogger<AuthController<TUser>> logger,
                              IUserManager<TUser> userManager,
                              IEmailService emailing,
                              IAuthDbMain<TUser> context,
                              IJwtProvider jwt,
                              MappingOptions map
        ) {
            _logger = logger;
            _userManager = userManager;
            _emailing = emailing;
            _context = (AuthDbMain<TUser>)context;
            _jwt = jwt;
            _map = map;
        }
#region  USER
    [HttpPost("signup")]
        public async Task<IActionResult> SignUp(Dictionary<string,string> data) {
            string email, username, password, passwordConf;

            try {
                email = data["email"];
                username = data["username"];
                password = data["password"];
                passwordConf = data["passwordConf"];
            }
            catch {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest("Invalid signup data.");
            }

            if(password != passwordConf)
                return Conflict("Passwords don't match.");

            var userResult = await _userManager.CreateUserAsync(email, username, password);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var user = userResult.ResultObject;
            
            var emailResult = await _emailing.EmailSaveAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Location.GetEntryAssemblyPath(),"EmailTemplates", "EmailConfirm.html"),
                                                            $"{Request.GetBaseUrl()}{_map.EmailConfirmRedirectUrl}?vrft={user.EmailVrfToken}",
                                                            "EmailConfirmation");
            if(emailResult.IsError)
                return emailResult.ToActionResult();
            
            var response = new DTO.SignInResponse{
                User = new DTO.AppUserDTO(user),
                Jwt = new() {
                    Jwt = _jwt.ResolveJwt(user, HttpContext),
                    Expiration = DateTime.UtcNow.AddMinutes(_jwt.Options.TokenLifetime).GetEpochMilliseconds()
                },
                RefreshJwt = _jwt.ResolveRefreshJwt(user, HttpContext)
            };
            
            await _context.SaveChangesAsync(); //apply token changes
            return CreatedAtAction("SignUp", response);
        }
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn(Dictionary<string,string> data) {
            string credential, password;
            try {
                credential = data["credential"];
                password = data["password"];
            }
            catch {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest("Invalid signin data.");
            }

            var result = await _userManager.GetUserByCredentialAsync(credential);
            if(result.IsError)
                return result.ToActionResult();

            var user = result.ResultObject;

            if(!Cryptography.ValidatePasswordHash(user.PasswordHash, password))
                return StatusCode(403, "Check your username/password and try again.");
            
            var response = new DTO.SignInResponse{
                User = new DTO.AppUserDTO(user),
                Jwt = new() {
                    Jwt = _jwt.ResolveJwt(user, HttpContext),
                    Expiration = DateTime.UtcNow.AddMinutes(_jwt.Options.TokenLifetime).GetEpochMilliseconds()
                },    
                RefreshJwt = _jwt.ResolveRefreshJwt(user, HttpContext),

            };
            await _context.SaveChangesAsync();

            return Ok(response);
        }
        [Authorize]
        [HttpPost("signout")]
        public new IActionResult SignOut() { 
            if(!_jwt.Options.SendAsCookie) {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return StatusCode(404);
            } 
            HttpContext.Response.Cookies.Append(Const.JWT_COOKIE_NAME, "", new CookieOptions{
                MaxAge = TimeSpan.FromDays(0),
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            HttpContext.Response.Cookies.Append(Const.REFRESH_JWT_COOKIE_NAME, "", new CookieOptions{
                MaxAge = TimeSpan.FromDays(0),
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
            return Ok();
        }
#endregion
#region EMAIL
        [HttpPost("email/vrf")]
        public async Task<IActionResult> EmailVerification(Dictionary<string, string> data) {
            if(!data.TryGetValue("token", out string token) || token is null || token=="") {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest();
            }

            var result = await _userManager.VerifyEmailAsync(token);
            if(result.IsError)
                return result.ToActionResult();

            await _context.SaveChangesAsync();

            var user = result.ResultObject;
            //update user's jwt so we have verified status updated
            return Ok(
                new DTO.JwtDTO{
                    Jwt = _jwt.ResolveJwt(user, HttpContext),
                    Expiration = DateTime.UtcNow.AddMinutes(_jwt.Options.TokenLifetime).GetEpochMilliseconds()
                }
            );
        }
        [Authorize]
        [HttpGet("email/revrf")]
        public async Task<IActionResult> EmailReVerification() {

            var uId = HttpContext.User.GetUserId();

            _logger.LogInformation("User {uId} requested resend of verification email.", uId);

            var userResult = await _userManager.GetUserAsync(uId);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var result = await _userManager.SetEmailVerificationAsync(userResult.ResultObject);
            if(result.IsError)
                return result.ToActionResult();
            
            var user = userResult.ResultObject;
            var emailResult = await _emailing.EmailSaveAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Location.GetEntryAssemblyPath(), "EmailTemplates", "EmailConfirm.html"),
                                                            $"{Request.GetBaseUrl()}{_map.EmailConfirmRedirectUrl}?vrft={user.EmailVrfToken}",
                                                            "EmailConfirmation");
            if(emailResult.IsError)
                return emailResult.ToActionResult();

            return Redirect("/");
        }
        [Authorize("IsEmailVerified")]
        [HttpPost("email/change")]
        public async Task<IActionResult> EmailChange(Dictionary<string,string> data) {
            string newEmail, password;
            try {
                newEmail = data["newEmail"];
                password = data["password"];
            }
            catch {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest("Invalid signin data.");
            }

            var id = HttpContext.User.GetUserId();

            var userResult = await _userManager.GetUserAsync(id);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var user = userResult.ResultObject;

            if(!Cryptography.ValidatePasswordHash(user.PasswordHash, password))
                return StatusCode(403, "Check your password and try again.");

            var result = await _userManager.SetEmailChangeAsync(userResult.ResultObject, newEmail);
            if(result.IsError)
                return result.ToActionResult();
            
            var emailResult = await _emailing.EmailSaveAsync(_context,
                                                            newEmail, 
                                                            Path.Join(Location.GetEntryAssemblyPath(),"EmailTemplates", "EmailConfirm.html"),
                                                            $"{Request.GetBaseUrl()}{_map.EmailConfirmRedirectUrl}?vrft={user.EmailVrfToken}",
                                                            "EmailConfirmation");
            if(emailResult.IsError)
                return emailResult.ToActionResult();

            return Ok();
        }
#endregion
#region PASSWORD
        [HttpPost("pwd/reset1")]
        public async Task<IActionResult> PasswordReset1(Dictionary<string, string> data) {
            if(_map.PasswordResetRedirectUrl is null) return NotFound();
            string email;
            try {
                email = data["email"];
            }
            catch {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest("Invalid signin data.");
            }

            var userResult = await _userManager.GetUserByEmailAsync(email);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var user = userResult.ResultObject;

            var result = await _userManager.SetPasswordResetAsync(user);
            if(result.IsError)
                return result.ToActionResult();
            
            var emailResult = await _emailing.EmailSaveAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Location.GetEntryAssemblyPath(),"EmailTemplates", "PasswordReset.html"),
                                                            $"{Request.GetBaseUrl()}{_map.PasswordResetRedirectUrl}?rst={user.PasswordResetToken}",
                                                            "PasswordReset");
            if(emailResult.IsError)
                return emailResult.ToActionResult();
            return Ok();
        }
        [HttpPost("pwd/reset2")]
        public async Task<IActionResult> PasswordReset2(Dictionary<string, string> data) {
            if(_map.PasswordResetRedirectUrl is null) return NotFound();
            string email, token, newPassword, newPasswordConf;
            try {
                email = data["email"];
                newPassword = data["newPassword"];
                newPasswordConf = data["newPasswordConf"];
                token = data["token"];
            }
            catch {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest("Invalid data.");
            }
            
            if(newPassword != newPasswordConf)
                return Conflict("Passwords don't match.");

            var userResult = await _userManager.GetUserByEmailAsync(email);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var result = _userManager.VerifyPassword(userResult.ResultObject, token, newPassword);
            if(result.IsError) {
                return result.ToActionResult();
            }
                

            await _context.SaveChangesAsync();
            return Ok();
        }
#endregion
        #region JWT
        [HttpPost(Const.REFRESH_JWT_PATH)]
        public async Task<IActionResult> RefreshJwt(Dictionary<string,string> data) {
            string accessToken, refreshToken;
            try {
                if(_jwt.Options.SendAsCookie) {
                    accessToken = HttpContext.Request.Cookies[Const.JWT_COOKIE_NAME];
                    refreshToken = HttpContext.Request.Cookies[Const.REFRESH_JWT_COOKIE_NAME];
                } 
                else {
                    accessToken = data["accessToken"];
                    refreshToken = data["refreshToken"];
                }
                    
            }
            catch {
                _logger.LogWarning("Got {action} request with one of the fields empty. Client - {host}", MethodBase.GetCurrentMethod().Name, HttpContext.GetRequestIP());
                return BadRequest("Invalid token data.");
            }
            (var claims, var secToken) = _jwt.ParseJwt(accessToken, false);
            if(claims is null)
                return StatusCode(403);
            var userId = claims.GetUserId();

            //check if token was revoked
            var dbRefreshToken = await _context.RefreshTokens.Include(_=>_.User).FirstOrDefaultAsync(_=>_.UserId==userId);
            //check with token from db
            if(dbRefreshToken is null || dbRefreshToken.IsExpired() || dbRefreshToken.Token != refreshToken)
                return StatusCode(403);
            var user = dbRefreshToken.User;
            var response = _jwt.ResolveJwt(user, HttpContext);
            return Ok(
                new DTO.JwtDTO{
                    Jwt = _jwt.ResolveJwt(user, HttpContext),
                    Expiration = DateTime.UtcNow.AddMinutes(_jwt.Options.TokenLifetime).GetEpochMilliseconds()
                }
            );
        }
        #endregion
        
    }
}