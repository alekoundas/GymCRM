using Core.Enums;
using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;

namespace DataAccess.Tests
{
    /// <summary>
    /// What the database itself does when a row goes: what goes with it, what stays and loses the
    /// link, and what may not go at all. Deleted with plain sql, so EF has no say in it.
    /// </summary>
    public sealed class DeleteRuleTests : IDisposable
    {
        private static readonly DateTime SomeDay = new DateTime(2031, 3, 5);

        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly User _member;
        private readonly User _trainer;

        public DeleteRuleTests()
        {
            _seed = new Seed(_database);
            _member = _seed.User();
            _trainer = _seed.User();
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private void Delete(string table, object id)
        {
            string value = id is Guid guid ? $"'{guid.ToString().ToUpperInvariant()}'" : id.ToString()!;

            _database.Execute($"DELETE FROM \"{table}\" WHERE Id = {value}");
        }

        private int Count<TEntity>() where TEntity : class
        {
            return _database.Read(db => db.Set<TEntity>().Count());
        }

        private void ShouldBeRefused(string table, object id)
        {
            Should.Throw<SqliteException>(() => Delete(table, id)).Message.ShouldContain("FOREIGN KEY constraint failed");
        }


        // A member.

        [Fact]
        public void A_deleted_member_takes_everything_of_theirs_along()
        {
            TrainGroup group = _seed.Group(_trainer);
            TrainGroupDate date = _seed.Weekly(group, DayOfWeek.Monday);
            _seed.Skip(_seed.Recurring(date, _member, SomeDay), SomeDay);
            _seed.Attendance(_member, SomeDay, group);
            _seed.Mail(_member);
            _seed.PhoneNumber(_member);
            _seed.Subscription(_member, 5);
            WorkoutPlan plan = _seed.Plan(_member);
            _seed.History(_seed.Exercise(plan));
            _seed.Recording(plan, SomeDay);
            _seed.Answer(_seed.Questionnaire("Knees?").Questions.Single(), _member, "Fine");
            _seed.UserRole(_member, _seed.Role("SimpleUser"));
            _database.Add(new IdentityUserClaim<Guid> { UserId = _member.Id, ClaimType = "a", ClaimValue = "b" });
            _database.Add(new IdentityUserLogin<Guid> { UserId = _member.Id, LoginProvider = "a", ProviderKey = "b" });
            _database.Add(new IdentityUserToken<Guid> { UserId = _member.Id, LoginProvider = "a", Name = "b" });

            Delete("AspNetUsers", _member.Id);

            Count<TrainGroupParticipant>().ShouldBe(0);
            Count<TrainGroupParticipantUnavailableDate>().ShouldBe(0);
            Count<TrainGroupΑttendance>().ShouldBe(0);
            Count<Mail>().ShouldBe(0);
            Count<PhoneNumber>().ShouldBe(0);
            Count<Subscription>().ShouldBe(0);
            Count<WorkoutPlan>().ShouldBe(0);
            Count<Exercise>().ShouldBe(0);
            Count<ExerciseHistory>().ShouldBe(0);
            Count<WorkoutPlanRecording>().ShouldBe(0);
            Count<QuestionnaireAnswer>().ShouldBe(0);
            Count<UserRole>().ShouldBe(0);
            Count<IdentityUserClaim<Guid>>().ShouldBe(0);
            Count<IdentityUserLogin<Guid>>().ShouldBe(0);
            Count<IdentityUserToken<Guid>>().ShouldBe(0);

            // And nothing of anybody else's.
            Count<User>().ShouldBe(1);
            Count<Role>().ShouldBe(1);
            Count<TrainGroup>().ShouldBe(1);
            Count<TrainGroupDate>().ShouldBe(1);
            Count<QuestionnaireQuestion>().ShouldBe(1);
        }

        [Fact]
        public void A_trainer_with_a_group_cannot_be_deleted()
        {
            _seed.Group(_trainer);

            ShouldBeRefused("AspNetUsers", _trainer.Id);
        }

        [Fact]
        public void A_status_somebody_has_cannot_be_deleted()
        {
            UserStatus status = _seed.UserStatus();
            _seed.User(x => x.UserStatusId = status.Id);

            ShouldBeRefused("UserStatuses", status.Id);
        }

        [Fact]
        public void A_status_nobody_has_can_be_deleted()
        {
            UserStatus status = _seed.UserStatus();

            Delete("UserStatuses", status.Id);

            Count<UserStatus>().ShouldBe(0);
        }

        [Fact]
        public void A_deleted_role_is_taken_off_everybody_and_keeps_its_members()
        {
            Role role = _seed.Role("Trainer");
            _seed.UserRole(_trainer, role);
            _database.Add(new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = "Permission", ClaimValue = "Users_View" });

            Delete("AspNetRoles", role.Id);

            Count<UserRole>().ShouldBe(0);
            Count<IdentityRoleClaim<Guid>>().ShouldBe(0);
            Count<User>().ShouldBe(2);
        }


        // A train group.

        [Fact]
        public void A_deleted_group_takes_its_days_and_its_bookings_along()
        {
            TrainGroup group = _seed.Group(_trainer);
            TrainGroupDate date = _seed.Weekly(group, DayOfWeek.Monday);
            _seed.Skip(_seed.Recurring(date, _member, SomeDay), SomeDay);
            _seed.Cancelled(group, SomeDay);

            Delete("TrainGroups", group.Id);

            Count<TrainGroupDate>().ShouldBe(0);
            Count<TrainGroupParticipant>().ShouldBe(0);
            Count<TrainGroupParticipantUnavailableDate>().ShouldBe(0);
            Count<TrainGroupUnavailableDate>().ShouldBe(0);
            Count<User>().ShouldBe(2);
        }

        // The balance is counted from the attendances, so losing one would hand a lesson back.
        [Fact]
        public void A_deleted_group_leaves_its_attendances_behind()
        {
            TrainGroup group = _seed.Group(_trainer);
            _seed.Attendance(_member, SomeDay, group);

            Delete("TrainGroups", group.Id);

            TrainGroupΑttendance attendance = _database.Read(db => db.TrainGroupΑttendances.Single());
            attendance.TrainGroupId.ShouldBeNull();
            attendance.TrainGroupTitle.ShouldBe(group.Title);
            attendance.TrainerId.ShouldBe(_trainer.Id);
        }

        [Fact]
        public void A_deleted_day_of_a_group_takes_its_bookings_along_and_no_other_days()
        {
            TrainGroup group = _seed.Group(_trainer);
            TrainGroupDate monday = _seed.Weekly(group, DayOfWeek.Monday);
            TrainGroupDate friday = _seed.Weekly(group, DayOfWeek.Friday);
            _seed.Skip(_seed.Recurring(monday, _member, SomeDay), SomeDay);
            TrainGroupParticipant kept = _seed.Recurring(friday, _member, SomeDay);

            Delete("TrainGroupDates", monday.Id);

            _database.Read(db => db.TrainGroupParticipants.Single()).Id.ShouldBe(kept.Id);
            Count<TrainGroupParticipantUnavailableDate>().ShouldBe(0);
        }

        [Fact]
        public void A_deleted_booking_takes_its_skipped_dates_along()
        {
            TrainGroupParticipant booking = _seed.Recurring(_seed.Weekly(_seed.Group(_trainer), DayOfWeek.Monday), _member, SomeDay);
            _seed.Skip(booking, SomeDay);

            Delete("TrainGroupParticipants", booking.Id);

            Count<TrainGroupParticipantUnavailableDate>().ShouldBe(0);
        }

        [Fact]
        public void A_deleted_category_leaves_its_groups_without_one()
        {
            TrainGroupCategory category = _seed.Category("Reformer");
            TrainGroup group = _seed.Group(_trainer, x => x.TrainGroupCategoryId = category.Id);

            Delete("TrainGroupCategories", category.Id);

            _database.Read(db => db.TrainGroups.Single(x => x.Id == group.Id)).TrainGroupCategoryId.ShouldBeNull();
        }


        // A workout plan.

        [Fact]
        public void A_deleted_plan_takes_its_exercises_and_their_history_along()
        {
            WorkoutPlan plan = _seed.Plan(_member);
            _seed.History(_seed.Exercise(plan));

            Delete("WorkoutPlans", plan.Id);

            Count<Exercise>().ShouldBe(0);
            Count<ExerciseHistory>().ShouldBe(0);
        }

        [Fact]
        public void A_deleted_plan_leaves_its_recordings_behind()
        {
            WorkoutPlan plan = _seed.Plan(_member);
            _seed.Recording(plan, SomeDay);

            Delete("WorkoutPlans", plan.Id);

            WorkoutPlanRecording recording = _database.Read(db => db.WorkoutPlanRecordings.Single());
            recording.WorkoutPlanId.ShouldBeNull();
            recording.WorkoutPlanTitle.ShouldBe(plan.Title);
        }

        [Fact]
        public void A_deleted_exercise_takes_its_history_along()
        {
            Exercise exercise = _seed.Exercise(_seed.Plan(_member));
            _seed.History(exercise);

            Delete("Exercises", exercise.Id);

            Count<ExerciseHistory>().ShouldBe(0);
            Count<WorkoutPlan>().ShouldBe(1);
        }

        [Fact]
        public void A_deleted_rule_takes_its_weeks_along_and_leaves_its_plans_without_one()
        {
            WorkoutPlanRule rule = _seed.Rule("Two weeks", 1, 2);
            _seed.Plan(_member, rule, currentWeek: 2);

            Delete("WorkoutPlanRules", rule.Id);

            Count<WorkoutPlanRuleWeek>().ShouldBe(0);
            WorkoutPlan plan = _database.Read(db => db.WorkoutPlans.Single());
            plan.WorkoutPlanRuleId.ShouldBeNull();
            plan.CurrentWeek.ShouldBe(2);
        }


        // The questionnaire.

        [Fact]
        public void A_deleted_question_takes_its_answers_along()
        {
            List<QuestionnaireQuestion> questions = _seed.Questionnaire("Knees?", "Back?").Questions.ToList();
            _seed.Answer(questions[0], _member, "Fine");
            _seed.Answer(questions[1], _member, "Sore");

            Delete("QuestionnaireQuestions", questions[0].Id);

            _database.Read(db => db.QuestionnaireAnswers.Single()).Answer.ShouldBe("Sore");
        }

        [Fact]
        public void A_deleted_questionnaire_takes_its_questions_and_answers_along()
        {
            Questionnaire questionnaire = _seed.Questionnaire("Knees?");
            _seed.Answer(questionnaire.Questions.Single(), _member, "Fine");

            Delete("Questionnaires", questionnaire.Id);

            Count<QuestionnaireQuestion>().ShouldBe(0);
            Count<QuestionnaireAnswer>().ShouldBe(0);
        }


        // What may be there only once.

        private void ShouldBeTaken(Action add, string column)
        {
            Should.Throw<DbUpdateException>(add).InnerException.ShouldBeOfType<SqliteException>().Message.ShouldContain($"UNIQUE constraint failed: {column}");
        }

        [Fact]
        public void Two_members_cannot_share_an_email()
        {
            ShouldBeTaken(() => _seed.User(x => x.Email = _member.Email), "AspNetUsers.Email");
        }

        [Fact]
        public void Two_members_cannot_share_a_username_however_it_is_typed()
        {
            ShouldBeTaken(() => _seed.User(x => x.UserName = _member.UserName!.ToUpperInvariant()), "AspNetUsers.NormalizedUserName");
        }

        [Fact]
        public void Two_roles_cannot_share_a_name()
        {
            _seed.Role("Trainer");

            ShouldBeTaken(() => _seed.Role("TRAINER"), "AspNetRoles.NormalizedName");
        }

        [Fact]
        public void Two_rules_cannot_share_a_name()
        {
            _seed.Rule("Four weeks");

            ShouldBeTaken(() => _seed.Rule("Four weeks"), "WorkoutPlanRules.Name");
        }

        [Fact]
        public void A_rule_cannot_have_the_same_week_twice()
        {
            WorkoutPlanRule rule = _seed.Rule("Four weeks", 1);

            ShouldBeTaken(
                () => _database.Add(new WorkoutPlanRuleWeek { WorkoutPlanRuleId = rule.Id, WeekNumber = 1, MaxRecordings = 1 }),
                "WorkoutPlanRuleWeeks.WorkoutPlanRuleId, WorkoutPlanRuleWeeks.WeekNumber");
        }

        [Fact]
        public void A_member_cannot_answer_the_same_question_twice()
        {
            QuestionnaireQuestion question = _seed.Questionnaire("Knees?").Questions.Single();
            _seed.Answer(question, _member, "Fine");

            ShouldBeTaken(
                () => _seed.Answer(question, _member, "Sore"),
                "QuestionnaireAnswers.QuestionnaireQuestionId, QuestionnaireAnswers.UserId");
        }


        // How a value is kept, where another program reading the file would have to know.

        [Fact]
        public void A_date_type_is_kept_as_its_name_and_a_status_as_its_number()
        {
            _seed.Monthly(_seed.Group(_trainer), 5);
            _seed.Subscription(_member, 1, SubscriptionStatusEnum.REJECTED);
            _seed.Mail(_member, x => x.Status = MailStatusEnum.FAILED);

            _database.Scalar("SELECT TrainGroupDateType FROM TrainGroupDates").ShouldBe("DAY_OF_MONTH");
            _database.Scalar("SELECT Status FROM Subscriptions").ShouldBe(2L);
            _database.Scalar("SELECT Status FROM Mails").ShouldBe(2L);
        }

        // Read back without saying it was UTC, which is why the api's json moves it on a machine
        // that is not in UTC.
        [Fact]
        public void A_time_comes_back_with_no_kind()
        {
            DateTime written = new DateTime(2031, 3, 5, 10, 20, 30, DateTimeKind.Utc);
            TrainGroup group = _seed.Group(_trainer, x => x.StartOn = written);

            DateTime read = _database.Read(db => db.TrainGroups.Single(x => x.Id == group.Id)).StartOn;

            read.ShouldBe(written);
            read.Kind.ShouldBe(DateTimeKind.Unspecified);
            _database.Scalar("SELECT StartOn FROM TrainGroups").ShouldBe("2031-03-05 10:20:30");
        }
    }
}
