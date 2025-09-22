using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Mail
{
    public class MailSendDto
    {

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject length must be between 5 and 200 character long!", MinimumLength = 5)]
        public string Subject { get; set; } = string.Empty;


        [Required(ErrorMessage = "Body is required")]
        public string Body { get; set; } = string.Empty;


        [Required(ErrorMessage = "UserIds is required")]
        public List<string> UserIds { get; set; } = new List<string>();

        public string CreatedOn { get; set; } = "";

    }
}
