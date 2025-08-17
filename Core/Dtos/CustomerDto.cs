using Core.Models;

namespace Core.Dtos
{
    public class CustomerDto : BaseModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; } 

        public virtual IEnumerable<ContactInformationDto>? ContactInformations { get; set; } 

    }
}
