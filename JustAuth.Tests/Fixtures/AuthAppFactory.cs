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

namespace JustAuth.Tests.Fixtures
{
    public class AuthAppFactory:WebApplicationFactory<Program>
    {
        private const string ConnectionString = "Data Source=JustAuth.Tests.Integration.db;Cache=Shared";
        public const string VERIFIED_USER_EMAIL = "act_verified@test.com";
        public const string VERIFIED_USER_USERNAME = "act_verified";
        public const string VERIFIED_USER_PASSWORD = "act_verified_pwd111";
        public const string UNVERIFIED_USER_EMAIL = "act_unverified@test.com";
        public const string UNVERIFIED_USER_USERNAME = "act_unverified";
        public const string UNVERIFIED_USER_PASSWORD = "act_unverified_pwd111";
        public const string UNVERIFIED_USER_VRFT = "UNVRFT";
        private static bool isDbInitialized;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ConfigurationBuilder configBuilder= new ();
            configBuilder.AddJsonFile("appsettings.Development.json");
            configBuilder.AddJsonFile("justauth.json");
            IConfiguration config = configBuilder.Build();
            builder.UseConfiguration(config);
            builder.ConfigureServices(services =>
            {
                var connectionString = ConnectionString;
                services.AddDbContext<AuthDbMain<TestUser>>(opt=> {
                    opt.UseSqlite(connectionString);
                });
                JwtOptions jwtOptions = new ();
                config.GetSection("JwtOptions").Bind(jwtOptions);
                services.AddJustAuth<TestUser>( opt=> {
                    opt.JwtOptions  = jwtOptions;
                });
                InitDatabase(services);
            });
            
            builder.Configure(app=>{
                Console.WriteLine("TTWADWAD");
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
            TestUser verifiedUser = new () {
                    Email = VERIFIED_USER_EMAIL,
                    Username = VERIFIED_USER_USERNAME,
                    PasswordHash = Cryptography.HashPassword(VERIFIED_USER_PASSWORD),
                    IsEmailVerified = true
                };
            TestUser unverifiedUser = new () {
                Email = UNVERIFIED_USER_EMAIL,
                Username = UNVERIFIED_USER_USERNAME,
                PasswordHash = Cryptography.HashPassword(UNVERIFIED_USER_PASSWORD),
                EmailVrfToken = "UNVRFT",
                EmailVrfTokenExpiration = DateTime.UtcNow.AddHours(24)
            };
            context.AddRange(verifiedUser, unverifiedUser);
            context.SaveChanges();
            isDbInitialized = true;
        }

            
    }
}
