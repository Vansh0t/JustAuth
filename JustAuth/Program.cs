
using JustAuth.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens ;
using JustAuth.Services.Auth;
using JustAuth.Services.Emailing;
using JustAuth.Services.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;
using JustAuth.Utils;
using JustAuth;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
//var services = builder.Services;
//builder.Configuration.AddJsonFile("justauth.json"); // justauth.json contains sensitive data, it is not stored on github
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//services.AddDbContext<AuthDbMain<AppUser>>(opt=> {
//    opt.UseSqlite(connectionString);
//});
//services.AddLogging(opt=> {
//    opt.AddConsole();
//});
//JwtOptions jwtOptions = new ();
//builder.Configuration.GetSection("JwtOptions").Bind(jwtOptions);
//services.AddJustAuth<AppUser>( opt=> {
//    opt.JwtOptions  = jwtOptions;
//}
//);

var app = builder.Build();


//if (!app.Environment.IsDevelopment())
//{
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();
//app.UseRouting();


//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller}/{action=Index}/{id?}");

//app.MapFallbackToFile("index.html");;

app.Run();
public partial class Program {}