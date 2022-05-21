# JustAuth
Simple, but customizable JWT auth library for .NET Core MVC. The main goal of the project is to create base REST auth library for all kinds of apps (Web, SPA, etc.).
## Features
1. Two JWT send modes: Cookies for Web and Authorize Header for the rest of apps
2. Sign In, Sign Up, Sign Out
3. Email, Username and Password validation
4. Custom validators support via dependency injection
5. Email verification
6. Email change
7. Passowrd reset
8. Secure password storage
9. Silent sign in with jwt refresh token
10. Email templates
## Setup
1. Add nuget to the project
2. Create your User model and make sure it inherits ``AppUser``
```C#
using JustAuth.Data;
public class ChatUser:AppUser
    {
      //Your properties
    }
```
3. Create your ``DbContext`` implementation and make sure it inherits from ``AuthDbMain<TYourUserModel>``
```C#
using JustAuth.Data;
public class DbMain: AuthDbMain<ChatUser>
    {
         //Your properties
        public DbMain(DbContextOptions options) : base(options) {

        }
    }
```
4. Create ``justauth.json`` file in your app's root directory with settings.
```json
{
    "Emailing": {
        "Service":"Email Service",  --> sender name in email title
        "Sender":"your@email.com",  --> sender address
        "Smtp":"smtp.gmail.com",    --> smtp server
        "Port":"587",               --> smtp port
        "Password": "mypassword"    --> smtp account password
    },
    "JwtOptions" : {
        "Issuer":"JustAuthServer",
        "ValidateIssuer":true,
        "Audience": "JustAuthUser",
        "ValidateAudience" : true,
        "ValidateLifetime" : true,
        "IssuerSigningKey" : "w0mgWEAl2xK27ZiN3E2gAbcHfSrSwQ==", --> key length must be 32 bytes(!) length
        "ValidateIssuerSigningKey" : true,
        "TokenLifetime": 60,                                     --> token lifetime in minutes
        "UseRefreshToken": true,                                 --> enable refresh token functionality
        "RefreshTokenLifetime": 60000,                           --> refresh token lifetime in minutes
        "SendAsCookie" : true,                                   --> should tokens be sent in secure cookies, if false jwt will be sent as part of json response
        "Claims": [
            {
                "Name": "MyPolicyName",            --> policy name to be used with [Authorize]
                "ModelProperty": "MyUserProperty", --> name of a property of your user model, its value is included into jwt tokens
                "AccessValues": ["true", "True"]   --> valid values for policy
            }
        ]
    }
}
```
5. Add JustAuth services and .NET Auth middleware to your Program.cs or Startup.cs
```C#
    services.AddDbContext<IAuthDbMain<ChatUser>, DbMain>(options=>{
        //your options
    });
    services.AddJustAuth<ChatUser>(options => {
        options.UseEmailConfirmRedirect("/Auth/EmailConfirm"); //an endpoint to which user will be sent by email confirmation email message
        options.UsePasswordResetRedirect("/Auth/PasswordReset"); //an endpoint to which user will be sent by password reset email message
        //custom services implementations are also supported here
    });
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseEndpoints(options=> {
        options.MapDefaultControllerRoute();
    });
```
6. Customize email templates in EmailTemplates root folder of your built app. You will find 2 mimimal templates for password reset and email confirmation there. You can customize them however you like, but keep ``{{actionData}}``. ``{{actionData}}`` will be replaced with email change or password reset url before sending to user email.
## REST Endpoints
For REST endpoint see [REST Doc](https://github.com/Vansh0t/JustAuth/blob/master/REST.md)
