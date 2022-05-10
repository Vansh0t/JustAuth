namespace JustAuth.Services.Validation
{
    public interface IValidator<TValue>
    {
        IServiceResult Validate(TValue value); 
    }
}