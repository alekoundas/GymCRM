using Core.Dtos.UserRole;
using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.Identity
{
    public class UserDto
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "UserName is required")]
        [StringLength(100, ErrorMessage = "UserName cannot exceed 100 characters")]
        public string UserName { get; set; } = "";


        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string Email { get; set; } = "";


        [Required(ErrorMessage = "FirstName is required")]
        [StringLength(50, ErrorMessage = "FirstName cannot exceed 50 characters")]
        public string FirstName { get; set; } = "";


        [Required(ErrorMessage = "LastName is required")]
        [StringLength(50, ErrorMessage = "LastName cannot exceed 50 characters")]
        public string LastName { get; set; } = "";


        public byte[]? ProfileImage { get; set; }

        public List<UserRoleDto> UserRoles { get; set; } = new List<UserRoleDto>();
        //public string? RoleId { get; set; }
    }
}
