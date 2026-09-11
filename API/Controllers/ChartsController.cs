using AutoMapper;
using Business.Services;
using Business.Services.Email;
using Core.Dtos;
using Core.Dtos.Chart;
using Core.Dtos.Subscription;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    // Derives from ControllerBase only so the subscription panels below can read the
    // caller's claims; the rest of the page never needed it.
    public class ChartsController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IMapper _mapper;
        private readonly IEmailService _emailService;
        private readonly ISubscriptionService _subscriptionService;
        //private readonly ILogger<TrainGroupDateController> _logger;

        public ChartsController(
            IDataService dataService,
            IMapper mapper,
            IEmailService emailService,
            ISubscriptionService subscriptionService)
        {
            _dataService = dataService;
            _mapper = mapper;
            _emailService = emailService;
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<ChartDataDto>>> GetChartData()
        {

            DateTime now = DateTime.UtcNow;
            DateTime start = now.AddDays(-6).Date;

            List<Mail> recentMails = await _dataService.Mails
                .Where(m => m.CreatedOn >= start)
                .ToListAsync();

            List<DailyEmailCountDto> dailyEmails = Enumerable.Range(0, 7).Select(i =>
            {
                var day = start.AddDays(i);
                var count = recentMails.Count(m => m.CreatedOn.Date == day.Date);
                return new DailyEmailCountDto { Date = day, Count = count };
            }).ToList();

            int usedLast24H = await _dataService.Mails.Where(m => m.CreatedOn >= now.AddHours(-24)).CountAsync();
            int availableEmails = 500 - usedLast24H;

            //ApiDbContext dbContext = _dataService.GetDbContext();
            var users = await _dataService.Users.OrderBy(x => x.CreatedOn).ToListAsync();

            var groupedUsers = users.GroupBy(u => new { u.CreatedOn.Year, u.CreatedOn.Month });
            List<UserGrowthDto> userGrowth = new List<UserGrowthDto>();
            int cumulative = 0;

            foreach (var group in groupedUsers)
            {
                cumulative += group.Count();
                DateTime date = new DateTime(group.Key.Year, group.Key.Month, 1);
                userGrowth.Add(new UserGrowthDto { Date = date, Cumulative = cumulative });
            }

            ChartDataDto chartData = new ChartDataDto
            {
                DailyEmails = dailyEmails,
                AvailableEmails = Math.Max(availableEmails, 0), // Prevent negative
                UserGrowth = userGrowth,
                Subscriptions = await GetSubscriptionChartsAsync()
            };

            return new ApiResponse<ChartDataDto>().SetSuccessResponse(chartData);
        }


        // The subscription panels, for whoever is allowed to see them. A member who
        // reaches this page gets the rest of the chart data and nothing here.
        private async Task<SubscriptionChartsDto?> GetSubscriptionChartsAsync()
        {
            if (!User.HasClaim("Permission", "SubscriptionsAdmin_View"))
                return null;

            SubscriptionChartsDto charts = new SubscriptionChartsDto();

            using ApiDbContext context = _dataService.GetDbContext();


            // Twelve months back, counted from the first of the current one, so a month
            // with nothing in it still shows as a gap rather than being skipped.
            DateTime firstOfThisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            DateTime from = firstOfThisMonth.AddMonths(-11);

            List<Subscription> approved = await context.Subscriptions
                .Where(x => x.Status == SubscriptionStatusEnum.APPROVED
                         && x.DecidedOn != null
                         && x.DecidedOn >= from)
                .ToListAsync();

            for (int i = 0; i < 12; i++)
            {
                DateTime month = from.AddMonths(i);
                DateTime next = month.AddMonths(1);

                charts.MonthlyApproved.Add(new SubscriptionMonthDto
                {
                    Year = month.Year,
                    Month = month.Month,
                    Amount = approved
                        .Where(x => x.DecidedOn >= month && x.DecidedOn < next)
                        .Sum(x => x.Amount)
                });
            }

            Dictionary<Guid, int> balances = await _subscriptionService.GetAllBalancesAsync();

            charts.Buckets = new List<SubscriptionBucketDto>
            {
                new SubscriptionBucketDto { Key = "ONE_OR_LESS", Count = balances.Count(x => x.Value <= 1) },
                new SubscriptionBucketDto { Key = "TWO_TO_FIVE", Count = balances.Count(x => x.Value >= 2 && x.Value <= 5) },
                new SubscriptionBucketDto { Key = "SIX_TO_TEN", Count = balances.Count(x => x.Value >= 6 && x.Value <= 10) },
                new SubscriptionBucketDto { Key = "ELEVEN_PLUS", Count = balances.Count(x => x.Value >= 11) }
            };

            List<Guid> debtorIds = balances
                .Where(x => x.Value < 0)
                .OrderBy(x => x.Value)
                .Take(5)
                .Select(x => x.Key)
                .ToList();

            if (debtorIds.Count > 0)
            {
                List<User> debtors = await context.Users
                    .Where(x => debtorIds.Contains(x.Id))
                    .ToListAsync();

                charts.TopDebtors = debtorIds
                    .Select(id => new { Id = id, User = debtors.FirstOrDefault(x => x.Id == id) })
                    .Where(x => x.User != null)
                    .Select(x => new SubscriptionDebtorDto
                    {
                        UserId = x.Id.ToString(),
                        FullName = (x.User!.FirstName + " " + x.User!.LastName).Trim(),
                        Balance = balances[x.Id]
                    })
                    .ToList();
            }


            return charts;
        }
    }
}
