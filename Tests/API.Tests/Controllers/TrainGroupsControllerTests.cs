using System.Net;
using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.Lookup;
using Core.Dtos.TrainGroup;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Microsoft.EntityFrameworkCore;

namespace API.Tests.Controllers
{
    public sealed class TrainGroupsControllerTests : ApiTest
    {
        private const string Url = "api/TrainGroups";

        // A Wednesday, the 5th.
        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _user;
        private readonly User _trainer;
        private readonly Caller _admin;

        public TrainGroupsControllerTests(ApiFixture api) : base(api)
        {
            _user = Seed.User();
            _trainer = Seed.User(x => { x.UserName = "nikos"; x.FirstName = "Νίκος"; x.LastName = "Παπάς"; x.Email = "nikos@gym.test"; x.Address = "Athens"; x.MedicalHistory = "private"; x.ProfileImage = [1, 2]; });
            _admin = Caller.Of(_user, "TrainGroups_View", "TrainGroups_Add", "TrainGroups_Edit", "TrainGroups_Delete");
        }


        // ================================================================
        // Adding
        // ================================================================

        private static object Weekly(DayOfWeek day, params object[] participants)
        {
            return new { trainGroupDateType = "DAY_OF_WEEK", recurrenceDayOfWeek = day.ToString(), trainGroupParticipants = participants };
        }

        private static object Monthly(int day, params object[] participants)
        {
            return new { trainGroupDateType = "DAY_OF_MONTH", recurrenceDayOfMonth = day, trainGroupParticipants = participants };
        }

        private static object Fixed(DateTime day, params object[] participants)
        {
            return new { trainGroupDateType = "FIXED_DAY", fixedDay = day, trainGroupParticipants = participants };
        }

        private object Participant(DateTime? selectedDate = null)
        {
            return new { userId = _user.Id, selectedDate, trainGroupDateId = 0, trainGroupId = 0 };
        }

        private Dictionary<string, object?> Form(params object[] dates)
        {
            return new Dictionary<string, object?>
            {
                ["title"] = "Pilates",
                ["description"] = "Easy",
                ["duration"] = new DateTime(2000, 1, 1, 1, 30, 0, DateTimeKind.Utc),
                ["startOn"] = new DateTime(2031, 3, 5, 18, 30, 0, DateTimeKind.Utc),
                ["maxParticipants"] = 8,
                ["trainerId"] = _trainer.Id,
                ["trainGroupDates"] = dates
            };
        }

        private Task<ApiResult<List<TrainGroup>>> AddAsync(params object[] forms)
        {
            return PostAsync<List<TrainGroup>>(_admin, Url, forms);
        }

        [Fact]
        public async Task A_group_is_added_with_its_days()
        {
            TrainGroupCategory category = Seed.Category("Reformer");
            Dictionary<string, object?> form = Form(Weekly(DayOfWeek.Monday), Weekly(DayOfWeek.Friday), Fixed(Day));
            form["trainGroupCategoryId"] = category.Id;

            (await AddAsync(form)).ShouldSucceed(English.Updated("TrainGroup"));

            TrainGroup group = Database.Read(db => db.TrainGroups.Include(x => x.TrainGroupDates).Single());
            group.Title.ShouldBe("Pilates");
            group.Description.ShouldBe("Easy");
            group.Duration.ShouldBe(new DateTime(2000, 1, 1, 1, 30, 0));
            group.StartOn.ShouldBe(new DateTime(2031, 3, 5, 18, 30, 0));
            group.MaxParticipants.ShouldBe(8);
            group.TrainerId.ShouldBe(_trainer.Id);
            group.TrainGroupCategoryId.ShouldBe(category.Id);
            group.TrainGroupDates.Select(x => $"{x.TrainGroupDateType} {x.RecurrenceDayOfWeek} {x.FixedDay:yyyy-MM-dd}").ShouldBe(["DAY_OF_WEEK Monday ", "DAY_OF_WEEK Friday ", "FIXED_DAY  2031-03-05"], ignoreOrder: true);
        }

        // Members put on a day before the group is there at all belong to both once it is.
        [Fact]
        public async Task The_members_of_a_day_are_added_with_it()
        {
            (await AddAsync(Form(Weekly(DayOfWeek.Monday, Participant()), Weekly(DayOfWeek.Wednesday, Participant(Day))))).ShouldSucceed(English.Updated("TrainGroup"));

            TrainGroup group = Database.Read(db => db.TrainGroups.Include(x => x.TrainGroupDates).ThenInclude(x => x.TrainGroupParticipants).Single());
            List<TrainGroupParticipant> participants = group.TrainGroupDates.SelectMany(x => x.TrainGroupParticipants).ToList();
            participants.Count.ShouldBe(2);
            participants.ShouldAllBe(x => x.TrainGroupId == group.Id && x.UserId == _user.Id);
            participants.Select(x => x.SelectedDate).ShouldBe([null, Day], ignoreOrder: true);
        }

        // Every pair of kinds of days, on a day where they meet and on one where they do not.
        public static readonly TheoryData<string, string, string[]> Pairs = new TheoryData<string, string, string[]>
        {
            { "Monday", "Friday", [] },
            { "5", "6", [] },
            { "2031-03-05", "2031-03-06", [] },
            { "Monday", "5", [TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix] },
            { "Wednesday", "2031-03-05", [TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_week_entry] },
            { "Wednesday", "2031-03-06", [] },
            { "5", "2031-03-05", [TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_month_entry] },
            { "5", "2031-03-06", [] },
            // The same one twice. The second message is about participants, and there are none.
            { "Monday", "Monday", [TranslationKeys.Duplicate_train_group_date_found, TranslationKeys.Duplicate_participant_found] },
            { "5", "5", [TranslationKeys.Duplicate_train_group_date_found, TranslationKeys.Duplicate_participant_found] },
            { "2031-03-05", "2031-03-05", [TranslationKeys.Duplicate_train_group_date_found, TranslationKeys.Duplicate_participant_found] },
        };

        private static object DateOf(string text)
        {
            if (int.TryParse(text, out int day))
                return Monthly(day);

            return Enum.TryParse(text, out DayOfWeek dayOfWeek) ? Weekly(dayOfWeek) : Fixed(DateTime.SpecifyKind(DateTime.Parse(text), DateTimeKind.Utc));
        }

        [Theory]
        [MemberData(nameof(Pairs))]
        public async Task Two_days_that_do_not_go_together_are_refused(string first, string second, string[] errors)
        {
            foreach ((string one, string two) in new[] { (first, second), (second, first) })
            {
                ApiResult<List<TrainGroup>> result = await AddAsync(Form(DateOf(one), DateOf(two)));

                if (errors.Length == 0)
                    result.ShouldSucceed(English.Updated("TrainGroup"));
                else
                    // No date is named, though the message has a place for one.
                    result.ShouldBeBad(errors.Select(x => English.Of(x, "")).ToArray());
            }
        }

        [Fact]
        public async Task Everything_that_does_not_go_together_is_said_at_once()
        {
            (await AddAsync(Form(Weekly(DayOfWeek.Wednesday), Monthly(5), Fixed(Day), Fixed(Day)))).ShouldBeBad(
                English.Of(TranslationKeys.Day_of_week_and_day_of_month_doesnt_mix),
                English.Of(TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_week_entry, ""),
                English.Of(TranslationKeys.Fixed_date_0_has_the_same_day_of_week_with_an_existing_day_of_month_entry, ""),
                English.Of(TranslationKeys.Duplicate_train_group_date_found),
                English.Of(TranslationKeys.Duplicate_participant_found));
        }


        // ================================================================
        // Members of the group itself, beside those of its days
        // ================================================================

        // With any there, each day is looked at: a weekday or a day of the month is refused unless a
        // member of it has picked a date it falls on, and a fixed day is refused if one has picked any.
        public static readonly TheoryData<string, string?, string?> Members = new TheoryData<string, string?, string?>
        {
            { "Wednesday", null, TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates },
            { "Wednesday", "2031-03-05", null },
            { "Wednesday", "2031-03-06", TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates },
            { "5", null, TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates },
            { "5", "2031-03-05", null },
            { "5", "2031-03-06", TranslationKeys.Participant_selected_date_doesnt_match_any_of_the_train_group_dates },
            { "2031-03-05", null, null },
            { "2031-03-05", "2031-03-05", TranslationKeys.Fixed_date_doesnt_allow_one_off_participants },
            { "2031-03-05", "2031-03-06", TranslationKeys.Fixed_date_doesnt_allow_one_off_participants },
        };

        private static object DateWith(string text, object[] participants)
        {
            if (int.TryParse(text, out int day))
                return Monthly(day, participants);

            return Enum.TryParse(text, out DayOfWeek dayOfWeek) ? Weekly(dayOfWeek, participants) : Fixed(DateTime.SpecifyKind(DateTime.Parse(text), DateTimeKind.Utc), participants);
        }

        [Theory]
        [MemberData(nameof(Members))]
        public async Task With_members_of_its_own_the_picked_dates_are_looked_at(string date, string? picked, string? error)
        {
            DateTime? selected = picked == null ? null : DateTime.SpecifyKind(DateTime.Parse(picked), DateTimeKind.Utc);
            Dictionary<string, object?> form = Form(DateWith(date, [Participant(selected)]));

            // Without any of its own nothing is.
            (await AddAsync(form)).ShouldSucceed(English.Updated("TrainGroup"));

            form["trainGroupParticipants"] = new[] { Participant() };
            ApiResult<List<TrainGroup>> result = await AddAsync(form);

            if (error != null)
                result.ShouldBeBad(English.Of(error));
            else
                // Let through, and then the database is asked to put them on no day at all.
                result.ShouldBreak();
        }


        // ================================================================
        // What the form has to have
        // ================================================================

        [Fact]
        public async Task A_group_with_something_missing_is_refused()
        {
            Dictionary<string, object?> form = Form();
            form["title"] = "";
            form["description"] = new string('x', 501);
            form["trainerId"] = "";

            (await AddAsync(form)).ShouldBeBad(
                English.Required("Title"),
                English.Of(TranslationKeys.Description_cannot_exceed_500_characters),
                English.Required("TrainerId"),
                English.Of(TranslationKeys.At_least_1_Train_Group_Date_is_required));
        }

        [Fact]
        public async Task A_day_that_does_not_say_which_is_refused()
        {
            (await AddAsync(Form(new { trainGroupDateType = "DAY_OF_WEEK" }, new { trainGroupDateType = "FIXED_DAY" }, new { trainGroupDateType = "DAY_OF_MONTH" }))).ShouldBeBad(
                "RecurrenceDayOfWeek is required when TrainGroupDateType is DAY_OF_WEEK.",
                "FixedDay is required when TrainGroupDateType is FIXED_DAY.",
                "RecurrenceDayOfMonth is required when TrainGroupDateType is DAY_OF_MONTH.");
        }

        [Fact]
        public async Task A_kind_of_day_there_is_not_is_refused()
        {
            (await AddAsync(Form(new { trainGroupDateType = "EVERY_DAY" }))).Status.ShouldBe(HttpStatusCode.BadRequest);
        }

        [Theory]
        [InlineData("not an id")]
        [InlineData("11111111-2222-3333-4444-555555555555")]
        public async Task A_trainer_who_is_not_there_breaks(string trainerId)
        {
            Dictionary<string, object?> form = Form(Weekly(DayOfWeek.Monday));
            form["trainerId"] = trainerId;

            (await AddAsync(form)).ShouldBreak();
        }

        [Fact]
        public async Task Adding_needs_the_permission()
        {
            (await PostAsync<List<TrainGroup>>(Caller.Of(_user, "TrainGroups_View", "TrainGroups_Edit"), Url, new[] { Form(Weekly(DayOfWeek.Monday)) })).ShouldFail(English.NotAuthorized);

            Database.Read(db => db.TrainGroups.Count()).ShouldBe(0);
        }


        // ================================================================
        // Reading, editing and deleting
        // ================================================================

        [Fact]
        public async Task A_group_is_read_without_its_days_and_its_trainer()
        {
            TrainGroup group = Seed.Group(_trainer, x => x.Title = "Pilates");
            Seed.Weekly(group, DayOfWeek.Monday);

            TrainGroupDto dto = (await GetAsync<TrainGroupDto>(_admin, $"{Url}/{group.Id}")).Value;

            dto.Title.ShouldBe("Pilates");
            dto.TrainerId.ShouldBe(_trainer.Id.ToString());
            dto.Trainer.ShouldBeNull();
            dto.TrainGroupDates.ShouldBeEmpty();
        }

        // Saving a group writes the group: whatever the form sends of its trainer is not.
        [Fact]
        public async Task An_edit_writes_the_group_and_leaves_its_trainer_and_its_days_alone()
        {
            TrainGroup group = Seed.Group(_trainer);
            Seed.Weekly(group, DayOfWeek.Monday);

            (await PutAsync<TrainGroup>(_admin, $"{Url}/{group.Id}", new
            {
                id = group.Id,
                title = "Renamed",
                duration = Day,
                startOn = Day.AddHours(9),
                maxParticipants = 3,
                trainerId = _trainer.Id,
                trainer = new { id = _trainer.Id, userName = "changed", email = "changed@gym.test", firstName = "Changed", lastName = "Changed" }
            })).ShouldSucceed(English.Updated("TrainGroup"));

            TrainGroup written = Database.Read(db => db.TrainGroups.Include(x => x.Trainer).Include(x => x.TrainGroupDates).Single());
            written.Title.ShouldBe("Renamed");
            written.MaxParticipants.ShouldBe(3);
            written.StartOn.Hour.ShouldBe(9);
            written.Trainer.FirstName.ShouldBe("Νίκος");
            written.Trainer.MedicalHistory.ShouldBe("private");
            written.TrainGroupDates.Count.ShouldBe(1);
        }

        [Fact]
        public async Task An_edit_that_names_no_category_takes_it_off()
        {
            TrainGroup group = Seed.Group(_trainer, x => x.TrainGroupCategoryId = Seed.Category("Reformer").Id);

            await PutAsync<TrainGroup>(_admin, $"{Url}/{group.Id}", new { id = group.Id, title = "Pilates", duration = Day, startOn = Day, maxParticipants = 3, trainerId = _trainer.Id });

            Database.Read(db => db.TrainGroups.Single()).TrainGroupCategoryId.ShouldBeNull();
        }

        [Fact]
        public async Task A_group_is_deleted_with_its_days()
        {
            TrainGroup group = Seed.Group(_trainer);
            Seed.Weekly(group, DayOfWeek.Monday);

            (await DeleteAsync<TrainGroup>(_admin, $"{Url}/{group.Id}")).ShouldSucceed(English.Deleted("TrainGroup"));

            Database.Read(db => db.TrainGroupDates.Count()).ShouldBe(0);
        }

        [Fact]
        public async Task Reading_editing_and_deleting_need_the_permission()
        {
            TrainGroup group = Seed.Group(_trainer);
            Caller anybody = Caller.Of(_user);

            (await GetAsync<TrainGroupDto>(anybody, $"{Url}/{group.Id}")).ShouldFail(English.NotAuthorized);
            (await PutAsync<TrainGroup>(anybody, $"{Url}/{group.Id}", new { id = group.Id, title = "x", duration = Day, startOn = Day, trainerId = _trainer.Id })).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<TrainGroup>(anybody, $"{Url}/{group.Id}")).ShouldFail(English.NotAuthorized);
        }


        // ================================================================
        // The grid
        // ================================================================

        [Fact]
        public async Task The_grid_shows_each_trainer_as_a_name_and_a_face()
        {
            Seed.Group(_trainer, x => x.Title = "Pilates");

            ApiResult<DataTableDto<TrainGroupDto>> result = await PostAsync<DataTableDto<TrainGroupDto>>(_admin, Url + "/GetDataTable", Grid<TrainGroupDto>());

            TrainGroupDto dto = result.Value.Data!.Single();
            dto.Title.ShouldBe("Pilates");
            dto.Trainer.ShouldNotBeNull().UserName.ShouldBe("nikos");
            dto.Trainer.FirstName.ShouldBe("Νίκος");
            dto.Trainer.LastName.ShouldBe("Παπάς");
            dto.Trainer.ProfileImage.ShouldBe([1, 2]);

            result.Body.ShouldNotContain("nikos@gym.test");
            result.Body.ShouldNotContain("Athens");
            result.Body.ShouldNotContain("private");
        }

        [Fact]
        public async Task The_grid_needs_the_permission_to_view()
        {
            Seed.Group(_trainer);

            (await PostAsync<DataTableDto<TrainGroupDto>>(Caller.Of(_user, "TrainGroups_Add", "TrainGroups_Edit", "TrainGroups_Delete"), Url + "/GetDataTable", Grid<TrainGroupDto>())).ShouldFail(English.NotAuthorized);
        }

        [Fact]
        public async Task The_grid_is_sorted_and_filtered()
        {
            User other = Seed.User(x => x.FirstName = "Άννα");
            Seed.Group(_trainer, x => { x.Title = "Pilates"; x.MaxParticipants = 3; });
            Seed.Group(other, x => { x.Title = "Γιόγκα"; x.MaxParticipants = 1; x.TrainGroupCategoryId = Seed.Category("Easy").Id; });

            async Task<string> TitlesAsync(DataTableDto<TrainGroupDto> grid)
            {
                return string.Join(" ", (await PostAsync<DataTableDto<TrainGroupDto>>(_admin, Url + "/GetDataTable", grid)).Value.Data!.Select(x => x.Title));
            }

            (await TitlesAsync(SortedGrid<TrainGroupDto>(("maxParticipants", 1)))).ShouldBe("Γιόγκα Pilates");
            (await TitlesAsync(SortedGrid<TrainGroupDto>(("trainer.firstName", -1)))).ShouldBe("Pilates Γιόγκα");
            (await TitlesAsync(Grid<TrainGroupDto>(Filter("title", "γιογκα")))).ShouldBe("Γιόγκα");
            (await TitlesAsync(Grid<TrainGroupDto>(Filter("trainer.firstName", "νικ")))).ShouldBe("Pilates");
            (await TitlesAsync(Grid<TrainGroupDto>(Filter("trainerId", [other.Id.ToString()], DataTableFiltersEnum.@in)))).ShouldBe("Γιόγκα");
            (await TitlesAsync(Grid<TrainGroupDto>(Filter("trainGroupCategoryId", "null", DataTableFiltersEnum.equals)))).ShouldBe("Pilates");
        }


        // ================================================================
        // The dropdown
        // ================================================================

        private async Task<LookupDto> LookupAsync(string id, string value, int skip = 0, int take = 10)
        {
            ApiResult<LookupDto> result = await PostAsync<LookupDto>(Caller.Of(_user), Url + "/Lookup", new { skip, take, filter = new { id, value } });

            result.ShouldSucceed();

            return result.Value;
        }

        [Theory]
        [InlineData("", "Barre Pilates Γιόγκα")]
        [InlineData("γιογκα", "Γιόγκα")]
        [InlineData("AR", "Barre")]
        [InlineData("x", "")]
        public async Task Groups_are_found_by_part_of_their_title_in_the_order_of_it(string value, string expected)
        {
            foreach (string title in new[] { "Pilates", "Γιόγκα", "Barre" })
                Seed.Group(_trainer, x => x.Title = title);

            LookupDto answer = await LookupAsync("", value);

            string.Join(" ", answer.Data.Select(x => x.Value)).ShouldBe(expected);
            answer.TotalRecords.ShouldBe(answer.Data.Count);
        }

        [Fact]
        public async Task One_group_is_found_by_its_id()
        {
            TrainGroup pilates = Seed.Group(_trainer, x => x.Title = "Pilates");
            Seed.Group(_trainer, x => x.Title = "Barre");

            (await LookupAsync(pilates.Id.ToString(), "")).Data.Single().Id.ShouldBe(pilates.Id.ToString());
            (await LookupAsync(pilates.Id.ToString(), "barre")).Data.ShouldBeEmpty();
            (await LookupAsync("99", "")).Data.ShouldBeEmpty();
            (await LookupAsync("x", "")).Data.Count.ShouldBe(2);
        }

        [Fact]
        public async Task A_page_counts_them_all()
        {
            for (int i = 0; i < 5; i++)
                Seed.Group(_trainer);

            LookupDto answer = await LookupAsync("", "", 3, 10);

            answer.Data.Count.ShouldBe(2);
            answer.TotalRecords.ShouldBe(5);
        }
    }
}
