using Business.Services.CalendarService;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace Business.Tests.Services.CalendarServices
{
    /// <summary>
    /// The calendar files a booking mail carries: what a member's calendar shows, and that the file
    /// of a cancelled booking names the same entry the file of the booking made.
    /// </summary>
    public sealed class CalendarServiceTests
    {
        internal static readonly Guid UserId = new Guid("11111111-2222-3333-4444-555555555555");

        private static readonly CalendarService Service = new CalendarService();

        internal static TrainGroup Group(int startHour = 18, int startMinute = 30, int hours = 1, int minutes = 15, int seconds = 0)
        {
            return new TrainGroup
            {
                Title = "Pilates",
                StartOn = new DateTime(2025, 1, 1, startHour, startMinute, 0, DateTimeKind.Utc),
                Duration = new DateTime(2000, 1, 1, hours, minutes, seconds)
            };
        }

        private static TrainGroupDate Weekly(DayOfWeek? day)
        {
            return new TrainGroupDate { Id = 7, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_WEEK, RecurrenceDayOfWeek = day };
        }

        private static TrainGroupDate Monthly(int? day)
        {
            return new TrainGroupDate { Id = 8, TrainGroupDateType = TrainGroupDateTypeEnum.DAY_OF_MONTH, RecurrenceDayOfMonth = day };
        }

        private static TrainGroupDate Fixed(DateTime? day)
        {
            return new TrainGroupDate { Id = 9, TrainGroupDateType = TrainGroupDateTypeEnum.FIXED_DAY, FixedDay = day };
        }

        internal static TrainGroupParticipant Booking(TrainGroupDate date, DateTime? selectedDate = null, DateTime? from = null, TrainGroup? group = null, int id = 3)
        {
            return new TrainGroupParticipant
            {
                Id = id,
                TrainGroup = group ?? Group(),
                TrainGroupDate = date,
                TrainGroupDateId = date.Id,
                SelectedDate = selectedDate,
                RecurringStartOnDate = from
            };
        }

        internal static List<(string IcsContent, string FileName)> Add(params TrainGroupParticipant[] bookings)
        {
            return Service.GenerateAddIcsContents(bookings.ToList(), "gym@gym.test", "maria@gym.test", UserId);
        }

        internal static List<(string IcsContent, string FileName)> Cancel(params TrainGroupParticipant[] bookings)
        {
            return Service.GenerateCancelIcsContents(bookings.ToList(), "gym@gym.test", "maria@gym.test", UserId);
        }

        // The lines of a file by their name, the long ones put back together.
        internal static Dictionary<string, string> Read(string icsContent)
        {
            return icsContent.ReplaceLineEndings("\n").Replace("\n ", "")
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !x.StartsWith("BEGIN:", StringComparison.Ordinal) && !x.StartsWith("END:", StringComparison.Ordinal))
                .ToDictionary(x => x[..x.IndexOfAny([':', ';'])], x => x[(x.IndexOfAny([':', ';']) + 1)..]);
        }

        private static Dictionary<string, string> Added(TrainGroupParticipant booking)
        {
            return Read(Add(booking).ShouldHaveSingleItem().IcsContent);
        }

        private static Dictionary<string, string> Cancelled(TrainGroupParticipant booking)
        {
            return Read(Cancel(booking).ShouldHaveSingleItem().IcsContent);
        }


        // ================================================================
        // The whole file
        // ================================================================

        [Fact]
        public void A_booked_session_is_one_entry_at_its_time_for_as_long_as_it_lasts()
        {
            Dictionary<string, string> file = Added(Booking(Weekly(DayOfWeek.Monday), selectedDate: new DateTime(2031, 3, 3)));

            file["METHOD"].ShouldBe("PUBLISH");
            file["VERSION"].ShouldBe("2.0");
            file["SUMMARY"].ShouldBe("Pilates");
            file["DTSTART"].ShouldBe("20310303T183000");
            file["DTEND"].ShouldBe("20310303T194500");
            file["ORGANIZER"].ShouldBe("MAILTO:gym@gym.test");
            file["ATTENDEE"].ShouldBe("RSVP=TRUE:mailto:maria@gym.test");
            file["UID"].ShouldBe("tg-3--7.000-11111111222233334444555555555555");
            file.Keys.ShouldNotContain("RRULE");
        }

        [Fact]
        public void A_cancelled_session_names_the_same_entry_and_says_it_is_cancelled()
        {
            Dictionary<string, string> file = Cancelled(Booking(Weekly(DayOfWeek.Monday), selectedDate: new DateTime(2031, 3, 3)));

            file["METHOD"].ShouldBe("CANCEL");
            file["SUMMARY"].ShouldBe("Cancelled: Pilates");
            file["DTSTART"].ShouldBe("20310303T183000");
            file["ORGANIZER"].ShouldBe("MAILTO:gym@gym.test");
            file["ATTENDEE"].ShouldBe("RSVP=TRUE:mailto:maria@gym.test");
            file["UID"].ShouldBe("tg-3--7.000-11111111222233334444555555555555");
            file.Keys.ShouldNotContain("DTEND");
            file.Keys.ShouldNotContain("RRULE");
        }

        [Fact]
        public void A_file_is_stamped_with_the_time_it_was_made()
        {
            DateTime before = DateTime.UtcNow.AddSeconds(-1);

            foreach (Dictionary<string, string> file in new[] { Added(Booking(Fixed(new DateTime(2031, 3, 5)))), Cancelled(Booking(Fixed(new DateTime(2031, 3, 5)))) })
                foreach (string stamp in new[] { "CREATED", "LAST-MODIFIED" })
                    DateTime.ParseExact(file[stamp], "yyyyMMdd'T'HHmmss'Z'", null).ShouldBeInRange(before, DateTime.UtcNow);
        }


        // ================================================================
        // One-offs
        // ================================================================

        // The day that was booked, whatever the group's own days are.
        [Fact]
        public void A_one_off_is_on_the_day_that_was_booked()
        {
            DateTime day = new DateTime(2031, 3, 3, 14, 20, 0);

            foreach (TrainGroupDate date in new[] { Weekly(DayOfWeek.Friday), Weekly(null), Monthly(20), Monthly(null), Fixed(new DateTime(2031, 9, 9)), Fixed(null) })
            {
                Dictionary<string, string> file = Added(Booking(date, selectedDate: day, from: new DateTime(2031, 8, 8)));

                file["DTSTART"].ShouldBe("20310303T183000");
                file["DTEND"].ShouldBe("20310303T194500");
                file.Keys.ShouldNotContain("RRULE");
                Cancelled(Booking(date, selectedDate: day))["DTSTART"].ShouldBe("20310303T183000");
            }
        }

        [Fact]
        public void A_fixed_day_is_on_its_date()
        {
            Dictionary<string, string> file = Added(Booking(Fixed(new DateTime(2031, 3, 5, 9, 0, 0))));

            file["DTSTART"].ShouldBe("20310305T183000");
            file["DTEND"].ShouldBe("20310305T194500");
            file.Keys.ShouldNotContain("RRULE");
            Cancelled(Booking(Fixed(new DateTime(2031, 3, 5))))["DTSTART"].ShouldBe("20310305T183000");
        }


        // ================================================================
        // Recurring
        // ================================================================

        // Every weekday, for a booking that starts on a Wednesday: its own day counts.
        [Theory]
        [InlineData(DayOfWeek.Wednesday, "20310305T183000", "WE")]
        [InlineData(DayOfWeek.Thursday, "20310306T183000", "TH")]
        [InlineData(DayOfWeek.Friday, "20310307T183000", "FR")]
        [InlineData(DayOfWeek.Saturday, "20310308T183000", "SA")]
        [InlineData(DayOfWeek.Sunday, "20310309T183000", "SU")]
        [InlineData(DayOfWeek.Monday, "20310310T183000", "MO")]
        [InlineData(DayOfWeek.Tuesday, "20310311T183000", "TU")]
        public void A_weekly_booking_repeats_on_its_day_from_its_first_session_for_a_year(DayOfWeek day, string start, string rule)
        {
            TrainGroupParticipant booking = Booking(Weekly(day), from: new DateTime(2031, 3, 5, 20, 0, 0));

            Dictionary<string, string> file = Added(booking);

            file["DTSTART"].ShouldBe(start);
            file["RRULE"].ShouldBe($"FREQ=WEEKLY;UNTIL={start.Replace("2031", "2032")};BYDAY={rule}");
            Cancelled(booking)["DTSTART"].ShouldBe(start);
        }

        [Theory]
        [InlineData(5, "20310305T183000", "20320305T183000")]
        [InlineData(20, "20310320T183000", "20320320T183000")]
        [InlineData(4, "20310404T183000", "20320404T183000")]
        // The first month that has a 31st.
        [InlineData(31, "20310331T183000", "20320331T183000")]
        public void A_monthly_booking_repeats_on_its_number_from_its_first_session_for_a_year(int day, string start, string until)
        {
            TrainGroupParticipant booking = Booking(Monthly(day), from: new DateTime(2031, 3, 5));

            Dictionary<string, string> file = Added(booking);

            file["DTSTART"].ShouldBe(start);
            file["RRULE"].ShouldBe($"FREQ=MONTHLY;UNTIL={until};BYMONTHDAY={day}");
            Cancelled(booking)["DTSTART"].ShouldBe(start);
        }

        [Fact]
        public void A_year_from_the_29th_of_february_is_the_28th()
        {
            Added(Booking(Monthly(29), from: new DateTime(2032, 2, 29)))["RRULE"].ShouldBe("FREQ=MONTHLY;UNTIL=20330228T183000;BYMONTHDAY=29");
        }

        // Added from the admin screens, or before the start was kept: from today on.
        [Fact]
        public void A_booking_with_no_start_begins_with_its_next_session()
        {
            DateTime today = Midnight.Today();

            for (int i = 0; i < 7; i++)
                Added(Booking(Weekly(today.AddDays(i).DayOfWeek)))["DTSTART"].ShouldBe($"{today.AddDays(i):yyyyMMdd}T183000");

            Added(Booking(Monthly(today.Day)))["DTSTART"].ShouldBe($"{today:yyyyMMdd}T183000");
        }


        // ================================================================
        // Nothing to show
        // ================================================================

        [Fact]
        public void A_booking_that_falls_on_no_day_has_no_file()
        {
            foreach (TrainGroupDate date in new[] { Weekly(null), Monthly(null), Monthly(0), Monthly(32), Fixed(null) })
            {
                Add(Booking(date, from: new DateTime(2031, 3, 5))).ShouldBeEmpty();
                Cancel(Booking(date, from: new DateTime(2031, 3, 5))).ShouldBeEmpty();
            }
        }

        [Fact]
        public void No_bookings_have_no_files()
        {
            Add().ShouldBeEmpty();
            Cancel().ShouldBeEmpty();
        }


        // ================================================================
        // Several
        // ================================================================

        [Fact]
        public void Each_booking_has_a_file_of_its_own_in_the_order_given()
        {
            TrainGroupParticipant[] bookings =
            [
                Booking(Fixed(new DateTime(2031, 3, 5)), id: 1),
                Booking(Fixed(null), id: 2),
                Booking(Weekly(DayOfWeek.Monday), selectedDate: new DateTime(2031, 3, 3), id: 3),
                Booking(Monthly(8), from: new DateTime(2031, 3, 3), id: 1234)
            ];

            Add(bookings).Select(x => x.FileName).ShouldBe(
            [
                "add-tg-1--9_000-11111111222233334444555555555555.ics",
                "add-tg-3--7_000-11111111222233334444555555555555.ics",
                "add-tg-1234--8_000-11111111222233334444555555555555.ics"
            ]);

            Cancel(bookings).Select(x => x.FileName).ShouldBe(
            [
                "cancel-tg-1--9_000-11111111222233334444555555555555.ics",
                "cancel-tg-3--7_000-11111111222233334444555555555555.ics",
                "cancel-tg-1234--8_000-11111111222233334444555555555555.ics"
            ]);
        }

        [Fact]
        public void Two_bookings_never_share_an_entry()
        {
            TrainGroupParticipant first = Booking(Weekly(DayOfWeek.Monday), selectedDate: new DateTime(2031, 3, 3), id: 1);
            TrainGroupParticipant second = Booking(Weekly(DayOfWeek.Monday), selectedDate: new DateTime(2031, 3, 3), id: 2);
            TrainGroupParticipant otherDate = Booking(Monthly(3), selectedDate: new DateTime(2031, 3, 3), id: 1);

            new[] { first, second, otherDate }.Select(x => Added(x)["UID"]).Distinct().Count().ShouldBe(3);
        }

        // The number of the group's day is written the way the language of the request writes
        // numbers, so the same booking has one name in English and another in Greek: cancelled in a
        // language other than it was made in, it stays in the member's calendar.
        [Theory]
        [InlineData("en", "tg-3--1\\,234.000-11111111222233334444555555555555", "add-tg-3--1_234_000-11111111222233334444555555555555.ics")]
        [InlineData("el", "tg-3--1.234\\,000-11111111222233334444555555555555", "add-tg-3--1_234_000-11111111222233334444555555555555.ics")]
        public void The_entrys_name_depends_on_the_language(string language, string uid, string fileName)
        {
            TrainGroupDate date = Weekly(DayOfWeek.Monday);
            date.Id = 1234;

            using (Culture.Use(language))
            {
                (string content, string name) = Add(Booking(date, selectedDate: new DateTime(2031, 3, 3))).Single();

                Read(content)["UID"].ShouldBe(uid);
                name.ShouldBe(fileName);
            }
        }


        // ================================================================
        // Times and lengths
        // ================================================================

        [Theory]
        [InlineData(0, 45, 0, "20310305T191500")]
        [InlineData(1, 0, 0, "20310305T193000")]
        [InlineData(2, 30, 0, "20310305T210000")]
        // Seconds are not counted, and an hour and a half past midnight is the next day.
        [InlineData(1, 0, 59, "20310305T193000")]
        [InlineData(7, 0, 0, "20310306T013000")]
        [InlineData(0, 0, 0, "20310305T183000")]
        public void A_session_ends_when_its_length_is_over(int hours, int minutes, int seconds, string end)
        {
            Added(Booking(Fixed(new DateTime(2031, 3, 5)), group: Group(hours: hours, minutes: minutes, seconds: seconds)))["DTEND"].ShouldBe(end);
        }

        [Fact]
        public void The_date_the_start_was_saved_with_does_not_matter()
        {
            TrainGroup group = Group();
            group.StartOn = new DateTime(1999, 12, 31, 7, 5, 0, DateTimeKind.Utc);

            Added(Booking(Fixed(new DateTime(2031, 3, 5)), group: group))["DTSTART"].ShouldBe("20310305T070500");
        }

        // It was meant for tests to fix the date with, and nothing reads it.
        [Fact]
        public void A_calendar_told_the_date_writes_the_same_file()
        {
            List<TrainGroupParticipant> bookings = [Booking(Weekly(DayOfWeek.Monday), from: new DateTime(2031, 3, 5))];

            string content = new CalendarService(new DateTime(1999, 1, 1)).GenerateAddIcsContents(bookings, "gym@gym.test", "maria@gym.test", UserId).Single().IcsContent;

            Read(content)["DTSTART"].ShouldBe("20310310T183000");
        }

        [Fact]
        public void An_address_that_is_empty_leaves_an_empty_one()
        {
            List<TrainGroupParticipant> bookings = [Booking(Fixed(new DateTime(2031, 3, 5)))];

            Dictionary<string, string> file = Read(Service.GenerateAddIcsContents(bookings, "", "", UserId).Single().IcsContent);

            file["ORGANIZER"].ShouldBe("MAILTO:");
            file["ATTENDEE"].ShouldBe("RSVP=TRUE:mailto:");
        }
    }

    /// <summary>
    /// The hour goes through the machine's own zone, as it does on the page. The length does not.
    /// </summary>
    [Collection(ProcessState.Name)]
    public sealed class CalendarServiceZoneTests
    {
        private static Dictionary<string, string> Added(DateTime startOn)
        {
            TrainGroup group = CalendarServiceTests.Group();
            group.StartOn = startOn;

            TrainGroupDate date = new TrainGroupDate { Id = 9, TrainGroupDateType = TrainGroupDateTypeEnum.FIXED_DAY, FixedDay = new DateTime(2031, 3, 5) };

            return CalendarServiceTests.Read(CalendarServiceTests.Add(CalendarServiceTests.Booking(date, group: group)).Single().IcsContent);
        }

        [Fact]
        public void A_time_with_no_kind_is_read_as_the_machines_own()
        {
            DateTime winter = new DateTime(2025, 1, 1, 18, 30, 0);
            DateTime summer = new DateTime(2025, 7, 1, 18, 30, 0);

            Added(winter)["DTSTART"].ShouldBe("20310305T183000");

            using (LocalTimeZone.Use(LocalTimeZone.Athens))
            {
                Added(winter)["DTSTART"].ShouldBe("20310305T163000");
                Added(winter)["DTEND"].ShouldBe("20310305T174500");
                Added(summer)["DTSTART"].ShouldBe("20310305T153000");
                Added(DateTime.SpecifyKind(winter, DateTimeKind.Utc))["DTSTART"].ShouldBe("20310305T183000");
            }
        }
    }
}
