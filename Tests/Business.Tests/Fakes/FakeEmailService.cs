using Business.Services.Email;
using Core.Models;

namespace Business.Tests.Fakes
{
    /// <summary>
    /// Sends nothing and keeps the subject of each mail it was handed. Throws on anything the queue
    /// has no business calling.
    /// </summary>
    internal sealed class FakeEmailService : IEmailService
    {
        public List<string> Sent { get; } = new List<string>();

        /// <summary>
        /// Called before each mail; what it throws, sending throws.
        /// </summary>
        public Action<Mail> OnSend { get; set; } = mail => { };

        public Task SendQueuedMailAsync(Mail mail)
        {
            OnSend(mail);
            Sent.Add($"{mail.Subject} to {mail.User.Email}");

            return Task.CompletedTask;
        }

        public Task SendEmailAsync(string to, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
        {
            throw new InvalidOperationException("Not from the queue.");
        }

        public Task QueueEmailAsync(List<User> users, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
        {
            throw new InvalidOperationException("Not from the queue.");
        }

        public Task SendBookingEmailAsync(User user, List<TrainGroupParticipant> emailDatesAdd, List<TrainGroupParticipant> emailDatesRemove)
        {
            throw new InvalidOperationException("Not from the queue.");
        }
    }
}
