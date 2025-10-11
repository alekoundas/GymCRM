using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.User
{
    public class UserPasswordChangeDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [MinLength(6, ErrorMessage = TranslationKeys.New_password_must_be_at_least_6_characters)]
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
