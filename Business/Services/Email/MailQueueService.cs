using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Logging;

namespace Business.Services.Email
{
    public class MailQueueService : IMailQueueService
    {
        // How many mails one pass takes. Small enough that a restart mid-batch loses
        // nothing and a long queue does not hold one database context open for minutes.
        public const int BatchSize = 20;

        private readonly IDataService _dataService;
        private readonly IEmailService _emailService;
        private readonly ILogger<MailQueueService> _logger;

        public MailQueueService(
            IDataService dataService,
            IEmailService emailService,
            ILogger<MailQueueService> logger)
        {
            _dataService = dataService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
        {
            List<Mail> mails = await _dataService.Mails
                .Include(x => x.User)
                .Where(x => x.Status == MailStatusEnum.PENDING)
                .OrderBy(x => x.Id)
                .AddPagging(0, BatchSize)
                .ToListAsync();

            int sentCount = 0;

            foreach (Mail mail in mails)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (mail.User == null || string.IsNullOrWhiteSpace(mail.User.Email))
                        throw new InvalidOperationException("The recipient has no email address.");

                    await _emailService.SendQueuedMailAsync(mail);

                    mail.Status = MailStatusEnum.SENT;
                    mail.SentOn = DateTime.UtcNow;
                    mail.Error = string.Empty;
                    sentCount++;
                }
                catch (Exception ex)
                {
                    // One bad address stops that one mail, never the rest of the batch.
                    mail.Status = MailStatusEnum.FAILED;
                    mail.Error = ex.Message;

                    _logger.LogError(ex, "Failed to send queued mail {MailId}", mail.Id);
                }

                // Written one at a time on purpose: a crash halfway through must not
                // leave sent mail looking pending and send it twice.
                await _dataService.UpdateAsync(mail);
            }

            return sentCount;
        }
    }
}
