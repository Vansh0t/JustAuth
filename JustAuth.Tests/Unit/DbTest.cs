using System.Linq;
using System.Threading.Tasks;
using JustAuth.Data;
using JustAuth.Tests.Fixtures;
using JustAuth.Controllers;
using Microsoft.EntityFrameworkCore;
using Xunit;
using JustAuth.Services;

namespace JustAuth.Tests.Unit
{
    public class DbTest:IClassFixture<DbContextFixture>
    {
        private readonly AuthDbMain<AppUser> _context;

        public DbTest(DbContextFixture fixture) {
            _context = fixture.CreateMockContext();
        }

        [Fact]
        public async Task TestUsingAtomicTransactionAsync() {
            var result = await _context.UsingAtomicTransactionAsync(
             async (transaction)=> {
                 var user = new AppUser {
                    Email = "TestSavepointsFail@test.com",
                    Username = "TestSavepointsFail",
                    PasswordHash = "adwfAWDAWDwFWFAWD=",
                };
                _context.Add(user);
                await _context.SaveChangesAsync();
                user = await _context.Users.FirstOrDefaultAsync(_=>_.Email==user.Email);
                await transaction.RollbackAsync();
                return ServiceResult.Success();
            });
            var u = await _context.Users.FirstOrDefaultAsync(_=>_.Email=="TestSavepointsFail@test.com");
            Assert.Null(u);
        }
    }
}