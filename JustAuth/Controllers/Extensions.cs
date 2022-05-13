using System.Security.Claims;
using JustAuth.Data;
using JustAuth.Services;
using JustAuth.Services.Emailing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.JsonWebTokens;

namespace JustAuth.Controllers
{
    public static class Extensions
    {
        public static IActionResult ToActionResult(this IServiceResult serviceResult) {
            IActionResult aResult = new ObjectResult(serviceResult.Error) {
                StatusCode = serviceResult.Code
            };
            return aResult;
        }
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
        /// <summary>
        /// Perform code within single transaction to revert db changes on errors.
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
        public static async Task<IServiceResult> EmailSafeAsync(
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
        /// Shortcut for creating Dictionary<string,object>
        /// </summary>
        /// <param name="kvp">Array of string in order ["key", "value", "key", "value", ...]</param>
        //public static Dictionary<string, object> Dict(params object[] kvp) {
        //    if(kvp.Length == 0 || kvp.Length % 2 != 0) 
        //        throw new ArgumentException("Invalid kvp length " + kvp.Length);
        //    var output = new Dictionary<string, string>();
        //    for (int i = 0; i < kvp.Length; i+=2) {
        //        output.Add((string)kvp[i], kvp[i+1]);
        //    }
        //    return output;
        //}
    }
}