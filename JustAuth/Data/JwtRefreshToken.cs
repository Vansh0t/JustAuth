namespace JustAuth.Data
{
    public class JwtRefreshToken
    {
        public int Id {get;set;}
        public string? Token {get;set;}
        public DateTime? IssuedAt {get;set;}
        public DateTime? ExpiresAt{get;set;}
        public int UserId {get;set;}
        public AppUser User {get;set;}
    }
}