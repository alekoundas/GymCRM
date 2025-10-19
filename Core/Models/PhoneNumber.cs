using System.ComponentModel.DataAnnotations;

namespace Core.Models
{
    public class PhoneNumber : BaseModel
    {
        public string Number { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
