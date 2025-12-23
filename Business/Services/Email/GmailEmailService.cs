using Business.Services.CalendarService;
using Core.Models;
using Core.Translations;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Utils;
using System.Text;

namespace Business.Services.Email
{
    public class GmailEmailService : IEmailService
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly IStringLocalizer _localizer;
        private readonly ICalendarService _calendarService;
        private readonly ILogger<GmailEmailService> _logger;

        public GmailEmailService(
            IDataService dataService,
            IConfiguration configuration,
            IStringLocalizer localizer,
            ICalendarService calendarService,
            ILogger<GmailEmailService> logger)
        {
            _dataService = dataService;
            _configuration = configuration;
            _localizer = localizer;
            _calendarService = calendarService;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
        {
            List<GoogleRefreshToken> dbtokens = await _dataService.GoogleRefreshTokens.ToListAsync();
            if (dbtokens.Count != 1)
            {
                return;
            }

            var clientId = _configuration["Gmail:ClientId"];
            var clientSecret = _configuration["Gmail:ClientSecret"];
            var refreshToken = dbtokens[0].RefreshToken;
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
                    ApplicationName = "RosaCoreLab"
                });

                // Create email.
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                mimeMessage.To.Add(new MailboxAddress(to, to));
                mimeMessage.Subject = Encoding.UTF8.GetString(Rfc2047.EncodeText(Encoding.UTF8, subject));
                var bodyBuilder = new BodyBuilder
                {
                    TextBody = htmlBody,
                    HtmlBody = htmlBody
                };

                // Add attachments if provided
                if (attachments != null && attachments.Any())
                {
                    foreach (var (content, fileName) in attachments)
                    {
                        // .ics is text/calendar MIME type
                        var attachment = new MimePart("text", "calendar")
                        {
                            Content = new MimeContent(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content))),
                            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment)
                            {
                                FileName = Path.GetFileName(fileName)
                            },
                            //FileName = Path.GetFileName(fileName)
                        };
                        bodyBuilder.Attachments.Add(attachment);
                    }
                }

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

        private List<string> GetDateStrings(List<TrainGroupParticipant> participants)
        {
            List<string> stringDates = new List<string>();
            string dateString = "";

            foreach (TrainGroupParticipant participant in participants)
            {

                if (participant.TrainGroupDate.RecurrenceDayOfMonth != null)
                {
                    if (participant.SelectedDate == null)
                        dateString = _localizer[TranslationKeys.Every_0_of_the_month, participant.TrainGroupDate.RecurrenceDayOfMonth.ToString()];
                    else
                        dateString = participant.SelectedDate.Value.ToString("yyyy-MM-dd");
                }
                if (participant.TrainGroupDate.RecurrenceDayOfWeek != null)
                {
                    if (participant.SelectedDate == null)
                        dateString = participant.TrainGroupDate.RecurrenceDayOfWeek.ToString();
                    else
                        dateString = participant.SelectedDate.Value.ToString("yyyy-MM-dd");
                }
                if (participant.TrainGroupDate.FixedDay != null)
                    dateString = participant.TrainGroupDate.FixedDay.Value.ToString("yyyy-MM-dd");

                if (dateString != null)
                    stringDates.Add(dateString);
            }

            return stringDates;
        }

        public async Task SendBookingEmailAsync(User user, List<TrainGroupParticipant> emailParticipantsAdd, List<TrainGroupParticipant> emailParticipantsRemove)
        {


            List<string> emailDatesAdd = GetDateStrings(emailParticipantsAdd);
            List<string> emailDatesRemove = GetDateStrings(emailParticipantsRemove);


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
                        <h2>" + _localizer[TranslationKeys.Booking_Information] + "</h2>";

            // Format added bookings
            emailBody += "<div class='section'>";
            emailBody += "<h3>" + _localizer[TranslationKeys.Added_Bookings] + "</h3>";
            if (emailDatesAdd == null || !emailDatesAdd.Any())
            {
                emailBody += "<p class='no-changes'>" + _localizer[TranslationKeys.No_bookings_were_added] + "</p>";
            }
            else
            {
                emailBody += "<ul>";
                foreach (var date in emailDatesAdd)
                {
                    //string formattedDate = FormatDate(date);
                    emailBody += $"<li class='added'>{date}</li>";
                }
                emailBody += "</ul>";
            }
            emailBody += "</div>";

            // Format removed bookings
            emailBody += "<div class='section'>";
            emailBody += "<h3>" + _localizer[TranslationKeys.Removed_Bookings] + "</h3>";
            if (emailDatesRemove == null || !emailDatesRemove.Any())
            {
                emailBody += "<p class='no-changes'>" + _localizer[TranslationKeys.No_bookings_were_removed] + "</p>";
            }
            else
            {
                emailBody += "<ul>";
                foreach (var date in emailDatesRemove)
                {
                    //string formattedDate = FormatDate(date);
                    emailBody += $"<li class='removed'>{date}</li>";
                }
                emailBody += "</ul>";
            }
            emailBody += "</div>";


            List<(string IcsContent, string FileName)> addIcsList = new List<(string IcsContent, string FileName)>();
            List<(string IcsContent, string FileName)> cancelIcsList = new List<(string IcsContent, string FileName)>();
            string? organizerEmail = _configuration["Gmail:Email"];
            if (organizerEmail != null)
            {

                addIcsList = _calendarService.GenerateAddIcsContents(
                    emailParticipantsAdd, organizerEmail, user.Email, user.Id);

                cancelIcsList = _calendarService.GenerateCancelIcsContents(
                    emailParticipantsRemove, organizerEmail, user.Email, user.Id);


                // New section: Calendar instructions
                if ((addIcsList?.Any() ?? false) || (cancelIcsList?.Any() ?? false))
                {
                    emailBody += "<div class='section'>";
                    emailBody += "<div class='calendar-note'>";
                    emailBody += "<h3>" + _localizer[TranslationKeys.Calendar_Integration] + "</h3>";
                    emailBody += "<p>" + _localizer[TranslationKeys.Attached_are_ics_files_for_the_changes_above_Open_the_attachments_in_Gmail_or_download_them_to_add_remove_events_from_your_Google_Calendar] + "</p>";

                    if (addIcsList?.Any() ?? false)
                        emailBody += "<p><strong>" + _localizer[TranslationKeys.Add_to_Calendar] + ":</strong> " + _localizer[TranslationKeys.Click_the_attached_add_ics_files_and_select_Add_to_Calendar] + "</p>";

                    if (cancelIcsList?.Any() ?? false)
                        emailBody += "<p><strong>" + _localizer[TranslationKeys.Remove_from_Calendar] + ":</strong> " + _localizer[TranslationKeys.Click_the_attached_cancel_ics_files_to_remove_the_events] + "</p>";

                    emailBody += "</div>";
                    emailBody += "</div>";
                }

            }




            // Close HTML tags
            emailBody += @"
            </div>
        </body>
        </html>";

            // Send the email
            await SendEmailAsync(
                user.Email,
                _localizer[TranslationKeys.Booking_Confirmation],
                emailBody,
                addIcsList.Concat(cancelIcsList).ToList()
            );
        }

        // Helper method to format dates based on input
        //private string FormatDate(string dateInput)
        //{
        //    // Check if the input is a day of the week (e.g., "Monday")
        //    if (Enum.TryParse(typeof(DayOfWeek), dateInput, true, out _))
        //    {
        //        return dateInput; // Return as-is for day of the week
        //    }

        //    // Check if the input is a day of the month (e.g., "31")
        //    if (int.TryParse(dateInput, out int day) && day >= 1 && day <= 31)
        //    {
        //        return $"Day {day}"; // Format as "Day 31"
        //    }

        //    // Check if the input is a parseable date (e.g., "2025-10-27")
        //    if (DateTime.TryParse(dateInput, out DateTime date))
        //    {
        //        return date.ToString("MMMM dd, yyyy"); // Format as "October 27, 2025"
        //    }

        //    // Fallback for unrecognized formats
        //    return dateInput;
        //}
    }
}