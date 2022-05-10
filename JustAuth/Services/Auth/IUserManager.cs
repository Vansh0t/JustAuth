using JustAuth.Data;

namespace JustAuth.Services.Auth
{
    public interface IUserManager<TUser> where TUser: new()
    {
#region USER
        Task<IServiceResult<TUser>> CreateUserAsync(string email, string username, string password);
        Task<IServiceResult<TUser>> GetUserAsync(int id);
        Task<IServiceResult<TUser>> GetUserAsync(string username);
#endregion
#region  PASSWORD
        IServiceResult SetPassword(TUser user, string newPassword);
        Task<IServiceResult> SetPasswordResetAsync(TUser user);
        Task<IServiceResult<TUser>> VerifyPasswordAsync(string token, string newPassword);
        void ClearPasswordReset(TUser user);
#endregion
#region EMAIL

        Task<IServiceResult> SetEmailAsync(TUser user, string newEmail);


        Task<IServiceResult> SetEmailChangeAsync(TUser user, string newEmail);

        Task<IServiceResult> SetEmailVerificationAsync(TUser user);

        Task<IServiceResult<TUser>> VerifyEmailAsync(string token);
        void ClearEmailVerification(TUser user);

        Task<bool> CheckEmailAvailableAsync(string email);

#endregion
#region USERNAME
    Task<IServiceResult> SetUsernameAsync(TUser user, string newUsername);
    Task<bool> CheckUsernameAvailableAsync(string username);
#endregion
    }

    
}