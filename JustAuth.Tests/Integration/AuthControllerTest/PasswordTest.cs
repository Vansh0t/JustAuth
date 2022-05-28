using System.Net.Http;
using System.Threading.Tasks;
using JustAuth.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace JustAuth.Tests.Integration.AuthControllerTest
{
    

    public class PasswordTest:IClassFixture<AuthAppFactory>
    {
        private readonly AuthAppFactory app;
        private HttpClient appClient;
        private const string NEW_PASSWORD = "somenewpassword111";
        public PasswordTest(AuthAppFactory app) {
            this.app = app;
        }
        [Fact]
        public async Task TestPasswordResetSuccess() {
            appClient = app.CreateClient();
            //setup pwd reset step 1
            string email = SharedUtils.GetNewUserEmail();
            var content = Utils.MakeStringContent(
                "email", email
                );
            string beforeEmail = null;
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username == AuthAppFactory.PASSWORD_RESET_USER.Username);
                beforeEmail = user.Email; //save email so it can be restored in cleanup
                user.Email = email; // Temporarily set to loopback email so we won't have 409 in other tests
                await ctx.SaveChangesAsync();
                
            });
            //action step 1
            var result = await appClient.PostAsync("auth/pwd/reset1", content);
            //assert step 1
            Assert.Equal(200, (int)result.StatusCode);
            //setup step 2
            string beforePasswordHash = null;
            string token = null;
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username == AuthAppFactory.PASSWORD_RESET_USER.Username);
                beforePasswordHash = user.PasswordHash;
                token = user.PasswordResetToken;
            });
            content = Utils.MakeStringContent(
                "email", email,
                "token", token,
                "newPassword", NEW_PASSWORD,
                "newPasswordConf", NEW_PASSWORD
            );
            //action step 2 
            var result1 = await appClient.PostAsync("auth/pwd/reset2", content);
            //assert step 2
            Assert.Equal(200, (int)result.StatusCode);
            //cleanup
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username == AuthAppFactory.PASSWORD_RESET_USER.Username);
                Assert.NotEqual(user.PasswordHash, beforePasswordHash);
                //cleanup
                user.PasswordHash = beforePasswordHash;
                user.Email = beforeEmail;
                await ctx.SaveChangesAsync();
            });

        }
    }
}