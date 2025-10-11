using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Mail
{
    public class MailAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(200, ErrorMessage = TranslationKeys.Subject_length_must_be_between_5_and_200_character_long, MinimumLength = 5)]
        public string Subject { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Body { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
    }
}
