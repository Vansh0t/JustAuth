
using MailKit.Net.Smtp;
using MimeKit;

namespace JustAuth.Services.Emailing
{
    public class EmailService:IEmailService
    {
        private readonly EmailingOptions _options;
        protected readonly SmtpClient client;

        private readonly ILogger<EmailService> _logger;
        public EmailService(ILogger<EmailService> logger, EmailingOptions options) {
            _options = options;
            client = new SmtpClient();
            _logger = logger;
        }

        public async Task<IServiceResult> SendEmailAsync(string recipient, string message, string actionData, string subject) {

            return await SendEmailAsync(recipient, message, actionData, subject, _options);
        }
        public async Task<IServiceResult> SendEmailAsync(string recipient, string message, string actionData, string subject, EmailingOptions options) {
            ServiceResult result;
            try {
                await client.ConnectAsync(options.Smtp, options.Port, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
                var validator = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                if(!validator.IsValid(recipient)) {
                    result = ServiceResult.Fail(400, "Provided email is invalid or does not exist.");
                    return result;
                }
                await client.AuthenticateAsync(options.Sender, options.Password);
                MimeMessage mime = await GetMimeMessageAsync(recipient, message, actionData, subject);
                await client.SendAsync(mime);
                result = ServiceResult.Success();
            }
            catch (Exception e) {
                _logger.LogError(e.ToString());
                result = ServiceResult.FailInternal();
            }
            finally {
                if(client.IsConnected) {
                    client.Disconnect(true);
                }
            }
            return result;
        }
        private async Task<MimeMessage> GetMimeMessageAsync(string recipient, string messageTemplatePath, string actionData, string subject) {
            var mime = new MimeMessage();
            var builder = new BodyBuilder();
            string msgHtml = await File.ReadAllTextAsync(messageTemplatePath);
            msgHtml = msgHtml.Replace("{{actionData}}", actionData);
            builder.HtmlBody = msgHtml;
            mime.From.Add(new MailboxAddress(_options.Service, _options.Sender));
            mime.To.Add(new MailboxAddress(recipient, recipient));
            mime.Subject = subject;
            mime.Body = builder.ToMessageBody();
            return mime;
        }
    }
}