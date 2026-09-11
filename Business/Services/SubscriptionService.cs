using Core.Enums;
using Core.Models;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Business.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IDataService _dataService;

        public SubscriptionService(IDataService dataService)
        {
            _dataService = dataService;
        }

        public async Task<int> GetBalanceAsync(Guid userId)
        {
            Dictionary<Guid, int> balances = await GetBalancesAsync(new List<Guid> { userId });
            return balances.TryGetValue(userId, out int balance) ? balance : 0;
        }

        // Anybody who has either attended something or been granted lessons. Somebody
        // with neither is not a member in any useful sense and would only pad the
        // charts with zeros.
        public async Task<Dictionary<Guid, int>> GetAllBalancesAsync()
        {
            using ApiDbContext context = _dataService.GetDbContext();

            List<KeyValuePair<Guid, int>> granted = await context.Subscriptions
                .AsNoTracking()
                .Where(x => x.Status == SubscriptionStatusEnum.APPROVED)
                .GroupBy(x => x.UserId)
                .Select(x => new KeyValuePair<Guid, int>(x.Key, x.Sum(y => y.Amount)))
                .ToListAsync();

            List<KeyValuePair<Guid, int>> used = await context.TrainGroupΑttendances
                .AsNoTracking()
                .GroupBy(x => x.UserId)
                .Select(x => new KeyValuePair<Guid, int>(x.Key, x.Count()))
                .ToListAsync();

            Dictionary<Guid, int> balances = new Dictionary<Guid, int>();

            foreach (KeyValuePair<Guid, int> row in granted)
                balances[row.Key] = balances.GetValueOrDefault(row.Key) + row.Value;

            foreach (KeyValuePair<Guid, int> row in used)
                balances[row.Key] = balances.GetValueOrDefault(row.Key) - row.Value;

            return balances;
        }

        // Worked out from the rows every time rather than kept in a column. A stored
        // counter would have to be corrected by every path that touches attendance or
        // a subscription, and the one that gets forgotten is silent; this cannot drift.
        public async Task<Dictionary<Guid, int>> GetBalancesAsync(List<Guid> userIds)
        {
            Dictionary<Guid, int> balances = userIds.Distinct().ToDictionary(x => x, x => 0);

            if (balances.Count == 0)
                return balances;

            using ApiDbContext context = _dataService.GetDbContext();

            List<Guid> ids = balances.Keys.ToList();

            List<KeyValuePair<Guid, int>> granted = await context.Subscriptions
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserId) && x.Status == SubscriptionStatusEnum.APPROVED)
                .GroupBy(x => x.UserId)
                .Select(x => new KeyValuePair<Guid, int>(x.Key, x.Sum(y => y.Amount)))
                .ToListAsync();

            List<KeyValuePair<Guid, int>> used = await context.TrainGroupΑttendances
                .AsNoTracking()
                .Where(x => ids.Contains(x.UserId))
                .GroupBy(x => x.UserId)
                .Select(x => new KeyValuePair<Guid, int>(x.Key, x.Count()))
                .ToListAsync();

            foreach (KeyValuePair<Guid, int> row in granted)
                balances[row.Key] += row.Value;

            foreach (KeyValuePair<Guid, int> row in used)
                balances[row.Key] -= row.Value;

            return balances;
        }
    }
}
