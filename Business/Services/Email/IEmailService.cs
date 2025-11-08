using Core.Models;

namespace Business.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null);
        Task SendBookingEmailAsync(User user, List<TrainGroupParticipant> emailDatesAdd, List<TrainGroupParticipant> emailDatesRemove);
    }
}
