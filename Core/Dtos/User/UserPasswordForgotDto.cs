using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.User
{
    public class UserPasswordForgotDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Email { get; set; } = string.Empty;
    }
}
