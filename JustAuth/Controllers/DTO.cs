using System.Text.Json.Serialization;
using JustAuth.Data;

namespace JustAuth.Controllers
{
    public static class DTO
    {
        public class SignInResponse {
            public AppUserDTO User {get;set;}
            public JwtDTO Jwt {get;set;}
            public string RefreshJwt{get;set;}
        }
        public class JwtDTO {
            public string Jwt {get;set;}
            public long Expiration {get;set;}
        }
        public class AppUserDTO {
            public int Id {get;set;}
            public string Username {get;set;}
            //public string Email {get;set;}
            public bool IsEmailVerified {get;set;}
            [JsonConstructor]
            public AppUserDTO() {

            }
            public AppUserDTO  (AppUser user) {
                Id = user.Id;
                Username = user.Username;
                IsEmailVerified = user.IsEmailVerified;
            }
        }
    }
}