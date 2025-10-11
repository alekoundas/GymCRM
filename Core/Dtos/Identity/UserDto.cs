using Core.Dtos.UserRole;
using Core.Translations;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Identity
{
    public class UserDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(100, ErrorMessage = TranslationKeys.UserName_cannot_exceed_100_characters)]
        public string UserName { get; set; } = "";


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [EmailAddress(ErrorMessage = TranslationKeys.Invalid_email_address)]
        [StringLength(100, ErrorMessage = TranslationKeys.Email_cannot_exceed_100_characters)]
        public string Email { get; set; } = "";


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(50, ErrorMessage = TranslationKeys.FirstName_cannot_exceed_50_characters)]
        public string FirstName { get; set; } = "";


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        [StringLength(50, ErrorMessage = TranslationKeys.LastName_cannot_exceed_50_characters)]
        public string LastName { get; set; } = "";


        public byte[]? ProfileImage { get; set; }

        public List<UserRoleDto> UserRoles { get; set; } = new List<UserRoleDto>();
        //public string? RoleId { get; set; }
    }
}
