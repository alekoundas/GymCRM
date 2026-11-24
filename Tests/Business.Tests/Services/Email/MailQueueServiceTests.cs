using Business.Services.Email;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace Business.Tests.Services.Email
{
    /// <summary>
    /// One pass over the mails still to be sent. Nothing may be sent twice, and one that fails may
    /// not hold up the rest.
    /// </summary>
    public sealed class MailQueueServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly FakeEmailService _email = new FakeEmailService();
        private readonly RecordingLogger<MailQueueService> _log = new RecordingLogger<MailQueueService>();
        private readonly MailQueueService _service;
        private readonly User _member;

        public MailQueueServiceTests()
        {
            _seed = new Seed(_database);
            _service = new MailQueueService(DataServices.On(_database), _email, _log);
            _member = _seed.User(x => x.Email = "maria@gym.test");
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private Mail Pending(string subject, User? user = null)
        {
            return _seed.Mail(user ?? _member, x => x.Subject = subject);
        }

        private Task<int> RunAsync()
        {
            return _service.ProcessPendingAsync(CancellationToken.None);
        }

        private List<string> Mails()
        {
            return _database.Read(db => db.Mails.OrderBy(x => x.Id).Select(x => $"{x.Subject} {x.Status} [{x.Error}]").ToList());
        }

        [Fact]
        public async Task With_nothing_to_send_nothing_is_sent()
        {
            (await RunAsync()).ShouldBe(0);

            _email.Sent.ShouldBeEmpty();
        }

        [Fact]
        public async Task Every_pending_mail_is_sent_the_oldest_first()
        {
            Pending("one");
            Pending("two", _seed.User(x => x.Email = "nikos@gym.test"));
            Pending("three");

            (await RunAsync()).ShouldBe(3);

            _email.Sent.ShouldBe(["one to maria@gym.test", "two to nikos@gym.test", "three to maria@gym.test"]);
            Mails().ShouldBe(["one SENT []", "two SENT []", "three SENT []"]);
            _log.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task A_sent_mail_says_when_and_keeps_everything_else()
        {
            Mail mail = _seed.Mail(_member, x =>
            {
                x.Subject = "one";
                x.Body = "<p>Hello</p>";
                x.Attachments = "[{}]";
                x.Error = "from before";
                x.CreatedBy_Id = "somebody";
                x.CreatedOn = new DateTime(2031, 3, 5);
            });
            DateTime before = DateTime.UtcNow;

            await RunAsync();

            Mail sent = _database.Read(db => db.Mails.Single());
            sent.Id.ShouldBe(mail.Id);
            sent.SentOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            sent.Error.ShouldBe("");
            sent.Body.ShouldBe("<p>Hello</p>");
            sent.Attachments.ShouldBe("[{}]");
            sent.CreatedBy_Id.ShouldBe("somebody");
            sent.CreatedOn.ShouldBe(new DateTime(2031, 3, 5));
            sent.UserId.ShouldBe(_member.Id);
        }

        [Fact]
        public async Task A_second_pass_sends_nothing_again()
        {
            Pending("one");

            (await RunAsync()).ShouldBe(1);
            (await RunAsync()).ShouldBe(0);

            _email.Sent.ShouldBe(["one to maria@gym.test"]);
        }

        [Theory]
        [InlineData(MailStatusEnum.SENT)]
        [InlineData(MailStatusEnum.FAILED)]
        public async Task A_mail_that_is_not_pending_is_left_alone(MailStatusEnum status)
        {
            _seed.Mail(_member, x => { x.Status = status; x.Error = "kept"; });

            (await RunAsync()).ShouldBe(0);

            Mail mail = _database.Read(db => db.Mails.Single());
            mail.Status.ShouldBe(status);
            mail.Error.ShouldBe("kept");
            mail.SentOn.ShouldBeNull();
        }

        // Twenty at a time, so a restart half way through loses nothing.
        [Theory]
        [InlineData(19, 19, 0)]
        [InlineData(20, 20, 0)]
        [InlineData(21, 20, 1)]
        [InlineData(45, 20, 20)]
        public async Task One_pass_takes_twenty(int waiting, int first, int second)
        {
            for (int i = 1; i <= waiting; i++)
                Pending($"mail {i}");

            MailQueueService.BatchSize.ShouldBe(20);
            (await RunAsync()).ShouldBe(first);
            _email.Sent.Last().ShouldBe($"mail {first} to maria@gym.test");
            (await RunAsync()).ShouldBe(second);
        }


        // What goes wrong.

        [Fact]
        public async Task A_mail_that_fails_says_why_and_the_rest_still_go()
        {
            Pending("one");
            Mail two = Pending("two");
            Pending("three");
            _email.OnSend = mail =>
            {
                if (mail.Subject == "two")
                    throw new InvalidOperationException("No such address.");
            };

            (await RunAsync()).ShouldBe(2);

            _email.Sent.ShouldBe(["one to maria@gym.test", "three to maria@gym.test"]);
            Mails().ShouldBe(["one SENT []", "two FAILED [No such address.]", "three SENT []"]);
            _log.Lines.ShouldBe([$"Error: Failed to send queued mail {two.Id} (InvalidOperationException)"]);
            _database.Read(db => db.Mails.Single(x => x.Subject == "two")).SentOn.ShouldBeNull();
        }

        [Fact]
        public async Task A_mail_that_failed_is_not_tried_again()
        {
            Pending("one");
            _email.OnSend = mail => throw new InvalidOperationException("No.");
            await RunAsync();

            _email.OnSend = mail => { };
            (await RunAsync()).ShouldBe(0);

            Mails().ShouldBe(["one FAILED [No.]"]);
        }

        [Fact]
        public async Task When_every_mail_fails_none_went()
        {
            Pending("one");
            Pending("two");
            _email.OnSend = mail => throw new InvalidOperationException("No.");

            (await RunAsync()).ShouldBe(0);

            Mails().ShouldBe(["one FAILED [No.]", "two FAILED [No.]"]);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public async Task A_member_with_no_address_is_not_sent_to(string email)
        {
            Mail mail = Pending("one", _seed.User(x => x.Email = email));

            (await RunAsync()).ShouldBe(0);

            _email.Sent.ShouldBeEmpty();
            Mails().ShouldBe(["one FAILED [The recipient has no email address.]"]);
            _log.Lines.ShouldBe([$"Error: Failed to send queued mail {mail.Id} (InvalidOperationException)"]);
        }


        // Shutting down.

        [Fact]
        public async Task Told_to_stop_before_it_starts_it_sends_nothing()
        {
            Pending("one");

            (await _service.ProcessPendingAsync(new CancellationToken(canceled: true))).ShouldBe(0);

            _email.Sent.ShouldBeEmpty();
            Mails().ShouldBe(["one PENDING []"]);
        }

        // The one under way is finished and written down; the rest wait for the next start.
        [Fact]
        public async Task Told_to_stop_half_way_it_finishes_the_mail_it_is_on()
        {
            using CancellationTokenSource stop = new CancellationTokenSource();
            Pending("one");
            Pending("two");
            Pending("three");
            _email.OnSend = mail =>
            {
                if (mail.Subject == "two")
                    stop.Cancel();
            };

            (await _service.ProcessPendingAsync(stop.Token)).ShouldBe(2);

            Mails().ShouldBe(["one SENT []", "two SENT []", "three PENDING []"]);
        }

        // Written one at a time: what was sent before a crash is not sent again after it.
        [Fact]
        public async Task Each_mail_is_written_down_before_the_next_one_goes()
        {
            Pending("one");
            Pending("two");
            List<string> seen = new List<string>();
            _email.OnSend = mail => seen.AddRange(Mails());

            await RunAsync();

            seen.ShouldBe(["one PENDING []", "two PENDING []", "one SENT []", "two PENDING []"]);
        }
    }
}
