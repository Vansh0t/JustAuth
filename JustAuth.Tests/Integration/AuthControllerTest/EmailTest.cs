using System.Net.Http;
using Microsoft.EntityFrameworkCore;
namespace JustAuth.Tests.Integration.AuthControllerTest
{
    using System;
    using System.Linq;
    using System.Net.Http.Json;

    using System.Threading.Tasks;
    using Fixtures;

    using Xunit;

    public class EmailTest:IClassFixture<AuthAppFactory>
    {
        private readonly AuthAppFactory app;
        private HttpClient appClient;
        private readonly string NEW_USER_EMAIL;
        public EmailTest(AuthAppFactory app) {
            this.app = app;
            NEW_USER_EMAIL = Utils.GetNewUserEmail();
        }
        [Fact]
        public async Task TestEmailVerification() {
            appClient = app.CreateClient();
            var result = await appClient.GetAsync($"/auth/email/vrf?vrft={AuthAppFactory.EMAIL_VERIFY_USER.EmailVrfToken}");
            Assert.Equal(200, (int)result.StatusCode);
            string emailBefore = null;
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username==AuthAppFactory.EMAIL_VERIFY_USER.Username);
                emailBefore = user.Email;
                Assert.True(user.IsEmailVerified);
                user.EmailVrfToken = "TestEmailVerificationVRFT";
                user.EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(24);
                user.NewEmail = NEW_USER_EMAIL;
                await ctx.SaveChangesAsync();
            });
            result = await appClient.GetAsync($"/auth/email/vrf?vrft=TestEmailVerificationVRFT");
            Assert.Equal(200, (int)result.StatusCode);
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username==AuthAppFactory.EMAIL_VERIFY_USER.Username);
                Assert.Equal(NEW_USER_EMAIL, user.Email);
                //cleanup
                user.Email = emailBefore;
                await ctx.SaveChangesAsync();
            });
        }
        [Fact]
        public async Task TestEmailChangeForbidden() {
            appClient = app.CreateClient();
            var content = Utils.MakeStringContent("newEmail", NEW_USER_EMAIL);
            var result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(401, (int)result.StatusCode);
            var signinContent = Utils.MakeStringContent(
                "username", AuthAppFactory.UNVERIFIED_USER.Username,
                "password", AuthAppFactory.UNVERIFIED_USER_PASSWORD
            );
            result = await appClient.PostAsync("/auth/signin", signinContent);
            var resp = await result.Content.ReadFromJsonAsync<Controllers.DTO.SignInResponse>();
            appClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resp.Jwt}");
            result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(403, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestEmailChangeSuccess() {
            appClient = app.CreateClient();
            var content = Utils.MakeStringContent("newEmail", NEW_USER_EMAIL);
            var signinContent = Utils.MakeStringContent(
                "username", AuthAppFactory.EMAIL_CHANGE_USER.Username,
                "password", AuthAppFactory.EMAIL_CHANGE_USER_PASSWORD
            );
            string emailBefore = AuthAppFactory.EMAIL_CHANGE_USER.Email;
            var result = await appClient.PostAsync("/auth/signin", signinContent);
            var resp = await result.Content.ReadFromJsonAsync<Controllers.DTO.SignInResponse>();
            appClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resp.Jwt}");
            result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(200, (int)result.StatusCode);
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username==AuthAppFactory.EMAIL_CHANGE_USER.Username);
                Assert.Equal(NEW_USER_EMAIL, user.NewEmail);
                //cleanup
                user.Email = emailBefore;
                await ctx.SaveChangesAsync();
            });
        }
    }
}