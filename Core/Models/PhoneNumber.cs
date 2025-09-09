using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class PhoneNumber : BaseModel
    {
        [Required]
        [StringLength(20)]
        public string Number { get; set; } = string.Empty;

        [Required]
        public bool IsPrimary { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; } = null!;
    }
}
