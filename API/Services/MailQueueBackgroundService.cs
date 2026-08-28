using Business.Services.Email;

namespace API.Services
{
    // Works through the mail that was written down as pending. Nothing sends on a
    // request thread any more, so a send to everyone cannot time the browser out.
    public class MailQueueBackgroundService : BackgroundService
    {
        // Waited when the queue was empty.
        private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(15);

        // Waited when the last pass sent something, so a long queue keeps moving
        // without hammering Google.
        private static readonly TimeSpan BusyDelay = TimeSpan.FromSeconds(2);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MailQueueBackgroundService> _logger;

        public MailQueueBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MailQueueBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                int sentCount = 0;

                try
                {
                    // A scope per pass. IDataService and IEmailService are scoped and this
                    // is a singleton, so they cannot be taken in the constructor. It also
                    // gives each batch one Gmail client rather than one per mail.
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IMailQueueService queueService = scope.ServiceProvider.GetRequiredService<IMailQueueService>();

                    sentCount = await queueService.ProcessPendingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    // A bad pass must never kill the loop - that would stop all mail quietly.
                    _logger.LogError(ex, "Mail queue pass failed");
                }

                try
                {
                    await Task.Delay(sentCount > 0 ? BusyDelay : IdleDelay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Shutting down.
                    break;
                }
            }
        }
    }
}
