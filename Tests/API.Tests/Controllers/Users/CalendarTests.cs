using API.Tests.Host;
using Core.Dtos.TrainGroupDate;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace API.Tests.Controllers.Users
{
    /// <summary>
    /// A member's week on their profile: what they booked, what they skipped, and what they came to.
    /// </summary>
    public sealed class CalendarTests : ApiTest
    {
        private const string Url = "api/Users/TimeSlots";

        // A Monday, the 3rd. The week that begins with it ends on Sunday the 9th.
        private static readonly DateTime Monday = new DateTime(2031, 3, 3, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Wednesday = Monday.AddDays(2);

        private readonly User _member;
        private readonly User _trainer;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;

        public CalendarTests(ApiFixture api) : base(api)
        {
            _member = Seed.User();
            _trainer = Seed.User(x => { x.UserName = "nikos"; x.FirstName = "Νίκος"; x.LastName = "Παπάς"; x.Email = "nikos@gym.test"; x.MedicalHistory = "private"; x.ProfileImage = [1, 2]; });
            _group = Seed.Group(_trainer, x => { x.Title = "Pilates"; x.Description = "Easy"; x.MaxParticipants = 7; });
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
        }

        private async Task<List<TimeSlotResponseDto>> WeekAsync(Caller caller, DateTime? from = null, string? userId = null)
        {
            ApiResult<List<TimeSlotResponseDto>> result = await PostAsync<List<TimeSlotResponseDto>>(caller, Url, new { selectedDate = from ?? Monday, userId = userId ?? _member.Id.ToString() });

            result.ShouldSucceed();

            return result.Value;
        }

        // The week in lines, one to a date: the group, the day and what there is to say about it.
        private async Task<List<string>> LinesAsync(DateTime? from = null)
        {
            return (await WeekAsync(Caller.Of(_member), from))
                .SelectMany(x => x.RecurrenceDates.Select(y => $"{x.Title} {y.Date:MM-dd}{(y.IsAttendance ? " came" : "")}{(y.IsUserJoined ? "" : " skipped")}{(y.IsOneOff && !y.IsAttendance ? " once" : "")}"))
                .ToList();
        }

        // Somebody came to something long ago, so that a date with no attendance was missed.
        private void AttendanceIsTaken()
        {
            Seed.Attendance(_trainer, new DateTime(2020, 1, 1));
        }


        // ================================================================
        // Whose
        // ================================================================

        [Fact]
        public async Task A_member_sees_their_own_whoever_they_ask_for()
        {
            User other = Seed.User();
            TrainGroupParticipant mine = Seed.OneOff(_wednesday, _member, Wednesday);
            TrainGroupParticipant theirs = Seed.OneOff(_wednesday, other, Wednesday);

            async Task<int?> OnlyAsync(Caller caller, string userId)
            {
                return (await WeekAsync(caller, userId: userId)).Single().RecurrenceDates.Single().TrainGroupParticipantId;
            }

            (await OnlyAsync(Caller.Of(_member), _member.Id.ToString())).ShouldBe(mine.Id);
            (await OnlyAsync(Caller.Of(_member, "Users_View", "TrainGroupParticipants_Delete"), other.Id.ToString())).ShouldBe(mine.Id);
            (await OnlyAsync(Staff(_member), other.Id.ToString())).ShouldBe(theirs.Id);
            (await OnlyAsync(Staff(_member), _member.Id.ToString())).ShouldBe(mine.Id);
            // One that is no id is their own.
            (await OnlyAsync(Staff(_member), "x")).ShouldBe(mine.Id);
        }

        [Fact]
        public async Task A_token_that_does_not_say_who_sees_nothing()
        {
            Seed.OneOff(_wednesday, _member, Wednesday);

            (await WeekAsync(Caller.WithNoId())).ShouldBeEmpty();
            (await WeekAsync(Caller.WithNoId("TrainGroupParticipants_View"))).Count.ShouldBe(1);
        }

        [Fact]
        public async Task Somebody_has_to_be_asked_for()
        {
            (await PostAsync<List<TimeSlotResponseDto>>(Caller.Of(_member), Url, new { selectedDate = Monday, userId = "" })).ShouldBeBad(English.Required("UserId"));
        }


        // ================================================================
        // A booking
        // ================================================================

        [Fact]
        public async Task A_booked_date_says_everything_about_its_group()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));

            TimeSlotResponseDto slot = (await WeekAsync(Caller.Of(_member))).Single();

            slot.Id.ShouldBe(_group.Id);
            slot.TrainGroupId.ShouldBe(_group.Id);
            slot.Title.ShouldBe("Pilates");
            slot.Description.ShouldBe("Easy");
            slot.StartOn.ShouldBe(_group.StartOn);
            slot.Duration.ShouldBe(_group.Duration);
            slot.MaxParticipants.ShouldBe(7);
            slot.TrainerId.ShouldBe(_trainer.Id);
            slot.TrainerFullName.ShouldBe("Νίκος Παπάς");

            TimeSlotRecurrenceDateDto date = slot.RecurrenceDates.Single();
            date.TrainGroupDateId.ShouldBe(_wednesday.Id);
            date.TrainGroupDateType.ShouldBe(TrainGroupDateTypeEnum.DAY_OF_WEEK);
            date.Date.ShouldBe(Wednesday);
            date.Date.Kind.ShouldBe(DateTimeKind.Utc);
            date.RecurrenceDayOfWeek.ShouldBe(3);
            date.RecurrenceDayOfMonth.ShouldBeNull();
            date.IsOneOff.ShouldBeFalse();
            date.IsUserJoined.ShouldBeTrue();
            date.IsAttendance.ShouldBeFalse();
            date.AttendanceId.ShouldBeNull();
            date.TrainGroupParticipantId.ShouldBe(booking.Id);
            date.TrainGroupParticipantUnavailableDateId.ShouldBeNull();
        }

        [Fact]
        public async Task The_trainer_is_a_name_and_a_face()
        {
            Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));

            ApiResult<List<TimeSlotResponseDto>> result = await PostAsync<List<TimeSlotResponseDto>>(Caller.Of(_member), Url, new { selectedDate = Monday, userId = _member.Id });

            result.Value.Single().Trainer.FirstName.ShouldBe("Νίκος");
            result.Value.Single().Trainer.ProfileImage.ShouldBe([1, 2]);
            result.Body.ShouldNotContain("nikos@gym.test");
            result.Body.ShouldNotContain("private");
            result.Body.ShouldNotContain("\"nikos\"");
        }

        [Theory]
        [InlineData("Νίκος", "", "Νίκος")]
        [InlineData("", "Παπάς", "Παπάς")]
        [InlineData("", "", "")]
        public async Task A_trainers_name_has_no_spare_space(string firstName, string lastName, string expected)
        {
            Database.Change(db => { User trainer = db.Users.Single(x => x.Id == _trainer.Id); trainer.FirstName = firstName; trainer.LastName = lastName; });
            Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));

            (await WeekAsync(Caller.Of(_member))).Single().TrainerFullName.ShouldBe(expected);
        }

        // Every kind, in the order of the days, and the groups in the order they come first.
        [Fact]
        public async Task The_week_is_every_date_that_is_booked_in_it()
        {
            TrainGroup yoga = Seed.Group(_trainer, x => x.Title = "Yoga");
            Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));
            Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Sunday), _member, Monday.AddDays(-70));
            Seed.Recurring(Seed.Monthly(yoga, 4), _member, Monday.AddDays(-70));
            Seed.OneOff(Seed.Weekly(yoga, DayOfWeek.Friday), _member, Monday.AddDays(4));
            Seed.Participant(Seed.Fixed(yoga, Monday.AddDays(5).AddHours(9)), _member);
            Seed.OneOff(_wednesday, _member, Monday.AddDays(-5));
            Seed.OneOff(_wednesday, _member, Monday.AddDays(9));

            (await LinesAsync()).ShouldBe([
                "Yoga 03-04",
                "Yoga 03-07 once",
                "Yoga 03-08 once",
                "Pilates 03-05",
                "Pilates 03-09"
            ]);
        }

        // Seven days from the date that is asked for, whatever its hour.
        [Theory]
        [InlineData(-6, 0, false)]
        [InlineData(-4, 0, true)]
        [InlineData(0, 0, true)]
        [InlineData(0, 23, true)]
        [InlineData(2, 0, true)]
        [InlineData(2, 23, true)]
        [InlineData(3, 0, false)]
        [InlineData(-5, 23, false)]
        public async Task A_week_is_seven_days_from_the_date_asked_for(int days, int hours, bool isThere)
        {
            Seed.OneOff(_wednesday, _member, Wednesday);

            (await LinesAsync(Monday.AddDays(days).AddHours(hours))).Count.ShouldBe(isThere ? 1 : 0);
        }

        [Fact]
        public async Task A_monthly_booking_is_its_number()
        {
            Seed.Recurring(Seed.Monthly(_group, 5), _member, Monday.AddDays(-70));

            TimeSlotRecurrenceDateDto date = (await WeekAsync(Caller.Of(_member))).Single().RecurrenceDates.Single();

            date.RecurrenceDayOfMonth.ShouldBe(5);
            date.RecurrenceDayOfWeek.ShouldBeNull();
            date.TrainGroupDateType.ShouldBe(TrainGroupDateTypeEnum.DAY_OF_MONTH);
            date.IsOneOff.ShouldBeFalse();
        }

        // Where the week runs over into the next month.
        [Fact]
        public async Task The_month_may_end_in_the_week()
        {
            Seed.Recurring(Seed.Monthly(_group, 30), _member, Monday.AddDays(-70));
            Seed.Recurring(Seed.Monthly(_group, 1), _member, Monday.AddDays(-70));

            (await LinesAsync(new DateTime(2031, 3, 28))).ShouldBe(["Pilates 03-30", "Pilates 04-01"]);
        }

        // A skipped date stays on it, and says which skip to take back.
        [Fact]
        public async Task A_skipped_date_is_there_as_skipped()
        {
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));
            TrainGroupParticipantUnavailableDate skip = Seed.Skip(booking, Wednesday.AddHours(5));
            Seed.Skip(booking, Wednesday.AddDays(7));

            TimeSlotRecurrenceDateDto date = (await WeekAsync(Caller.Of(_member))).Single().RecurrenceDates.Single();

            date.IsUserJoined.ShouldBeFalse();
            date.TrainGroupParticipantUnavailableDateId.ShouldBe(skip.Id);
        }

        [Fact]
        public async Task What_was_given_up_is_not_there_and_what_came_before_is()
        {
            AttendanceIsTaken();
            Seed.OneOff(_wednesday, _member, Wednesday, x => x.RemovedOn = Monday);
            Seed.Participant(Seed.Fixed(_group, Wednesday), _member, x => x.RemovedOn = Monday);
            Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Tuesday), _member, Monday.AddDays(-70), Monday.AddDays(2), x => x.RemovedOn = Monday);
            Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Friday), _member, Monday.AddDays(-70), Monday.AddDays(2), x => x.RemovedOn = Monday);

            (await LinesAsync()).ShouldBe(["Pilates 03-04"]);
        }

        // Only that day of it: the rest of the week stays.
        [Fact]
        public async Task A_session_the_gym_called_off_is_not_there()
        {
            Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));
            Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Friday), _member, Monday.AddDays(-70));
            Seed.Cancelled(_group, Wednesday.AddHours(3));
            Seed.Cancelled(Seed.Group(_trainer), Wednesday.AddDays(2));

            (await LinesAsync()).ShouldBe(["Pilates 03-07"]);
        }


        // ================================================================
        // Since when a date can have been missed
        // ================================================================

        // From the first attendance anybody ever had. Before it, nothing says they did not come.
        [Theory]
        [InlineData(0, "Pilates 03-05")]
        [InlineData(2, "Pilates 03-05")]
        [InlineData(3, "")]
        public async Task Dates_before_attendance_was_taken_are_not_there(int daysOfTheFirst, string expected)
        {
            DateTime monday = new DateTime(2025, 3, 3);
            Seed.Recurring(_wednesday, _member, new DateTime(2024, 1, 1));
            Seed.Attendance(_trainer, monday.AddDays(daysOfTheFirst).AddHours(18));

            string.Join(" ", await LinesAsync(monday)).ShouldBe(expected);
        }

        // With none ever taken it is from today.
        [Fact]
        public async Task With_none_ever_taken_only_today_and_after_is_there()
        {
            DateTime today = Midnight.Today();
            for (int i = 0; i < 7; i++)
                Seed.Recurring(Seed.Weekly(_group, (DayOfWeek)i), _member, new DateTime(2024, 1, 1));

            (await LinesAsync(today.AddDays(-3))).ShouldBe(Enumerable.Range(0, 4).Select(x => $"Pilates {today.AddDays(x):MM-dd}"));
            (await LinesAsync(today.AddDays(-7))).ShouldBeEmpty();
            (await LinesAsync(today)).Count.ShouldBe(7);
        }


        // ================================================================
        // What they came to
        // ================================================================

        [Fact]
        public async Task An_attendance_is_what_was_written_down_of_it()
        {
            TrainGroupΑttendance attendance = Seed.Attendance(_member, Wednesday.AddHours(18), _group, x =>
            {
                x.TrainGroupTitle = "Then";
                x.TrainGroupDescription = "As it was";
                x.TrainGroupStartOn = new DateTime(2025, 1, 1, 9, 0, 0);
                x.TrainGroupDuration = new DateTime(2000, 1, 1, 0, 45, 0);
                x.TrainerFullName = "Who it was";
            });

            TimeSlotResponseDto slot = (await WeekAsync(Caller.Of(_member))).Single();

            slot.Id.ShouldBe(_group.Id);
            slot.TrainGroupId.ShouldBe(_group.Id);
            slot.Title.ShouldBe("Then");
            slot.Description.ShouldBe("As it was");
            slot.StartOn.ShouldBe(new DateTime(2025, 1, 1, 9, 0, 0));
            slot.Duration.ShouldBe(new DateTime(2000, 1, 1, 0, 45, 0));
            slot.TrainerId.ShouldBe(_trainer.Id);
            slot.TrainerFullName.ShouldBe("Who it was");
            slot.Trainer.FirstName.ShouldBe("Νίκος");
            slot.Trainer.UserName.ShouldBe("");
            slot.IsUnavailableTrainGroup.ShouldBeFalse();
            slot.SpotsLeft.ShouldBe(0);
            slot.MaxParticipants.ShouldBe(0);

            TimeSlotRecurrenceDateDto date = slot.RecurrenceDates.Single();
            date.Date.ShouldBe(Wednesday);
            date.Date.Kind.ShouldBe(DateTimeKind.Utc);
            date.IsAttendance.ShouldBeTrue();
            date.AttendanceId.ShouldBe(attendance.Id);
            date.IsUserJoined.ShouldBeTrue();
            date.IsOneOff.ShouldBeTrue();
            date.TrainGroupDateId.ShouldBeNull();
            date.TrainGroupDateType.ShouldBeNull();
            date.TrainGroupParticipantId.ShouldBeNull();
        }

        // By hand, or whose group and trainer are gone.
        [Fact]
        public async Task An_attendance_of_no_group_is_there_all_the_same()
        {
            Seed.Attendance(_member, Wednesday, change: x => { x.TrainGroupTitle = "By hand"; x.TrainerFullName = "Somebody"; });
            Seed.Attendance(_member, Wednesday, change: x => { x.TrainGroupTitle = "Gone"; x.TrainerId = Guid.NewGuid(); });

            List<TimeSlotResponseDto> slots = (await WeekAsync(Caller.Of(_member))).OrderBy(x => x.Title).ToList();

            slots.Select(x => x.Title).ShouldBe(["By hand", "Gone"]);
            slots.ShouldAllBe(x => x.TrainGroupId == 0 && x.Id == 0 && x.Trainer == null);
            slots[0].TrainerId.ShouldBe(Guid.Empty);
            slots[0].TrainerFullName.ShouldBe("Somebody");
            slots[1].TrainerId.ShouldNotBe(Guid.Empty);
        }

        // One that was attended is not also one that was booked.
        [Fact]
        public async Task Having_come_stands_for_the_booking()
        {
            Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));
            Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Friday), _member, Monday.AddDays(-70));
            Seed.Attendance(_member, Wednesday.AddHours(18), _group);

            (await LinesAsync()).ShouldBe(["Pilates 03-07", "Pilates 03-05 came"]);
        }

        // Nor one of another group, or by hand, or another member's.
        [Fact]
        public async Task Having_come_to_something_else_does_not()
        {
            Seed.Recurring(_wednesday, _member, Monday.AddDays(-70));
            Seed.Attendance(_member, Wednesday, Seed.Group(_trainer, x => x.Title = "Yoga"));
            Seed.Attendance(_member, Wednesday);
            Seed.Attendance(_trainer, Wednesday, _group);

            (await LinesAsync()).ShouldBe(["Pilates 03-05", "Yoga 03-05 came", "By hand 03-05 came"]);
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(60 * 24 * 7 - 1, true)]
        [InlineData(60 * 24 * 7, false)]
        public async Task An_attendance_is_in_the_week_it_was_in(int minutes, bool isThere)
        {
            Seed.Attendance(_member, Monday.AddMinutes(minutes), _group);

            (await LinesAsync()).Count.ShouldBe(isThere ? 1 : 0);
        }

        [Fact]
        public async Task An_empty_week_is_empty()
        {
            (await WeekAsync(Caller.Of(_member))).ShouldBeEmpty();
        }
    }
}
