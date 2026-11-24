using API.Tests.Host;
using Core.Dtos.Chart;
using Core.Dtos.Subscription;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The numbers on the administrator's home page.
    /// </summary>
    public sealed class ChartsControllerTests : ApiTest
    {
        private readonly DateTime _today = Midnight.Today();
        private readonly User _user;

        public ChartsControllerTests(ApiFixture api) : base(api)
        {
            _user = Seed.User(x => x.CreatedOn = new DateTime(2031, 3, 5));
        }

        private async Task<ChartDataDto> ChartsAsync(params string[] permissions)
        {
            ApiResult<ChartDataDto> result = await GetAsync<ChartDataDto>(Caller.Of(_user, permissions), "api/Charts");

            result.ShouldSucceed();

            return result.Value;
        }

        private async Task<SubscriptionChartsDto> SubscriptionsAsync()
        {
            return (await ChartsAsync("SubscriptionsAdmin_View")).Subscriptions.ShouldNotBeNull();
        }


        // ================================================================
        // Mails
        // ================================================================

        // Seven days ending with today, each with what was written on it, whatever its state.
        [Fact]
        public async Task The_mails_of_the_last_seven_days_are_counted_by_day()
        {
            foreach (int daysAgo in new[] { 0, 0, 1, 6, 6, 6, 7, 30 })
                Seed.Mail(_user, x => x.CreatedOn = _today.AddDays(-daysAgo).AddMinutes(1));
            Seed.Mail(_user, x => { x.CreatedOn = _today.AddDays(-3).AddHours(23); x.Status = MailStatusEnum.FAILED; });

            List<DailyEmailCountDto> days = (await ChartsAsync()).DailyEmails;

            days.Select(x => x.Date).ShouldBe(Enumerable.Range(0, 7).Select(x => _today.AddDays(x - 6)));
            days.Select(x => x.Count).ShouldBe([3, 0, 0, 1, 0, 1, 2]);
        }

        [Fact]
        public async Task A_gym_that_sent_nothing_has_seven_empty_days()
        {
            (await ChartsAsync()).DailyEmails.Select(x => x.Count).ShouldBe([0, 0, 0, 0, 0, 0, 0]);
        }

        // Google allows five hundred a day; what went in the last twenty-four hours is taken off.
        [Theory]
        [InlineData(0, 500)]
        [InlineData(1, 499)]
        [InlineData(499, 1)]
        [InlineData(500, 0)]
        [InlineData(501, 0)]
        public async Task What_is_left_of_the_days_mails_is_never_less_than_none(int sent, int left)
        {
            Database.Change(db => db.Mails.AddRange(Enumerable.Range(0, sent).Select(x => new Mail { UserId = _user.Id, CreatedOn = DateTime.UtcNow.AddHours(-1) })));

            (await ChartsAsync()).AvailableEmails.ShouldBe(left);
        }

        [Fact]
        public async Task A_mail_from_before_the_last_twenty_four_hours_no_longer_counts()
        {
            Seed.Mail(_user, x => x.CreatedOn = DateTime.UtcNow.AddHours(-24).AddMinutes(1));
            Seed.Mail(_user, x => x.CreatedOn = DateTime.UtcNow.AddHours(-24).AddMinutes(-1));

            (await ChartsAsync()).AvailableEmails.ShouldBe(499);
        }


        // ================================================================
        // Members
        // ================================================================

        [Fact]
        public async Task The_members_are_added_up_month_by_month()
        {
            foreach (string joined in new[] { "2031-05-31", "2031-03-31", "2031-05-01", "2030-03-10", "2032-01-01" })
                Seed.User(x => x.CreatedOn = DateTime.Parse(joined));

            List<UserGrowthDto> months = (await ChartsAsync()).UserGrowth;

            // A month nobody joined in is not there; the same month of another year is another.
            months.Select(x => $"{x.Date:yyyy-MM-dd} {x.Cumulative}").ShouldBe(["2030-03-01 1", "2031-03-01 3", "2031-05-01 5", "2032-01-01 6"]);
        }


        // ================================================================
        // Subscriptions
        // ================================================================

        [Fact]
        public async Task Somebody_without_the_permission_gets_everything_but_the_subscriptions()
        {
            (await ChartsAsync()).Subscriptions.ShouldBeNull();
            (await ChartsAsync("Subscriptions_View", "SubscriptionsAdmin_Edit")).Subscriptions.ShouldBeNull();
        }

        // Twelve months ending with this one, empty ones and all.
        [Fact]
        public async Task The_lessons_approved_are_added_up_for_twelve_months()
        {
            DateTime month = new DateTime(_today.Year, _today.Month, 1);

            void Approve(int amount, DateTime? on, SubscriptionStatusEnum status = SubscriptionStatusEnum.APPROVED)
            {
                Seed.Subscription(_user, amount, status, x => x.DecidedOn = on);
            }

            Approve(1, month);
            Approve(2, month.AddDays(5));
            Approve(4, month.AddMonths(1).AddTicks(-1));
            Approve(8, month.AddTicks(-1));
            Approve(16, month.AddMonths(-11));
            Approve(-3, month.AddMonths(-5));
            // Too old, too new, never decided, or not approved.
            Approve(100, month.AddMonths(-11).AddTicks(-1));
            Approve(100, month.AddMonths(1));
            Approve(100, null);
            Approve(100, month, SubscriptionStatusEnum.REJECTED);
            Approve(100, month, SubscriptionStatusEnum.PENDING);

            List<SubscriptionMonthDto> months = (await SubscriptionsAsync()).MonthlyApproved;

            months.Select(x => new DateTime(x.Year, x.Month, 1)).ShouldBe(Enumerable.Range(-11, 12).Select(x => month.AddMonths(x)));
            months.Select(x => x.Amount).ShouldBe([16, 0, 0, 0, 0, 0, -3, 0, 0, 0, 8, 7]);
        }

        [Fact]
        public async Task A_gym_with_no_subscriptions_has_empty_panels()
        {
            SubscriptionChartsDto charts = await SubscriptionsAsync();

            charts.MonthlyApproved.Sum(x => x.Amount).ShouldBe(0);
            charts.Buckets.Select(x => $"{x.Key} {x.Count}").ShouldBe(["ONE_OR_LESS 0", "TWO_TO_FIVE 0", "SIX_TO_TEN 0", "ELEVEN_PLUS 0"]);
            charts.TopDebtors.ShouldBeEmpty();
            charts.OldestPendingRequests.ShouldBeEmpty();
        }

        // A member for every balance around every edge.
        [Fact]
        public async Task The_members_are_sorted_into_four_by_what_they_have_left()
        {
            foreach (int balance in new[] { -5, 0, 1, 2, 3, 5, 6, 7, 10, 11, 12, 100 })
                Seed.Subscription(Seed.User(), balance);

            (await SubscriptionsAsync()).Buckets.Select(x => $"{x.Key} {x.Count}").ShouldBe(["ONE_OR_LESS 3", "TWO_TO_FIVE 3", "SIX_TO_TEN 3", "ELEVEN_PLUS 3"]);
        }

        [Fact]
        public async Task The_five_who_owe_most_are_listed_the_worst_first()
        {
            for (int owed = 1; owed <= 7; owed++)
            {
                int number = owed;
                Seed.Subscription(Seed.User(x => { x.FirstName = "Member"; x.LastName = number.ToString(); }), -owed);
            }
            Seed.Subscription(Seed.User(), 0);
            Seed.Subscription(Seed.User(), 5);

            (await SubscriptionsAsync()).TopDebtors.Select(x => $"{x.FullName} {x.Balance}")
                .ShouldBe(["Member 7 -7", "Member 6 -6", "Member 5 -5", "Member 4 -4", "Member 3 -3"]);
        }

        [Theory]
        [InlineData("Maria", "", "Maria")]
        [InlineData("", "Papa", "Papa")]
        [InlineData("", "", "")]
        [InlineData("Maria", "Papa", "Maria Papa")]
        public async Task A_name_has_no_spare_space(string firstName, string lastName, string expected)
        {
            User member = Seed.User(x => { x.FirstName = firstName; x.LastName = lastName; });
            Seed.Subscription(member, -1);
            Seed.Subscription(member, 0, SubscriptionStatusEnum.PENDING);

            SubscriptionChartsDto charts = await SubscriptionsAsync();

            charts.TopDebtors.Single().FullName.ShouldBe(expected);
            charts.TopDebtors.Single().UserId.ShouldBe(member.Id.ToString());
            charts.OldestPendingRequests.Single().FullName.ShouldBe(expected);
            charts.OldestPendingRequests.Single().UserId.ShouldBe(member.Id.ToString());
        }

        [Fact]
        public async Task The_five_requests_waiting_longest_are_listed_the_oldest_first()
        {
            foreach (int days in new[] { 3, 7, 1, 5, 2, 6 })
                Seed.Subscription(Seed.User(), 0, SubscriptionStatusEnum.PENDING, x => { x.RequestedAmount = days * 10; x.CreatedOn = DateTime.UtcNow.AddDays(-days).AddMinutes(-1); });
            Seed.Subscription(_user, 0, SubscriptionStatusEnum.REJECTED, x => x.CreatedOn = DateTime.UtcNow.AddDays(-100));
            Seed.Subscription(_user, 0, SubscriptionStatusEnum.APPROVED, x => x.CreatedOn = DateTime.UtcNow.AddDays(-100));

            List<SubscriptionPendingRequestDto> requests = (await SubscriptionsAsync()).OldestPendingRequests;

            requests.Select(x => $"{x.WaitingDays} {x.RequestedAmount}").ShouldBe(["7 70", "6 60", "5 50", "3 30", "2 20"]);
            requests.ShouldAllBe(x => x.Id > 0);
        }

        // Asked a moment ago, or by a clock that is ahead, it has waited no days; asked for no
        // number, it reads as none.
        [Fact]
        public async Task A_request_never_waits_less_than_no_days()
        {
            Seed.Subscription(_user, 0, SubscriptionStatusEnum.PENDING, x => { x.RequestedAmount = null; x.CreatedOn = DateTime.UtcNow.AddDays(3); });

            SubscriptionPendingRequestDto request = (await SubscriptionsAsync()).OldestPendingRequests.Single();

            request.WaitingDays.ShouldBe(0);
            request.RequestedAmount.ShouldBe(0);
        }
    }
}
