using JustAuth.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
namespace JustAuth.Services.Auth
{
    public class JwtProvider:IJwtProvider
    {
        private readonly JwtOptions _options;
        public JwtProvider (JwtOptions options) {
            _options = options;
        }
        public string GenerateJwt(AppUser user) {
            var now = DateTime.UtcNow;
            var claims = new List<Claim>() { 
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username), 
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString())
                };
            foreach (var c in _options.Claims)
            {
                claims.Add(new Claim(c.Name, user.GetType().GetProperty(c.ModelProperty).GetValue(user).ToString())); 
            }
            var jwt = new JwtSecurityToken(
                    issuer: _options.Issuer,
                    audience: _options.Audience,
                    notBefore: now,
                    claims: claims,
                    expires: now.Add(TimeSpan.FromMinutes(_options.TokenLifetime)),
                    signingCredentials: new SigningCredentials(_options.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}