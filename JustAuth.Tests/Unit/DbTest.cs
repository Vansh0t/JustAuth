using System.Linq;
using System.Threading.Tasks;
using JustAuth.Data;
using JustAuth.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace JustAuth.Tests.Unit
{
    public class DbTest:IClassFixture<DbContextFixture>
    {
        private readonly AuthDbMain<AppUser> _context;

        public DbTest(DbContextFixture fixture) {
            _context = fixture.CreateMockContext();
        }

        [Fact]
        public async Task TestSavepointsFail() {
            using (var trans = await _context.Database.BeginTransactionAsync()) {
                var user = new AppUser {
                    Email = "TestSavepointsFail@test.com",
                    Username = "TestSavepointsFail",
                    PasswordHash = "adwfAWDAWDwFWFAWD=",
                };
                await trans.CreateSavepointAsync("TestSavepointsFail");
                _context.Add(user);
                await _context.SaveChangesAsync();
                //await trans.CommitAsync();
                user = await _context.Users.FirstOrDefaultAsync(_=>_.Email==user.Email);
                Assert.NotNull(user);
                await trans.RollbackToSavepointAsync("TestSavepointsFail");
                await trans.CommitAsync();
            }
            var u = await _context.Users.FirstOrDefaultAsync(_=>_.Email=="TestSavepointsFail@test.com");
            Assert.Null(u);
        }
    }
}