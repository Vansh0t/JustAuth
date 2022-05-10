namespace JustAuth.Services.Emailing
{
    public class EmailingOptions
    {
        public string Service {get;set;}
        public string Sender{get;set;}
        public string Password{get;set;}
        public string Smtp{get;set;}
        public int Port{get;set;}
        
    }
}