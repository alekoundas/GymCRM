using Core.Enums;

namespace Core.Models
{
    public class Mail : BaseModel
    {
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        // Nothing is sent on the request thread any more - a mail is written down
        // first and the background sender works through what is still pending.
        public MailStatusEnum Status { get; set; }
        public DateTime? SentOn { get; set; }
        public string Error { get; set; } = string.Empty;

        // The .ics files of a booking mail, as JSON. They are small, always read
        // together with the mail and never queried on their own, so a table of
        // their own would only buy joins.
        public string Attachments { get; set; } = string.Empty;

        // Recipient.
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
