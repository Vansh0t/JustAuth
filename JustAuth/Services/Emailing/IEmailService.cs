

namespace JustAuth.Services.Emailing
{
    public interface IEmailService
    {
        Task<IServiceResult> SendEmailAsync(string recipient, string templatePath, string actionData, string subject);
        Task<IServiceResult> SendEmailAsync(string recipient, string templatePath, string actionData, string subject, EmailingOptions options);
        

    }
}