using System.Collections.ObjectModel;

namespace Core.Models
{
    public class Customer : BaseModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        //public virtual ICollection<ContactInformation> ContactInformations { get; set; } = new Collection<ContactInformation>();
    }
}
