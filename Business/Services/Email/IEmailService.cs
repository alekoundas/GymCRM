using Core.Models;

namespace Business.Services.Email
{
    public interface IEmailService
    {
        // Sends there and then. Kept for the one mail somebody is sat waiting for -
        // the password reset - where a queue would only add delay.
        Task SendEmailAsync(string to, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null);

        // Writes the mail down as pending and returns. The background sender takes it.
        // One row per recipient, written in a single insert.
        Task QueueEmailAsync(List<User> users, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null);

        // Used by the background sender only - the row already exists, so this one
        // does not write history, it fills in the outcome.
        Task SendQueuedMailAsync(Mail mail);

        Task SendBookingEmailAsync(User user, List<TrainGroupParticipant> emailDatesAdd, List<TrainGroupParticipant> emailDatesRemove);
    }
}
