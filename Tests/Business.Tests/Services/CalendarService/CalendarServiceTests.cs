using System.Globalization;
using Business.Services.CalendarService;
using Core.Enums;
using Core.Models;
using Tests.Shared;

namespace Business.Tests.Services.CalendarServices
{
    /// <summary>
    /// The calendar files a booking mail carries: what a member's calendar shows, and that the file
    /// of a booking that is stopped or cancelled names the same entry the file of the booking made.
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

        internal static List<(string IcsContent, string FileName)> End(params TrainGroupParticipant[] bookings)
        {
            return Service.GenerateEndIcsContents(bookings.ToList(), "gym@gym.test", "maria@gym.test", UserId);
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

        private static Dictionary<string, string> Ended(TrainGroupParticipant booking)
        {
            return Read(End(booking).ShouldHaveSingleItem().IcsContent);
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
            file["UID"].ShouldBe("tg-3--7\\,000-11111111222233334444555555555555");
            file["SEQUENCE"].ShouldBe("0");
            file.Keys.ShouldNotContain("RRULE");
            file.Keys.ShouldNotContain("EXDATE");
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
            file["UID"].ShouldBe("tg-3--7\\,000-11111111222233334444555555555555");
            file.Keys.ShouldNotContain("DTEND");
            file.Keys.ShouldNotContain("RRULE");
        }

        [Fact]
        public void A_file_is_stamped_with_the_time_it_was_made()
        {
            DateTime before = DateTime.UtcNow.AddSeconds(-1);

            foreach (Dictionary<string, string> file in new[] { Added(Booking(Fixed(new DateTime(2031, 3, 5)))), Cancelled(Booking(Fixed(new DateTime(2031, 3, 5)))), Ended(Stopped(new DateTime(2031, 4, 2))) })
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
        // Stopped from a later date
        // ================================================================

        // Every Wednesday from the 5th of March on, up to a date that is not theirs any more.
        private static TrainGroupParticipant Stopped(DateTime end, params DateTime[] skipped)
        {
            TrainGroupParticipant booking = Booking(Weekly(DayOfWeek.Wednesday), from: new DateTime(2031, 3, 5));
            booking.RecurringEndOnDate = end;

            foreach (DateTime day in skipped)
                booking.TrainGroupParticipantUnavailableDates.Add(new TrainGroupParticipantUnavailableDate { UnavailableDate = day });

            return booking;
        }

        [Fact]
        public void A_booking_stopped_from_a_later_date_is_written_again_up_to_its_last_session()
        {
            TrainGroupParticipant booking = Stopped(new DateTime(2031, 4, 2));

            Dictionary<string, string> file = Ended(booking);

            file["METHOD"].ShouldBe("PUBLISH");
            file["VERSION"].ShouldBe("2.0");
            file["SUMMARY"].ShouldBe("Pilates");
            file["DTSTART"].ShouldBe("20310305T183000");
            file["DTEND"].ShouldBe("20310305T194500");
            file["RRULE"].ShouldBe("FREQ=WEEKLY;UNTIL=20310326T183000;BYDAY=WE");
            file["ORGANIZER"].ShouldBe("MAILTO:gym@gym.test");
            file["ATTENDEE"].ShouldBe("RSVP=TRUE:mailto:maria@gym.test");
            file.Keys.ShouldNotContain("EXDATE");
        }

        // The entry it was booked with, which is not taken out.
        [Fact]
        public void It_is_the_same_entry_and_is_not_cancelled()
        {
            TrainGroupParticipant booking = Stopped(new DateTime(2031, 4, 2));

            Ended(booking)["UID"].ShouldBe(Added(booking)["UID"]);
            End(booking).Single().FileName.ShouldBe(Add(booking).Single().FileName);
            Cancel(booking).ShouldBeEmpty();
        }

        // The date it ends on is the first that is not theirs, whatever the hour.
        [Theory]
        [InlineData("2031-03-06", "20310305")]
        [InlineData("2031-03-12", "20310305")]
        [InlineData("2031-03-12 23:59", "20310305")]
        [InlineData("2031-03-13", "20310312")]
        [InlineData("2031-04-02", "20310326")]
        [InlineData("2033-03-02", "20330223")]
        public void Its_last_session_is_the_one_before_the_end(string end, string last)
        {
            TrainGroupParticipant booking = Stopped(DateTime.Parse(end, CultureInfo.InvariantCulture));

            Ended(booking)["RRULE"].ShouldBe($"FREQ=WEEKLY;UNTIL={last}T183000;BYDAY=WE");
            Cancel(booking).ShouldBeEmpty();
        }

        // Nothing was kept, so nothing stays.
        [Theory]
        [InlineData("2031-03-05")]
        [InlineData("2031-03-05 23:59")]
        [InlineData("2031-02-01")]
        public void A_booking_stopped_before_its_first_session_is_cancelled(string end)
        {
            TrainGroupParticipant booking = Stopped(DateTime.Parse(end, CultureInfo.InvariantCulture));

            End(booking).ShouldBeEmpty();
            Cancelled(booking)["METHOD"].ShouldBe("CANCEL");
        }

        // Written again without them the series would bring them back.
        [Fact]
        public void The_dates_that_were_skipped_stay_out()
        {
            Dictionary<string, string> file = Ended(Stopped(new DateTime(2031, 4, 16),
                new DateTime(2031, 3, 19, 5, 0, 0), new DateTime(2031, 3, 5), new DateTime(2031, 3, 19)));

            file["DTSTART"].ShouldBe("20310305T183000");
            file["EXDATE"].ShouldBe("20310305T183000,20310319T183000");
            file["RRULE"].ShouldBe("FREQ=WEEKLY;UNTIL=20310409T183000;BYDAY=WE");
        }

        // The last session is one that is kept; what is skipped after it needs no mention.
        [Fact]
        public void The_last_session_is_not_a_skipped_one()
        {
            Dictionary<string, string> file = Ended(Stopped(new DateTime(2031, 4, 2),
                new DateTime(2031, 3, 12), new DateTime(2031, 3, 26), new DateTime(2031, 4, 2), new DateTime(2031, 2, 26)));

            file["RRULE"].ShouldBe("FREQ=WEEKLY;UNTIL=20310319T183000;BYDAY=WE");
            file["EXDATE"].ShouldBe("20310312T183000");
        }

        [Fact]
        public void With_every_session_skipped_it_is_cancelled()
        {
            TrainGroupParticipant booking = Stopped(new DateTime(2031, 3, 19), new DateTime(2031, 3, 5), new DateTime(2031, 3, 12));

            End(booking).ShouldBeEmpty();
            Cancelled(booking)["DTSTART"].ShouldBe("20310305T183000");
        }

        [Fact]
        public void A_monthly_booking_ends_the_same_way()
        {
            TrainGroupParticipant booking = Booking(Monthly(31), from: new DateTime(2031, 3, 5));
            booking.RecurringEndOnDate = new DateTime(2031, 8, 31);

            Ended(booking)["DTSTART"].ShouldBe("20310331T183000");
            Ended(booking)["RRULE"].ShouldBe("FREQ=MONTHLY;UNTIL=20310731T183000;BYMONTHDAY=31");
            Cancel(booking).ShouldBeEmpty();
        }

        // From before the start was kept. The day it was made counts, whatever the hour.
        [Theory]
        [InlineData(6, "2025-04-02", "20250312", "20250326")]
        [InlineData(5, "2025-04-02", "20250305", "20250326")]
        [InlineData(5, "2025-03-06", "20250305", "20250305")]
        public void One_with_no_start_begins_the_day_it_was_made(int dayOfMarch, string end, string first, string last)
        {
            TrainGroupParticipant booking = Booking(Weekly(DayOfWeek.Wednesday));
            booking.CreatedOn = new DateTime(2025, 3, dayOfMarch, 15, 0, 0);
            booking.RecurringEndOnDate = DateTime.Parse(end, CultureInfo.InvariantCulture);

            Ended(booking)["DTSTART"].ShouldBe(first + "T183000");
            Ended(booking)["RRULE"].ShouldBe($"FREQ=WEEKLY;UNTIL={last}T183000;BYDAY=WE");
        }

        [Fact]
        public void A_booking_that_goes_on_does_not_end()
        {
            TrainGroupParticipant booking = Booking(Weekly(DayOfWeek.Wednesday), from: new DateTime(2031, 3, 5));

            End(booking).ShouldBeEmpty();
            Cancelled(booking)["DTSTART"].ShouldBe("20310305T183000");
        }

        // There is nothing of them to keep, whatever else the row says.
        [Fact]
        public void A_single_session_is_cancelled_and_never_ends()
        {
            TrainGroupParticipant oneOff = Booking(Weekly(DayOfWeek.Wednesday), selectedDate: new DateTime(2031, 3, 5));
            TrainGroupParticipant fixedDay = Booking(Fixed(new DateTime(2031, 3, 5)));

            foreach (TrainGroupParticipant booking in new[] { oneOff, fixedDay })
            {
                booking.RecurringEndOnDate = new DateTime(2031, 4, 2);

                End(booking).ShouldBeEmpty();
                Cancelled(booking)["DTSTART"].ShouldBe("20310305T183000");
            }
        }

        // A weekday that names a day of the month and no weekday covers a date, and never comes up.
        [Fact]
        public void One_that_falls_on_no_day_has_no_file()
        {
            TrainGroupDate date = Weekly(null);
            date.RecurrenceDayOfMonth = 5;
            TrainGroupParticipant booking = Booking(date, from: new DateTime(2031, 3, 5));
            booking.RecurringEndOnDate = new DateTime(2031, 4, 2);

            End(booking).ShouldBeEmpty();
            Cancel(booking).ShouldBeEmpty();
        }

        // A calendar takes a change whose number is higher than that of the entry it has, and an entry
        // may be changed more than once.
        [Fact]
        public void A_change_has_a_higher_number_than_the_entry_and_than_any_before_it()
        {
            int since2020 = (int)(DateTime.UtcNow - new DateTime(2020, 1, 1)).TotalMinutes;

            foreach (Dictionary<string, string> file in new[] { Ended(Stopped(new DateTime(2031, 4, 2))), Cancelled(Stopped(new DateTime(2031, 3, 5))), Cancelled(Booking(Fixed(new DateTime(2031, 3, 5)))) })
                int.Parse(file["SEQUENCE"], CultureInfo.InvariantCulture).ShouldBeInRange(since2020, since2020 + 1);

            Added(Stopped(new DateTime(2031, 4, 2)))["SEQUENCE"].ShouldBe("0");
        }

        [Fact]
        public void Of_several_each_is_in_one_file_or_the_other()
        {
            TrainGroupParticipant[] bookings =
            [
                Booking(Fixed(new DateTime(2031, 3, 5)), id: 1),
                Stopped(new DateTime(2031, 4, 2)),
                Booking(Fixed(null), id: 2),
                Stopped(new DateTime(2031, 3, 5))
            ];
            bookings[1].Id = 11;
            bookings[3].Id = 12;

            End(bookings).Select(x => x.FileName).ShouldBe(["add-tg-11--7_000-11111111222233334444555555555555.ics"]);
            Cancel(bookings).Select(x => x.FileName).ShouldBe(
            [
                "cancel-tg-1--9_000-11111111222233334444555555555555.ics",
                "cancel-tg-12--7_000-11111111222233334444555555555555.ics"
            ]);
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
            End().ShouldBeEmpty();
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

        // The languages the app is in.
        public static TheoryData<string> Languages
        {
            get
            {
                return ["ar", "cs", "da", "de", "el", "en", "es", "fi", "fr", "it", "ja", "ko", "nl", "no", "pl", "pt", "ru", "sq", "sv", "tr", "zh"];
            }
        }

        // It used to be written the way the language of the request writes numbers, so what was booked in
        // Greek was not found again in English. It is the Greek way: nearly every entry that is out there
        // was made in Greek, and is found by it.
        [Theory]
        [MemberData(nameof(Languages))]
        public void An_entry_has_one_name_in_every_language(string language)
        {
            TrainGroupDate date = Weekly(DayOfWeek.Wednesday);
            date.Id = 1234;
            TrainGroupParticipant single = Booking(date, selectedDate: new DateTime(2031, 3, 5), id: 5678);
            TrainGroupParticipant stopped = Booking(date, from: new DateTime(2031, 3, 5), id: 5678);
            stopped.RecurringEndOnDate = new DateTime(2031, 4, 2);

            using (Culture.Use(language))
            {
                foreach (Dictionary<string, string> file in new[] { Added(single), Cancelled(single), Ended(stopped) })
                    file["UID"].ShouldBe("tg-5678--1.234\\,000-11111111222233334444555555555555");

                Add(single).Single().FileName.ShouldBe("add-tg-5678--1_234_000-11111111222233334444555555555555.ics");
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
