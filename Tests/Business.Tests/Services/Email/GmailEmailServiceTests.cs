using System.Net;
using System.Reflection;
using System.Text;
using Business.Services.Email;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models;
using Google.Apis.Gmail.v1;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Tests.Shared;

namespace Business.Tests.Services.Email
{
    /// <summary>
    /// What is handed to Gmail, what is written down about it, and what stops it.
    /// </summary>
    public sealed class GmailEmailServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly StubGmail _gmail = new StubGmail();
        private readonly FakeCalendarService _calendar = new FakeCalendarService();
        private readonly RecordingLogger<GmailEmailService> _log = new RecordingLogger<GmailEmailService>();
        private readonly Dictionary<string, string?> _settings = new Dictionary<string, string?>
        {
            ["Gmail:ClientId"] = "client-id",
            ["Gmail:ClientSecret"] = "client-secret",
            ["Gmail:Email"] = "gym@gym.test",
            ["Gmail:FromName"] = "RosaCoreLab"
        };

        public GmailEmailServiceTests()
        {
            _seed = new Seed(_database);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        // The client it would build for itself.
        private GmailEmailService OwnClient()
        {
            IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(_settings).Build();

            return new GmailEmailService(DataServices.On(_database), configuration, new FakeLocalizer(), _calendar, _log);
        }

        private GmailEmailService Service()
        {
            GmailEmailService service = OwnClient();
            _gmail.PutInto(service);

            return service;
        }

        private void AddToken(string token = "refresh-token")
        {
            _database.Add(new GoogleRefreshToken { RefreshToken = token });
        }


        // ================================================================
        // The client
        // ================================================================

        // Built without a word to Google: the access token is only fetched by the first mail.
        private static async Task<GmailService> ClientAsync(GmailEmailService service)
        {
            MethodInfo method = typeof(GmailEmailService).GetMethod("GetGmailServiceAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

            return await (Task<GmailService>)method.Invoke(service, null)!;
        }

        [Fact]
        public async Task The_client_is_built_from_the_settings_and_the_one_token_there_is()
        {
            AddToken();

            GmailService client = await ClientAsync(OwnClient());

            client.ApplicationName.ShouldBe("RosaCoreLab");
            client.HttpClientInitializer.ShouldNotBeNull();
            _log.Lines.ShouldBeEmpty();
        }

        // A batch of mails must not pay for a new one each time.
        [Fact]
        public async Task The_client_is_built_once()
        {
            AddToken();
            GmailEmailService service = OwnClient();

            GmailService first = await ClientAsync(service);
            _settings.Clear();
            _database.Reset();

            (await ClientAsync(service)).ShouldBeSameAs(first);
        }

        [Theory]
        [InlineData("Gmail:ClientId")]
        [InlineData("Gmail:ClientSecret")]
        [InlineData("Gmail:Email")]
        public async Task A_setting_that_is_missing_or_blank_stops_every_mail(string setting)
        {
            AddToken();

            foreach (string? value in new[] { null, "", "  " })
            {
                _settings[setting] = value;

                (await Should.ThrowAsync<InvalidOperationException>(() => OwnClient().SendEmailAsync("maria@gym.test", "Hello", "Body"))).Message
                    .ShouldBe("Gmail API configuration is incomplete.");
            }
        }

        // With two there is no telling which of them is the one.
        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task Anything_but_one_token_stops_every_mail(int tokens)
        {
            for (int i = 0; i < tokens; i++)
                AddToken();

            await Should.ThrowAsync<InvalidOperationException>(() => OwnClient().SendEmailAsync("maria@gym.test", "Hello", "Body"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task An_empty_token_stops_every_mail(string token)
        {
            AddToken(token);

            await Should.ThrowAsync<InvalidOperationException>(() => OwnClient().SendEmailAsync("maria@gym.test", "Hello", "Body"));
        }

        // The log says what is there and what is not. Neither secret is written out.
        [Fact]
        public async Task What_is_missing_is_logged()
        {
            AddToken();
            _settings["Gmail:ClientId"] = null;

            await Should.ThrowAsync<InvalidOperationException>(() => OwnClient().SendEmailAsync("maria@gym.test", "Hello", "Body"));

            _log.Lines.ShouldBe(["Error: Gmail API configuration is incomplete: ClientId=(null), ClientSecret=[REDACTED], RefreshToken=[REDACTED], Username=gym@gym.test, TokenRows=1"]);
        }

        [Fact]
        public async Task A_missing_token_is_logged_as_missing()
        {
            await Should.ThrowAsync<InvalidOperationException>(() => OwnClient().SendEmailAsync("maria@gym.test", "Hello", "Body"));

            _log.Lines.ShouldBe(["Error: Gmail API configuration is incomplete: ClientId=client-id, ClientSecret=[REDACTED], RefreshToken=(null), Username=gym@gym.test, TokenRows=0"]);
        }

        [Theory]
        [InlineData(null, "(null)")]
        [InlineData("", "")]
        [InlineData("  ", "  ")]
        public async Task A_missing_secret_is_logged_as_it_is(string? secret, string logged)
        {
            AddToken();
            _settings["Gmail:ClientSecret"] = secret;

            await Should.ThrowAsync<InvalidOperationException>(() => OwnClient().SendEmailAsync("maria@gym.test", "Hello", "Body"));

            _log.Lines.ShouldBe([$"Error: Gmail API configuration is incomplete: ClientId=client-id, ClientSecret={logged}, RefreshToken=[REDACTED], Username=gym@gym.test, TokenRows=1"]);
        }


        // ================================================================
        // Sending there and then
        // ================================================================

        [Fact]
        public async Task A_mail_is_handed_to_gmail_from_the_gym()
        {
            await Service().SendEmailAsync("maria@gym.test", "Reset Your Password", "<p>Click</p>");

            _gmail.Requests.ShouldBe(["POST https://gmail.googleapis.com/gmail/v1/users/gym%40gym.test/messages/send"]);

            MimeMessage mail = _gmail.Mails.Single();
            mail.From.Mailboxes.Single().Name.ShouldBe("RosaCoreLab");
            mail.From.Mailboxes.Single().Address.ShouldBe("gym@gym.test");
            mail.To.Mailboxes.Single().Address.ShouldBe("maria@gym.test");
            mail.To.Mailboxes.Single().Name.ShouldBe("maria@gym.test");
            mail.Subject.ShouldBe("Reset Your Password");
            mail.HtmlBody.ShouldBe("<p>Click</p>");
            mail.TextBody.ShouldBe("<p>Click</p>");
            mail.Attachments.ShouldBeEmpty();
            _log.Lines.ShouldBe(["Information: Email sent successfully to maria@gym.test"]);
        }

        [Fact]
        public async Task Greek_arrives_as_greek()
        {
            await Service().SendEmailAsync("maria@gym.test", "Επιβεβαίωση κράτησης", "<p>Καλησπέρα</p>");

            MimeMessage mail = _gmail.Mails.Single();
            mail.Subject.ShouldBe("Επιβεβαίωση κράτησης");
            mail.HtmlBody.ShouldBe("<p>Καλησπέρα</p>");
        }

        // Gmail takes the letters a url may hold, and no padding.
        [Fact]
        public async Task The_mail_is_written_so_that_it_may_be_in_a_url()
        {
            for (int i = 0; i < 12; i++)
                await Service().SendEmailAsync("maria@gym.test", "Hello", new string('ψ', i) + "???>>>~~~");

            string written = string.Concat(_gmail.Raw);
            written.ShouldNotContain("+");
            written.ShouldNotContain("/");
            written.ShouldNotContain("=");
            _gmail.Mails.Last().HtmlBody.ShouldBe(new string('ψ', 11) + "???>>>~~~");
        }

        [Fact]
        public async Task A_gym_with_no_name_sends_from_its_address_alone()
        {
            _settings["Gmail:FromName"] = null;

            await Service().SendEmailAsync("maria@gym.test", "Hello", "Body");

            _gmail.Mails.Single().From.Mailboxes.Single().Name.ShouldBeNullOrEmpty();
            _gmail.Mails.Single().From.Mailboxes.Single().Address.ShouldBe("gym@gym.test");
        }

        private static string TextOf(MimePart part)
        {
            using MemoryStream stream = new MemoryStream();
            part.Content.ShouldNotBeNull().DecodeTo(stream);

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        [Fact]
        public async Task Its_files_are_calendar_files()
        {
            await Service().SendEmailAsync("maria@gym.test", "Hello", "Body", [("BEGIN:VCALENDAR Καλό", "add-1.ics"), ("second", "folder/add-2.ics")]);

            List<MimePart> files = _gmail.Mails.Single().Attachments.Cast<MimePart>().ToList();

            files.Select(x => x.FileName).ShouldBe(["add-1.ics", "add-2.ics"]);
            files.Select(x => x.ContentType.MimeType).ShouldBe(["text/calendar", "text/calendar"]);
            files.Select(x => x.ContentDisposition.ShouldNotBeNull().Disposition).ShouldBe(["attachment", "attachment"]);
            files.Select(TextOf).ShouldBe(["BEGIN:VCALENDAR Καλό", "second"]);
        }

        [Fact]
        public async Task An_empty_list_of_files_is_no_files()
        {
            await Service().SendEmailAsync("maria@gym.test", "Hello", "Body", []);

            _gmail.Mails.Single().Attachments.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_mail_to_a_member_is_kept_as_sent()
        {
            User member = _seed.User(x => x.Email = "maria@gym.test");
            DateTime before = DateTime.UtcNow;

            await Service().SendEmailAsync("maria@gym.test", "Hello", "<p>Body</p>", [("content", "a.ics")]);

            Mail mail = _database.Read(db => db.Mails.Single());
            mail.UserId.ShouldBe(member.Id);
            mail.Subject.ShouldBe("Hello");
            mail.Body.ShouldBe("<p>Body</p>");
            mail.Status.ShouldBe(MailStatusEnum.SENT);
            mail.SentOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            mail.Error.ShouldBe("");
            mail.Attachments.ShouldBe("");
        }

        // Found letter for letter: an address typed in another case is nobody's.
        [Theory]
        [InlineData("nobody@gym.test")]
        [InlineData("MARIA@gym.test")]
        public async Task A_mail_to_nobody_we_know_is_sent_and_not_kept(string to)
        {
            _seed.User(x => x.Email = "maria@gym.test");

            await Service().SendEmailAsync(to, "Hello", "Body");

            _gmail.Mails.Count.ShouldBe(1);
            _database.Read(db => db.Mails.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task A_mail_gmail_refuses_throws_is_logged_and_not_kept()
        {
            _seed.User(x => x.Email = "maria@gym.test");
            _gmail.Answer = HttpStatusCode.BadRequest;

            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => Service().SendEmailAsync("maria@gym.test", "Hello", "Body"));

            exception.Message.ShouldStartWith("Failed to send email: ");
            exception.Message.ShouldContain("Recipient address required");
            exception.InnerException.ShouldNotBeNull();
            _log.Lines.ShouldBe(["Error: Failed to send email to maria@gym.test (GoogleApiException)"]);
            _database.Read(db => db.Mails.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task A_mail_that_cannot_be_put_together_throws_like_one_that_is_refused()
        {
            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(() => Service().SendEmailAsync("maria@gym.test", null!, "Body"));

            exception.Message.ShouldStartWith("Failed to send email: ");
            _gmail.Mails.ShouldBeEmpty();
        }


        // ================================================================
        // Writing down to be sent later
        // ================================================================

        [Fact]
        public async Task A_queued_mail_is_one_pending_row_for_each_member()
        {
            User maria = _seed.User();
            User nikos = _seed.User();

            await Service().QueueEmailAsync([maria, nikos], "Hello", "<p>Body</p>");

            List<Mail> mails = _database.Read(db => db.Mails.OrderBy(x => x.Id).ToList());
            mails.Select(x => x.UserId).ShouldBe([maria.Id, nikos.Id]);
            mails.ShouldAllBe(x => x.Subject == "Hello" && x.Body == "<p>Body</p>" && x.Status == MailStatusEnum.PENDING && x.SentOn == null && x.Attachments == "" && x.Error == "");
            _gmail.Mails.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_mail_to_nobody_is_no_rows()
        {
            await Service().QueueEmailAsync([], "Hello", "Body");

            _database.Read(db => db.Mails.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task An_empty_list_of_files_is_kept_as_none()
        {
            await Service().QueueEmailAsync([_seed.User()], "Hello", "Body", []);

            _database.Read(db => db.Mails.Single()).Attachments.ShouldBe("");
        }

        [Fact]
        public async Task Files_are_kept_with_the_row()
        {
            await Service().QueueEmailAsync([_seed.User()], "Hello", "Body", [("first", "a.ics"), ("second", "b.ics")]);

            _database.Read(db => db.Mails.Single()).Attachments
                .ShouldBe("""[{"FileName":"a.ics","Content":"first"},{"FileName":"b.ics","Content":"second"}]""");
        }


        // ================================================================
        // Sending what was written down
        // ================================================================

        private Mail Queued(string attachments)
        {
            User member = _seed.User(x => x.Email = "maria@gym.test");
            Mail mail = _seed.Mail(member, x =>
            {
                x.Subject = "Hello";
                x.Body = "<p>Body</p>";
                x.Attachments = attachments;
            });
            mail.User = member;

            return mail;
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        [InlineData("null")]
        [InlineData("[]")]
        public async Task A_queued_mail_goes_to_its_member(string attachments)
        {
            await Service().SendQueuedMailAsync(Queued(attachments));

            MimeMessage mail = _gmail.Mails.Single();
            mail.To.Mailboxes.Single().Address.ShouldBe("maria@gym.test");
            mail.Subject.ShouldBe("Hello");
            mail.HtmlBody.ShouldBe("<p>Body</p>");
            mail.Attachments.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_queued_mail_goes_with_its_files()
        {
            GmailEmailService service = Service();
            User member = _seed.User();
            await service.QueueEmailAsync([member], "Hello", "Body", [("Καλό \"quoted\" \r\n line", "a.ics"), ("second", "b.ics")]);
            Mail mail = _database.Read(db => db.Mails.Single());
            mail.User = member;

            await service.SendQueuedMailAsync(mail);

            List<MimePart> files = _gmail.Mails.Single().Attachments.Cast<MimePart>().ToList();
            files.Select(x => x.FileName).ShouldBe(["a.ics", "b.ics"]);
            files.Select(TextOf).ShouldBe(["Καλό \"quoted\" \r\n line", "second"]);
        }

        // The row is there already; the queue fills in how it went.
        [Fact]
        public async Task A_queued_mail_is_not_written_down_again()
        {
            await Service().SendQueuedMailAsync(Queued(""));

            Mail mail = _database.Read(db => db.Mails.Single());
            mail.Status.ShouldBe(MailStatusEnum.PENDING);
            mail.SentOn.ShouldBeNull();
        }

        [Fact]
        public async Task Files_that_cannot_be_read_stop_the_mail()
        {
            await Should.ThrowAsync<System.Text.Json.JsonException>(() => Service().SendQueuedMailAsync(Queued("not json")));

            _gmail.Mails.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_queued_mail_gmail_refuses_throws()
        {
            _gmail.Answer = HttpStatusCode.BadRequest;

            await Should.ThrowAsync<InvalidOperationException>(() => Service().SendQueuedMailAsync(Queued("")));
        }


        // ================================================================
        // About a booking
        // ================================================================

        private static readonly DateTime Selected = new DateTime(2031, 3, 5);

        private static TrainGroupParticipant Booking(TrainGroupDate date, DateTime? selectedDate = null)
        {
            return new TrainGroupParticipant { TrainGroupDate = date, SelectedDate = selectedDate };
        }

        private static TrainGroupDate Weekly()
        {
            return new TrainGroupDate { RecurrenceDayOfWeek = DayOfWeek.Monday };
        }

        private static TrainGroupDate Monthly()
        {
            return new TrainGroupDate { RecurrenceDayOfMonth = 20 };
        }

        private static TrainGroupDate FixedDay()
        {
            return new TrainGroupDate { FixedDay = new DateTime(2031, 12, 24) };
        }

        private async Task<Mail> SendAsync(List<TrainGroupParticipant> added, List<TrainGroupParticipant> removed)
        {
            User member = _seed.User(x => { x.Id = new Guid("11111111-2222-3333-4444-555555555555"); x.Email = "maria@gym.test"; });

            await Service().SendBookingEmailAsync(member, added, removed);

            return _database.Read(db => db.Mails.Single());
        }

        // The dates a mail lists under one of its two headings.
        private static List<string> Listed(string body, string kind)
        {
            return body.Split($"<li class='{kind}'>").Skip(1).Select(x => x[..x.IndexOf("</li>", StringComparison.Ordinal)]).ToList();
        }

        [Fact]
        public async Task A_booking_mail_is_queued_for_the_member_and_nothing_is_sent_yet()
        {
            Mail mail = await SendAsync([Booking(Weekly())], []);

            mail.Subject.ShouldBe("[Booking_Confirmation]");
            mail.Status.ShouldBe(MailStatusEnum.PENDING);
            mail.UserId.ShouldBe(new Guid("11111111-2222-3333-4444-555555555555"));
            mail.Attachments.ShouldBe("");
            _gmail.Mails.ShouldBeEmpty();
        }

        [Fact]
        public async Task What_was_booked_and_what_was_given_up_are_listed()
        {
            _calendar.AddFiles = 2;
            _calendar.EndFiles = 1;
            _calendar.CancelFiles = 1;

            Mail mail = await SendAsync([Booking(Weekly()), Booking(Weekly(), Selected)], [Booking(FixedDay())]);

            ApprovedFile.ShouldMatch(mail.Body, "BookingMail");
            mail.Attachments.ShouldBe(
                """[{"FileName":"add-1.ics","Content":"content of add 1"},{"FileName":"add-2.ics","Content":"content of add 2"},{"FileName":"add-end-1.ics","Content":"content of end 1"},{"FileName":"cancel-1.ics","Content":"content of cancel 1"}]""");
            _calendar.Lines.ShouldBe(
            [
                "add 2 from gym@gym.test for maria@gym.test 11111111-2222-3333-4444-555555555555",
                "end 1 from gym@gym.test for maria@gym.test 11111111-2222-3333-4444-555555555555",
                "cancel 1 from gym@gym.test for maria@gym.test 11111111-2222-3333-4444-555555555555"
            ]);
        }

        [Fact]
        public async Task Nothing_booked_and_nothing_given_up_says_so()
        {
            Mail mail = await SendAsync([], []);

            ApprovedFile.ShouldMatch(mail.Body, "EmptyBookingMail");
        }

        public static TheoryData<string, bool, string> EveryDate
        {
            get
            {
                return new TheoryData<string, bool, string>
                {
                    { "weekly", false, "Monday" },
                    { "weekly", true, "2031-03-05" },
                    { "monthly", false, "[Every_0_of_the_month: 20]" },
                    { "monthly", true, "2031-03-05" },
                    // A fixed day is its own date, whatever was selected.
                    { "fixed", false, "2031-12-24" },
                    { "fixed", true, "2031-12-24" },
                };
            }
        }

        private static TrainGroupDate DateOf(string kind)
        {
            switch (kind)
            {
                case "weekly":
                    return Weekly();
                case "monthly":
                    return Monthly();
                default:
                    return FixedDay();
            }
        }

        [Theory]
        [MemberData(nameof(EveryDate))]
        public async Task A_date_reads_as_the_day_or_as_how_it_repeats(string kind, bool isOneOff, string expected)
        {
            TrainGroupParticipant booking = Booking(DateOf(kind), isOneOff ? Selected : null);

            Mail mail = await SendAsync([booking], [booking, booking]);

            Listed(mail.Body, "added").ShouldBe([expected]);
            Listed(mail.Body, "removed").ShouldBe([expected, expected]);
        }

        // A day with nothing on it shows whatever the one before it did.
        [Fact]
        public async Task A_booking_on_no_day_reads_like_the_one_before_it()
        {
            TrainGroupParticipant nothing = Booking(new TrainGroupDate());

            Mail mail = await SendAsync([nothing, Booking(Weekly()), nothing], []);

            Listed(mail.Body, "added").ShouldBe(["", "Monday", "Monday"]);
        }

        // How to use the files, only as far as there are any. A booking that was stopped from a later date
        // is added again, with its end.
        [Theory]
        [InlineData(0, 0, 0, false, false)]
        [InlineData(1, 0, 0, true, false)]
        [InlineData(0, 1, 0, true, false)]
        [InlineData(0, 0, 1, false, true)]
        [InlineData(0, 1, 1, true, true)]
        [InlineData(2, 2, 2, true, true)]
        public async Task The_calendar_is_explained_for_the_files_there_are(int addFiles, int endFiles, int cancelFiles, bool explainsAdding, bool explainsRemoving)
        {
            _calendar.AddFiles = addFiles;
            _calendar.EndFiles = endFiles;
            _calendar.CancelFiles = cancelFiles;

            Mail mail = await SendAsync([Booking(Weekly())], [Booking(Weekly())]);

            mail.Body.Contains("[Calendar_Integration]").ShouldBe(explainsAdding || explainsRemoving);
            mail.Body.Contains("[Add_to_Calendar]").ShouldBe(explainsAdding);
            mail.Body.Contains("[Remove_from_Calendar]").ShouldBe(explainsRemoving);
        }

        [Fact]
        public async Task Without_an_address_of_the_gyms_own_there_are_no_calendar_files()
        {
            _settings["Gmail:Email"] = null;
            _calendar.AddFiles = 1;

            Mail mail = await SendAsync([Booking(Weekly())], [Booking(Weekly())]);

            mail.Attachments.ShouldBe("");
            mail.Body.ShouldNotContain("[Calendar_Integration]");
            _calendar.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_calendar_that_hands_back_nothing_at_all_stops_the_mail()
        {
            _calendar.IsBroken = true;

            await Should.ThrowAsync<ArgumentNullException>(() => SendAsync([Booking(Weekly())], [Booking(Weekly())]));
        }

        // Only a missing address counts as none.
        [Fact]
        public async Task An_empty_address_of_the_gyms_own_is_one()
        {
            _settings["Gmail:Email"] = "";
            _calendar.AddFiles = 1;

            Mail mail = await SendAsync([Booking(Weekly())], []);

            mail.Attachments.ShouldContain("add-1.ics");
        }
    }
}
