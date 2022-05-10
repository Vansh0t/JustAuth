using System.Linq;
using JustAuth.Data;
using JustAuth.Tests.Unit;
using JustAuth.Utils;
using Microsoft.EntityFrameworkCore;

namespace JustAuth.Tests.Fixtures
{
    public class DbContextFixture
    {
        private const string ConnectionString = "Data Source=JustAuth.Tests.Unit.db;Cache=Shared";
        public const string VERIFIED_USER_EMAIL = "verified1@test.com";
        public const string VERIFIED_USER_USERNAME = "VerifiedUser1";
        public const string UNVERIFIED_USER_EMAIL = "unverified1@test.com";
        public const string UNVERIFIED_USER_USERNAME = "UnverifiedUser1";
        private static readonly object _lock = new();
        private static bool _databaseInitialized;

        public DbContextFixture()
        {
            lock (_lock)
            {
                if (!_databaseInitialized)
                {
                    using (var context = CreateMockContext())
                    {
                        context.Database.EnsureDeleted();
                        context.Database.EnsureCreated();

                        AppUser verifiedUser = new () {
                            Email = VERIFIED_USER_EMAIL,
                            Username = VERIFIED_USER_USERNAME,
                            PasswordHash = Cryptography.HashPassword("testpwd111"),
                            IsEmailVerified = true
                        };
                        AppUser unverifiedUser = new () {
                            Email = UNVERIFIED_USER_EMAIL,
                            Username = UNVERIFIED_USER_USERNAME,
                            PasswordHash = Cryptography.HashPassword("testpwd111")
                        };
                        
                        context.AddRange(verifiedUser, unverifiedUser);
                        context.SaveChanges();
                    }
                    _databaseInitialized = true;
                }
            }
        }
        public AuthDbMain<AppUser> CreateMockContext()
            => new (
                new DbContextOptionsBuilder<AuthDbMain<AppUser>>()
                    .UseSqlite(ConnectionString)
                    .Options);
        //public class MockDbMain:AuthDbMain<AppUser> {
        //    public MockDbMain (DbContextOptions options): base(options) {
//
        //    }
        //}
    }
}