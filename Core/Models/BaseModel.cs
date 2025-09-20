namespace Core.Models
{
    public class BaseModel
    {
        public int Id { get; set; }

        public string CreatedBy_Id { get; set; } = string.Empty;

        public string CreatedBy_FullName { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
