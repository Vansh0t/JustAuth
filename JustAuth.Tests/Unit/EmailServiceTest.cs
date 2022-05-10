using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading.Tasks;
using JustAuth.Services.Emailing;
namespace JustAuth.Tests.Unit {
    public class EmailServiceTest
    {
        private readonly IConfiguration _config;
        private readonly IEmailService _emailing;
        public EmailServiceTest() {
            LoggerFactory factory = new ();
            var logger = factory.CreateLogger<EmailService>();
            ConfigurationBuilder confBuilder = new ();
            confBuilder.AddJsonFile("justauth.json");
            _config = confBuilder.Build();
            _emailing = new EmailService(_config, logger);
        }
        [Fact(Skip = "Avoid spamming")]
        public async Task TestSendEmailAsync()
        {
            var result = await _emailing.SendEmailAsync(_emailing.EmailingOptions.Sender, "Test Message", "UnitTesting", "JustAuth Unit Test");
            Assert.False(result.IsError);
        }
    }
}