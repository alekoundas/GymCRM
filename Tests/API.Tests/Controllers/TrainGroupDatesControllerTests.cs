using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroupDate;
using Core.Enums;
using Core.Models;
using Core.Translations;

namespace API.Tests.Controllers
{
    /// <summary>
    /// The days a group runs on: changing them, and the sessions there are on one day.
    /// </summary>
    public sealed class TrainGroupDatesControllerTests : ApiTest
    {
        private const string Url = "api/TrainGroupDates";

        // A Wednesday, the 5th.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _member;
        private readonly User _trainer;
        private readonly TrainGroup _group;

        public TrainGroupDatesControllerTests(ApiFixture api) : base(api)
        {
            _member = Seed.User();
            _trainer = Seed.User(x => { x.FirstName = "Νίκος"; x.LastName = "Παπάς"; x.UserName = "nikos"; x.Email = "nikos@gym.test"; x.MedicalHistory = "private"; x.ProfileImage = [1, 2]; });
            _group = Seed.Group(_trainer, x => { x.Title = "Pilates"; x.Description = "Easy"; x.MaxParticipants = 3; });
        }


        // ================================================================
        // The sessions on a day
        // ================================================================

        private async Task<List<TimeSlotResponseDto>> SlotsAsync(Caller caller, DateTime day, User? of = null)
        {
            ApiResult<List<TimeSlotResponseDto>> result = await PostAsync<List<TimeSlotResponseDto>>(caller, Url + "/TimeSlots", new { selectedDate = day, userId = (of ?? _member).Id });

            result.ShouldSucceed();

            return result.Value;
        }

        private async Task<TimeSlotResponseDto> SlotAsync(DateTime? day = null)
        {
            return (await SlotsAsync(Caller.Of(_member), day ?? Day)).ShouldHaveSingleItem();
        }

        // One day of a group as it is listed: the kind, which, and whether the member has it.
        private static string Describe(TimeSlotRecurrenceDateDto date)
        {
            string which = date.IsOneOff ? "once" : date.RecurrenceDayOfWeek != null ? "weekday " + date.RecurrenceDayOfWeek : "day " + date.RecurrenceDayOfMonth;

            return $"{date.TrainGroupDateType?.ToString() ?? "-"} {which} {date.Date:yyyy-MM-dd}{(date.IsUserJoined ? " joined " + date.TrainGroupParticipantId : "")}";
        }

        [Fact]
        public async Task A_session_is_the_group_as_it_is()
        {
            TrainGroupCategory category = Seed.Category("Reformer");
            Database.Change(db => db.TrainGroups.Single().TrainGroupCategoryId = category.Id);
            TrainGroupDate wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);

            TimeSlotResponseDto slot = await SlotAsync();

            slot.Id.ShouldBe(_group.Id);
            slot.TrainGroupId.ShouldBe(_group.Id);
            slot.TrainGroupDateId.ShouldBe(wednesday.Id);
            slot.TrainGroupCategoryId.ShouldBe(category.Id);
            slot.Title.ShouldBe("Pilates");
            slot.Description.ShouldBe("Easy");
            slot.StartOn.ShouldBe(_group.StartOn);
            slot.Duration.ShouldBe(_group.Duration);
            slot.MaxParticipants.ShouldBe(3);
            slot.SpotsLeft.ShouldBe(3);
            slot.IsUnavailableTrainGroup.ShouldBeFalse();
            slot.UnavailableTrainGroupId.ShouldBeNull();
            slot.BookedParticipantId.ShouldBeNull();
            slot.IsBookedRecurring.ShouldBeFalse();
            slot.SkippedUnavailableDateId.ShouldBeNull();
        }

        [Fact]
        public async Task The_trainer_is_a_name_and_a_face()
        {
            Seed.Weekly(_group, DayOfWeek.Wednesday);

            ApiResult<List<TimeSlotResponseDto>> result = await PostAsync<List<TimeSlotResponseDto>>(Caller.Of(_member), Url + "/TimeSlots", new { selectedDate = Day, userId = _member.Id });

            TimeSlotResponseDto slot = result.Value.Single();
            slot.TrainerId.ShouldBe(_trainer.Id);
            slot.TrainerFullName.ShouldBe("Νίκος Παπάς");
            slot.Trainer.FirstName.ShouldBe("Νίκος");
            slot.Trainer.ProfileImage.ShouldBe([1, 2]);
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
            Seed.Weekly(_group, DayOfWeek.Wednesday);
            Database.Change(db => { User trainer = db.Users.Single(x => x.Id == _trainer.Id); trainer.FirstName = firstName; trainer.LastName = lastName; });

            (await SlotAsync()).TrainerFullName.ShouldBe(expected);
        }

        // Every kind of day, on the days around the one it falls on.
        [Fact]
        public async Task A_group_is_there_on_its_days_and_no_others()
        {
            Seed.Weekly(_group, DayOfWeek.Wednesday);
            Seed.Monthly(Seed.Group(_trainer, x => x.Title = "Monthly"), 6);
            Seed.Fixed(Seed.Group(_trainer, x => x.Title = "Fixed"), Day.AddDays(2).AddHours(10));
            Seed.Group(_trainer, x => x.Title = "Never");

            async Task<string> TitlesAsync(int days)
            {
                return string.Join(" ", (await SlotsAsync(Caller.Of(_member), Day.AddDays(days))).Select(x => x.Title));
            }

            (await TitlesAsync(-1)).ShouldBe("");
            (await TitlesAsync(0)).ShouldBe("Pilates");
            (await TitlesAsync(1)).ShouldBe("Monthly");
            (await TitlesAsync(2)).ShouldBe("Fixed");
            (await TitlesAsync(3)).ShouldBe("");
            (await TitlesAsync(7)).ShouldBe("Pilates");
        }

        // Not for which sessions there are, nor for what is known about them.
        [Fact]
        public async Task The_time_of_the_day_asked_for_does_not_matter()
        {
            TrainGroup fixedGroup = Seed.Group(_trainer, x => x.Title = "Fixed");
            Seed.Fixed(fixedGroup, Day);
            TrainGroupParticipant booking = Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Wednesday), _member, Day.AddDays(-7), Day.AddDays(1));
            TrainGroupParticipantUnavailableDate skip = Seed.Skip(booking, Day);
            TrainGroupUnavailableDate cancelled = Seed.Cancelled(_group, Day);

            List<TimeSlotResponseDto> slots = await SlotsAsync(Caller.Of(_member), Day.AddHours(23));

            slots.Select(x => x.Title).ShouldBe(["Pilates", "Fixed"], ignoreOrder: true);
            TimeSlotResponseDto slot = slots.Single(x => x.Title == "Pilates");
            slot.SkippedUnavailableDateId.ShouldBe(skip.Id);
            slot.UnavailableTrainGroupId.ShouldBe(cancelled.Id);
            slot.RecurrenceDates.Select(Describe).ShouldBe([$"DAY_OF_WEEK weekday 3 2000-01-05 joined {booking.Id}", "- once 2031-03-05"]);
            slot.RecurrenceDates.Last().Date.ShouldBe(Day);
        }

        // Both a weekday and a day of the month of the group: the session is the one that was made first.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Of_two_days_on_one_date_the_first_is_the_session(bool isWeekdayFirst)
        {
            TrainGroupDate first = isWeekdayFirst ? Seed.Weekly(_group, DayOfWeek.Wednesday) : Seed.Monthly(_group, 5);
            TrainGroupDate second = isWeekdayFirst ? Seed.Monthly(_group, 5) : Seed.Weekly(_group, DayOfWeek.Wednesday);

            TimeSlotResponseDto slot = await SlotAsync();

            slot.TrainGroupDateId.ShouldBe(first.Id);
            slot.RecurrenceDates.Select(x => x.TrainGroupDateId).ShouldBe([first.Id, second.Id, first.Id]);
        }

        // The session itself comes last, and is the fixed day when there is one.
        [Fact]
        public async Task The_days_of_the_group_are_listed_and_then_the_session()
        {
            TrainGroupDate wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            Seed.Weekly(_group, DayOfWeek.Sunday);
            Seed.Weekly(_group, DayOfWeek.Saturday);

            TimeSlotResponseDto slot = await SlotAsync();

            slot.RecurrenceDates.Select(Describe).ShouldBe(["DAY_OF_WEEK weekday 3 2000-01-05", "DAY_OF_WEEK weekday 0 2000-01-02", "DAY_OF_WEEK weekday 6 2000-01-08", "- once 2031-03-05"]);
            slot.RecurrenceDates.Last().TrainGroupDateId.ShouldBe(wednesday.Id);
        }

        [Fact]
        public async Task A_day_of_the_month_is_listed_by_its_number()
        {
            Seed.Monthly(_group, 5);
            Seed.Monthly(_group, 31);

            (await SlotAsync()).RecurrenceDates.Select(Describe).ShouldBe(["DAY_OF_MONTH day 5 2000-01-05", "DAY_OF_MONTH day 31 2000-01-31", "- once 2031-03-05"]);
        }

        [Fact]
        public async Task A_fixed_day_is_the_session()
        {
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day);
            Seed.Fixed(_group, Day.AddDays(1));

            TimeSlotResponseDto slot = await SlotAsync();

            slot.TrainGroupDateId.ShouldBe(fixedDay.Id);
            slot.RecurrenceDates.Select(Describe).ShouldBe(["FIXED_DAY once 2031-03-05"]);
        }

        // The two may not be on one date, but if they are the fixed day is the one.
        [Fact]
        public async Task A_fixed_day_comes_before_a_weekday()
        {
            Seed.Weekly(_group, DayOfWeek.Wednesday);
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day);

            TimeSlotResponseDto slot = await SlotAsync();

            slot.TrainGroupDateId.ShouldBe(fixedDay.Id);
            slot.RecurrenceDates.Select(Describe).ShouldBe(["DAY_OF_WEEK weekday 3 2000-01-05", "FIXED_DAY once 2031-03-05"]);
        }

        // A weekday that also names a day of the month passes for one of them when it is looked for
        // and for neither when it is looked at. One such day, and the whole page fails on that date.
        [Fact]
        public async Task A_day_that_names_two_kinds_breaks_every_session_of_the_day()
        {
            Database.Add(new TrainGroupDate { TrainGroupId = _group.Id, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = DayOfWeek.Monday, RecurrenceDayOfMonth = 5 });
            Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Wednesday);

            (await PostAsync<List<TimeSlotResponseDto>>(Caller.Of(_member), Url + "/TimeSlots", new { selectedDate = Day, userId = _member.Id })).ShouldBreak();
        }


        // ================================================================
        // Where the member stands
        // ================================================================

        [Fact]
        public async Task A_one_off_shows_as_the_session_joined()
        {
            TrainGroupDate wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            TrainGroupParticipant booking = Seed.OneOff(wednesday, _member, Day);

            TimeSlotResponseDto slot = await SlotAsync();

            slot.BookedParticipantId.ShouldBe(booking.Id);
            slot.IsBookedRecurring.ShouldBeFalse();
            slot.SpotsLeft.ShouldBe(2);
            slot.RecurrenceDates.Select(Describe).ShouldBe(["DAY_OF_WEEK weekday 3 2000-01-05", $"- once 2031-03-05 joined {booking.Id}"]);
        }

        [Fact]
        public async Task A_recurring_booking_shows_as_its_weekday_joined()
        {
            TrainGroupDate wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            Seed.Weekly(_group, DayOfWeek.Friday);
            TrainGroupParticipant booking = Seed.Recurring(wednesday, _member, Day.AddDays(-7));

            TimeSlotResponseDto slot = await SlotAsync();

            slot.BookedParticipantId.ShouldBe(booking.Id);
            slot.IsBookedRecurring.ShouldBeTrue();
            slot.SpotsLeft.ShouldBe(2);
            slot.RecurrenceDates.Select(Describe).ShouldBe([$"DAY_OF_WEEK weekday 3 2000-01-05 joined {booking.Id}", "DAY_OF_WEEK weekday 5 2000-01-07", "- once 2031-03-05"]);
        }

        [Fact]
        public async Task A_fixed_day_shows_as_the_session_joined()
        {
            TrainGroupParticipant booking = Seed.Participant(Seed.Fixed(_group, Day), _member);

            TimeSlotResponseDto slot = await SlotAsync();

            slot.BookedParticipantId.ShouldBe(booking.Id);
            slot.IsBookedRecurring.ShouldBeFalse();
            slot.RecurrenceDates.Select(Describe).ShouldBe([$"FIXED_DAY once 2031-03-05 joined {booking.Id}"]);
        }

        // It is still theirs, and the place is free.
        [Fact]
        public async Task A_skipped_date_is_still_booked_and_takes_no_place()
        {
            TrainGroupParticipant booking = Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Wednesday), _member, Day.AddDays(-7));
            TrainGroupParticipantUnavailableDate skip = Seed.Skip(booking, Day.AddHours(5));

            TimeSlotResponseDto slot = await SlotAsync();

            slot.BookedParticipantId.ShouldBe(booking.Id);
            slot.IsBookedRecurring.ShouldBeTrue();
            slot.SkippedUnavailableDateId.ShouldBe(skip.Id);
            slot.SpotsLeft.ShouldBe(3);
        }

        // A booking that starts later runs already as far as the weekday is concerned, one that has
        // ended does not, and neither holds a place.
        [Theory]
        [InlineData(7, null, true)]
        [InlineData(-70, 0, false)]
        [InlineData(-70, -7, false)]
        public async Task A_booking_outside_its_time_holds_no_place(int from, int? end, bool isWeekdayJoined)
        {
            Seed.Recurring(Seed.Weekly(_group, DayOfWeek.Wednesday), _member, Day.AddDays(from), end == null ? null : Day.AddDays(end.Value));

            TimeSlotResponseDto slot = await SlotAsync();

            slot.BookedParticipantId.ShouldBeNull();
            slot.SpotsLeft.ShouldBe(3);
            slot.RecurrenceDates[0].IsUserJoined.ShouldBe(isWeekdayJoined);
        }

        [Fact]
        public async Task A_cancelled_one_off_is_not_there()
        {
            Seed.OneOff(Seed.Weekly(_group, DayOfWeek.Wednesday), _member, Day, x => x.RemovedOn = Day.AddDays(-3));

            TimeSlotResponseDto slot = await SlotAsync();

            slot.BookedParticipantId.ShouldBeNull();
            slot.SpotsLeft.ShouldBe(3);
        }

        // Everybody's bookings take places, and only the member's own show.
        [Fact]
        public async Task The_places_left_are_those_nobody_has()
        {
            TrainGroupDate wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            Seed.Recurring(wednesday, Seed.User(), Day.AddDays(-7));
            Seed.OneOff(wednesday, Seed.User(), Day);

            (await SlotAsync()).SpotsLeft.ShouldBe(1);
            (await SlotAsync()).BookedParticipantId.ShouldBeNull();

            Seed.OneOff(wednesday, Seed.User(), Day);
            Seed.OneOff(wednesday, Seed.User(), Day);

            // More than there is room for, put there by hand.
            (await SlotAsync()).SpotsLeft.ShouldBe(-1);
        }

        [Fact]
        public async Task A_session_the_gym_called_off_says_so()
        {
            Seed.Weekly(_group, DayOfWeek.Wednesday);
            Seed.Cancelled(_group, Day.AddDays(7));
            (await SlotAsync()).IsUnavailableTrainGroup.ShouldBeFalse();

            TrainGroupUnavailableDate cancelled = Seed.Cancelled(_group, Day.AddHours(3));

            TimeSlotResponseDto slot = await SlotAsync();
            slot.IsUnavailableTrainGroup.ShouldBeTrue();
            slot.UnavailableTrainGroupId.ShouldBe(cancelled.Id);
        }


        // ================================================================
        // Whose
        // ================================================================

        [Fact]
        public async Task A_member_only_sees_where_they_stand_themselves_and_staff_anybody()
        {
            User other = Seed.User();
            TrainGroupDate wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
            TrainGroupParticipant mine = Seed.OneOff(wednesday, _member, Day);
            TrainGroupParticipant theirs = Seed.OneOff(wednesday, other, Day);

            (await SlotsAsync(Caller.Of(_member), Day, _member)).Single().BookedParticipantId.ShouldBe(mine.Id);
            (await SlotsAsync(Caller.Of(_member), Day, other)).Single().BookedParticipantId.ShouldBe(mine.Id);
            (await SlotsAsync(Staff(_member), Day, other)).Single().BookedParticipantId.ShouldBe(theirs.Id);
            (await SlotsAsync(Staff(_member), Day, _member)).Single().BookedParticipantId.ShouldBe(mine.Id);
            (await SlotsAsync(Caller.WithNoId(), Day, other)).Single().BookedParticipantId.ShouldBeNull();
            (await SlotsAsync(Caller.WithNoId("TrainGroupParticipants_View"), Day, other)).Single().BookedParticipantId.ShouldBe(theirs.Id);
        }

        [Fact]
        public async Task Somebody_who_is_no_id_is_the_caller()
        {
            TrainGroupParticipant mine = Seed.OneOff(Seed.Weekly(_group, DayOfWeek.Wednesday), _member, Day);

            (await PostAsync<List<TimeSlotResponseDto>>(Staff(_member), Url + "/TimeSlots", new { selectedDate = Day, userId = "x" })).Value.Single().BookedParticipantId.ShouldBe(mine.Id);
            (await PostAsync<List<TimeSlotResponseDto>>(Staff(_member), Url + "/TimeSlots", new { selectedDate = Day, userId = "" })).ShouldBeBad(English.Required("UserId"));
        }


        // ================================================================
        // Changing the days
        // ================================================================

        private static Dictionary<string, object?> Body(string date, int groupId, int? id = null)
        {
            Dictionary<string, object?> body = new Dictionary<string, object?> { ["trainGroupId"] = groupId };
            if (id != null)
                body["id"] = id;

            if (int.TryParse(date, out int day))
            {
                body["trainGroupDateType"] = "DAY_OF_MONTH";
                body["recurrenceDayOfMonth"] = day;
            }
            else if (Enum.TryParse(date, out DayOfWeek dayOfWeek))
            {
                body["trainGroupDateType"] = "DAY_OF_WEEK";
                body["recurrenceDayOfWeek"] = dayOfWeek.ToString();
            }
            else
            {
                body["trainGroupDateType"] = "FIXED_DAY";
                body["fixedDay"] = DateTime.SpecifyKind(DateTime.Parse(date), DateTimeKind.Utc);
            }

            return body;
        }

        private TrainGroupDate Add(string date)
        {
            if (int.TryParse(date, out int day))
                return Seed.Monthly(_group, day);

            return Enum.TryParse(date, out DayOfWeek dayOfWeek) ? Seed.Weekly(_group, dayOfWeek) : Seed.Fixed(_group, DateTime.Parse(date));
        }

        private Caller Admin()
        {
            return Caller.Of(_member, "TrainGroupDates_View", "TrainGroupDates_Add", "TrainGroupDates_Edit", "TrainGroupDates_Delete");
        }

        // What is there, what is added, and what is wrong with the two together. The date a message
        // names is the fixed one.
        public static readonly TheoryData<string, string, string[]> Pairs = new TheoryData<string, string, string[]>
        {
            { "Monday", "Friday", [] },
            { "5", "6", [] },
            { "2031-03-05", "2031-03-06", [] },
            { "Monday", "5", [TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix] },
            { "5", "Monday", [TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix] },
            { "Wednesday", "2031-03-05", [TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_week_entry] },
            { "Wednesday", "2031-03-06", [] },
            { "5", "2031-03-05", [TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_month_entry] },
            { "5", "2031-03-06", [] },
            // Only a fixed day being added is looked at; the weekday it falls on may be added after it.
            { "2031-03-05", "Wednesday", [] },
            { "2031-03-05", "5", [] },
            { "Monday", "Monday", [TranslationKeys.Duplicate_train_group_date_found] },
            { "5", "5", [TranslationKeys.Duplicate_train_group_date_found] },
            { "2031-03-05", "2031-03-05", [TranslationKeys.Duplicate_train_group_date_found] },
        };

        [Theory]
        [MemberData(nameof(Pairs))]
        public async Task A_day_is_added_unless_it_does_not_go_with_the_others(string there, string added, string[] errors)
        {
            Add(there);

            ApiResult<List<TrainGroupDate>> result = await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body(added, _group.Id) });

            if (errors.Length == 0)
                result.ShouldSucceed(English.Updated("TrainGroupDate"));
            else
                result.ShouldBeBad(errors.Select(x => English.Of(x, added)).ToArray());

            Database.Read(db => db.TrainGroupDates.Count()).ShouldBe(errors.Length == 0 ? 2 : 1);
        }

        [Theory]
        [MemberData(nameof(Pairs))]
        public async Task A_day_is_changed_unless_it_does_not_go_with_the_others(string there, string changed, string[] errors)
        {
            Add(there);
            TrainGroupDate date = Add("2031-12-24");

            ApiResult<TrainGroupDate> result = await PutAsync<TrainGroupDate>(Admin(), $"{Url}/{date.Id}", Body(changed, _group.Id, date.Id));

            if (errors.Length == 0)
                result.ShouldSucceed(English.Updated("TrainGroupDate"));
            else
                result.ShouldBeBad(errors.Select(x => English.Of(x, changed)).ToArray());
        }

        // A day changed into itself is no duplicate.
        [Theory]
        [InlineData("Monday")]
        [InlineData("5")]
        [InlineData("2031-03-05")]
        public async Task A_day_may_stay_what_it_is(string date)
        {
            TrainGroupDate existing = Add(date);

            (await PutAsync<TrainGroupDate>(Admin(), $"{Url}/{existing.Id}", Body(date, _group.Id, existing.Id))).ShouldSucceed(English.Updated("TrainGroupDate"));
        }

        [Fact]
        public async Task The_days_of_another_group_do_not_matter()
        {
            Seed.Weekly(Seed.Group(_trainer), DayOfWeek.Monday);

            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("Monday", _group.Id) })).ShouldSucceed(English.Updated("TrainGroupDate"));
            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("5", Database.Read(db => db.TrainGroups.OrderBy(x => x.Id).Last().Id)) }))
                .ShouldBeBad(English.Of(TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix));
        }

        [Fact]
        public async Task Everything_that_does_not_go_together_is_said_at_once()
        {
            Seed.Weekly(_group, DayOfWeek.Wednesday);
            Seed.Monthly(_group, 5);
            Seed.Fixed(_group, Day);

            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("2031-03-05", _group.Id) })).ShouldBeBad(
                English.Of(TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_week_entry, "2031-03-05"),
                English.Of(TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_month_entry, "2031-03-05"),
                English.Of(TranslationKeys.Duplicate_train_group_date_found));
        }

        // Duplicates that are there already hold up any other day.
        [Fact]
        public async Task Two_of_one_day_that_are_there_already_are_found_too()
        {
            Seed.Weekly(_group, DayOfWeek.Monday);
            Seed.Weekly(_group, DayOfWeek.Monday);

            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("Friday", _group.Id) })).ShouldBeBad(English.Of(TranslationKeys.Duplicate_train_group_date_found));
        }

        // Days that name no day are passed over when a fixed one is added.
        [Fact]
        public async Task A_day_that_names_none_is_passed_over()
        {
            Database.Add(new TrainGroupDate { TrainGroupId = _group.Id, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK });

            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("2031-03-05", _group.Id) })).ShouldSucceed(English.Updated("TrainGroupDate"));
        }

        [Fact]
        public async Task A_day_of_no_group_breaks()
        {
            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("Monday", 99) })).ShouldBreak();
        }

        // Any number at all: the 40th never comes, and is not refused.
        [Fact]
        public async Task A_day_no_month_has_is_taken()
        {
            (await PostAsync<List<TrainGroupDate>>(Admin(), Url, new[] { Body("40", _group.Id) })).ShouldSucceed(English.Updated("TrainGroupDate"));
        }


        // ================================================================
        // Reading, deleting, listing
        // ================================================================

        [Fact]
        public async Task A_day_is_read_listed_and_deleted_with_its_bookings()
        {
            TrainGroupDate date = Seed.Weekly(_group, DayOfWeek.Monday);
            Seed.Recurring(date, _member, Day);

            TrainGroupDateDto dto = (await GetAsync<TrainGroupDateDto>(Admin(), $"{Url}/{date.Id}")).Value;
            dto.TrainGroupDateType.ShouldBe(TrainGroupDateTypeEnum.DAY_OF_WEEK);
            dto.RecurrenceDayOfWeek.ShouldBe(DayOfWeek.Monday);
            dto.TrainGroupId.ShouldBe(_group.Id);

            (await PostAsync<DataTableDto<TrainGroupDateDto>>(Admin(), Url + "/GetDataTable", Grid<TrainGroupDateDto>(Filter("trainGroupId", _group.Id.ToString(), DataTableFiltersEnum.equals))))
                .Value.TotalRecords.ShouldBe(1);

            (await DeleteAsync<TrainGroupDate>(Admin(), $"{Url}/{date.Id}")).ShouldSucceed(English.Deleted("TrainGroupDate"));
            Database.Read(db => db.TrainGroupParticipants.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task Changing_the_days_needs_the_permission()
        {
            TrainGroupDate date = Seed.Weekly(_group, DayOfWeek.Monday);
            Caller anybody = Caller.Of(_member, "TrainGroups_Add", "TrainGroups_Edit", "TrainGroups_Delete", "TrainGroups_View");

            (await GetAsync<TrainGroupDateDto>(anybody, $"{Url}/{date.Id}")).ShouldFail(English.NotAuthorized);
            (await PostAsync<List<TrainGroupDate>>(anybody, Url, new[] { Body("Friday", _group.Id) })).ShouldFail(English.NotAuthorized);
            (await PutAsync<TrainGroupDate>(anybody, $"{Url}/{date.Id}", Body("Friday", _group.Id, date.Id))).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<TrainGroupDate>(anybody, $"{Url}/{date.Id}")).ShouldFail(English.NotAuthorized);
        }
    }
}
