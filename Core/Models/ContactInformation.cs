using Core.Enums;

namespace Core.Models
{
    public class ContactInformation : BaseModel
    {
        public ContactInformationTypesEnum Type { get; set; }
        public string Value { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsFavorite { get; set; }


        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
