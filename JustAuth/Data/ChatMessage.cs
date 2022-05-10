using System.ComponentModel.DataAnnotations;

namespace JustAuth.Data
{
    public class ChatMessage
    {
        public int Id {get;set;}
        [Required]
        public DateTime SendTime{get;set;}
        public string Text {get;set;}
        public int SenderId {get;set;}
        public AppUser Sender {get;set;}
    }
}