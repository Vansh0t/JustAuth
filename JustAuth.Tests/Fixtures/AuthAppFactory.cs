using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using JustAuth.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using JustAuth.Services.Auth;
using JustAuth.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using System;
using System.Threading.Tasks;

namespace JustAuth.Tests.Fixtures
{
    public class AuthAppFactory:WebApplicationFactory<Program>
    {
        private const string ConnectionString = "Data Source=JustAuth.Tests.Integration.db;Cache=Shared";

        public const string UNVERIFIED_USER_PASSWORD = "act_unverified_pwd111";
        public const string VERIFIED_USER_PASSWORD = "act_verified_pwd111";
        public const string EMAIL_CHANGE_USER_PASSWORD = "act_emailchange_pwd111";
        public const string EMAIL_VERIFY_USER_PASSWORD = "act_emailverify_pwd111";
        public const string PASSWORD_RESET_USER_PASSWORD = "act_pwdreset_pwd111";
        public static readonly TestUser UNVERIFIED_USER = new () {
                Email = "act_unverified@test.com",
                Username = "act_unverified",
                PasswordHash = Cryptography.HashPassword(UNVERIFIED_USER_PASSWORD),
                EmailVrfToken = "UNVERIFIED_USER",
                EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(24)
            };
        public static readonly TestUser VERIFIED_USER = new () {
                Email = "act_verified@test.com",
                Username = "act_verified",
                PasswordHash = Cryptography.HashPassword(VERIFIED_USER_PASSWORD),
            };
        public static readonly TestUser EMAIL_CHANGE_USER = new () {
                Email = "act_emailchange@test.com",
                Username = "act_emailchange",
                PasswordHash = Cryptography.HashPassword(EMAIL_CHANGE_USER_PASSWORD),
                IsEmailVerified = true
            };
        public static readonly TestUser EMAIL_VERIFY_USER = new () {
                Email = "act_emailverify@test.com",
                Username = "act_emailverify",
                PasswordHash = Cryptography.HashPassword(EMAIL_VERIFY_USER_PASSWORD),
                EmailVrfToken = "EMAIL_VERIFY_USER",
                EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(24)
            };
        public static readonly TestUser PASSWORD_RESET_USER = new () {
                Email = "act_pwdreset@test.com",
                Username = "act_pwdreset",
                PasswordHash = Cryptography.HashPassword(PASSWORD_RESET_USER_PASSWORD),
                IsEmailVerified = true
            };
        private static bool isDbInitialized;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //ConfigurationBuilder configBuilder= new ();
            //configBuilder.AddJsonFile("appsettings.Development.json");
            //configBuilder.AddJsonFile("justauth.json");
            //IConfiguration config = configBuilder.Build();
            //builder.UseConfiguration(config);
            builder.ConfigureServices(services =>
            {
                var connectionString = ConnectionString;
                services.AddDbContext<IAuthDbMain<TestUser>, AuthDbMain<TestUser>>(opt=> {
                    opt.UseSqlite(connectionString);
                });
                services.AddJustAuth<TestUser>( opt=> {
                    opt.UsePasswordResetRedirect("/pwd/fake");
                    opt.UseEmailConfirmRedirect("/email/fake");
                });
                InitDatabase(services);
            });
            
            builder.Configure(app=>{
                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(opt=>{
                    opt.MapControllers();
                });
            });
        }
        private static void InitDatabase(IServiceCollection services) {
            if (isDbInitialized) return;
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope() ;
            using var context = scope.ServiceProvider.GetRequiredService<AuthDbMain<TestUser>>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            Console.WriteLine("CREATED");

            context.AddRange(UNVERIFIED_USER,
                            VERIFIED_USER,
                            EMAIL_CHANGE_USER,
                            EMAIL_VERIFY_USER,
                            PASSWORD_RESET_USER);
            context.SaveChanges();
            isDbInitialized = true;
        }

        public async Task UsingContext(Func<AuthDbMain<TestUser>, Task> action) {
            using var scope = Services.CreateScope() ;
            using var context = scope.ServiceProvider.GetRequiredService<AuthDbMain<TestUser>>();
            await action(context);
        }

            
    }
}
