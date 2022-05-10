using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using JustAuth.Services.Emailing;
namespace JustAuth.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Fixtures;
    using Microsoft.AspNetCore.TestHost;
    using Xunit;

    public class AuthControllerTest:IClassFixture<AuthAppFactory>
    {
        private readonly AuthAppFactory app;
        private HttpClient appClient;
        private readonly string NEW_USER_EMAIL;
        private const string NEW_USER_USERNAME = "newuser";
        private const string NEW_USER_PASSWORD = "newuser_pwd111";
        public AuthControllerTest() {
            app = new AuthAppFactory();
            NEW_USER_EMAIL = GetNewUserEmail();
        }
        [Fact]
         public async Task TestSignIn() {
            appClient = app.CreateClient();
            Dictionary<string, string> data = new();
            data.Add("username", AuthAppFactory.VERIFIED_USER_USERNAME);
            data.Add("password", AuthAppFactory.VERIFIED_USER_PASSWORD);
            var serialized = JsonConvert.SerializeObject(data);
            var sContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            var result = await appClient.PostAsync("/auth/signin", sContent);
            Assert.Equal(200, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestSignUp() {
            appClient = app.CreateClient();
            Dictionary<string, string> data = new();
            data.Add("username", NEW_USER_USERNAME);
            data.Add("email", NEW_USER_EMAIL);
            data.Add("password", NEW_USER_PASSWORD);
            data.Add("passwordConf", NEW_USER_PASSWORD);
            var serialized = JsonConvert.SerializeObject(data);
            var sContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            var result = await appClient.PostAsync("/auth/signup", sContent);
            Assert.Equal(201, (int)result.StatusCode);
        }
        //Get the same email as used for sending, i.e send email to self
        private string GetNewUserEmail() {
            LoggerFactory factory = new ();
            var logger = factory.CreateLogger<EmailService>();
            ConfigurationBuilder confBuilder = new ();
            confBuilder.AddJsonFile("justauth.json");
            var config = confBuilder.Build();
            return new EmailService(config, logger).EmailingOptions.Sender;
        }
    }
}