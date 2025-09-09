using System.ComponentModel.DataAnnotations;

namespace Core.Dtos.PhoneNumber
{
    public class PhoneNumberDto
    {
        public int? Id { get; set; } // Optional for creation

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format")]
        public string Number { get; set; } = string.Empty;

        [Required(ErrorMessage = "Primary status is required")]
        public bool IsPrimary { get; set; }

        [Required(ErrorMessage = "UserId is required")]
        public string UserId { get; set; } = "";
    }
}
