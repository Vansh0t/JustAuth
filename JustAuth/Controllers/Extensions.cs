using JustAuth.Services;
using Microsoft.AspNetCore.Mvc;

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