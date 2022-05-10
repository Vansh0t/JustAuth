using JustAuth.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JustAuth.Tests.Fixtures
{
    [Table("IntegrationTestUser")]
    public class TestUser:AppUser
    {
        
    }
}