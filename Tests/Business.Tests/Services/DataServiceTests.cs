using Business.Repository;
using Business.Services;
using Business.Tests.Fakes;
using Core.Models;
using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tests.Shared;
using static Tests.Shared.TestToken;

namespace Business.Tests.Services
{
    public sealed class DataServiceTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;
        private readonly DataService _service;
        private readonly User _user;

        public DataServiceTests()
        {
            _seed = new Seed(_database);
            _service = DataServices.On(_database);
            _user = _seed.User();
        }

        public void Dispose()
        {
            _database.Dispose();
        }


        // The repositories.

        private void ShouldBe<TEntity>(IGenericRepository<TEntity> repository) where TEntity : class
        {
            repository.ShouldNotBeNull();
            _service.GetGenericRepository<TEntity>().ShouldBeSameAs(repository);
        }

        [Fact]
        public void Each_kind_of_row_has_the_repository_of_its_name()
        {
            ShouldBe<Mail>(_service.Mails);
            ShouldBe<Exercise>(_service.Exercises);
            ShouldBe<TrainGroup>(_service.TrainGroups);
            ShouldBe<UserStatus>(_service.UserStatuses);
            ShouldBe<TrainGroupCategory>(_service.TrainGroupCategories);
            ShouldBe<WorkoutPlan>(_service.WorkoutPlans);
            ShouldBe<WorkoutPlanRule>(_service.WorkoutPlanRules);
            ShouldBe<WorkoutPlanRuleWeek>(_service.WorkoutPlanRuleWeeks);
            ShouldBe<WorkoutPlanRecording>(_service.WorkoutPlanRecordings);
            ShouldBe<Subscription>(_service.Subscriptions);
            ShouldBe<PhoneNumber>(_service.PhoneNumbers);
            ShouldBe<TrainGroupDate>(_service.TrainGroupDates);
            ShouldBe<ExerciseHistory>(_service.ExerciseHistories);
            ShouldBe<TrainGroupΑttendance>(_service.TrainGroupΑttendances);
            ShouldBe<TrainGroupParticipant>(_service.TrainGroupParticipants);
            ShouldBe<TrainGroupUnavailableDate>(_service.TrainGroupUnavailableDates);
            ShouldBe<TrainGroupParticipantUnavailableDate>(_service.TrainGroupParticipantUnavailableDates);
            ShouldBe<User>(_service.Users);
            ShouldBe<Role>(_service.Roles);
            ShouldBe<UserRole>(_service.UserRoles);
            ShouldBe<IdentityRoleClaim<Guid>>(_service.RoleClaims);
            ShouldBe<GoogleRefreshToken>(_service.GoogleRefreshTokens);
        }

        // The health questionnaire has none: its controller reads the database itself.
        [Fact]
        public void A_kind_of_row_with_no_repository_throws_and_is_named()
        {
            Should.Throw<InvalidOperationException>(() => _service.GetGenericRepository<Questionnaire>()).Message.ShouldBe("No repository found for type Questionnaire");
            Should.Throw<InvalidOperationException>(() => _service.GetGenericRepository<QuestionnaireQuestion>()).Message.ShouldBe("No repository found for type QuestionnaireQuestion");
            Should.Throw<InvalidOperationException>(() => _service.GetGenericRepository<QuestionnaireAnswer>()).Message.ShouldBe("No repository found for type QuestionnaireAnswer");
            Should.Throw<InvalidOperationException>(() => _service.GetGenericRepository<string>()).Message.ShouldBe("No repository found for type String");
        }

        [Fact]
        public void Every_context_is_a_new_one_on_the_same_database()
        {
            using ApiDbContext first = _service.GetDbContext();
            using ApiDbContext second = _service.GetDbContext();

            first.ShouldNotBeSameAs(second);
            first.Users.Single().Id.ShouldBe(_user.Id);
            second.Users.Single().Id.ShouldBe(_user.Id);
        }


        // Updating.

        private PhoneNumber Read(int id)
        {
            return _database.Read(db => db.PhoneNumbers.Single(x => x.Id == id));
        }

        [Fact]
        public async Task An_update_writes_every_column_of_the_row()
        {
            PhoneNumber first = _seed.PhoneNumber(_user, "1");
            PhoneNumber second = _seed.PhoneNumber(_user, "2");

            _service.Update(new PhoneNumber { Id = first.Id, UserId = _user.Id, Number = "11", IsPrimary = true, CreatedBy_Id = "me" });
            await _service.UpdateAsync(new PhoneNumber { Id = second.Id, UserId = _user.Id, Number = "22", IsPrimary = true, CreatedBy_Id = "me" });

            foreach (PhoneNumber number in new[] { Read(first.Id), Read(second.Id) })
            {
                number.Number.ShouldBe(number.Id == first.Id ? "11" : "22");
                number.IsPrimary.ShouldBeTrue();
                number.CreatedBy_Id.ShouldBe("me");
            }
        }

        // What hangs off the row is not written with it.
        [Fact]
        public async Task An_update_leaves_the_related_rows_alone()
        {
            PhoneNumber number = _seed.PhoneNumber(_user, "1");

            _service.Update(new PhoneNumber { Id = number.Id, UserId = _user.Id, Number = "11", User = new User { Id = _user.Id, FirstName = "Changed", UserName = "changed" } });
            await _service.UpdateAsync(new PhoneNumber { Id = number.Id, UserId = _user.Id, Number = "12", User = new User { Id = _user.Id, FirstName = "Changed", UserName = "changed" } });

            _database.Read(db => db.Users.Single()).FirstName.ShouldBe(_user.FirstName);
            Read(number.Id).Number.ShouldBe("12");
        }

        [Fact]
        public async Task An_update_of_nothing_does_nothing()
        {
            Should.NotThrow(() => _service.Update<PhoneNumber>(null!));
            await Should.NotThrowAsync(() => _service.UpdateAsync<PhoneNumber>(null!));
        }

        [Fact]
        public async Task An_update_of_a_row_that_is_gone_throws()
        {
            PhoneNumber gone = new PhoneNumber { Id = 99, UserId = _user.Id, Number = "1" };

            Should.Throw<DbUpdateConcurrencyException>(() => _service.Update(gone));
            await Should.ThrowAsync<DbUpdateConcurrencyException>(() => _service.UpdateAsync(gone));
        }

        [Fact]
        public async Task An_update_of_something_that_is_not_a_row_throws()
        {
            Should.Throw<InvalidOperationException>(() => _service.Update("text"));
            await Should.ThrowAsync<InvalidOperationException>(() => _service.UpdateAsync("text"));
        }

        [Fact]
        public async Task An_update_of_several_writes_them_all_and_passes_over_the_missing()
        {
            PhoneNumber first = _seed.PhoneNumber(_user, "1");
            PhoneNumber second = _seed.PhoneNumber(_user, "2");

            first.Number = "11";
            second.Number = "22";
            _service.UpdateRange(new List<PhoneNumber?> { first, null, second });

            Read(first.Id).Number.ShouldBe("11");
            Read(second.Id).Number.ShouldBe("22");

            first.Number = "111";
            second.Number = "222";
            await _service.UpdateRangeAsync(new List<PhoneNumber?> { first, null, second });

            Read(first.Id).Number.ShouldBe("111");
            Read(second.Id).Number.ShouldBe("222");
        }

        [Fact]
        public async Task An_update_of_none_does_nothing()
        {
            Should.NotThrow(() => _service.UpdateRange(new List<PhoneNumber>()));
            await Should.NotThrowAsync(() => _service.UpdateRangeAsync(new List<PhoneNumber>()));
        }

        [Fact]
        public async Task An_update_of_several_writes_all_of_them_or_none()
        {
            PhoneNumber first = _seed.PhoneNumber(_user, "1");
            PhoneNumber gone = new PhoneNumber { Id = 99, UserId = _user.Id, Number = "2" };
            first.Number = "11";

            Should.Throw<DbUpdateConcurrencyException>(() => _service.UpdateRange(new List<PhoneNumber> { first, gone }));
            await Should.ThrowAsync<DbUpdateConcurrencyException>(() => _service.UpdateRangeAsync(new List<PhoneNumber> { first, gone }));

            Read(first.Id).Number.ShouldBe("1");
        }

        // Everything behind the service writes to the one database.
        [Fact]
        public async Task What_a_repository_adds_a_context_reads()
        {
            await _service.PhoneNumbers.AddAsync(new PhoneNumber { UserId = _user.Id, Number = "1" });

            using ApiDbContext db = _service.GetDbContext();
            (await db.PhoneNumbers.SingleAsync(Ct)).Number.ShouldBe("1");
        }
    }
}
