using JustAuth.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
namespace JustAuth.Services.Auth
{
    public class JwtProvider:IJwtProvider
    {
        public JwtOptions Options {get;}
        private readonly TokenValidationParameters _validationParams;
        public JwtProvider (JwtOptions options, TokenValidationParameters validationParams) {
            Options = options;
            _validationParams = validationParams;
        }
        public string GenerateJwt(AppUser user) {
            var now = DateTime.UtcNow;
            var claims = new List<Claim>() { 
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username), 
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                };
            foreach (var c in Options.Claims)
            {
                claims.Add(new Claim(c.Name, user.GetType().GetProperty(c.ModelProperty).GetValue(user).ToString())); 
            }
            var jwt = new JwtSecurityToken(
                    issuer: Options.Issuer,
                    audience: Options.Audience,
                    notBefore: now,
                    claims: claims,
                    expires: now.Add(TimeSpan.FromMinutes(Options.TokenLifetime)),
                    signingCredentials: new SigningCredentials(Options.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        public JwtSecurityToken ParseJwt(string token) {
            return new JwtSecurityToken(token);
        }
        public (ClaimsPrincipal, SecurityToken) ValidateJwt(string token) {
            var handler = new JwtSecurityTokenHandler();
            try {
                SecurityToken validToken;
                var claims = handler.ValidateToken(token, _validationParams, out validToken);
                return (claims, validToken);
            }
            catch {
                return (null, null);
            }
            
        }
    }
}