namespace Business.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
        Task SendBookingEmailAsync(string userEmail, List<string> emailDatesAdd, List<string> emailDatesRemove);
    }
}
