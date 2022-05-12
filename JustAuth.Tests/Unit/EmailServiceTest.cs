using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading.Tasks;
using JustAuth.Services.Emailing;
namespace JustAuth.Tests.Unit {
    public class EmailServiceTest
    {
        private readonly IEmailService _emailing;
        public EmailServiceTest() {
            _emailing = SharedUtils.CreateEmailServiceMock();
        }
        [Fact(Skip = "Avoid spamming")]
        public async Task TestSendEmailAsync()
        {
            var result = await _emailing.SendEmailAsync(SharedUtils.GetNewUserEmail(), "Test Message", "UnitTesting", "JustAuth Unit Test");
            Assert.False(result.IsError);
        }
    }
}