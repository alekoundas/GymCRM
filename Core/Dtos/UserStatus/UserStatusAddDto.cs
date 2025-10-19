using Core.Dtos.User;
using Core.Translations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.UserStatus
{
    public class UserStatusAddDto
    {
        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Name { get; set; } = string.Empty;


        [Required(ErrorMessage = TranslationKeys._0_is_required)]
        public string Color { get; set; } = string.Empty;


        public virtual ICollection<UserDto> Users { get; set; } = new Collection<UserDto>();
    }
}
