using API.AutoMapper;
using Core.Dtos.TrainGroupDate;
using Core.Models;

namespace API.Tests.AutoMapper
{
    /// <summary>
    /// Four resolvers for a group's fixed day. No map uses them.
    /// </summary>
    public sealed class ResolverTests
    {
        // With its date and not its hour.
        private static readonly DateTime Moment = new DateTime(2031, 3, 5, 18, 30, 15, 250, DateTimeKind.Utc);

        [Fact]
        public void A_day_goes_out_at_midnight()
        {
            TrainGroupDate date = new TrainGroupDate { FixedDay = Moment };

            DateTime? day = new TrainGroupDateDtoDateOnlyToDateTimeResolver().Resolve(date, new TrainGroupDateDto(), null, null!);
            DateTime? added = new TrainGroupDateAddDtoDateOnlyToDateTimeResolver().Resolve(date, new TrainGroupDateAddDto(), null, null!);

            day.ShouldBe(new DateTime(2031, 3, 5));
            day!.Value.Kind.ShouldBe(DateTimeKind.Unspecified);
            added.ShouldBe(day);
        }

        [Fact]
        public void A_day_comes_in_as_a_date()
        {
            new TrainGroupDateDtoDateTimeToDateOnlyResolver().Resolve(new TrainGroupDateDto { FixedDay = Moment }, new TrainGroupDate(), null, null!).ShouldBe(new DateOnly(2031, 3, 5));
            new TrainGroupDateAddDtoDateTimeToDateOnlyResolver().Resolve(new TrainGroupDateAddDto { FixedDay = Moment }, new TrainGroupDate(), null, null!).ShouldBe(new DateOnly(2031, 3, 5));
        }

        // Whatever was there before.
        [Fact]
        public void No_day_is_no_day()
        {
            new TrainGroupDateDtoDateOnlyToDateTimeResolver().Resolve(new TrainGroupDate(), new TrainGroupDateDto(), Moment, null!).ShouldBeNull();
            new TrainGroupDateAddDtoDateOnlyToDateTimeResolver().Resolve(new TrainGroupDate(), new TrainGroupDateAddDto(), Moment, null!).ShouldBeNull();
            new TrainGroupDateDtoDateTimeToDateOnlyResolver().Resolve(new TrainGroupDateDto(), new TrainGroupDate(), new DateOnly(2031, 3, 5), null!).ShouldBeNull();
            new TrainGroupDateAddDtoDateTimeToDateOnlyResolver().Resolve(new TrainGroupDateAddDto(), new TrainGroupDate(), new DateOnly(2031, 3, 5), null!).ShouldBeNull();
        }
    }
}
