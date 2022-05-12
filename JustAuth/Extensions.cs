using JustAuth.Data;
using JustAuth.Services.Auth;
using JustAuth.Services.Validation;
using JustAuth.Services.Emailing;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using JustAuth.Controllers;
using System.Reflection;

namespace JustAuth
{
    
    public class JustAuthOptions<TUser> where TUser: AppUser, new()
     {
        public MappingOptions MappingOptions = new();
        

        private readonly IServiceCollection services;
        public JustAuthOptions (IServiceCollection services) {
            this.services = services;
        }
        public void SetPasswordValidator<TValidator>()
            where TValidator: class, IPasswordValidator
        {
            services.AddTransient<IPasswordValidator, TValidator>();
        }
        public void SetEmailValidator<TValidator>()
            where TValidator: class, IEmailValidator
        {
            services.AddTransient<IEmailValidator, TValidator>();
        }
        public void SetUsernameValidator<TValidator>()
            where TValidator: class, IUsernameValidator
        {
            services.AddTransient<IUsernameValidator, TValidator>();
        }
        public void SetUserManager<TUserManager>()
            where TUserManager: class, IUserManager<TUser>
        {
            services.AddTransient<IUserManager<TUser>, TUserManager>();
        }
        public void SetEmailService<TEmailService>()
            where TEmailService: class, IEmailService
        {
            services.AddScoped<IEmailService, TEmailService>();
        }
        public void SetJwtProvider<TJwtProvider>()
            where TJwtProvider: class, IJwtProvider
        {
            services.AddSingleton<IJwtProvider, TJwtProvider>();
        }
        public void UseEmailConfirmRedirect(string url) {
            MappingOptions.EmailConfirmRedirectUrl = url;
        }
        public void UsePasswordResetRedirect(string url) {
            MappingOptions.PasswordResetRedirectUrl = url;
        }
    }
    public static class Extensions
    {
        public static IServiceCollection AddJustAuth<TUser>(this IServiceCollection services, Action<JustAuthOptions<TUser>> options)
            where TUser: AppUser, new()
        {
            var controllers = services.AddControllers();
            controllers.ConfigureApplicationPartManager(opt=>{
                opt.FeatureProviders.Add(new AuthControllerFeatureProvider<TUser>());
            });
            JustAuthOptions<TUser> opt = new (services);
            options(opt);

            ConfigurationBuilder configBuilder = new();
            configBuilder.AddJsonFile("justauth.json");
            IConfiguration config = configBuilder.Build();
            JwtOptions jwtOptions = new();
            EmailingOptions emailingOptions = new();
            config.GetSection("JwtOptions").Bind(jwtOptions);
            config.GetSection("Emailing").Bind(emailingOptions);
            SetDefaultValidators(services);
            SetDefaultServices<TUser>(services);
            SetDefaultProviders(services);
            
            services.AddSingleton(jwtOptions);
            services.AddSingleton(emailingOptions);
            if(opt.MappingOptions.PasswordResetRedirectUrl is null)
                Console.WriteLine("WARNING. Redirect Url for password reset wasn't set. Configure it with UsePasswordResetRedirect(). Password reset functionality disabled!");
            services.AddSingleton(opt.MappingOptions);
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                                .AddJwtBearer(options =>
                                {
                                    options.RequireHttpsMetadata = false;
                                    options.TokenValidationParameters = new ()
                                    {
                                        ValidateIssuer = jwtOptions.ValidateIssuer,
                                        ValidIssuer = jwtOptions.Issuer,
                                        ValidateAudience = jwtOptions.ValidateAudience,
                                        ValidAudience = jwtOptions.Audience,
                                        ValidateLifetime = jwtOptions.ValidateLifetime,
                                        IssuerSigningKey = jwtOptions.GetSymmetricSecurityKey(),
                                        ValidateIssuerSigningKey = jwtOptions.ValidateIssuerSigningKey,
                                    };
                                });
            services.AddAuthorization(options=> {
                foreach(var claim in jwtOptions.Claims) {
                    Console.WriteLine($"{claim.Name}, {claim.AccessValues}");
                    options.AddPolicy(claim.Name, policy=>policy.RequireClaim(claim.Name, claim.AccessValues));
                } 
            });
            return services;
        }
        private static void SetDefaultValidators(IServiceCollection services) {
            //If services are not set, use default implementations
            if(!services.Any(_=>_.ServiceType == typeof(IPasswordValidator))) {
                services.AddTransient<IPasswordValidator, PasswordValidator>();
            }
            if(!services.Any(_=>_.ServiceType == typeof(IEmailValidator))) {
                services.AddTransient<IEmailValidator, EmailValidator>();
            }
            if(!services.Any(_=>_.ServiceType == typeof(IUsernameValidator))) {
                services.AddTransient<IUsernameValidator, UsernameValidator>();
            }
        }
        private static void SetDefaultServices<TUser>(IServiceCollection services)
            where TUser: AppUser, new()
         {
            //If services are not set, use default implementations
            if(!services.Any(_=>_.ServiceType == typeof(IUserManager<>))) {
                services.AddScoped<IUserManager<TUser>, UserManager<TUser>>();
            }
            if(!services.Any(_=>_.ServiceType == typeof(IEmailService))) {
                services.AddScoped<IEmailService, EmailService>();
            }
        }
        private static void SetDefaultProviders(IServiceCollection services) {
            //If services are not set, use default implementations
            if(!services.Any(_=>_.ServiceType == typeof(IJwtProvider))) {
                services.AddSingleton<IJwtProvider, JwtProvider>();
            }
        }
    }
    public class AuthControllerFeatureProvider<TUser> : IApplicationFeatureProvider<ControllerFeature>
        where TUser: AppUser, new()
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            feature.Controllers.Add(typeof(AuthController<TUser>).GetTypeInfo());
        }
    }
}