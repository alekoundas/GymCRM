using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.User
{
    public class UserPasswordResetDto
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Token is required")]
        public string Token { get; set; } = string.Empty;


        [Required(ErrorMessage = "Old password is required")]
        public string OldPassword { get; set; } = string.Empty;


        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
        
        
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
