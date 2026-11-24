using API.Tests.Host;
using Core.Dtos.DataTable;
using Core.Dtos.TrainGroupΑttendance;
using Core.Enums;
using Core.Models;
using Core.Translations;
using Tests.Shared;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Who came to what. Its permissions are those of the groups.
    /// </summary>
    public sealed class TrainGroupAttendancesControllerTests : ApiTest
    {
        private const string Url = "api/TrainGroupAttendances";

        private static readonly DateTime Day = new DateTime(2031, 3, 5, 0, 0, 0, DateTimeKind.Utc);

        private readonly User _maria;
        private readonly User _nikos;
        private readonly User _trainer;
        private readonly TrainGroup _group;

        public TrainGroupAttendancesControllerTests(ApiFixture api) : base(api)
        {
            _maria = Seed.User(x => x.Email = "maria@gym.test");
            _nikos = Seed.User(x => x.Email = "nikos@gym.test");
            _trainer = Seed.User(x => { x.FirstName = "Νίκος"; x.LastName = "Παπάς"; });
            _group = Seed.Group(_trainer, x =>
            {
                x.Title = "Pilates";
                x.Description = "Easy";
                x.StartOn = new DateTime(2025, 1, 1, 18, 30, 0);
                x.Duration = new DateTime(2000, 1, 1, 1, 15, 0);
            });
        }

        private Caller Trainer
        {
            get { return Caller.Of(_trainer, "TrainGroups_View", "TrainGroups_Add", "TrainGroups_Edit", "TrainGroups_Delete"); }
        }

        private object Row(User user, DateTime? day = null, TrainGroup? group = null)
        {
            return new { userId = user.Id, attendanceDate = day ?? Day, trainGroupId = (group ?? _group).Id };
        }

        private static object ByHand(User user, string title = "By hand", DateTime? day = null)
        {
            return new { userId = user.Id, attendanceDate = day ?? Day, trainGroupTitle = title };
        }

        private Task<ApiResult<List<TrainGroupΑttendance>>> AddAsync(Caller caller, params object[] rows)
        {
            return PostAsync<List<TrainGroupΑttendance>>(caller, Url, rows);
        }

        private List<TrainGroupΑttendance> All()
        {
            return Database.Read(db => db.TrainGroupΑttendances.OrderBy(x => x.Id).ToList());
        }


        // ================================================================
        // Taking attendance
        // ================================================================

        // The group can be renamed, moved or deleted. What the member came to stays what it was.
        [Fact]
        public async Task What_the_session_was_is_copied_from_the_group()
        {
            ApiResult<List<TrainGroupΑttendance>> result = await AddAsync(Trainer, new
            {
                userId = _maria.Id,
                attendanceDate = Day.AddHours(18),
                trainGroupId = _group.Id,
                trainGroupTitle = "sent",
                trainGroupDescription = "sent",
                trainGroupStartOn = Day,
                trainGroupDuration = Day,
                trainerFullName = "sent"
            });

            result.ShouldSucceed(English.Updated("TrainGroupΑttendance"));
            result.Value.Single().Id.ShouldBe(All().Single().Id);

            TrainGroupΑttendance attendance = All().Single();
            attendance.UserId.ShouldBe(_maria.Id);
            attendance.AttendanceDate.ShouldBe(Day.AddHours(18));
            attendance.TrainGroupId.ShouldBe(_group.Id);
            attendance.TrainGroupTitle.ShouldBe("Pilates");
            attendance.TrainGroupDescription.ShouldBe("Easy");
            attendance.TrainGroupStartOn.ShouldBe(new DateTime(2025, 1, 1, 18, 30, 0));
            attendance.TrainGroupDuration.ShouldBe(new DateTime(2000, 1, 1, 1, 15, 0));
            attendance.TrainerId.ShouldBe(_trainer.Id);
            attendance.TrainerFullName.ShouldBe("Νίκος Παπάς");
        }

        [Theory]
        [InlineData("Νίκος", "", "Νίκος")]
        [InlineData("", "Παπάς", "Παπάς")]
        [InlineData("", "", "")]
        public async Task A_trainers_name_has_no_spare_space(string firstName, string lastName, string expected)
        {
            Database.Change(db => { User trainer = db.Users.Single(x => x.Id == _trainer.Id); trainer.FirstName = firstName; trainer.LastName = lastName; });

            await AddAsync(Trainer, Row(_maria));

            All().Single().TrainerFullName.ShouldBe(expected);
        }

        [Fact]
        public async Task One_written_by_hand_is_what_was_written()
        {
            ApiResult<List<TrainGroupΑttendance>> result = await AddAsync(Trainer, new
            {
                userId = _maria.Id,
                attendanceDate = Day,
                trainGroupTitle = "Old group",
                trainGroupDescription = "Gone",
                trainGroupStartOn = Day.AddHours(9),
                trainGroupDuration = Day.AddHours(1),
                trainerFullName = "Somebody"
            });

            result.ShouldSucceed(English.Updated("TrainGroupΑttendance"));
            TrainGroupΑttendance attendance = All().Single();
            attendance.TrainGroupId.ShouldBeNull();
            attendance.TrainGroupTitle.ShouldBe("Old group");
            attendance.TrainGroupDescription.ShouldBe("Gone");
            attendance.TrainGroupStartOn.ShouldBe(Day.AddHours(9));
            attendance.TrainGroupDuration.ShouldBe(Day.AddHours(1));
            attendance.TrainerFullName.ShouldBe("Somebody");
            attendance.TrainerId.ShouldBeNull();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task One_written_by_hand_says_what_it_was(string title)
        {
            (await AddAsync(Trainer, ByHand(_maria, title))).ShouldBeBad(English.Required("TrainGroupTitle"));

            All().ShouldBeEmpty();
        }

        [Fact]
        public async Task With_a_group_there_is_no_need_to()
        {
            (await AddAsync(Trainer, Row(_maria))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));
        }

        // Some from groups and some by hand, in one go.
        [Fact]
        public async Task Each_row_is_copied_from_its_own_group()
        {
            TrainGroup yoga = Seed.Group(_maria, x => x.Title = "Yoga");

            (await AddAsync(Trainer, Row(_maria), ByHand(_maria, "Outside"), Row(_nikos), Row(_nikos, group: yoga))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));

            All().Select(x => $"{x.UserId == _maria.Id} {x.TrainGroupTitle}").ShouldBe(["True Pilates", "True Outside", "False Pilates", "False Yoga"]);
        }


        // ================================================================
        // Twice in a day
        // ================================================================

        // Minutes from midnight of the attendance that is there already.
        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(60 * 18, true)]
        [InlineData(60 * 24 - 1, true)]
        [InlineData(60 * 24, false)]
        public async Task A_member_comes_to_a_group_once_a_day(int minutes, bool isRefused)
        {
            Seed.Attendance(_maria, Day.AddMinutes(minutes), _group);

            foreach (DateTime sent in new[] { Day, Day.AddHours(12), Day.AddDays(1).AddTicks(-1) })
            {
                ApiResult<List<TrainGroupΑttendance>> result = await AddAsync(Trainer, Row(_maria, sent));

                if (isRefused)
                    result.ShouldBeBad(English.Of(TranslationKeys.Duplicate_participant_found));
                else
                    result.ShouldSucceed(English.Updated("TrainGroupΑttendance"));

                Database.Execute($"DELETE FROM TrainGroupΑttendances WHERE Id <> (SELECT MIN(Id) FROM TrainGroupΑttendances)");
            }
        }

        [Fact]
        public async Task Another_member_and_another_group_are_not_the_same()
        {
            Seed.Attendance(_nikos, Day, _group);
            Seed.Attendance(_maria, Day, Seed.Group(_trainer));
            Seed.Attendance(_maria, Day);

            (await AddAsync(Trainer, Row(_maria))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));
        }

        [Fact]
        public async Task One_written_by_hand_may_be_written_again()
        {
            (await AddAsync(Trainer, ByHand(_maria))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));
            (await AddAsync(Trainer, ByHand(_maria))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));

            All().Count.ShouldBe(2);
        }

        [Fact]
        public async Task One_row_that_is_refused_refuses_them_all()
        {
            Seed.Attendance(_nikos, Day, _group);

            (await AddAsync(Trainer, Row(_maria), Row(_nikos))).ShouldBeBad(English.Of(TranslationKeys.Duplicate_participant_found));

            All().Count.ShouldBe(1);
            Emails.Lines.ShouldBeEmpty();
        }

        // The rows are held against those that are there already, not against each other.
        [Fact]
        public async Task The_same_member_twice_in_one_request_is_not_noticed()
        {
            (await AddAsync(Trainer, Row(_maria), Row(_maria))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));

            All().Count.ShouldBe(2);
        }


        // ================================================================
        // What it refuses, and what breaks it
        // ================================================================

        [Fact]
        public async Task Taking_attendance_needs_the_permission_to_add_groups()
        {
            (await AddAsync(Caller.Of(_trainer, "TrainGroups_View", "TrainGroups_Edit", "TrainGroups_Delete", "TrainGroupAttendances_Add"), Row(_maria))).ShouldFail(English.NotAuthorized);

            All().ShouldBeEmpty();
            (await AddAsync(Caller.Of(_trainer, "TrainGroups_Add"), Row(_maria))).ShouldSucceed(English.Updated("TrainGroupΑttendance"));
        }

        [Fact]
        public async Task The_member_has_to_be_named()
        {
            (await AddAsync(Trainer, new { userId = "", attendanceDate = Day, trainGroupId = _group.Id })).ShouldBeBad(English.Required("UserId"));
        }

        [Fact]
        public async Task A_member_who_is_no_id_breaks_it()
        {
            (await AddAsync(Trainer, new { userId = "x", attendanceDate = Day, trainGroupId = _group.Id })).ShouldBreak();
            (await AddAsync(Trainer, new { userId = "x", attendanceDate = Day, trainGroupTitle = "By hand" })).ShouldBreak();
        }

        [Fact]
        public async Task A_member_or_a_group_that_is_not_there_breaks_it()
        {
            (await AddAsync(Trainer, new { userId = Guid.NewGuid(), attendanceDate = Day, trainGroupId = _group.Id })).ShouldBreak();
            (await AddAsync(Trainer, new { userId = _maria.Id, attendanceDate = Day, trainGroupId = 99 })).ShouldBreak();

            All().ShouldBeEmpty();
        }

        [Fact]
        public async Task Nobody_at_all_is_an_error()
        {
            (await AddAsync(Trainer)).ShouldFail(English.Of(TranslationKeys.An_error_occurred_while_creating_the_entity));
        }


        // ================================================================
        // The last lesson
        // ================================================================

        // How many lessons the member has before this one, and whether they are written to. Only on
        // the one that leaves them exactly one.
        [Theory]
        [InlineData(3, false)]
        [InlineData(2, true)]
        [InlineData(1, false)]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        public async Task A_member_is_told_when_they_are_down_to_one(int lessons, bool isMailed)
        {
            Seed.Subscription(_maria, lessons);

            await AddAsync(Trainer, Row(_maria));

            if (isMailed)
                Emails.Lines.ShouldBe([$"queue maria@gym.test: {English.Of(TranslationKeys.Only_one_subscription_left)}"]);
            else
                Emails.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task The_mail_says_where_they_were_and_how_many_are_left()
        {
            Seed.Subscription(_maria, 2);

            await AddAsync(Trainer, Row(_maria, Day.AddHours(23)));

            ApprovedFile.ShouldMatch(Emails.Bodies.Single(), "LastLessonMail");
        }

        // Whoever takes the attendance, not the member who reads it.
        [Theory]
        [InlineData("en", "05/03/2031")]
        [InlineData("el", "05/03/2031")]
        [InlineData("de", "05.03.2031")]
        [InlineData("nl", "05-03-2031")]
        public async Task The_mail_is_in_the_language_of_the_one_who_sends_it(string language, string date)
        {
            Seed.Subscription(_maria, 2);

            await PostAsync<List<TrainGroupΑttendance>>(Trainer, Url, new[] { Row(_maria) }, language);

            string subject = Language.Text(language, TranslationKeys.Only_one_subscription_left);
            Emails.Lines.ShouldBe([$"queue maria@gym.test: {subject}"]);
            Emails.Bodies.Single().ShouldContain($"<h2>{subject}</h2>");
            Emails.Bodies.Single().ShouldContain($"<strong>{Language.Text(language, TranslationKeys.Attendance_date)}:</strong> {date}</li>");
        }

        // Everyone from one group on one date reads the same mail.
        [Fact]
        public async Task A_group_is_written_to_together()
        {
            TrainGroup yoga = Seed.Group(_trainer, x => x.Title = "Yoga");
            User eleni = Seed.User(x => x.Email = "eleni@gym.test");
            User petros = Seed.User(x => x.Email = "petros@gym.test");
            foreach (User user in new[] { _maria, _nikos, eleni })
                Seed.Subscription(user, 2);
            Seed.Subscription(petros, 5);

            await AddAsync(Trainer, Row(_maria), Row(petros), Row(_nikos), Row(eleni, group: yoga), Row(petros, group: yoga));

            string subject = English.Of(TranslationKeys.Only_one_subscription_left);
            Emails.Lines.ShouldBe([$"queue maria@gym.test, nikos@gym.test: {subject}", $"queue eleni@gym.test: {subject}"]);
            Emails.Bodies[0].ShouldContain("Pilates");
            Emails.Bodies[1].ShouldContain("Yoga");
        }

        [Fact]
        public async Task A_group_with_nobody_down_to_one_is_not_written_to()
        {
            TrainGroup yoga = Seed.Group(_trainer, x => x.Title = "Yoga");
            Seed.Subscription(_maria, 2);
            Seed.Subscription(_nikos, 5);

            await AddAsync(Trainer, Row(_nikos, group: yoga), Row(_maria));

            Emails.Lines.Single().ShouldStartWith("queue maria@gym.test:");
            Emails.Bodies.Single().ShouldContain("Pilates");
        }

        // Two dates at once, the last of which leaves them with one: a mail for each.
        [Fact]
        public async Task Each_date_is_a_mail_of_its_own()
        {
            Seed.Subscription(_maria, 3);

            await AddAsync(Trainer, Row(_maria), Row(_maria, Day.AddDays(7)));

            Emails.Lines.Count.ShouldBe(2);
            Emails.Bodies[0].ShouldContain("05/03/2031");
            Emails.Bodies[1].ShouldContain("12/03/2031");
        }

        [Fact]
        public async Task A_member_twice_in_one_batch_is_named_twice()
        {
            Seed.Subscription(_maria, 3);

            await AddAsync(Trainer, Row(_maria), Row(_maria));

            Emails.Lines.Single().ShouldStartWith("queue maria@gym.test, maria@gym.test:");
        }

        // What was written by hand is not looked at.
        [Fact]
        public async Task The_mail_for_one_written_by_hand_does_not_say_where_they_were()
        {
            Seed.Subscription(_maria, 2);

            await AddAsync(Trainer, ByHand(_maria, "Outside"));

            string body = Emails.Bodies.Single();
            body.ShouldNotContain("Outside");
            body.ShouldContain($"<li><strong>{English.Of(TranslationKeys.Train_group)}:</strong> </li>");
        }

        // A request is not a lesson.
        [Fact]
        public async Task Only_lessons_that_were_granted_count()
        {
            Seed.Subscription(_maria, 2, SubscriptionStatusEnum.PENDING);
            Seed.Subscription(_nikos, 2, SubscriptionStatusEnum.REJECTED);

            await AddAsync(Trainer, Row(_maria), Row(_nikos));

            Emails.Lines.ShouldBeEmpty();
        }


        // ================================================================
        // Reading, changing, deleting
        // ================================================================

        [Fact]
        public async Task One_is_read_by_its_id_with_the_groups_permission()
        {
            TrainGroupΑttendance attendance = Seed.Attendance(_maria, Day, _group);

            TrainGroupΑttendanceDto dto = (await GetAsync<TrainGroupΑttendanceDto>(Trainer, $"{Url}/{attendance.Id}")).Value;

            dto.Id.ShouldBe(attendance.Id);
            dto.TrainGroupTitle.ShouldBe("Pilates");
            dto.TrainerId.ShouldBe(_trainer.Id);
            dto.UserId.ShouldBe(_maria.Id.ToString());
            dto.User.ShouldBeNull();
            dto.TrainGroup.ShouldBeNull();
            (await GetAsync<TrainGroupΑttendanceDto>(Caller.Of(_maria, "TrainGroups_Add"), $"{Url}/{attendance.Id}")).ShouldFail(English.NotAuthorized);
            (await GetAsync<TrainGroupΑttendanceDto>(Trainer, $"{Url}/99")).ShouldFail(English.NotFound("TrainGroupΑttendance"));
        }

        [Fact]
        public async Task One_is_deleted_with_the_groups_permission()
        {
            TrainGroupΑttendance attendance = Seed.Attendance(_maria, Day, _group);

            (await DeleteAsync<TrainGroupΑttendance>(Caller.Of(_maria, "TrainGroups_View", "TrainGroups_Add", "TrainGroups_Edit"), $"{Url}/{attendance.Id}")).ShouldFail(English.NotAuthorized);
            (await DeleteAsync<TrainGroupΑttendance>(Trainer, $"{Url}/99")).ShouldFail(English.NotFound("TrainGroupΑttendance"));
            All().Count.ShouldBe(1);

            (await DeleteAsync<TrainGroupΑttendance>(Trainer, $"{Url}/{attendance.Id}")).ShouldSucceed(English.Deleted("TrainGroupΑttendance"));
            All().ShouldBeEmpty();
        }

        // The form has to bring the member along.
        [Fact]
        public async Task A_change_without_the_member_is_refused()
        {
            TrainGroupΑttendance attendance = Seed.Attendance(_maria, Day, _group);

            (await PutAsync<TrainGroupΑttendance>(Trainer, $"{Url}/{attendance.Id}", new { id = attendance.Id, attendanceDate = Day, trainGroupId = _group.Id, userId = _maria.Id }))
                .ShouldBeBad("The User field is required.");

            All().Single().TrainGroupTitle.ShouldBe("Pilates");
        }

        // Every column is written as the form has it, and what it leaves out is emptied - the copy of
        // the session too. The account that comes along is not.
        [Fact]
        public async Task A_change_writes_every_column_and_leaves_the_account_alone()
        {
            TrainGroupΑttendance attendance = Seed.Attendance(_maria, Day, _group, x => { x.CreatedBy_Id = "maker"; x.TrainerFullName = "Νίκος Παπάς"; });

            (await PutAsync<TrainGroupΑttendance>(Trainer, $"{Url}/{attendance.Id}", new
            {
                id = attendance.Id,
                attendanceDate = Day.AddDays(1),
                trainGroupId = _group.Id,
                userId = _nikos.Id,
                user = new { id = _nikos.Id, userName = "changed", email = "changed@gym.test", firstName = "Changed", lastName = "Changed" }
            })).ShouldSucceed(English.Updated("TrainGroupΑttendance"));

            TrainGroupΑttendance changed = All().Single();
            changed.AttendanceDate.ShouldBe(Day.AddDays(1));
            changed.UserId.ShouldBe(_nikos.Id);
            changed.TrainGroupId.ShouldBe(_group.Id);
            changed.TrainGroupTitle.ShouldBe("");
            changed.TrainerFullName.ShouldBe("");
            changed.TrainerId.ShouldBeNull();
            changed.CreatedBy_Id.ShouldBe("");
            changed.CreatedOn.ShouldBe(default);
            Database.Read(db => db.Users.Single(x => x.Id == _nikos.Id)).UserName.ShouldBe(_nikos.UserName);
        }

        [Fact]
        public async Task A_change_to_a_row_that_is_not_there_is_not_found()
        {
            (await PutAsync<TrainGroupΑttendance>(Trainer, $"{Url}/99", new
            {
                id = 99,
                attendanceDate = Day,
                userId = _maria.Id,
                user = new { id = _maria.Id, userName = "x", email = "x@gym.test", firstName = "x", lastName = "x" }
            })).ShouldFail(English.NotFound("TrainGroupΑttendance"));
        }

        [Fact]
        public async Task A_change_needs_the_groups_permission()
        {
            (await PutAsync<TrainGroupΑttendance>(Caller.Of(_maria, "TrainGroups_View", "TrainGroups_Add", "TrainGroups_Delete"), $"{Url}/1", new
            {
                id = 1,
                attendanceDate = Day,
                userId = _maria.Id,
                user = new { id = _maria.Id, userName = "x", email = "x@gym.test", firstName = "x", lastName = "x" }
            })).ShouldFail(English.NotAuthorized);
        }


        // ================================================================
        // The grid
        // ================================================================

        private async Task<DataTableDto<TrainGroupΑttendanceDto>> GridAsync(Caller caller, params DataTableFilterDto[] filters)
        {
            return (await PostAsync<DataTableDto<TrainGroupΑttendanceDto>>(caller, Url + "/GetDataTable", Grid<TrainGroupΑttendanceDto>(filters))).Value;
        }

        [Fact]
        public async Task Staff_see_everybodys_and_a_member_their_own()
        {
            TrainGroupΑttendance maria = Seed.Attendance(_maria, Day, _group);
            TrainGroupΑttendance nikos = Seed.Attendance(_nikos, Day, _group);

            (await GridAsync(Trainer)).Data!.Select(x => x.Id!.Value).ShouldBe([maria.Id, nikos.Id], ignoreOrder: true);

            DataTableDto<TrainGroupΑttendanceDto> own = await GridAsync(Caller.Of(_nikos, "TrainGroupParticipants_View"));
            own.Data!.Single().Id.ShouldBe(nikos.Id);
            own.TotalRecords.ShouldBe(1);

            (await GridAsync(Caller.Of(_nikos), Filter("UserId", _maria.Id.ToString(), DataTableFiltersEnum.equals))).TotalRecords.ShouldBe(0);
            (await GridAsync(Caller.WithNoId())).TotalRecords.ShouldBe(0);
        }

        [Fact]
        public async Task A_row_brings_the_member_and_the_group()
        {
            Seed.Attendance(_maria, Day, _group);
            Seed.Attendance(_maria, Day);

            List<TrainGroupΑttendanceDto> rows = (await GridAsync(Trainer)).Data!.OrderBy(x => x.Id).ToList();

            rows.ShouldAllBe(x => x.User.Email == "maria@gym.test");
            rows[0].TrainGroup.ShouldNotBeNull().Title.ShouldBe("Pilates");
            rows[1].TrainGroup.ShouldBeNull();
            rows[1].TrainGroupTitle.ShouldBe("By hand");
        }

        // The session is named by the copy on the row, whatever has become of the group.
        [Fact]
        public async Task A_row_outlives_its_group()
        {
            Seed.Attendance(_maria, Day, _group);
            Database.Change(db => db.TrainGroups.Remove(db.TrainGroups.Single()));

            TrainGroupΑttendanceDto row = (await GridAsync(Trainer, Filter("trainGroupTitle", "Pil"))).Data!.Single();

            row.TrainGroupId.ShouldBeNull();
            row.TrainGroupTitle.ShouldBe("Pilates");
        }
    }
}
