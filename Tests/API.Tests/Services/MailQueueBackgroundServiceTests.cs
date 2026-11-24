using System.Diagnostics;
using System.Reflection;
using API.Services;
using API.Tests.Host;
using Business.Services.Email;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Tests.Shared;

namespace API.Tests.Services
{
    /// <summary>
    /// What sends the queued mails, pass after pass for as long as the app runs.
    /// </summary>
    public sealed class MailQueueBackgroundServiceTests : IAsyncLifetime
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(20);

        private readonly Passes _passes = new Passes();
        private readonly RecordingLogger<MailQueueBackgroundService> _log = new RecordingLogger<MailQueueBackgroundService>();
        private readonly MailQueueBackgroundService _service;

        public MailQueueBackgroundServiceTests()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton(_passes);
            services.AddScoped<IMailQueueService, FakeQueue>();

            _service = new MailQueueBackgroundService(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), _log);
        }

        public ValueTask InitializeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            await _service.StopAsync(CancellationToken.None);
            _service.Dispose();
        }

        // What each pass sends, and when it came.
        private sealed class Passes
        {
            private readonly SemaphoreSlim _came = new SemaphoreSlim(0);
            private readonly Stopwatch _clock = Stopwatch.StartNew();

            public Queue<object> Results { get; } = new Queue<object>();

            public List<TimeSpan> Times { get; } = new List<TimeSpan>();

            public List<FakeQueue> Queues { get; } = new List<FakeQueue>();

            public int Disposed { get; set; }

            public CancellationToken Token { get; private set; }

            public int Come(FakeQueue queue, CancellationToken token)
            {
                Times.Add(_clock.Elapsed);
                Queues.Add(queue);
                Token = token;

                object result = Results.Count == 0 ? 0 : Results.Dequeue();
                _came.Release();

                return result is Exception failure ? throw failure : (int)result;
            }

            public Task<bool> WaitAsync(TimeSpan? patience = null)
            {
                return _came.WaitAsync(patience ?? Patience, TestToken.Ct);
            }
        }

        private sealed class FakeQueue : IMailQueueService, IDisposable
        {
            private readonly Passes _passes;

            public FakeQueue(Passes passes)
            {
                _passes = passes;
            }

            public Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_passes.Come(this, cancellationToken));
            }

            public void Dispose()
            {
                _passes.Disposed++;
            }
        }

        private static TimeSpan Delay(string name)
        {
            return (TimeSpan)typeof(MailQueueBackgroundService).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        }


        [Fact]
        public void It_waits_fifteen_seconds_when_idle_and_two_when_busy()
        {
            Delay("IdleDelay").ShouldBe(TimeSpan.FromSeconds(15));
            Delay("BusyDelay").ShouldBe(TimeSpan.FromSeconds(2));
        }

        // Something was sent, so there may be more. Each pass has a connection to the database and to
        // Google of its own, and lets go of them.
        [Fact]
        public async Task After_a_pass_that_sent_something_the_next_comes_soon()
        {
            _passes.Results.Enqueue(1);
            await _service.StartAsync(TestToken.Ct);

            (await _passes.WaitAsync()).ShouldBeTrue();
            (await _passes.WaitAsync()).ShouldBeTrue();
            await _service.StopAsync(TestToken.Ct);

            (_passes.Times[1] - _passes.Times[0]).ShouldBeInRange(TimeSpan.FromSeconds(1.9), TimeSpan.FromSeconds(10));
            _passes.Queues.Distinct().Count().ShouldBe(2);
            _passes.Disposed.ShouldBe(2);
        }

        [Fact]
        public async Task After_one_that_sent_nothing_it_does_not()
        {
            await _service.StartAsync(TestToken.Ct);

            (await _passes.WaitAsync()).ShouldBeTrue();
            (await _passes.WaitAsync(TimeSpan.FromSeconds(3))).ShouldBeFalse();
        }

        // It would stop all mail, and nobody would know. The failure is written down and it waits.
        [Fact]
        public async Task A_pass_that_fails_does_not_end_it()
        {
            _passes.Results.Enqueue(new InvalidOperationException("No."));
            await _service.StartAsync(TestToken.Ct);

            (await _passes.WaitAsync()).ShouldBeTrue();
            await Task.Delay(200, TestToken.Ct);

            _service.ExecuteTask!.IsCompleted.ShouldBeFalse();
            _log.Lines.ShouldHaveSingleItem().ShouldContain("Mail queue pass failed");
            _passes.Disposed.ShouldBe(1);
            // As after one that sent nothing.
            (await _passes.WaitAsync(TimeSpan.FromSeconds(2.5))).ShouldBeFalse();
        }

        [Fact]
        public async Task It_ends_at_once_when_the_app_stops()
        {
            await _service.StartAsync(TestToken.Ct);
            await _passes.WaitAsync();
            await Task.Delay(100, TestToken.Ct);

            Stopwatch clock = Stopwatch.StartNew();
            await _service.StopAsync(TestToken.Ct);

            clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(5));
            _service.ExecuteTask!.IsCompletedSuccessfully.ShouldBeTrue();
            _passes.Token.IsCancellationRequested.ShouldBeTrue();
            _log.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task An_app_that_stops_before_it_starts_sends_nothing()
        {
            using CancellationTokenSource stopped = new CancellationTokenSource();
            stopped.Cancel();

            await _service.StartAsync(stopped.Token);
            await Task.Delay(100, TestToken.Ct);

            _passes.Times.ShouldBeEmpty();
        }


        // ================================================================
        // In the app
        // ================================================================

        // A mail that was waiting when it started is sent.
        [Fact]
        public async Task The_app_sends_what_is_waiting()
        {
            using TestDatabase database = new TestDatabase();
            Seed seed = new Seed(database);
            User user = seed.User();
            Mail waiting = seed.Mail(user);
            Mail sent = seed.Mail(user, x => x.Status = MailStatusEnum.SENT);
            SendingEmailService emails = new SendingEmailService();

            await using ApiHost host = await ApiHost.StartAsync(Settings.For(database), services =>
            {
                services.RemoveAll<ILoggerProvider>();
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(emails);
            });

            (await emails.Sent.Task.WaitAsync(Patience, TestToken.Ct)).ShouldBe(waiting.Id);

            // Written down after it is sent.
            for (int i = 0; i < 100 && database.Read(db => db.Mails.Single(x => x.Id == waiting.Id)).Status != MailStatusEnum.SENT; i++)
                await Task.Delay(50, TestToken.Ct);

            database.Read(db => db.Mails.Single(x => x.Id == waiting.Id)).Status.ShouldBe(MailStatusEnum.SENT);
            emails.Count.ShouldBe(1);
            sent.Id.ShouldNotBe(waiting.Id);
        }

        private sealed class SendingEmailService : IEmailService
        {
            public TaskCompletionSource<int> Sent { get; } = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

            public int Count { get; private set; }

            public Task SendQueuedMailAsync(Mail mail)
            {
                Count++;
                Sent.TrySetResult(mail.Id);

                return Task.CompletedTask;
            }

            public Task SendEmailAsync(string to, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
            {
                throw new NotSupportedException();
            }

            public Task QueueEmailAsync(List<User> users, string subject, string htmlBody, List<(string Content, string FileName)>? attachments = null)
            {
                throw new NotSupportedException();
            }

            public Task SendBookingEmailAsync(User user, List<TrainGroupParticipant> emailDatesAdd, List<TrainGroupParticipant> emailDatesRemove)
            {
                throw new NotSupportedException();
            }
        }
    }
}
