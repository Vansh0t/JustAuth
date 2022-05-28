using JustAuth.Utils;
using Microsoft.IdentityModel.Tokens;
namespace JustAuth.Services.Auth
{
    public class JwtOptions
    {
        public string Issuer {get;set;}
        public bool ValidateIssuer {get;set;}
        public string Audience {get;set;}
        public bool ValidateAudience {get;set;}
        public bool ValidateLifetime {get;set;}
        public string IssuerSigningKey {get;set;}
        public bool ValidateIssuerSigningKey {get;set;}
        public int TokenLifetime {get;set;}
        public bool UseRefreshToken{get;set;}
        public int RefreshTokenLifetime {get;set;}
        public bool SendAsCookie {get;set;}
        public IEnumerable<JwtClaim> Claims {get;set;}

        private SymmetricSecurityKey symKey;

        public SymmetricSecurityKey GetSymmetricSecurityKey() {
            if(symKey is null) {
                symKey = Cryptography.GetJwtSigningKey(IssuerSigningKey);
            }
            return symKey;
        }

        public class JwtClaim {
            public string Name {get;set;}

            public string ModelProperty {get;set;}
            public IEnumerable<string> AccessValues{get;set;}
        }
    }
}