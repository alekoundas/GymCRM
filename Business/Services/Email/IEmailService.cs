namespace Business.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string plainTextBody, string htmlBody = null);
    }
}
