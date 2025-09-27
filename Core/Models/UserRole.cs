using Microsoft.AspNetCore.Identity;

namespace Core.Models
{
    public class UserRole : IdentityUserRole<Guid>
    {
        public User User { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
