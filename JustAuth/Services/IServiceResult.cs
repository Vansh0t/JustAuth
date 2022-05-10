namespace JustAuth.Services
{
    public interface IServiceResult
    {
        public int Code {get;set;}
        public string Error {get;set;}
        public bool IsError {get;set;}
    }
    public interface IServiceResult<T>:IServiceResult
    {
        public T ResultObject {get;set;}
    }
}