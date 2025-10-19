using System.Collections.ObjectModel;

namespace Core.Models
{
    public class UserStatus : BaseModel
    {
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new Collection<User>();
    }
}
