using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;
using JustAuth.Tests.Fixtures;
using Xunit;
namespace JustAuth.Tests.Integration.AuthControllerTest
{
    public class EmailTest:IClassFixture<AuthAppFactory>
    {
        private readonly AuthAppFactory app;
        private HttpClient appClient;
        private readonly string NEW_USER_EMAIL;
        public EmailTest(AuthAppFactory app) {
            this.app = app;
            NEW_USER_EMAIL = SharedUtils.GetNewUserEmail();
        }
        [Fact]
        public async Task TestEmailVerification() {
            appClient = app.CreateClient();
            //test first email verification
            var content = Utils.MakeStringContent(
                "token", AuthAppFactory.EMAIL_VERIFY_USER.EmailVrfToken
            );
            var result = await appClient.PostAsync($"/auth/email/vrf", content);
            Assert.Equal(200, (int)result.StatusCode);
            string emailBefore = null;
            //cleanup and prepare for email change verification
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username==AuthAppFactory.EMAIL_VERIFY_USER.Username);
                emailBefore = user.Email;
                Assert.True(user.IsEmailVerified);
                user.EmailVrfToken = "TestEmailVerificationVRFT";
                user.EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(24);
                user.NewEmail = NEW_USER_EMAIL;
                await ctx.SaveChangesAsync();
            });
            content = Utils.MakeStringContent(
                "token", "TestEmailVerificationVRFT"
            );
            result = await appClient.PostAsync($"/auth/email/vrf", content);
            //verify email was changed after confirm
            Assert.Equal(200, (int)result.StatusCode);
            //cleanup
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
            var content = Utils.MakeStringContent(
                "newEmail", NEW_USER_EMAIL,
                "password", AuthAppFactory.UNVERIFIED_USER_PASSWORD
                );
            var result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(401, (int)result.StatusCode);
            var signinContent = Utils.MakeStringContent(
                "credential", AuthAppFactory.UNVERIFIED_USER.Username,
                "password", AuthAppFactory.UNVERIFIED_USER_PASSWORD
            );
            result = await appClient.PostAsync("/auth/signin", signinContent);
            var resp = await result.Content.ReadFromJsonAsync<Controllers.DTO.SignInResponse>();
            if(resp.Jwt is not null){
                appClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resp.Jwt}");
            }  
            else {
                var jwtCookie = result.Headers.Single(_=>_.Key=="Set-Cookie").Value.First();
                var cookieValue = jwtCookie.Remove(0, 4);//remove jwt=
                appClient.DefaultRequestHeaders.Add("Authorization", 
                                                    $"Bearer {cookieValue}");
            }
                
            result = await appClient.PostAsync("/auth/email/change", content);
            Assert.Equal(403, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestEmailChangeSuccess() {
            appClient = app.CreateClient();
            var content = Utils.MakeStringContent(
                "newEmail", NEW_USER_EMAIL,
                "password", AuthAppFactory.EMAIL_CHANGE_USER_PASSWORD
                );
            var signinContent = Utils.MakeStringContent(
                "credential", AuthAppFactory.EMAIL_CHANGE_USER.Username,
                "password", AuthAppFactory.EMAIL_CHANGE_USER_PASSWORD
            );
            string emailBefore = AuthAppFactory.EMAIL_CHANGE_USER.Email;
            var result = await appClient.PostAsync("/auth/signin", signinContent);
            var s = await result.Content.ReadAsStringAsync();
            var resp = await result.Content.ReadFromJsonAsync<Controllers.DTO.SignInResponse>();
            appClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resp.Jwt.Jwt}");
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