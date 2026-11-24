using Business.Services;
using Business.Tests.Fakes;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace Business.Tests.Services
{
    /// <summary>
    /// A balance is what was approved less what was attended, counted from the rows every time.
    /// </summary>
    public sealed class SubscriptionServiceTests : IDisposable
    {
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private static readonly int?[] Amounts = [null, -3, 0, 1, 10];
        private static readonly int[] Attendances = [0, 1, 3];

        public static readonly MatrixTheoryData<int?, int?, int> EveryBalance = new MatrixTheoryData<int?, int?, int>(Amounts, Amounts, Attendances);

        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly SubscriptionService _service;
        private readonly User _member;

        public SubscriptionServiceTests()
        {
            _seed = new Seed(_database);
            _service = new SubscriptionService(DataServices.On(_database));
            _member = _seed.User();
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private void Attend(User user, int times)
        {
            for (int i = 0; i < times; i++)
                _seed.Attendance(user, SomeDay.AddDays(i));
        }

        // Two entries or fewer, of every size, against every number of attendances; and beside them
        // an entry still waiting and one turned down, which never count.
        [Theory]
        [MemberData(nameof(EveryBalance))]
        public async Task A_balance_is_what_was_approved_less_what_was_attended(int? first, int? second, int attended)
        {
            if (first != null)
                _seed.Subscription(_member, first.Value);
            if (second != null)
                _seed.Subscription(_member, second.Value);

            _seed.Subscription(_member, 100, SubscriptionStatusEnum.PENDING);
            _seed.Subscription(_member, 200, SubscriptionStatusEnum.REJECTED);
            Attend(_member, attended);

            int expected = (first ?? 0) + (second ?? 0) - attended;

            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(expected);
            (await _service.GetBalancesAsync([_member.Id])).ShouldBe(new Dictionary<Guid, int> { [_member.Id] = expected });

            // Everyone's balances are those of whoever has an approved entry or an attendance.
            bool counts = first != null || second != null || attended > 0;
            Dictionary<Guid, int> all = counts ? new Dictionary<Guid, int> { [_member.Id] = expected } : [];
            (await _service.GetAllBalancesAsync()).ShouldBe(all);
        }

        [Fact]
        public async Task Somebody_who_is_not_there_has_nothing()
        {
            Guid nobody = Guid.NewGuid();

            (await _service.GetBalanceAsync(nobody)).ShouldBe(0);
            (await _service.GetBalancesAsync([nobody])).ShouldBe(new Dictionary<Guid, int> { [nobody] = 0 });
        }

        [Fact]
        public async Task One_members_balance_takes_nothing_from_another()
        {
            User other = _seed.User();
            _seed.Subscription(_member, 5);
            _seed.Subscription(other, 7);
            Attend(other, 2);

            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(5);
            (await _service.GetBalanceAsync(other.Id)).ShouldBe(5);
            (await _service.GetBalancesAsync([_member.Id])).ShouldHaveSingleItem();
        }

        [Fact]
        public async Task A_page_of_members_gets_a_balance_each_whether_they_have_rows_or_not()
        {
            User owing = _seed.User();
            User newcomer = _seed.User();
            _seed.Subscription(_member, 5);
            Attend(owing, 2);

            Dictionary<Guid, int> balances = await _service.GetBalancesAsync([_member.Id, owing.Id, newcomer.Id]);

            balances.ShouldBe(new Dictionary<Guid, int> { [_member.Id] = 5, [owing.Id] = -2, [newcomer.Id] = 0 }, ignoreOrder: true);
        }

        [Fact]
        public async Task The_same_member_twice_is_one_balance()
        {
            _seed.Subscription(_member, 5);

            (await _service.GetBalancesAsync([_member.Id, _member.Id])).ShouldBe(new Dictionary<Guid, int> { [_member.Id] = 5 });
        }

        [Fact]
        public async Task Nobody_asked_about_is_no_balances()
        {
            _seed.Subscription(_member, 5);

            (await _service.GetBalancesAsync([])).ShouldBeEmpty();
        }

        [Fact]
        public async Task Everyones_balances_are_those_who_were_granted_or_attended_something()
        {
            User owing = _seed.User();
            User even = _seed.User();
            User waiting = _seed.User();
            User refused = _seed.User();
            _seed.User();

            _seed.Subscription(_member, 5);
            Attend(owing, 2);
            _seed.Subscription(even, 1);
            Attend(even, 1);
            _seed.Subscription(waiting, 5, SubscriptionStatusEnum.PENDING);
            _seed.Subscription(refused, 5, SubscriptionStatusEnum.REJECTED);

            Dictionary<Guid, int> balances = await _service.GetAllBalancesAsync();

            balances.ShouldBe(new Dictionary<Guid, int> { [_member.Id] = 5, [owing.Id] = -2, [even.Id] = 0 }, ignoreOrder: true);
        }

        [Fact]
        public async Task Nobody_at_all_is_no_balances()
        {
            (await _service.GetAllBalancesAsync()).ShouldBeEmpty();
        }

        // The group is gone, the lesson was still taken.
        [Fact]
        public async Task An_attendance_with_no_group_counts()
        {
            TrainGroup group = _seed.Group(_seed.User());
            _seed.Attendance(_member, SomeDay, group);
            _seed.Attendance(_member, SomeDay);

            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(-2);
        }

        // Counted as it stands now, so a change shows at once.
        [Fact]
        public async Task A_balance_follows_the_rows()
        {
            Subscription subscription = _seed.Subscription(_member, 5, SubscriptionStatusEnum.PENDING);
            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(0);

            _database.Change(db => db.Subscriptions.Single(x => x.Id == subscription.Id).Status = SubscriptionStatusEnum.APPROVED);
            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(5);

            Attend(_member, 1);
            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(4);

            _database.Change(db => db.Subscriptions.Remove(db.Subscriptions.Single()));
            (await _service.GetBalanceAsync(_member.Id)).ShouldBe(-1);
        }
    }
}
