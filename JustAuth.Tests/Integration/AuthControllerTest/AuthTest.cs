using System.Net.Http;
using Newtonsoft.Json;
namespace JustAuth.Tests.Integration.AuthControllerTest
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Fixtures;
    using Microsoft.EntityFrameworkCore;
    using Xunit;

    public class AuthTest:IClassFixture<AuthAppFactory>
    {
        private readonly AuthAppFactory app;
        private HttpClient appClient;
        private readonly string NEW_USER_EMAIL;
        private const string NEW_USER_USERNAME = "newuser";
        private const string NEW_USER_PASSWORD = "newuser_pwd111";
        public AuthTest(AuthAppFactory app) {
            this.app = app;
            NEW_USER_EMAIL = SharedUtils.GetNewUserEmail();
        }
        [Fact]
         public async Task TestSignIn() {
            appClient = app.CreateClient();
            Dictionary<string, string> data = new();
            data.Add("username", AuthAppFactory.VERIFIED_USER.Username);
            data.Add("password", AuthAppFactory.VERIFIED_USER_PASSWORD);
            var serialized = JsonConvert.SerializeObject(data);
            var sContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            var result = await appClient.PostAsync("/auth/signin", sContent);
            Assert.Equal(200, (int)result.StatusCode);
        }
        [Fact]
        public async Task TestSignUp() {
            appClient = app.CreateClient();
            var content = Utils.MakeStringContent(
                "username", NEW_USER_USERNAME,
                "email", NEW_USER_EMAIL,
                "password", NEW_USER_PASSWORD,
                "passwordConf", NEW_USER_PASSWORD
            );
            var result = await appClient.PostAsync("/auth/signup", content);
            Assert.Equal(201, (int)result.StatusCode);
            //cleanup
            await app.UsingContext(async (ctx)=> {
                var user = await ctx.Users.FirstAsync(_=>_.Username==NEW_USER_USERNAME);
                ctx.Remove(user);
                await ctx.SaveChangesAsync();
            });
        }
    }
}