using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.PhoneNumber
{
    public class PhoneNumberAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(20, ErrorMessage = TranslationKeys.Phone_number_cannot_exceed_20_characters)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = TranslationKeys.Invalid_phone_number_format)]
        public string Number { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string UserId { get; set; } = "";
    }
}
