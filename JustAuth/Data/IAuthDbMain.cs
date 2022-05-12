using Microsoft.EntityFrameworkCore;
namespace JustAuth.Data
{
    public interface IAuthDbMain<TUser> where TUser : AppUser
    {
        DbSet<TUser> Users {get;set;}
    }
}