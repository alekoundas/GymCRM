using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
                // Replace this block:
                /*
                var credential = new GoogleCredential(new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                }, new[] { GmailService.Scope.GmailSend }, refreshToken);
                */

                // With the following code:
                var credential = GoogleCredential
                    .FromJson($@"{{
                        ""client_id"": ""{clientId}"",
                        ""client_secret"": ""{clientSecret}"",
                        ""refresh_token"": ""{refreshToken}"",
                        ""type"": ""authorized_user""
                    }}")
                    .CreateScoped(GmailService.Scope.GmailSend);

                // Initialize Gmail service
                var service = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "GymCRM"
                });

                // Create email
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                mimeMessage.To.Add(new MailboxAddress(to, to));
                mimeMessage.Subject = subject;
                var bodyBuilder = new BodyBuilder
                {
                    TextBody = plainTextBody,
                    HtmlBody = htmlBody
                };
                mimeMessage.Body = bodyBuilder.ToMessageBody();

                // Convert to raw base64
                using var stream = new MemoryStream();
                await mimeMessage.WriteToAsync(stream);
                var rawMessage = Convert.ToBase64String(stream.ToArray())
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                // Send email
                var message = new Message { Raw = rawMessage };
                var request = service.Users.Messages.Send(message, username);
                await request.ExecuteAsync();

                _logger.LogInformation("Email sent successfully to {To}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", to);
                throw new InvalidOperationException($"Failed to send email: {ex.Message}", ex);
            }
        }
    }
}