using Core.Models;

namespace Business.Services
{
    public interface ISubscriptionService
    {
        // Remaining lessons for one member.
        Task<int> GetBalanceAsync(Guid userId);

        // The same for a page of members, in one pass rather than one query each.
        Task<Dictionary<Guid, int>> GetBalancesAsync(List<Guid> userIds);
    }
}
