namespace Core.Models
{
    public class Mail : BaseModel
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // Recipient.
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
