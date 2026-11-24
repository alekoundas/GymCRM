using Business.Services.Email;
using Core.Models;

namespace API.Tests.Fakes
{
    /// <summary>
    /// Keeps every mail the app meant to send, each as a line that reads like it.
    /// </summary>
    public sealed class RecordingEmailService : IEmailService
    {
        private readonly List<string> _lines = new List<string>();
        private readonly List<string> _bodies = new List<string>();

        /// <summary>
        /// "send maria@gym.test: Subject", "queue maria@gym.test, nikos@gym.test: Subject" and
        /// "booking maria@gym.test: +1,2 -3", where the numbers are the bookings' ids.
        /// </summary>
        public IReadOnlyList<string> Lines
        {
            get { return _lines; }
        }

        public IReadOnlyList<string> Bodies
        {
            get { return _bodies; }
        }

        /// <summary>
        /// The bookings of the last booking mail, as they were handed over.
        /// </summary>
        public List<TrainGroupParticipant> Added { get; private set; } = new List<TrainGroupParticipant>();

        public List<TrainGroupParticipant> Removed { get; private set; } = new List<TrainGroupParticipant>();

        /// <summary>
        /// What sending a booking mail throws.
        /// </summary>
        public Exception? Failure { get; set; }

        public void Reset()
        {
            _lines.Clear();
            _bodies.Clear();
            Added = new List<TrainGroupParticipant>();
            Removed = new List<TrainGroupParticipant>();
            Failure = null;
        }

        public Task SendEmailAsync(string to, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
        {
            _lines.Add($"send {to}: {subject}");
            _bodies.Add(htmlBody);

            return Task.CompletedTask;
        }

        public Task QueueEmailAsync(List<User> users, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
        {
            _lines.Add($"queue {string.Join(", ", users.Select(x => x.Email).Order(StringComparer.Ordinal))}: {subject}");
            _bodies.Add(htmlBody);

            return Task.CompletedTask;
        }

        public Task SendBookingEmailAsync(User user, List<TrainGroupParticipant> emailDatesAdd, List<TrainGroupParticipant> emailDatesRemove)
        {
            if (Failure != null)
                throw Failure;

            _lines.Add($"booking {user.Email}: +{IdsOf(emailDatesAdd)} -{IdsOf(emailDatesRemove)}");
            Added = emailDatesAdd;
            Removed = emailDatesRemove;

            return Task.CompletedTask;
        }

        public Task SendQueuedMailAsync(Mail mail)
        {
            throw new InvalidOperationException("Only the background sender calls this.");
        }

        private static string IdsOf(List<TrainGroupParticipant> participants)
        {
            return string.Join(",", participants.Select(x => x.Id));
        }
    }
}
