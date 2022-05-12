using JustAuth.Services.Emailing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JustAuth.Tests
{
    public static class SharedUtils
    {
        public static IConfiguration GetJustAuthConfig() {
            ConfigurationBuilder confBuilder = new ();
            confBuilder.AddJsonFile("justauth.json");
            var config = confBuilder.Build();
            return config;
        }
        public static IEmailService CreateEmailServiceMock() {
            LoggerFactory factory = new ();
            var logger = factory.CreateLogger<EmailService>();
            var emailingOptions = new EmailingOptions();
            var config = GetJustAuthConfig();
            config.GetSection("Emailing").Bind(emailingOptions);
            return new EmailService(logger, emailingOptions);
        }
        /// <summary>
        /// Gets Sender email from justauth.json
        /// </summary>
        /// <returns></returns>
        public static string GetNewUserEmail() {
            
            var emailConfig =new EmailingOptions();
            var config = GetJustAuthConfig();
            config.GetSection("Emailing").Bind(emailConfig);
            return emailConfig.Sender;
        }
    }
}