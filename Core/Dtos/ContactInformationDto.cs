using Core.Enums;
using Core.Models;

namespace Core.Dtos
{
    public class ContactInformationDto : BaseModel
    {
        public ContactInformationTypesEnum? Type { get; set; }
        public string? Value{ get; set; }
        public string? Description { get; set; }
        public bool? IsFavorite { get; set; }


        public int? CustomerId { get; set; }
        public CustomerDto? Customer { get; set; }
    }
}
