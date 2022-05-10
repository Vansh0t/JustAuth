using JustAuth.Data;

namespace JustAuth.Services.Auth
{
    public interface IJwtProvider
    {
        string GenerateJwt(AppUser user);
    }
}