using Microsoft.AspNetCore.Identity;
using System.Collections.ObjectModel;

namespace Core.Models
{
    public class Role : IdentityRole<Guid>
    {
        public virtual ICollection<UserRole> UserRoles { get; set; } = new Collection<UserRole>();
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
