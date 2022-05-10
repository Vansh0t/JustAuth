using Microsoft.EntityFrameworkCore;
namespace JustAuth.Data
{
    public class AuthDbMain<TUser>:DbContext where TUser : AppUser
    {
        public DbSet<TUser> Users {get;set;}
        public AuthDbMain(DbContextOptions options) : base(options) {

        }
    }
}