using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Business.Services.Email
{
    public class GmailEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GmailEmailService> _logger;

        public GmailEmailService(IConfiguration configuration, ILogger<GmailEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string plainTextBody, string htmlBody)
        {
            var smtpHost = _configuration["Gmail:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Gmail:SmtpPort"] ?? "587");
            var username = _configuration["Gmail:Email"];
            var appPassword = _configuration["Gmail:EmailAppPassword"];
            var fromEmail = _configuration["Gmail:Email"];
            var fromName = _configuration["Gmail:FromName"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(appPassword) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Gmail configuration is incomplete: SmtpHost={SmtpHost}, Username={Username}, AppPassword={AppPassword}, FromEmail={FromEmail}",
                    smtpHost, username, appPassword != null ? "[REDACTED]" : null, fromEmail);
                throw new InvalidOperationException("Gmail configuration is incomplete.");
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    TextBody = plainTextBody,
                    HtmlBody = htmlBody
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                _logger.LogInformation("Connecting to SMTP server {SmtpHost}:{SmtpPort}", smtpHost, smtpPort);
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);

                _logger.LogInformation("Authenticating with username {Username}", username);
                await client.AuthenticateAsync(username, appPassword);

                _logger.LogInformation("Sending email to {To}", to);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP command error while sending email to {To}. ErrorCode: {ErrorCode}, StatusCode: {StatusCode}", to, ex.ErrorCode, ex.StatusCode);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}