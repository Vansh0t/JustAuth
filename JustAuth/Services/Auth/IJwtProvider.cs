using System.Security.Claims;
using JustAuth.Data;
using Microsoft.IdentityModel.Tokens;

namespace JustAuth.Services.Auth
{
    public interface IJwtProvider
    {
        JwtOptions Options {get;}
        string GenerateJwt(AppUser user);
        string GenerateJwtRefresh();
        (ClaimsPrincipal, SecurityToken) ParseJwt(string token, bool withValidation);
    }
}