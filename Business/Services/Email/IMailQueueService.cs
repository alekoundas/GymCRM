namespace Business.Services.Email
{
    public interface IMailQueueService
    {
        // Works through one batch of pending mail and returns how many went out.
        Task<int> ProcessPendingAsync(CancellationToken cancellationToken);
    }
}
