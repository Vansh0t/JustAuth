using JustAuth.Data;
using JustAuth.Services;
using JustAuth.Services.Auth;
using JustAuth.Services.Emailing;
using JustAuth.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

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

        public AuthController(ILogger<AuthController<TUser>> logger,
                              IUserManager<TUser> userManager,
                              IEmailService emailing,
                              AuthDbMain<TUser> context,
                              IJwtProvider jwt
        ) {
            _logger = logger;
            _userManager = userManager;
            _emailing = emailing;
            _context = context;
            _jwt = jwt;
        }
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
                return BadRequest("Passwords don't match.");
            var userResult = await _userManager.CreateUserAsync(email, username, password);
            if(userResult.IsError)
                return userResult.ToActionResult();
            var user = userResult.ResultObject;
            
            using (var transaction = await _context.Database.BeginTransactionAsync()) {
                var tGuid = Guid.NewGuid().ToString();
                await transaction.CreateSavepointAsync(tGuid);
                //Create user in database
                await _context.SaveChangesAsync();
                Console.WriteLine("TTT " + Request.PathBase);
                //Try send email
                var emailResult = await _emailing.SendEmailAsync(
                    user.Email, 
                    Path.Join("EmailTemplates", "EmailConfirm.html"),
                    $"{Request.GetBaseUrl()}/auth/email/vrf?vrft={user.EmailVrfToken}",
                    "EmailConfirmation"
                );
                if(emailResult.IsError) {
                    //On email sending error, revert changes to db
                    await transaction.RollbackToSavepointAsync(tGuid);
                    return emailResult.ToActionResult();
                }
                await transaction.CommitAsync();
                
            }
            return CreatedAtAction("SignUp", 
                new DTO.SignInResponse {
                User = new DTO.AppUserDTO (user),
                Jwt = _jwt.GenerateJwt(user)
            });
        }
        [HttpPost("signin")]
        public async Task<IActionResult> SignIn(Dictionary<string,string> data) {
            string username, password;
            try {
                username = data["username"];
                password = data["password"];
            }
            catch {
                _logger.LogWarning("Got SignIn request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }
            var result = await _userManager.GetUserAsync(username);
            Console.WriteLine(result.Error);
            if(result.IsError)
                return result.ToActionResult();
            var user = result.ResultObject;
            if(!Cryptography.ValidatePasswordHash(user.PasswordHash, password))
                return StatusCode(403, "Check your username/password and try again.");
            return Ok(
                new DTO.SignInResponse {
                    User = new DTO.AppUserDTO(user),
                    Jwt = _jwt.GenerateJwt(user)
                }
            );
        }
        [HttpGet("email/vrf")]
        public async Task<IActionResult> EmailVerfification(string vrft) {
            if(vrft is null || vrft=="") {
                _logger.LogWarning("Got SignIn request with one of the fields empty. Host {host}", HttpContext.Request.Host.Value);
                return BadRequest("Invalid signin data.");
            }
            var result = await _userManager.VerifyEmailAsync(vrft);
            if(result.IsError)
                return result.ToActionResult();
            return Redirect("/");
        }



        
    }
}