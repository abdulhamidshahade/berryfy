namespace Berryfy.Application.Dtos.EmailDtos.Requests
{
    public class SendEmailRequestDto
    {
        public string Recipient { get; set; }
        public string Subject { get; set; } 
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; } 
    }
}