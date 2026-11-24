using API.Tests.Host;
using Core.Models;
using Core.Translations;
using Tests.Shared;

namespace API.Tests.Controllers.TrainGroupParticipants
{
    /// <summary>
    /// What the admin screens do to a group's members: add, change and take out.
    /// </summary>
    public sealed class AdminChangesTests : ApiTest
    {
        private const string Url = "api/TrainGroupParticipants";

        // A Wednesday, the 5th.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _member;
        private readonly User _admin;
        private readonly TrainGroup _group;
        private readonly TrainGroupDate _wednesday;

        public AdminChangesTests(ApiFixture api) : base(api)
        {
            _member = Seed.User();
            _admin = Seed.User(x => { x.FirstName = "Desk"; x.LastName = "Person"; });
            _group = Seed.Group(Seed.User());
            _wednesday = Seed.Weekly(_group, DayOfWeek.Wednesday);
        }

        private Caller Admin
        {
            get { return Caller.Of(_admin, "TrainGroupParticipants_View", "TrainGroupParticipants_Add", "TrainGroupParticipants_Edit", "TrainGroupParticipants_Delete"); }
        }

        private TrainGroupParticipant Read(int id)
        {
            return Database.Read(db => db.TrainGroupParticipants.Single(x => x.Id == id));
        }


        // ================================================================
        // Taking out
        // ================================================================

        private Task<ApiResult<TrainGroupParticipant>> DeleteAsync(Caller caller, object id)
        {
            return DeleteAsync<TrainGroupParticipant>(caller, $"{Url}/{id}");
        }

        [Fact]
        public async Task Staff_take_a_member_out_and_the_row_stays()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);
            DateTime before = DateTime.UtcNow;

            ApiResult<TrainGroupParticipant> result = await DeleteAsync(Admin, booking.Id);

            result.ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
            TrainGroupParticipant kept = Read(booking.Id);
            kept.SelectedDate.ShouldBe(Day);
            kept.RecurringEndOnDate.ShouldBeNull();
            kept.RemovedOn.ShouldNotBeNull().ShouldBeInRange(before, DateTime.UtcNow);
            kept.RemovedBy_Id.ShouldBe(_admin.Id.ToString());
            kept.RemovedBy_FullName.ShouldBe("Desk Person");
        }

        // The group it belongs to would bring the trainer's account along.
        [Fact]
        public async Task The_answer_is_three_values_and_no_more()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            ApiResult<TrainGroupParticipant> result = await DeleteAsync(Admin, booking.Id);

            result.Value.Id.ShouldBe(booking.Id);
            result.Value.TrainGroupId.ShouldBe(_group.Id);
            result.Value.UserId.ShouldBe(_member.Id);
            result.Value.TrainGroup.ShouldBeNull();
            result.Value.TrainGroupDate.ShouldBeNull();
            result.Value.SelectedDate.ShouldBeNull();
            result.Value.RemovedOn.ShouldBeNull();
        }

        [Fact]
        public async Task Taking_out_needs_the_delete_permission_and_being_staff()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            (await DeleteAsync(Caller.Of(_admin), booking.Id)).ShouldFail(English.NotAuthorized);
            (await DeleteAsync(Caller.Of(_admin, "TrainGroupParticipants_View"), booking.Id)).ShouldFail(English.NotAuthorized);
            (await DeleteAsync(Caller.Of(_admin, "TrainGroupParticipants_Delete"), booking.Id)).ShouldFail(English.NotAuthorized);
            // Not even their own.
            (await DeleteAsync(Caller.Of(_member, "TrainGroupParticipants_Delete"), booking.Id)).ShouldFail(English.NotAuthorized);
            Read(booking.Id).RemovedOn.ShouldBeNull();

            (await DeleteAsync(Caller.Of(_admin, "TrainGroupParticipants_View", "TrainGroupParticipants_Delete"), booking.Id)).ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
        }

        [Theory]
        [InlineData("99")]
        [InlineData("x")]
        [InlineData("1.5")]
        [InlineData("99999999999")]
        public async Task A_booking_that_is_not_there_is_not_found(string id)
        {
            (await DeleteAsync(Admin, id)).ShouldFail(English.NotFound("TrainGroupParticipant"));
        }

        // It ends today, unless it ends sooner.
        [Fact]
        public async Task A_recurring_booking_ends_today()
        {
            DateTime today = Midnight.Today();

            async Task<DateTime?> EndAfterDeleteAsync(DateTime? end)
            {
                TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, today.AddDays(-70), end);
                (await DeleteAsync(Admin, booking.Id)).ShouldSucceed(English.Of(TranslationKeys.Booking_removed));
                Read(booking.Id).RemovedOn.ShouldNotBeNull();

                return Read(booking.Id).RecurringEndOnDate;
            }

            (await EndAfterDeleteAsync(null)).ShouldBe(today);
            (await EndAfterDeleteAsync(today.AddDays(1))).ShouldBe(today);
            (await EndAfterDeleteAsync(today.AddDays(70).AddHours(5))).ShouldBe(today);
            (await EndAfterDeleteAsync(today)).ShouldBe(today);
            // A time on today is not after today.
            (await EndAfterDeleteAsync(today.AddHours(5))).ShouldBe(today.AddHours(5));
            (await EndAfterDeleteAsync(today.AddDays(-1))).ShouldBe(today.AddDays(-1));
        }

        [Fact]
        public async Task A_fixed_day_is_marked_and_given_no_end()
        {
            TrainGroupParticipant booking = Seed.Participant(Seed.Fixed(_group, Day), _member);

            (await DeleteAsync(Admin, booking.Id)).ShouldSucceed(English.Of(TranslationKeys.Booking_removed));

            Read(booking.Id).RemovedOn.ShouldNotBeNull();
            Read(booking.Id).RecurringEndOnDate.ShouldBeNull();
        }

        // Neither how late it is nor whether it was removed before is asked.
        [Fact]
        public async Task It_is_never_too_late_and_the_last_one_to_do_it_is_named()
        {
            TrainGroupParticipant past = Seed.OneOff(_wednesday, _member, new DateTime(2020, 1, 1), x => { x.RemovedOn = new DateTime(2019, 12, 1); x.RemovedBy_FullName = "before"; });

            (await DeleteAsync(Admin, past.Id)).ShouldSucceed(English.Of(TranslationKeys.Booking_removed));

            Read(past.Id).RemovedBy_FullName.ShouldBe("Desk Person");
        }

        [Fact]
        public async Task Somebody_with_no_id_or_no_account_is_not_named()
        {
            TrainGroupParticipant first = Seed.OneOff(_wednesday, _member, Day);
            TrainGroupParticipant second = Seed.OneOff(_wednesday, _member, Day);

            await DeleteAsync(Caller.WithNoId("TrainGroupParticipants_View", "TrainGroupParticipants_Delete"), first.Id);
            Guid gone = Guid.NewGuid();
            await DeleteAsync(Caller.Of(new User { Id = gone }, "TrainGroupParticipants_View", "TrainGroupParticipants_Delete"), second.Id);

            Read(first.Id).RemovedBy_Id.ShouldBe("");
            Read(first.Id).RemovedBy_FullName.ShouldBe("");
            Read(second.Id).RemovedBy_Id.ShouldBe(gone.ToString());
            Read(second.Id).RemovedBy_FullName.ShouldBe("");
        }

        [Fact]
        public async Task Nobody_is_mailed()
        {
            await DeleteAsync(Admin, Seed.OneOff(_wednesday, _member, Day).Id);

            Emails.Lines.ShouldBeEmpty();
        }


        // ================================================================
        // Adding
        // ================================================================

        private static object Row(TrainGroupDate date, User user, DateTime? day = null, DateTime? start = null)
        {
            return new { trainGroupId = date.TrainGroupId, trainGroupDateId = date.Id, userId = user.Id, selectedDate = day, recurringStartOnDate = start };
        }

        private Task<ApiResult<List<TrainGroupParticipant>>> AddAsync(Caller caller, params object[] rows)
        {
            return PostAsync<List<TrainGroupParticipant>>(caller, Url, rows);
        }

        private List<TrainGroupParticipant> All()
        {
            return Database.Read(db => db.TrainGroupParticipants.OrderBy(x => x.Id).ToList());
        }

        [Fact]
        public async Task Adding_needs_the_add_permission()
        {
            (await AddAsync(Caller.Of(_admin, "TrainGroupParticipants_View", "TrainGroupParticipants_Edit", "TrainGroupParticipants_Delete"), Row(_wednesday, _member))).ShouldFail(English.NotAuthorized);

            All().ShouldBeEmpty();
            (await AddAsync(Caller.Of(_admin, "TrainGroupParticipants_Add"), Row(_wednesday, _member))).ShouldSucceed(English.Updated("TrainGroupParticipant"));
            All().Count.ShouldBe(1);
        }

        // Left out, it starts today.
        [Fact]
        public async Task A_recurring_booking_starts_today_or_when_it_is_told_to()
        {
            DateTime today = Midnight.Today();

            (await AddAsync(Admin, Row(_wednesday, _member), Row(_wednesday, _admin, start: Day))).ShouldSucceed(English.Updated("TrainGroupParticipant"));

            List<TrainGroupParticipant> added = All();
            added[0].RecurringStartOnDate.ShouldBe(today);
            added[0].SelectedDate.ShouldBeNull();
            added[0].CreatedBy_Id.ShouldBe(_admin.Id.ToString());
            added[0].CreatedBy_FullName.ShouldBe("");
            added[1].RecurringStartOnDate.ShouldBe(Day);
        }

        [Fact]
        public async Task A_one_off_has_no_start()
        {
            (await AddAsync(Admin, Row(_wednesday, _member, Day))).ShouldSucceed(English.Updated("TrainGroupParticipant"));

            TrainGroupParticipant added = All().Single();
            added.SelectedDate.ShouldBe(Day);
            added.RecurringStartOnDate.ShouldBeNull();
            added.UserId.ShouldBe(_member.Id);
            added.TrainGroupId.ShouldBe(_group.Id);
            added.TrainGroupDateId.ShouldBe(_wednesday.Id);
        }

        // A one-off that brings a start keeps it.
        [Fact]
        public async Task A_start_that_is_sent_is_kept_either_way()
        {
            await AddAsync(Admin, Row(_wednesday, _member, Day, Day.AddDays(1)));

            All().Single().RecurringStartOnDate.ShouldBe(Day.AddDays(1));
        }

        [Fact]
        public async Task A_caller_with_no_id_is_not_written_down()
        {
            await AddAsync(Caller.WithNoId("TrainGroupParticipants_Add"), Row(_wednesday, _member));

            All().Single().CreatedBy_Id.ShouldBe("");
        }

        [Fact]
        public async Task The_member_has_to_be_named()
        {
            (await PostAsync<List<TrainGroupParticipant>>(Admin, Url, new[] { new { trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = "" } })).ShouldBeBad(English.Required("UserId"));
        }

        // The check before it maps the row, and that does not get past the id.
        [Fact]
        public async Task A_member_who_is_no_id_breaks_it()
        {
            (await PostAsync<List<TrainGroupParticipant>>(Admin, Url, new[] { new { trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = "x" } })).ShouldBreak();
        }

        [Fact]
        public async Task A_member_or_a_date_that_is_not_there_breaks_it()
        {
            (await PostAsync<List<TrainGroupParticipant>>(Admin, Url, new[] { new { trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = Guid.NewGuid() } })).ShouldBreak();
            (await PostAsync<List<TrainGroupParticipant>>(Admin, Url, new[] { new { trainGroupId = _group.Id, trainGroupDateId = 99, userId = _member.Id } })).ShouldBreak();
            (await PostAsync<List<TrainGroupParticipant>>(Admin, Url, new[] { new { trainGroupId = 99, trainGroupDateId = _wednesday.Id, userId = _member.Id } })).ShouldBreak();

            All().ShouldBeEmpty();
        }

        // Neither the group's size, nor what else the member does at that hour, nor whether the gym is
        // closed that day.
        [Fact]
        public async Task Staff_may_fill_a_group_past_its_size()
        {
            Database.Change(db => db.TrainGroups.Single().MaxParticipants = 0);
            Seed.Cancelled(_group, Day);

            (await AddAsync(Admin, Row(_wednesday, _member, Day))).ShouldSucceed(English.Updated("TrainGroupParticipant"));
        }

        [Fact]
        public async Task One_row_that_is_refused_refuses_them_all()
        {
            (await AddAsync(Admin, Row(_wednesday, _member, Day), Row(_wednesday, _admin, Day.AddDays(1)))).ShouldBeBad(
                English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));

            All().ShouldBeEmpty();
        }

        // The rows are held against those that are there already, not against each other.
        [Fact]
        public async Task The_same_member_twice_in_one_request_is_not_noticed()
        {
            (await AddAsync(Admin, Row(_wednesday, _member), Row(_wednesday, _member))).ShouldSucceed(English.Updated("TrainGroupParticipant"));

            All().Count.ShouldBe(2);
        }

        [Fact]
        public async Task Nothing_to_add_is_an_error()
        {
            (await AddAsync(Admin)).ShouldFail(English.Of(TranslationKeys.An_error_occurred_while_creating_the_entity));
        }


        // ================================================================
        // Which day a one-off may be
        // ================================================================

        // Any of the group's days will do, whichever of them the row is about.
        [Theory]
        [InlineData(0, true)]
        [InlineData(7, true)]
        [InlineData(-7, true)]
        [InlineData(1, false)]
        // The 20th, a Thursday.
        [InlineData(15, true)]
        // The 20th of April, a Sunday.
        [InlineData(46, true)]
        [InlineData(16, false)]
        public async Task A_one_off_is_on_one_of_the_groups_days(int days, bool isAllowed)
        {
            Seed.Monthly(_group, 20);

            ApiResult<List<TrainGroupParticipant>> result = await AddAsync(Admin, Row(_wednesday, _member, Day.AddDays(days)));

            if (isAllowed)
                result.ShouldSucceed(English.Updated("TrainGroupParticipant"));
            else
                result.ShouldBeBad(English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));
        }

        [Fact]
        public async Task Another_groups_days_do_not_count()
        {
            TrainGroup other = Seed.Group(_admin);
            Seed.Weekly(other, DayOfWeek.Thursday);

            (await AddAsync(Admin, Row(_wednesday, _member, Day.AddDays(1)))).ShouldBeBad(English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));
        }

        [Fact]
        public async Task A_fixed_day_takes_no_one_offs()
        {
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day.AddDays(1));

            (await AddAsync(Admin, Row(fixedDay, _member, Day.AddDays(1)))).ShouldBeBad(English.Of(TranslationKeys.Fixed_date_doesnt_allow_one_off_participants));
            // It is booked with no date of its own.
            (await AddAsync(Admin, Row(fixedDay, _member))).ShouldSucceed(English.Updated("TrainGroupParticipant"));
        }

        // To the moment: the same day at another hour is neither the fixed day nor one of the others.
        [Fact]
        public async Task A_fixed_day_is_its_exact_moment()
        {
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day.AddDays(1).AddHours(9));

            (await AddAsync(Admin, Row(fixedDay, _member, Day.AddDays(1)))).ShouldBeBad(English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));
            (await AddAsync(Admin, Row(fixedDay, _member, Day.AddDays(1).AddHours(9)))).ShouldBeBad(English.Of(TranslationKeys.Fixed_date_doesnt_allow_one_off_participants));
        }

        // The date is a Wednesday of the group and its fixed day as well.
        [Fact]
        public async Task A_fixed_day_that_falls_on_a_weekday_of_the_group_still_takes_none()
        {
            Seed.Fixed(_group, Day);

            (await AddAsync(Admin, Row(_wednesday, _member, Day))).ShouldBeBad(English.Of(TranslationKeys.Fixed_date_doesnt_allow_one_off_participants));
        }

        [Fact]
        public async Task Both_reasons_are_given()
        {
            TrainGroupParticipant there = Seed.OneOff(_wednesday, _member, Day.AddDays(1));

            (await AddAsync(Admin, Row(_wednesday, _member, Day.AddDays(1)))).ShouldBeBad(
                English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates),
                English.Of(TranslationKeys.Participant_already_joined));

            All().Single().Id.ShouldBe(there.Id);
        }


        // ================================================================
        // Twice
        // ================================================================

        // What is there already, and whether a one-off on that day and a booking for every week are
        // refused beside it.
        [Theory]
        [InlineData("nothing", false, false)]
        [InlineData("one-off", true, false)]
        [InlineData("one-off given up", false, false)]
        [InlineData("one-off next week", false, false)]
        [InlineData("every week", true, true)]
        [InlineData("every week, that day skipped", true, true)]
        [InlineData("every week from the week after", false, true)]
        [InlineData("every week until that day", false, true)]
        [InlineData("every week until today", false, false)]
        [InlineData("every week until tomorrow", false, true)]
        [InlineData("every week, marked removed", true, true)]
        public async Task A_member_is_not_added_twice(string there, bool isOneOffRefused, bool isRecurringRefused)
        {
            DateTime today = Midnight.Today();

            switch (there)
            {
                case "one-off": Seed.OneOff(_wednesday, _member, Day); break;
                case "one-off given up": Seed.OneOff(_wednesday, _member, Day, x => x.RemovedOn = today); break;
                case "one-off next week": Seed.OneOff(_wednesday, _member, Day.AddDays(7)); break;
                case "every week": Seed.Recurring(_wednesday, _member, Day.AddDays(-700)); break;
                case "every week, that day skipped": Seed.Skip(Seed.Recurring(_wednesday, _member, Day.AddDays(-700)), Day); break;
                case "every week from the week after": Seed.Recurring(_wednesday, _member, Day.AddDays(1)); break;
                case "every week until that day": Seed.Recurring(_wednesday, _member, Day.AddDays(-700), Day); break;
                case "every week until today": Seed.Recurring(_wednesday, _member, today.AddDays(-70), today); break;
                case "every week until tomorrow": Seed.Recurring(_wednesday, _member, today.AddDays(-70), today.AddDays(1)); break;
                case "every week, marked removed": Seed.Recurring(_wednesday, _member, Day.AddDays(-700), change: x => x.RemovedOn = today); break;
            }

            ApiResult<List<TrainGroupParticipant>> recurring = await AddAsync(Admin, Row(_wednesday, _member));
            if (isRecurringRefused)
                recurring.ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
            else
                recurring.ShouldSucceed(English.Updated("TrainGroupParticipant"));

            // Whatever was just added is taken away again.
            Database.Execute("DELETE FROM TrainGroupParticipants WHERE CreatedBy_Id <> ''");

            ApiResult<List<TrainGroupParticipant>> oneOff = await AddAsync(Admin, Row(_wednesday, _member, Day));
            if (isOneOffRefused)
                oneOff.ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
            else
                oneOff.ShouldSucceed(English.Updated("TrainGroupParticipant"));
        }

        [Fact]
        public async Task Another_member_another_day_and_another_group_are_not_the_same_booking()
        {
            TrainGroupDate friday = Seed.Weekly(_group, DayOfWeek.Friday);
            TrainGroupDate elsewhere = Seed.Weekly(Seed.Group(_admin), DayOfWeek.Wednesday);
            Seed.Recurring(_wednesday, _admin, Day.AddDays(-70));
            Seed.Recurring(friday, _member, Day.AddDays(-70));
            Seed.Recurring(elsewhere, _member, Day.AddDays(-70));

            (await AddAsync(Admin, Row(_wednesday, _member))).ShouldSucceed(English.Updated("TrainGroupParticipant"));
        }

        // It is booked with no date of its own, and once given up it can be booked again.
        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task A_fixed_day_is_not_booked_twice(bool isGivenUp, bool isRefused)
        {
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day.AddDays(1));
            Seed.Participant(fixedDay, _member, x => x.RemovedOn = isGivenUp ? Day : null);

            ApiResult<List<TrainGroupParticipant>> result = await AddAsync(Admin, Row(fixedDay, _member));

            if (isRefused)
                result.ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
            else
                result.ShouldSucceed(English.Updated("TrainGroupParticipant"));
        }


        // ================================================================
        // Changing
        // ================================================================

        private Task<ApiResult<TrainGroupParticipant>> ChangeAsync(Caller caller, object id, object body)
        {
            return PutAsync<TrainGroupParticipant>(caller, $"{Url}/{id}", body);
        }

        private static object Body(TrainGroupParticipant booking, User user, TrainGroupDate date, DateTime? day)
        {
            return new { id = booking.Id, trainGroupId = date.TrainGroupId, trainGroupDateId = date.Id, userId = user.Id, selectedDate = day };
        }

        // Who, which day, and which of the group's days. What else the row says stays.
        [Fact]
        public async Task A_change_is_to_the_member_the_date_and_the_day_alone()
        {
            TrainGroupDate friday = Seed.Weekly(_group, DayOfWeek.Friday);
            TrainGroup otherGroup = Seed.Group(_admin);
            Seed.Weekly(otherGroup, DayOfWeek.Friday);
            TrainGroupParticipant booking = Seed.Recurring(_wednesday, _member, Day.AddDays(-70), Day.AddDays(70), x =>
            {
                x.CreatedBy_Id = "maker";
                x.CreatedOn = new DateTime(2020, 1, 2);
                x.RemovedOn = new DateTime(2021, 1, 2);
                x.RemovedBy_FullName = "remover";
            });

            ApiResult<TrainGroupParticipant> result = await ChangeAsync(Admin, booking.Id, new
            {
                id = booking.Id,
                trainGroupId = otherGroup.Id,
                trainGroupDateId = friday.Id,
                userId = _admin.Id,
                selectedDate = Day.AddDays(2),
                recurringStartOnDate = Day
            });

            result.ShouldSucceed(English.Updated("TrainGroupParticipant"));
            result.Value.UserId.ShouldBe(_admin.Id);
            result.Value.SelectedDate.ShouldBe(Day.AddDays(2));
            result.Value.TrainGroupDateId.ShouldBe(friday.Id);

            TrainGroupParticipant changed = Read(booking.Id);
            changed.UserId.ShouldBe(_admin.Id);
            changed.SelectedDate.ShouldBe(Day.AddDays(2));
            changed.TrainGroupDateId.ShouldBe(friday.Id);
            changed.TrainGroupId.ShouldBe(_group.Id);
            changed.RecurringStartOnDate.ShouldBe(Day.AddDays(-70));
            changed.RecurringEndOnDate.ShouldBe(Day.AddDays(70));
            changed.CreatedBy_Id.ShouldBe("maker");
            changed.CreatedOn.ShouldBe(new DateTime(2020, 1, 2));
            changed.RemovedOn.ShouldBe(new DateTime(2021, 1, 2));
            changed.RemovedBy_FullName.ShouldBe("remover");
        }

        [Fact]
        public async Task Leaving_the_date_out_takes_it_away()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            (await ChangeAsync(Admin, booking.Id, Body(booking, _member, _wednesday, null))).ShouldSucceed(English.Updated("TrainGroupParticipant"));

            Read(booking.Id).SelectedDate.ShouldBeNull();
        }

        [Fact]
        public async Task Changing_needs_the_edit_permission()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);
            Caller caller = Caller.Of(_admin, "TrainGroupParticipants_View", "TrainGroupParticipants_Add", "TrainGroupParticipants_Delete");

            (await ChangeAsync(caller, booking.Id, Body(booking, _member, _wednesday, null))).ShouldFail(English.NotAuthorized);

            Read(booking.Id).SelectedDate.ShouldBe(Day);
        }

        [Fact]
        public async Task A_change_is_held_to_the_same_rules()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);
            Seed.OneOff(_wednesday, _member, Day.AddDays(7));
            Seed.Recurring(_wednesday, _admin, Day.AddDays(-70));
            TrainGroupDate fixedDay = Seed.Fixed(_group, Day.AddDays(1));

            (await ChangeAsync(Admin, booking.Id, Body(booking, _member, _wednesday, Day.AddDays(2)))).ShouldBeBad(English.Of(TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates));
            (await ChangeAsync(Admin, booking.Id, Body(booking, _member, fixedDay, Day.AddDays(1)))).ShouldBeBad(English.Of(TranslationKeys.Fixed_date_doesnt_allow_one_off_participants));
            (await ChangeAsync(Admin, booking.Id, Body(booking, _member, _wednesday, Day.AddDays(7)))).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
            (await ChangeAsync(Admin, booking.Id, Body(booking, _admin, _wednesday, null))).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));

            Read(booking.Id).SelectedDate.ShouldBe(Day);
            Read(booking.Id).UserId.ShouldBe(_member.Id);
        }

        // The row itself is not in its own way - by the id in the body, not the one in the address.
        [Fact]
        public async Task A_row_is_not_a_duplicate_of_itself()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            (await ChangeAsync(Admin, booking.Id, Body(booking, _member, _wednesday, Day))).ShouldSucceed(English.Updated("TrainGroupParticipant"));
            (await ChangeAsync(Admin, booking.Id, new { id = 0, trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = _member.Id, selectedDate = Day })).ShouldBeBad(English.Of(TranslationKeys.Participant_already_joined));
        }

        [Fact]
        public async Task The_address_says_which_row_is_changed()
        {
            TrainGroupParticipant first = Seed.OneOff(_wednesday, _member, Day);
            TrainGroupParticipant second = Seed.OneOff(_wednesday, _member, Day.AddDays(7));

            // Excused for being the first, and written on the second.
            (await ChangeAsync(Admin, second.Id, Body(first, _member, _wednesday, Day))).ShouldSucceed(English.Updated("TrainGroupParticipant"));

            Read(second.Id).SelectedDate.ShouldBe(Day);
        }

        [Fact]
        public async Task A_row_that_is_not_there_is_not_found()
        {
            (await ChangeAsync(Admin, 99, new { id = 99, trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = _member.Id })).ShouldFail(English.NotFound("TrainGroupParticipant"));
        }

        [Theory]
        [InlineData("x")]
        [InlineData("1.5")]
        [InlineData("99999999999")]
        public async Task An_address_that_is_no_number_is_refused(string id)
        {
            (await ChangeAsync(Admin, id, new { id = 1, trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = _member.Id })).ShouldFail(English.Invalid);
        }

        [Fact]
        public async Task The_member_of_a_change_has_to_be_named()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            (await ChangeAsync(Admin, booking.Id, new { id = booking.Id, trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = "" })).ShouldBeBad(English.Required("UserId"));
            (await ChangeAsync(Admin, booking.Id, new { id = booking.Id, trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = "x" })).ShouldBreak();
            (await ChangeAsync(Admin, booking.Id, new { id = booking.Id, trainGroupId = _group.Id, trainGroupDateId = _wednesday.Id, userId = Guid.NewGuid() })).ShouldBreak();
        }

        // The member's details come along with the form; their account is not touched by it.
        [Fact]
        public async Task The_members_details_in_the_form_are_not_saved()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            (await ChangeAsync(Admin, booking.Id, new
            {
                id = booking.Id,
                trainGroupId = _group.Id,
                trainGroupDateId = _wednesday.Id,
                userId = _member.Id,
                selectedDate = Day.AddDays(7),
                user = new { id = _member.Id, firstName = "Changed", lastName = "Changed", userName = "changed", email = "changed@gym.test", userColor = "#000000" }
            })).ShouldSucceed(English.Updated("TrainGroupParticipant"));

            Read(booking.Id).SelectedDate.ShouldBe(Day.AddDays(7));
            Database.Read(db => db.Users.Single(x => x.Id == _member.Id)).FirstName.ShouldBe(_member.FirstName);
        }


        // ================================================================
        // By id
        // ================================================================

        [Fact]
        public async Task One_is_read_by_its_id_with_the_view_permission()
        {
            TrainGroupParticipant booking = Seed.OneOff(_wednesday, _member, Day);

            TrainGroupParticipantDto dto = (await GetAsync<TrainGroupParticipantDto>(Staff(_admin), $"{Url}/{booking.Id}")).Value;

            dto.Id.ShouldBe(booking.Id);
            dto.SelectedDate.ShouldBe(Day);
            dto.UserId.ShouldBe(_member.Id.ToString());
            dto.User.ShouldBeNull();
            (await GetAsync<TrainGroupParticipantDto>(Caller.Of(_member), $"{Url}/{booking.Id}")).ShouldFail(English.NotAuthorized);
            (await GetAsync<TrainGroupParticipantDto>(Staff(_admin), $"{Url}/99")).ShouldFail(English.NotFound("TrainGroupParticipant"));
        }
    }
}
