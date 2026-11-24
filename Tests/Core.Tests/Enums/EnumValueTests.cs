using Core.Enums;

namespace Core.Tests.Enums
{
    /// <summary>
    /// The numbers are in the database and the names are in the client, so neither may move: a
    /// status inserted in the middle would turn every approved subscription into something else.
    /// </summary>
    public sealed class EnumValueTests
    {
        private static string ListOf<TEnum>() where TEnum : struct, Enum
        {
            return string.Join(", ", Enum.GetValues<TEnum>().Select(x => $"{x}={Convert.ToInt32(x)}"));
        }

        [Fact]
        public void Subscription_statuses()
        {
            ListOf<SubscriptionStatusEnum>().ShouldBe("PENDING=0, APPROVED=1, REJECTED=2");
        }

        [Fact]
        public void Mail_statuses()
        {
            ListOf<MailStatusEnum>().ShouldBe("PENDING=0, SENT=1, FAILED=2");
        }

        [Fact]
        public void Train_group_date_types()
        {
            ListOf<TrainGroupDateTypeEnum>().ShouldBe("DAY_OF_WEEK=0, DAY_OF_MONTH=1, FIXED_DAY=2");
        }

        [Fact]
        public void Workout_plan_start_scenarios()
        {
            ListOf<WorkoutPlanStartScenarioEnum>().ShouldBe("NoRule=0, Running=1, FirstEver=2, UnderMax=3, AtMax=4, AwayTooLong=5, Orphaned=6");
        }

        [Fact]
        public void Grid_filters()
        {
            ListOf<DataTableFiltersEnum>().ShouldBe(
                "startsWith=0, contains=1, notContains=2, endsWith=3, equals=4, notEquals=5, notIn=6, lt=7, lte=8, gt=9, gte=10, " +
                "between=11, dateIs=12, dateIsNot=13, dateBefore=14, dateAfter=15, custom=16, in=17");
        }

        [Fact]
        public void Order_directions()
        {
            ListOf<OrderDirectionEnum>().ShouldBe("ASCENDING=0, DESCENDING=1");
        }

        [Fact]
        public void Contact_information_types()
        {
            ListOf<ContactInformationTypesEnum>().ShouldBe("EMAIL=0, PHONE=1");
        }

        // The same numbers as .NET's own, which the train group dates are stored with.
        [Fact]
        public void Days_of_the_week()
        {
            ListOf<DayOfWeekEnum>().ShouldBe("Sunday=0, Monday=1, Tuesday=2, Wednesday=3, Thursday=4, Friday=5, Saturday=6");
            ListOf<DayOfWeekEnum>().ShouldBe(ListOf<DayOfWeek>());
        }
    }
}
