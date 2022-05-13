using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using JustAuth.Data;
using JustAuth.Services;
using JustAuth.Services.Auth;
using JustAuth.Services.Emailing;
using JustAuth.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
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
                _logger.LogWarning("Got SignUp request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signup data.");
            }

            if(password != passwordConf)
                return Conflict("Passwords don't match.");

            var userResult = await _userManager.CreateUserAsync(email, username, password);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var user = userResult.ResultObject;
            
            var emailResult = await _emailing.EmailSafeAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Utils.GetEntryAssemblyPath(),"EmailTemplates", "EmailConfirm.html"),
                                                            $"{Request.GetBaseUrl()}{_map.EmailConfirmRedirectUrl}?vrft={user.EmailVrfToken}",
                                                            "EmailConfirmation");
            if(emailResult.IsError)
                return emailResult.ToActionResult();
            var jwt = _jwt.GenerateJwt(user);
            if(_jwt.Options.SendAsCookie)
                HttpContext.Response.Cookies.Append("jwt", _jwt.GenerateJwt(user),
                new CookieOptions{
                    HttpOnly = true,
                    MaxAge = TimeSpan.FromMinutes(_jwt.Options.TokenLifetime-1)//-1 minute to avoid clock conflict with frontend
                });
            return CreatedAtAction("SignUp", 
                new DTO.SignInResponse {
                User = new DTO.AppUserDTO (user),
                Jwt = _jwt.Options.SendAsCookie ? null:jwt
            });
        }
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn(Dictionary<string,string> data) {
            string credential, password;
            try {
                credential = data["credential"];
                password = data["password"];
            }
            catch {
                _logger.LogWarning("Got SignIn request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }

            var result = await _userManager.GetUserAsync(credential);
            if(result.IsError)
                return result.ToActionResult();

            var user = result.ResultObject;

            if(!Cryptography.ValidatePasswordHash(user.PasswordHash, password))
                return StatusCode(403, "Check your username/password and try again.");
                
            var jwt = _jwt.GenerateJwt(user);
            if(_jwt.Options.SendAsCookie)
                HttpContext.Response.Cookies.Append("jwt", _jwt.GenerateJwt(user),
                new CookieOptions{
                    HttpOnly = true,
                    MaxAge = TimeSpan.FromMinutes(_jwt.Options.TokenLifetime-1)//-1 minute to avoid clock conflict with frontend
                }
                );
            return Ok(
                new DTO.SignInResponse {
                    User = new DTO.AppUserDTO(user),
                    Jwt = _jwt.Options.SendAsCookie ? null:jwt
                }
            );
        }
#endregion
#region EMAIL
        [HttpGet("email/vrf")]
        public async Task<IActionResult> EmailVerification(string vrft) {
            if(vrft is null || vrft=="") {
                _logger.LogWarning("Got EmailVerification request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest();
            }

            var result = await _userManager.VerifyEmailAsync(vrft);
            if(result.IsError)
                return result.ToActionResult();

            await _context.SaveChangesAsync();

            return Ok();
        }
        [Authorize]
        [HttpPost("email/revrf")]
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
            var emailResult = await _emailing.EmailSafeAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Utils.GetEntryAssemblyPath(), "EmailTemplates", "EmailConfirm.html"),
                                                            $"{Request.GetBaseUrl()}{_map.EmailConfirmRedirectUrl}?vrft={user.EmailVrfToken}",
                                                            "EmailConfirmation");
            if(emailResult.IsError)
                return emailResult.ToActionResult();

            return Ok();
        }
        [Authorize("IsEmailVerified")]
        [HttpPost("email/change")]
        public async Task<IActionResult> EmailChange(Dictionary<string,string> data) {
            string newEmail;
            try {
                newEmail = data["newEmail"];
            }
            catch {
                _logger.LogWarning("Got EmailChange request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }

            var id = HttpContext.User.GetUserId();

            var userResult = await _userManager.GetUserAsync(id);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var user = userResult.ResultObject;

            var result = await _userManager.SetEmailChangeAsync(userResult.ResultObject, newEmail);
            if(result.IsError)
                return result.ToActionResult();
            
            var emailResult = await _emailing.EmailSafeAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Utils.GetEntryAssemblyPath(),"EmailTemplates", "EmailConfirm.html"),
                                                            $"{Request.GetBaseUrl()}{_map.EmailConfirmRedirectUrl}?vrft={user.EmailVrfToken}",
                                                            "EmailConfirmation");
            if(emailResult.IsError)
                return emailResult.ToActionResult();

            //await _context.SaveChangesAsync();
            return Ok();
        }
#endregion
#region PASSWORD
        [HttpPost("pwd/reset1")]
        public async Task<IActionResult> PasswordReset1(Dictionary<string, string> data) {
            if(_map.PasswordResetRedirectUrl is null) return NotFound();
            string credential;
            try {
                credential = data["credential"];
            }
            catch {
                _logger.LogWarning("Got PasswordReset1 request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }

            var userResult = await _userManager.GetUserAsync(credential);
            if(userResult.IsError)
                return userResult.ToActionResult();

            var user = userResult.ResultObject;

            var result = await _userManager.SetPasswordResetAsync(user);
            if(result.IsError)
                return result.ToActionResult();
            
            var emailResult = await _emailing.EmailSafeAsync(_context,
                                                            user.Email, 
                                                            Path.Join(Utils.GetEntryAssemblyPath(),"EmailTemplates", "PasswordReset.html"),
                                                            $"{Request.GetBaseUrl()}{_map.PasswordResetRedirectUrl}?rst={user.PasswordResetToken}",
                                                            "PasswordReset");
            if(emailResult.IsError)
                return emailResult.ToActionResult();
            return Ok();
        }
        [HttpPost("pwd/reset2")]
        public async Task<IActionResult> PasswordReset2(Dictionary<string, string> data) {
            if(_map.PasswordResetRedirectUrl is null) return NotFound();
            string credential, token, newPassword, newPasswordConf;
            try {
                credential = data["credential"];
                newPassword = data["newPassword"];
                newPasswordConf = data["newPasswordConf"];
                token = data["token"];
            }
            catch {
                _logger.LogWarning("Got PasswordReset2 request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }
            
            if(newPassword != newPasswordConf)
                return Conflict("Passwords don't match.");

            var userResult = await _userManager.GetUserAsync(credential);
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
        [HttpPost("jwt/refresh")]
        public async Task<IActionResult> RefreshJwt(Dictionary<string,string> data) {
            string refreshToken;
            try {
                refreshToken = data["refreshToken"];
            }
            catch {
                _logger.LogWarning("Got RefreshJwt request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }
            return Ok();
            //var userId = token.Header.
            //var jwt = _jwt.GenerateJwt(user);
            //if(_jwt.Options.SendAsCookie)
            //    HttpContext.Response.Cookies.Append("jwt", _jwt.GenerateJwt(user),
            //    new CookieOptions{
            //        HttpOnly = true,
            //        MaxAge = TimeSpan.FromMinutes(_jwt.Options.TokenLifetime-1)//-1 minute to avoid clock conflict with frontend
            //    }
            //    );
            //return Ok(
            //    new DTO.SignInResponse {
            //        User = new DTO.AppUserDTO(user),
            //        Jwt = _jwt.Options.SendAsCookie ? null:jwt
            //    }
            //);
        }
        #endregion
        
    }
}