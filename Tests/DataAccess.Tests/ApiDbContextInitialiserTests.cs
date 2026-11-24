using System.Security.Claims;
using Core.Models;
using DataAccess.Tests.Fakes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tests.Shared;

namespace DataAccess.Tests
{
    /// <summary>
    /// What the app does to its database every time it starts.
    /// </summary>
    public sealed class ApiDbContextInitialiserTests : IDisposable
    {
        private const string Password = "P@ssw0rd";

        private static readonly string[] AdministratorClaims =
        [
            "Roles_View", "Roles_Add", "Roles_Edit", "Roles_Delete",
            "Users_View", "Users_Add", "Users_Edit", "Users_Delete",
            "Mails_View", "Mails_Add", "Mails_Edit", "Mails_Delete",
            "TrainGroups_View", "TrainGroups_Add", "TrainGroups_Edit", "TrainGroups_Delete",
            "TrainGroupDates_View", "TrainGroupDates_Add", "TrainGroupDates_Edit", "TrainGroupDates_Delete",
            "TrainGroupParticipants_View", "TrainGroupParticipants_Add", "TrainGroupParticipants_Edit", "TrainGroupParticipants_Delete",
            "WorkoutPlans_View", "WorkoutPlans_Add", "WorkoutPlans_Edit", "WorkoutPlans_Delete",
            "WorkoutPlansAdmin_View", "WorkoutPlansAdmin_Add", "WorkoutPlansAdmin_Edit", "WorkoutPlansAdmin_Delete",
            "UserStatuses_View", "UserStatuses_Add", "UserStatuses_Edit", "UserStatuses_Delete",
            "WorkoutPlanRules_View", "WorkoutPlanRules_Add", "WorkoutPlanRules_Edit", "WorkoutPlanRules_Delete",
            "WorkoutPlanRecordings_View", "WorkoutPlanRecordings_Add", "WorkoutPlanRecordings_Edit", "WorkoutPlanRecordings_Delete",
            "WorkoutPlanRecordingsAdmin_View",
            "SubscriptionsAdmin_View", "SubscriptionsAdmin_Add", "SubscriptionsAdmin_Edit", "SubscriptionsAdmin_Delete",
            "Subscriptions_View", "Subscriptions_Add", "Subscriptions_Delete",
            "TrainGroupCategories_View", "TrainGroupCategories_Add", "TrainGroupCategories_Edit", "TrainGroupCategories_Delete",
            "Questionnaires_View", "Questionnaires_Add", "Questionnaires_Edit", "Questionnaires_Delete"
        ];

        private static readonly string[] TrainerClaims =
        [
            "Mails_View", "Mails_Add", "Mails_Edit", "Mails_Delete",
            "TrainGroups_View", "TrainGroups_Add", "TrainGroups_Edit", "TrainGroups_Delete",
            "TrainGroupDates_View", "TrainGroupDates_Add", "TrainGroupDates_Edit", "TrainGroupDates_Delete",
            "TrainGroupParticipants_View", "TrainGroupParticipants_Add", "TrainGroupParticipants_Edit", "TrainGroupParticipants_Delete"
        ];

        private static readonly string[] SimpleUserClaims =
        [
            "WorkoutPlans_View", "WorkoutPlans_Edit",
            "TrainGroupParticipants_Delete",
            "WorkoutPlanRecordings_View", "WorkoutPlanRecordings_Add",
            "Subscriptions_View", "Subscriptions_Add", "Subscriptions_Delete"
        ];

        private readonly TestDatabase _database = new TestDatabase();
        private readonly IdentityServices _services;

        public ApiDbContextInitialiserTests()
        {
            _services = new IdentityServices(_database);
        }

        public void Dispose()
        {
            _services.Dispose();
            _database.Dispose();
        }

        private Task SeedAsync()
        {
            return _services.Initialiser(_database).SeedAsync();
        }

        private List<string> ClaimsOf(string role)
        {
            return _database.Read(db => db.RoleClaims
                .Where(x => db.Roles.Any(y => y.Id == x.RoleId && y.Name == role))
                .Select(x => x.ClaimType + ": " + x.ClaimValue)
                .ToList());
        }

        private static IEnumerable<string> Permissions(IEnumerable<string> claims)
        {
            return claims.Select(x => "Permission: " + x);
        }


        // The migrations.

        [Fact]
        public async Task Starting_builds_a_missing_database()
        {
            using TestDatabase empty = new TestDatabase(isMigrated: false);

            await _services.Initialiser(empty).RunMigrationsAsync();

            empty.Read(db => db.Database.GetPendingMigrations()).ShouldBeEmpty();
            _services.Log.Lines.ShouldBeEmpty();
        }

        [Fact]
        public async Task Starting_on_an_up_to_date_database_changes_nothing()
        {
            new Seed(_database).User();

            await _services.Initialiser(_database).RunMigrationsAsync();

            _database.Read(db => db.Users.Count()).ShouldBe(1);
        }

        [Fact]
        public async Task A_database_that_cannot_be_opened_is_logged_and_stops_the_start()
        {
            IConfiguration nowhere = new ConfigurationBuilder()
                .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:DefaultConnection", "Data Source=/no/such/folder/gym.db")])
                .Build();

            ApiDbContextInitialiser initialiser = new ApiDbContextInitialiser(_services.Log, new ApiDbContextFactory(nowhere), null!);

            await Should.ThrowAsync<SqliteException>(initialiser.RunMigrationsAsync);
            _services.Log.Lines.ShouldBe(["Error: An error occurred while running migrations. (SqliteException)"]);
        }


        // The roles.

        [Fact]
        public async Task An_empty_database_gets_the_three_roles_with_their_claims()
        {
            await SeedAsync();

            _database.Read(db => db.Roles.Select(x => x.Name + "/" + x.NormalizedName).ToList())
                .ShouldBe(["Administrator/ADMINISTRATOR", "Trainer/TRAINER", "SimpleUser/SIMPLEUSER"], ignoreOrder: true);

            ClaimsOf("Administrator").ShouldBe(Permissions(AdministratorClaims), ignoreOrder: true);
            ClaimsOf("Trainer").ShouldBe(Permissions(TrainerClaims), ignoreOrder: true);
            ClaimsOf("SimpleUser").ShouldBe(Permissions(SimpleUserClaims), ignoreOrder: true);
        }

        // The administrator may take a claim off a role. The next start must not put it back.
        [Theory]
        [InlineData("Administrator")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_role_that_is_there_keeps_the_claims_it_has(string name)
        {
            Role role = new Seed(_database).Role(name);
            _database.Add(new IdentityRoleClaim<Guid> { RoleId = role.Id, ClaimType = "Permission", ClaimValue = "Own" });

            await SeedAsync();

            ClaimsOf(name).ShouldBe(["Permission: Own"]);
            _database.Read(db => db.Roles.Count()).ShouldBe(3);
        }

        [Fact]
        public async Task A_claim_the_new_role_already_has_is_not_added_twice()
        {
            _services.Roles.Claims = role => [new Claim("Permission", "Mails_View"), new Claim("Other", "Mails_Add")];

            await SeedAsync();

            // Everything but the one it has; the same value under another type is a different claim.
            ClaimsOf("Trainer").ShouldBe(Permissions(TrainerClaims.Except(["Mails_View"])), ignoreOrder: true);
        }


        // The people.

        [Theory]
        [InlineData("Admin", "Admin@Admin.Admin", "Admin", "User", "Administrator")]
        [InlineData("Trainer", "Trainer@Trainer.Trainer", "Trainer", "User", "Trainer")]
        [InlineData("SimpleUser", "SimpleUser@SimpleUser.SimpleUser", "Simple", "User", "SimpleUser")]
        public async Task An_empty_database_gets_one_user_of_each_role(string userName, string email, string firstName, string lastName, string role)
        {
            await SeedAsync();

            User user = _database.Read(db => db.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role).Single(x => x.UserName == userName));

            user.Email.ShouldBe(email);
            user.FirstName.ShouldBe(firstName);
            user.LastName.ShouldBe(lastName);
            user.UserRoles.Select(x => x.Role.Name).ShouldBe([role]);
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash!, Password).ShouldNotBe(PasswordVerificationResult.Failed);
        }

        [Fact]
        public async Task Starting_again_adds_nobody_and_nothing()
        {
            await SeedAsync();
            List<string> before = _database.Read(db => db.Users.Select(x => x.Id + x.ConcurrencyStamp + x.PasswordHash).ToList());
            int claims = _database.Read(db => db.RoleClaims.Count());

            await SeedAsync();

            _database.Read(db => db.Users.Select(x => x.Id + x.ConcurrencyStamp + x.PasswordHash).ToList()).ShouldBe(before, ignoreOrder: true);
            _database.Read(db => db.RoleClaims.Count()).ShouldBe(claims);
            _database.Read(db => db.UserRoles.Count()).ShouldBe(3);
        }

        // However it was renamed, promoted or given another password.
        [Theory]
        [InlineData("Admin")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_user_who_is_there_is_left_alone(string userName)
        {
            User existing = new Seed(_database).User(x =>
            {
                x.UserName = userName;
                x.FirstName = "Own";
                x.PasswordHash = "own";
            });

            await SeedAsync();

            User user = _database.Read(db => db.Users.Include(x => x.UserRoles).Single(x => x.UserName == userName));
            user.Id.ShouldBe(existing.Id);
            user.FirstName.ShouldBe("Own");
            user.PasswordHash.ShouldBe("own");
            user.UserRoles.ShouldBeEmpty();
        }

        // Their password stands in the source for everybody to read.
        [Theory]
        [InlineData("Admin")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_user_who_was_deleted_does_not_come_back(string userName)
        {
            await SeedAsync();
            _database.Change(db => db.Users.Remove(db.Users.Single(x => x.UserName == userName)));

            await SeedAsync();

            _database.Read(db => db.Users.Count()).ShouldBe(2);
            _database.Read(db => db.Users.Any(x => x.UserName == userName)).ShouldBeFalse();
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_user_who_was_renamed_does_not_come_back(string userName)
        {
            await SeedAsync();
            _database.Change(db =>
            {
                User user = db.Users.Single(x => x.UserName == userName);
                user.UserName = "Own";
                user.NormalizedUserName = "OWN";
            });

            await SeedAsync();

            _database.Read(db => db.Users.Count()).ShouldBe(3);
            _database.Read(db => db.Users.Any(x => x.UserName == userName)).ShouldBeFalse();
        }

        [Fact]
        public async Task Where_there_is_anybody_only_the_roles_are_made()
        {
            User member = new Seed(_database).User();

            await SeedAsync();

            _database.Read(db => db.Users.Single()).Id.ShouldBe(member.Id);
            _database.Read(db => db.UserRoles.Count()).ShouldBe(0);
            _database.Read(db => db.Roles.Count()).ShouldBe(3);
        }

        [Theory]
        [InlineData("Admin", "Administrator")]
        [InlineData("Trainer", "Trainer")]
        [InlineData("SimpleUser", "SimpleUser")]
        public async Task A_user_whose_role_is_gone_by_then_gets_none(string userName, string role)
        {
            // There when the roles are made, gone when the people are.
            int asked = 0;
            _services.Roles.Exists = name => name == role ? ++asked == 1 : null;

            await SeedAsync();

            _database.Read(db => db.Users.Include(x => x.UserRoles).Single(x => x.UserName == userName)).UserRoles.ShouldBeEmpty();
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_new_user_who_has_a_role_already_is_taken_out_of_it_first(string userName)
        {
            _services.Users.Roles = name => name == userName ? ["Old", "Older"] : null;

            await SeedAsync();

            _services.Users.RemovedFromRoles.ShouldBe([$"{userName}: Old, Older"]);
        }

        // The claims of a release are added to the roles that are there, and there are none.
        [Fact]
        public async Task Roles_that_cannot_be_found_at_the_end_are_passed_over()
        {
            _services.Roles.IsMissing = name => true;

            await SeedAsync();

            _services.Log.Lines.ShouldBeEmpty();
        }


        // What goes wrong stops the start, and is logged.

        private static IdentityResult Failed(params string[] errors)
        {
            return IdentityResult.Failed(errors.Select(x => new IdentityError { Description = x }).ToArray());
        }

        private async Task ShouldStopWithAsync(string message)
        {
            InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(SeedAsync);

            exception.Message.ShouldBe(message);
            _services.Log.Lines.ShouldBe(["Error: An error occurred while seeding the database. (InvalidOperationException)"]);
        }

        [Theory]
        [InlineData("Administrator")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_role_that_cannot_be_made_stops_the_start(string role)
        {
            _services.Roles.Create = name => name == role ? Failed("first", "second") : null;

            await ShouldStopWithAsync("Failed to create role: first, second");
        }

        [Theory]
        [InlineData("Roles_View")]
        [InlineData("Mails_View")]
        [InlineData("WorkoutPlans_View")]
        public async Task A_claim_that_cannot_be_added_stops_the_start(string claim)
        {
            _services.Roles.AddClaim = value => value == claim ? Failed("first", "second") : null;

            await ShouldStopWithAsync($"Failed to add claim {claim}: first, second");
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_user_who_cannot_be_made_stops_the_start(string userName)
        {
            _services.Users.Create = name => name == userName ? Failed("first", "second") : null;

            await ShouldStopWithAsync("Failed to create user: first, second");
        }

        [Theory]
        [InlineData("Admin")]
        [InlineData("Trainer")]
        [InlineData("SimpleUser")]
        public async Task A_user_who_cannot_be_given_the_role_stops_the_start(string userName)
        {
            _services.Users.AddToRole = name => name == userName ? Failed("first", "second") : null;

            await ShouldStopWithAsync("Failed to assign role: first, second");
        }
    }
}
