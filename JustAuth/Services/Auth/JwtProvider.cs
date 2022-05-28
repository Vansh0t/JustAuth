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
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("IsEmailVerified", user.IsEmailVerified.ToString()) //email verification is always present
                };
            if(Options.Claims is not null)
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
        public string GenerateJwtRefresh() {
            return Utils.Cryptography.GetRandomToken();
        }
        public JwtSecurityToken ParseJwt(string token) {
            return new JwtSecurityToken(token);
        }
        public (ClaimsPrincipal, SecurityToken) ParseJwt(string token, bool withValidation) {
            var handler = new JwtSecurityTokenHandler();
            try {
                var valParams = withValidation?_validationParams:new TokenValidationParameters{
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _validationParams.IssuerSigningKey,
                    ValidateLifetime = false
                };
                var claims = handler.ValidateToken(token, valParams, out var validToken);
                return (claims, validToken);
            }
            catch {
                return (null, null);
            }
            
        }
    }
}