using System.Security.Claims;
using JustAuth.Data;
using JustAuth.Services;
using JustAuth.Services.Auth;
using JustAuth.Services.Emailing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JustAuth.Controllers
{
    public static class Extensions
    {
        /// <summary>
        /// Get IAction result from IServiceResult with the same status code.
        /// In case of an error, the error string is passed to IActionResult as a result object
        /// </summary>
        public static IActionResult ToActionResult(this IServiceResult serviceResult) {
            IActionResult aResult = new ObjectResult(serviceResult.Error) {
                StatusCode = serviceResult.Code
            };
            return aResult;
        }
        /// <summary>
        /// Get IAction result from IServiceResult<TObj> with the same status code.
        /// In case of success the ResultObject is passed to IActionResult
        /// In case of an error, the error string is passed to IActionResult as a result object
        /// </summary>
        public static IActionResult ToActionResult<TObj>(this IServiceResult<TObj> serviceResult) {
            object obj = serviceResult.IsError?serviceResult.Error:serviceResult.ResultObject;
            IActionResult aResult = new ObjectResult(obj) {
                StatusCode = serviceResult.Code
            };
            return aResult;
        }
        public static string GetBaseUrl(this HttpRequest request) {
            return $"{request.Scheme}://{request.Host.Value}";
        }
        public static int GetUserId(this ClaimsPrincipal user) {
            return int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        }
        public static string GetUserName(this ClaimsPrincipal user) {
            return user.FindFirstValue(ClaimTypes.Name);
        }
        public static string GetRefreshClaim(this ClaimsPrincipal user) {
            return user.FindFirstValue("IsRefreshToken");
        }
        /// <summary>
        /// Perform code within single transaction with ability to revert db changes on errors.
        /// </summary>
        public static async Task<IServiceResult> UsingAtomicTransactionAsync<TDbContext>(
            this TDbContext context, 
            Func<IDbContextTransaction, Task<IServiceResult>> action)
            where TDbContext: DbContext
        {
            try {
                using var transaction = await context.Database.BeginTransactionAsync();
                return await action(transaction);
            }
            catch {
                return ServiceResult.FailInternal();
            }
        }
        /// <summary>
        /// Saves all changes to database only if email is sent successfully
        /// </summary>
        /// <typeparam name="TDbContext"></typeparam>
        /// <returns></returns>
        public static async Task<IServiceResult> EmailSaveAsync(
            this IEmailService service,
            DbContext context,
            string email,
            string htmlTemplate,
            string actionData,
            string subject
            )
         {
            var emailResult = await context.UsingAtomicTransactionAsync(async (transaction)=>{
                //prepare to save any previous operations
                await context.SaveChangesAsync();
                //Try send email
                var emailResult = await service.SendEmailAsync(
                    email, 
                    htmlTemplate,
                    actionData,
                    subject
                );
                if(emailResult.IsError) {
                    //On email sending error rollback
                    await transaction.RollbackAsync();
                    return emailResult;
                }
                await transaction.CommitAsync();
                return ServiceResult.Success();
            });
            return emailResult;
        }
        /// <summary>
        /// Get refresh jwt for user or set it to cookie
        /// </summary>
        /// <returns>refresh jwt or null if SendAsCookie = true</returns>
        public static string ResolveRefreshJwt(this IJwtProvider jwtProvider, AppUser user, HttpContext httpContext) {
            if(jwtProvider.Options.UseRefreshToken) {
                string refreshJwt;
                int expiration = jwtProvider.Options.RefreshTokenLifetime;
                if(user.JwtRefreshToken is null || user.JwtRefreshToken.IsExpired()) {
                    refreshJwt = jwtProvider.GenerateJwtRefresh();
                    user.JwtRefreshToken = new JwtRefreshToken {
                        Token = refreshJwt,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(expiration),
                        IssuedAt = DateTime.UtcNow
                    };
                }
                else
                    refreshJwt = user.JwtRefreshToken.Token;
                if(jwtProvider.Options.SendAsCookie) {
                    httpContext.Response.Cookies.Append(Const.REFRESH_JWT_COOKIE_NAME, refreshJwt,
                    new CookieOptions{
                        HttpOnly = true,
                        MaxAge = TimeSpan.FromMinutes(expiration),
                        Path = Const.REFRESH_JWT_PATH,
                        SameSite = SameSiteMode.Strict,
                        Secure = true

                    });
                    return null;
                }
                return refreshJwt;
            }
            return null;
            
        }
        /// <summary>
        /// Get jwt for user or set it to cookie
        /// </summary>
        /// <returns>jwt or null if SendAsCookie = true</returns>
        public static string ResolveJwt(this IJwtProvider jwtProvider, AppUser user, HttpContext httpContext) {
            var jwt = jwtProvider.GenerateJwt(user);
            if(jwtProvider.Options.SendAsCookie) {
                httpContext.Response.Cookies.Append(Const.JWT_COOKIE_NAME, jwt,
                new CookieOptions{
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                });
                return null;
            }
            return jwt;
        }
        public static long GetEpochMilliseconds(this DateTime dt) {
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }
        public static string GetRequestIP(this HttpContext context) {
           return context.Connection.RemoteIpAddress.ToString();
        }
    }
}