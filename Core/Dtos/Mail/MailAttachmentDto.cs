namespace Core.Dtos.Mail
{
    // The shape of the Attachments column. A booking email keeps its .ics files
    // here until the background sender picks the mail up.
    public class MailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
