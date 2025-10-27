using Core.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Business.Services.Email
{
    public class GmailEmailService : IEmailService
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GmailEmailService> _logger;

        public GmailEmailService(IDataService dataService, IConfiguration configuration, ILogger<GmailEmailService> logger)
        {
            _dataService = dataService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            var clientId = _configuration["Gmail:ClientId"];
            var clientSecret = _configuration["Gmail:ClientSecret"];
            var refreshToken = _configuration["Gmail:RefreshToken"];
            var username = _configuration["Gmail:Email"];
            var fromEmail = _configuration["Gmail:Email"];
            var fromName = _configuration["Gmail:FromName"];

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) ||
                string.IsNullOrWhiteSpace(refreshToken) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Gmail API configuration is incomplete: ClientId={ClientId}, ClientSecret={ClientSecret}, RefreshToken={RefreshToken}, Username={Username}, FromEmail={FromEmail}",
                    clientId, clientSecret, refreshToken != null ? "[REDACTED]" : null, username, fromEmail);
                throw new InvalidOperationException("Gmail API configuration is incomplete.");
            }

            try
            {
                var credential = GoogleCredential
                    .FromJson($@"{{
                        ""client_id"": ""{clientId}"",
                        ""client_secret"": ""{clientSecret}"",
                        ""refresh_token"": ""{refreshToken}"",
                        ""type"": ""authorized_user""
                    }}")
                    .CreateScoped(GmailService.Scope.GmailSend);

                // Initialize Gmail service.
                var service = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GymCRM"
                });

                // Create email.
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                mimeMessage.To.Add(new MailboxAddress(to, to));
                mimeMessage.Subject = subject;
                var bodyBuilder = new BodyBuilder
                {
                    TextBody = htmlBody,
                    HtmlBody = htmlBody
                };
                mimeMessage.Body = bodyBuilder.ToMessageBody();

                // Convert to raw base64.
                using var stream = new MemoryStream();
                await mimeMessage.WriteToAsync(stream);
                var rawMessage = Convert.ToBase64String(stream.ToArray())
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                // Send email.
                var message = new Message { Raw = rawMessage };
                var request = service.Users.Messages.Send(message, username);
                await request.ExecuteAsync();

                // Save mail to database.
                User? user = await _dataService.Users.FirstOrDefaultAsync(x => x.Email == to);
                if (user != null)
                    _dataService.Mails.Add(new Mail
                    {
                        UserId = user.Id,
                        Subject = subject,
                        Body = htmlBody,
                    });

                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }

        public async Task SendBookingEmailAsync(string userEmail, List<string> emailDatesAdd, List<string> emailDatesRemove)
        {
            // Initialize HTML body with professional styling
            string emailBody = @"
        <html>
        <head>
            <style>
                body { font-family: Arial, sans-serif; color: #333; line-height: 1.6; }
                .container { max-width: 600px; margin: 0 auto; padding: 20px; }
                h2 { color: #2c3e50; }
                .section { margin-bottom: 20px; }
                ul { list-style-type: none; padding: 0; }
                li { padding: 5px 0; }
                .added { color: #27ae60; }
                .removed { color: #c0392b; }
                .no-changes { font-style: italic; color: #7f8c8d; }
            </style>
        </head>
        <body>
            <div class='container'>
                <h2>Booking Information</h2>";

            // Format added bookings
            emailBody += "<div class='section'>";
            emailBody += "<h3>Added Bookings</h3>";
            if (emailDatesAdd == null || !emailDatesAdd.Any())
            {
                emailBody += "<p class='no-changes'>No bookings were added.</p>";
            }
            else
            {
                emailBody += "<ul>";
                foreach (var date in emailDatesAdd)
                {
                    string formattedDate = FormatDate(date);
                    emailBody += $"<li class='added'>{formattedDate}</li>";
                }
                emailBody += "</ul>";
            }
            emailBody += "</div>";

            // Format removed bookings
            emailBody += "<div class='section'>";
            emailBody += "<h3>Removed Bookings</h3>";
            if (emailDatesRemove == null || !emailDatesRemove.Any())
            {
                emailBody += "<p class='no-changes'>No bookings were removed.</p>";
            }
            else
            {
                emailBody += "<ul>";
                foreach (var date in emailDatesRemove)
                {
                    string formattedDate = FormatDate(date);
                    emailBody += $"<li class='removed'>{formattedDate}</li>";
                }
                emailBody += "</ul>";
            }
            emailBody += "</div>";

            // Close HTML tags
            emailBody += @"
            </div>
        </body>
        </html>";

            // Send the email
            await SendEmailAsync(
                userEmail,
                "Booking Confirmation",
                emailBody
            );
        }

        // Helper method to format dates based on input
        private string FormatDate(string dateInput)
        {
            // Check if the input is a day of the week (e.g., "Monday")
            if (Enum.TryParse(typeof(DayOfWeek), dateInput, true, out _))
            {
                return dateInput; // Return as-is for day of the week
            }

            // Check if the input is a day of the month (e.g., "31")
            if (int.TryParse(dateInput, out int day) && day >= 1 && day <= 31)
            {
                return $"Day {day}"; // Format as "Day 31"
            }

            // Check if the input is a parseable date (e.g., "2025-10-27")
            if (DateTime.TryParse(dateInput, out DateTime date))
            {
                return date.ToString("MMMM dd, yyyy"); // Format as "October 27, 2025"
            }

            // Fallback for unrecognized formats
            return dateInput;
        }
    }
}