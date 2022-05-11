namespace JustAuth.Controllers
{
    public class MappingOptions
    {
        public string EmailConfirmRedirectUrl {get;set;} = "/auth/email/vrf";
        public string PasswordResetRedirectUrl {get;set;}
    }
}