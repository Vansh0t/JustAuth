namespace JustAuth.Services
{
    public class ServiceResult:IServiceResult
    {
        /// <summary>
        /// An http status code
        /// </summary>
        public int Code {get;set;}
        /// <summary>
        /// Error message, null if IsError = false
        /// </summary>
        public string Error {get;set;}
        /// <summary>
        /// Whether the result operation failed
        /// </summary>
        public bool IsError {get;set;}
        /// <summary>
        /// Get success result with code 200
        /// </summary>
        /// <returns></returns>
        public static ServiceResult Success() {
            return new ServiceResult {Code = 200};
        }
        /// <summary>
        /// Get failed result with code and error provided
        /// </summary>
        /// <param name="code"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static ServiceResult Fail(int code, string error) {
            return new ServiceResult {Code = code, Error = error, IsError = true};
        }
        /// <summary>
        /// Error shortcut with Code = 500 and message
        /// "Server has encountered an unexpected error while processing the request. Please, try again later."
        /// </summary>
        /// <returns></returns>
        public static ServiceResult FailInternal() {
            return new ServiceResult {Code = 500,
            Error = "Server has encountered an unexpected error while processing the request. Please, try again later.",
            IsError = true};
        }
    }
    public class ServiceResult<T>:ServiceResult, IServiceResult<T>
    {
        /// <summary>
        /// An object returned by operation
        /// </summary>
        public T ResultObject {get;set;}
        /// <summary>
        /// Get success result with code 200 and ResultObject
        /// </summary>
        /// <returns></returns>
        public static ServiceResult<T> Success(T resultObject) {
            return new ServiceResult<T> {Code = 200, ResultObject = resultObject};
        }
        /// <summary>
        /// Get failed result with code and error provided
        /// ResultObject is always set to default
        /// </summary>
        /// <param name="code"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public static new ServiceResult<T> Fail(int code, string error) {
            return new ServiceResult<T> {Code = code, Error = error, IsError = true, ResultObject = default};
        }
        /// <summary>
        /// Get failed result with code and error provided by IResult
        /// ResultObject is always set to default
        /// </summary>
        /// <param name="result"></param>
        public static ServiceResult<T> Fail(IServiceResult result) {
            return Fail(result.Code, result.Error);
        }
        /// <summary>
        /// Error shortcut with Code = 500 and message
        /// "Server has encountered an unexpected error while processing the request. Please, try again later."
        /// </summary>
        /// <returns>Result<T></returns>
        public static new ServiceResult<T> FailInternal() {
            return new ServiceResult<T> {Code = 500,
            Error = "Server has encountered an unexpected error while processing the request. Please, try again later.",
            IsError = true, ResultObject = default};
        }
        
        /// <summary>
        /// If resultObject exists return success, else fail with nullCase values
        /// </summary>
        /// <param name="resultObject"></param>
        /// <param name="nullCaseCode"></param>
        /// <param name="nullCaseError"></param>
        /// <returns></returns>
        public static ServiceResult<T> FromResultObject(T resultObject, int nullCaseCode = 404, string nullCaseError = "Requested object does not exist") {
            if (resultObject is null)
                return Fail(nullCaseCode, nullCaseError);
            return Success(resultObject);
        }
        
    }
}