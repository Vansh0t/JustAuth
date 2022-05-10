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
    using System.Net.Http.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Fixtures;
    using JustAuth.Controllers;
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
            var content = MakeStringContent(
                "username", NEW_USER_USERNAME,
                "email", NEW_USER_EMAIL,
                "password", NEW_USER_PASSWORD,
                "passwordConf", NEW_USER_PASSWORD
            );
            var result = await appClient.PostAsync("/auth/signup", content);
            Assert.Equal(201, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestEmailVerification() {
            appClient = app.CreateClient();
            var result = await appClient.GetAsync($"/auth/email/vrf?vrft={AuthAppFactory.UNVERIFIED_USER_VRFT}");
            Assert.Equal(200, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestEmailChangeForbidden() {
            appClient = app.CreateClient();
            var content = MakeStringContent("newEmail", "some@test.com");
            var result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(401, (int)result.StatusCode);
            var signinContent = MakeStringContent(
                "username", AuthAppFactory.UNVERIFIED_USER_USERNAME,
                "password", AuthAppFactory.UNVERIFIED_USER_PASSWORD
            );
            result = await appClient.PostAsync("/auth/signin", signinContent);
            var resp = await result.Content.ReadFromJsonAsync<DTO.SignInResponse>();
            appClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resp.Jwt}");
            result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(403, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestEmailChangeSuccess() {
            appClient = app.CreateClient();
            var content = MakeStringContent("newEmail", "some@test.com");
            var result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(401, (int)result.StatusCode);
            var signinContent = MakeStringContent(
                "username", AuthAppFactory.UNVERIFIED_USER_USERNAME,
                "password", AuthAppFactory.UNVERIFIED_USER_PASSWORD
            );
            result = await appClient.PostAsync("/auth/signin", signinContent);
            var resp = await result.Content.ReadFromJsonAsync<DTO.SignInResponse>();
            appClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resp.Jwt}");
            result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(403, (int)result.StatusCode);
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
        private StringContent MakeStringContent(params string[] kvp) {
            if(kvp.Length == 0 || kvp.Length%2!=0) throw new ArgumentException();
            Dictionary<string, string> dict = new();
            for(int i = 0; i < kvp.Length; i+=2) {
                dict.Add(kvp[i], kvp[i+1]);
            }
            var serialized = JsonConvert.SerializeObject(dict);
            return new StringContent(serialized, Encoding.UTF8, "application/json");
        }
    }
}