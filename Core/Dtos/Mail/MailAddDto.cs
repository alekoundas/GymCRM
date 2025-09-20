using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Mail
{
    public class MailAddDto
    {
        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, ErrorMessage = "Subject length must be between 5 and 200 character long!", MinimumLength = 5)]
        public string Subject { get; set; } = string.Empty;


        [Required(ErrorMessage = "Body is required")]
        public string Body { get; set; } = string.Empty;


        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = "";
    }
}
