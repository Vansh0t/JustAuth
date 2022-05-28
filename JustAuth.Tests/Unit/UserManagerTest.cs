using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Threading.Tasks;
using JustAuth.Services.Auth;
using Microsoft.EntityFrameworkCore;
using JustAuth.Data;
using JustAuth.Tests.Fixtures;
using JustAuth.Services.Validation;
using System.Linq;
using JustAuth.Utils;
using System;

namespace JustAuth.Tests.Unit {
    public class UserManagerTest: IClassFixture<DbContextFixture>
    {
        private readonly AuthDbMain<AppUser> _context;
        private readonly IUserManager<AppUser> _userManager;

        
        public UserManagerTest(DbContextFixture ctxFixture) {
            LoggerFactory factory = new ();
            var logger = factory.CreateLogger<UserManager<AppUser>>();
            ConfigurationBuilder confBuilder = new ();
            confBuilder.AddJsonFile("justauth.json");
            _context = ctxFixture.CreateMockContext();
            _userManager = new UserManager<AppUser>(_context, logger, new EmailValidator(), new PasswordValidator(), new UsernameValidator());

        }

#region  USER
        [Theory]
        //Invalid email
        [InlineData("TestCreateUserFail", "TestCreateUserFail", "validpwd111", 400)]
        //Invalid password
        [InlineData("TestCreateUserFail@test.com", "TestCreateUserFail", "short", 400)]
        //Email occupied
        [InlineData(DbContextFixture.VERIFIED_USER_EMAIL, "TestUser", "validpwd111", 409)]
        //Username occupied
        [InlineData("TestCreateUserFail@test.com", DbContextFixture.VERIFIED_USER_USERNAME, "validpwd111", 409)]
        public async Task TestCreateUserFail(string email, string username, string password, int code)
        {
            var result = await _userManager.CreateUserAsync(email, username, password);
            Assert.Equal(code, result.Code);
        }
        [Fact]
        public async Task TestCreateUserSuccess()
        {
            var result = await _userManager.CreateUserAsync("test1@test.com", "TestUser", "validpwd111");
            Assert.Equal(200, result.Code);
            _context.Users.Add((AppUser)result.ResultObject);
            await _context.SaveChangesAsync();
            var user = _context.Users.First(_=>_.Email == "test1@test.com");
            Assert.NotNull(user);
            Assert.Equal("TestUser", user.Username);
            Assert.NotNull(user.PasswordHash);
            Assert.NotEmpty(user.PasswordHash);
        }
#endregion
#region EMAIL
        [Theory]
        //already verified
        [InlineData(DbContextFixture.VERIFIED_USER_EMAIL, 403)]
        public async Task TestSetEmailVerificationFail(string email, int code) {
            AppUser user = _context.Users.First(_=>_.Email == email);
            var result = await _userManager.SetEmailVerificationAsync(user);
            Assert.Equal(code, result.Code);
            Assert.Null(user.EmailVrfToken);
            Assert.Null(user.EmailVrfTokenExpiration);
        }
        [Fact]
        public async Task TestVerifyEmailSuccess() {
            AppUser user = await CreateTestUser(
                new AppUser {
                    Email = "TestVerifyEmailSuccess@test.com",
                    Username = "TestVerifyEmailSuccess",
                    PasswordHash = Cryptography.HashPassword("testpwd111"),
                    IsEmailVerified = false
                }
            );
            var result = await _userManager.SetEmailVerificationAsync(user);
            Assert.Equal(200, result.Code);
            Assert.NotNull(user.EmailVrfToken);
            Assert.NotNull(user.EmailVrfTokenExpiration);
            string vrfToken = user.EmailVrfToken;
            //reset verification
            result = await _userManager.SetEmailVerificationAsync(user);
            Assert.Equal(200, result.Code);
            Assert.NotEqual(vrfToken, user.EmailVrfToken);
            await _context.SaveChangesAsync();
            var vrfResult = await _userManager.VerifyEmailAsync(user.EmailVrfToken);
            user = vrfResult.ResultObject;
            Assert.Equal(200, vrfResult.Code);
            Assert.Null(user.EmailVrfToken);
            Assert.Null(user.EmailVrfTokenExpiration);
            Assert.Null(user.NewEmail);
            Assert.True(user.IsEmailVerified);
            
        }
        [Theory]
        //Invalid email
        [InlineData(DbContextFixture.VERIFIED_USER_EMAIL, "wrong", 400)]
        [InlineData(DbContextFixture.VERIFIED_USER_EMAIL, null, 400)]
        //Already occupied
        [InlineData(DbContextFixture.VERIFIED_USER_EMAIL, DbContextFixture.UNVERIFIED_USER_EMAIL, 409)]
        //Unverified user can't change email
        [InlineData(DbContextFixture.UNVERIFIED_USER_EMAIL, "test1@test.com", 403)]
        public async Task TestChangeEmailFail(string userEmail, string newEmail, int code)
        {
            AppUser user = _context.Users.First(_=>_.Email == userEmail);
            //Invalid email
            var result = await _userManager.SetEmailChangeAsync(user, newEmail);
            Assert.Equal(code, result.Code);
        }
        [Fact]
        public async Task TestChangeEmailSuccess()
        {
            AppUser user = await CreateTestUser(
                new AppUser {
                    Email = "TestChangeEmailSuccess@test.com",
                    Username = "TestChangeEmailSuccess",
                    PasswordHash = Cryptography.HashPassword("testpwd111"),
                    IsEmailVerified = true
                }
            );
            var result = await _userManager.SetEmailChangeAsync(user, "NewTestChangeEmailSuccess@test.com");
            Assert.Equal(200, result.Code);
            await _context.SaveChangesAsync();
            var vrfResult = await _userManager.VerifyEmailAsync(user.EmailVrfToken);
            user = vrfResult.ResultObject;
            Assert.Equal(200, vrfResult.Code);
            Assert.Null(user.EmailVrfToken);
            Assert.Null(user.EmailVrfTokenExpiration);
            Assert.Null(user.NewEmail);
            Assert.Equal("NewTestChangeEmailSuccess@test.com", user.Email);
            await _context.SaveChangesAsync();
        }
        [Theory]
        //invalid tokens
        [InlineData("wrong", 403)]
        [InlineData("", 403)]
        [InlineData(null, 403)]
        public async Task TestVerifyEmailFail(string token, int code)
        {
            var result = await _userManager.VerifyEmailAsync(token);
            Assert.Equal(code, result.Code);
        }
#endregion
#region PASSWORD
        [Fact]
        public async Task TestResetPasswordFail()
        {
            //Unverified user can't reset password
            AppUser userUnverified = _context.Users.First(_=>_.Email == DbContextFixture.UNVERIFIED_USER_EMAIL);
            var result = await _userManager.SetPasswordResetAsync(userUnverified);
            Assert.Equal(403, result.Code);
            await _context.SaveChangesAsync();
        }
        [Theory]
        //invalid tokens
        [InlineData("", 24, "newvalidpwd111", 403, 1)]
        [InlineData(null, 24, "newvalidpwd111", 403, 2)]
        //invalid passwords
        [InlineData("valid3", 24, null, 400, 3)]
        [InlineData("valid4", 24, "", 400, 4)]
        //token expired
        [InlineData("valid5", -48, "newvalidpwd111", 401, 5)]
        public async Task TestVerifyPasswordFail(string token, int hours, string newPassword, int code, int testNum)
        {
            var pwdInit = Cryptography.HashPassword("validpwd111");
            var user = await CreateTestUser(
                new AppUser {
                    Email = $"TestVerfifyPasswordFail{testNum}@test.com",
                    Username = $"TestVerfifyPasswordFail{testNum}",
                    PasswordHash = pwdInit,
                    IsEmailVerified = true,
                    PasswordResetToken = $"valid{testNum}",
                    PasswordResetTokenExpiration = DateTime.UtcNow.AddHours(hours)
                } 
            );
            var result = _userManager.VerifyPassword(user, token, newPassword);
            Assert.Equal(code, result.Code);
        }
        [Fact]
        public async Task TestChangePasswordSuccess()  {
            var pwdInit = Cryptography.HashPassword("validpwd111");
            var user = await CreateTestUser(
                new AppUser {
                    Email = "TestChangePassword@test.com",
                    Username = "TestChangePassword",
                    PasswordHash = pwdInit,
                    IsEmailVerified = true
                }
            );
            var result = await _userManager.SetPasswordResetAsync(user);
            Assert.Equal(200, result.Code);
            Assert.NotNull(user.PasswordResetToken);
            await _context.SaveChangesAsync();
            var uResult = _userManager.VerifyPassword(user, user.PasswordResetToken, "newvalidpwd111");
            Assert.Equal(200, uResult.Code);
            user = uResult.ResultObject;
            Assert.NotNull(user.PasswordHash);
            Assert.NotEmpty(user.PasswordHash);
            Assert.NotEqual(pwdInit, user.PasswordHash);
            Assert.Null(user.PasswordResetToken);
            Assert.Null(user.PasswordResetTokenExpiration);
            await _context.SaveChangesAsync();
        }
#endregion
#region  USERNAME
    [Theory]
    //username occupied
    [InlineData(DbContextFixture.UNVERIFIED_USER_USERNAME, 409)]
    //username invalid
    [InlineData("22", 400)]
    [InlineData(null, 400)]
    public async Task SetUsernameFail(string username, int code) {
        var user = await _context.Users.FirstAsync(_=>_.Username == DbContextFixture.VERIFIED_USER_USERNAME);
        var result = await _userManager.SetUsernameAsync(user, username);
        Assert.Equal(code, result.Code);
    }
    [Fact]
    public async Task SetUsernameSuccess() {
        var user = await CreateTestUser(new AppUser {
            Email = "SetUsernameSuccess@test.com",
            Username = "SetUsernameSuccess",
            PasswordHash = Cryptography.HashPassword("testpwd111")
        });
        //username occupied
        var result = await _userManager.SetUsernameAsync(user, "NewSetUsernameSuccess");
        Assert.Equal(200, result.Code);
        Assert.Equal("NewSetUsernameSuccess", user.Username);
    }
#endregion
#region UTILS
        private async Task<AppUser> CreateTestUser(AppUser user) {
            _context.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
#endregion
    }
}