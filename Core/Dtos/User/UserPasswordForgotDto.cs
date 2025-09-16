using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.User
{
    public class UserPasswordForgotDto
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = string.Empty;
    }
}
