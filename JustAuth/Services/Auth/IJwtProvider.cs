using JustAuth.Data;

namespace JustAuth.Services.Auth
{
    public interface IJwtProvider
    {
        JwtOptions Options {get;}
        string GenerateJwt(AppUser user);
    }
}