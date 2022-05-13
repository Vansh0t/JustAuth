using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace JustAuth.Data
{
    
    [Index(nameof(Username), nameof(Email), IsUnique = true)]
    public class AppUser
    {
        public int Id {get; set;}
        [Required]
        [EmailAddress]
        public string Email {get;set;}
        [Required]
        public string Username {get;set;}
        [Required]
        public string PasswordHash {get;set;}
        public string? EmailVrfToken {get;set;}
        public DateTime? EmailVrfTokenExpiration {get;set;}
        [EmailAddress]
        public string NewEmail {get;set;}
        public bool IsEmailVerified {get;set;}
        public string? PasswordResetToken {get;set;}
        public DateTime? PasswordResetTokenExpiration {get;set;}
        public JwtRefreshToken JwtRefreshToken {get;set;}
    }
}