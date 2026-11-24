using System.Reflection;
using Core.Models;
using Core.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Tests.Shared;

namespace DataAccess.Tests
{
    public sealed class ApiDbContextTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();
        private readonly Seed _seed;

        public ApiDbContextTests()
        {
            _seed = new Seed(_database);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        public static TheoryData<string> Tables
        {
            get
            {
                IEnumerable<string> names = typeof(ApiDbContext).GetProperties()
                    .Where(x => x.PropertyType.IsGenericType && x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .Select(x => x.Name);

                return new TheoryData<string>(names);
            }
        }

        // The queryable of any set, without knowing what it holds.
        private static int CountOf(ApiDbContext db, string table)
        {
            PropertyInfo property = typeof(ApiDbContext).GetProperty(table)!;
            MethodInfo count = typeof(Queryable).GetMethods().Single(x => x.Name == nameof(Queryable.Count) && x.GetParameters().Length == 1);

            return (int)count.MakeGenericMethod(property.PropertyType.GetGenericArguments()[0]).Invoke(null, [property.GetValue(db)])!;
        }

        [Theory]
        [MemberData(nameof(Tables))]
        public void Every_table_can_be_read(string table)
        {
            using ApiDbContext db = _database.Open();

            CountOf(db, table).ShouldBe(0);
        }

        [Fact]
        public void There_is_a_table_for_everything_the_app_keeps()
        {
            Tables.Select(x => x.Data).Order().ShouldBe(
            [
                "DataProtectionKeys", "ExerciseHistories", "Exercises", "GoogleRefreshTokens", "Mails", "PhoneNumbers",
                "QuestionnaireAnswers", "QuestionnaireQuestions", "Questionnaires", "RoleClaims", "Roles", "Subscriptions",
                "TrainGroupCategories", "TrainGroupDates", "TrainGroupParticipantUnavailableDates", "TrainGroupParticipants",
                "TrainGroupUnavailableDates", "TrainGroups", "TrainGroupΑttendances", "UserClaims", "UserLogins", "UserRoles",
                "UserStatuses", "UserTokens", "Users", "WorkoutPlanRecordings", "WorkoutPlanRules", "WorkoutPlanRuleWeeks", "WorkoutPlans"
            ], ignoreOrder: true);
        }

        // The options that come in are not what decides: the configuration is.
        [Fact]
        public void It_opens_the_database_the_configuration_names()
        {
            DbContextOptions<ApiDbContext> elsewhere = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite("Data Source=somewhere-else.db").Options;

            using ApiDbContext db = new ApiDbContext(elsewhere, _database.Configuration);

            db.Database.GetConnectionString().ShouldBe(_database.ConnectionString);
        }

        [Fact]
        public void It_is_sqlite()
        {
            using ApiDbContext db = _database.Open();

            db.Database.IsSqlite().ShouldBeTrue();
        }

        [Fact]
        public void Errors_say_which_column_they_are_about()
        {
            using ApiDbContext db = _database.Open();

            CoreOptionsOf(db).DetailedErrorsEnabled.ShouldBeTrue();
        }

        [Fact]
        public void It_builds_a_missing_database()
        {
            using TestDatabase empty = new TestDatabase(isMigrated: false);

            using (ApiDbContext db = empty.Open())
                db.RunMigrations();

            using ApiDbContext built = empty.Open();
            built.Database.GetPendingMigrations().ShouldBeEmpty();
            built.Users.Count().ShouldBe(0);
        }

        // A filter may use it inside a query, and it is sqlite that runs it.
        [Fact]
        public void Text_is_normalised_inside_the_query()
        {
            _seed.User(x => x.FirstName = "Ελένη");
            _seed.User(x => x.FirstName = "Maria");

            using ApiDbContext db = _database.Open();
            IQueryable<User> query = db.Users.Where(x => TextNormalizer.Normalize(x.FirstName) == "ελενη");

            query.ToQueryString().ShouldContain("\"normalize\"(");
            query.Single().FirstName.ShouldBe("Ελένη");
        }

        [Fact]
        public async Task Text_is_normalised_inside_an_asynchronous_query()
        {
            _seed.User(x => x.FirstName = "ΝΙΚΟΣ");

            await using ApiDbContext db = _database.Open();

            (await db.Users.SingleAsync(x => TextNormalizer.Normalize(x.FirstName).Contains("νικοσ"), TestToken.Ct)).FirstName.ShouldBe("ΝΙΚΟΣ");
        }

        internal static CoreOptionsExtension CoreOptionsOf(ApiDbContext db)
        {
            return db.GetService<IDbContextOptions>().FindExtension<CoreOptionsExtension>().ShouldNotBeNull();
        }
    }

    /// <summary>
    /// The values of a query - emails, password hashes - reach the log on a developer's machine and
    /// nowhere else.
    /// </summary>
    [Collection(ProcessState.Name)]
    public sealed class LoggedValuesTests : IDisposable
    {
        private const string Variable = "ASPNETCORE_ENVIRONMENT";

        private readonly string? _previous = Environment.GetEnvironmentVariable(Variable);
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(Variable, _previous);
            _database.Dispose();
        }

        [Theory]
        [InlineData("Development", true)]
        [InlineData("development", true)]
        [InlineData("DEVELOPMENT", true)]
        [InlineData("Production", false)]
        [InlineData("Staging", false)]
        [InlineData("Develop", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Values_are_logged_only_while_developing(string? environment, bool isLogged)
        {
            Environment.SetEnvironmentVariable(Variable, environment);

            // Made by the factory, and by hand as the container does.
            using ApiDbContext made = new ApiDbContextFactory(_database.Configuration).CreateDbContext();
            using ApiDbContext byHand = new ApiDbContext(new DbContextOptions<ApiDbContext>(), _database.Configuration);

            ApiDbContextTests.CoreOptionsOf(made).IsSensitiveDataLoggingEnabled.ShouldBe(isLogged);
            ApiDbContextTests.CoreOptionsOf(byHand).IsSensitiveDataLoggingEnabled.ShouldBe(isLogged);
        }
    }

    public sealed class ApiDbContextFactoryTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void Every_context_is_a_new_one_on_the_same_database()
        {
            ApiDbContextFactory factory = new ApiDbContextFactory(_database.Configuration);

            using ApiDbContext first = factory.CreateDbContext();
            using ApiDbContext second = factory.CreateDbContext();

            first.ShouldNotBeSameAs(second);
            first.Database.GetConnectionString().ShouldBe(_database.ConnectionString);
            second.Database.GetConnectionString().ShouldBe(_database.ConnectionString);
        }

        [Fact]
        public void What_one_context_writes_the_next_one_reads()
        {
            new Seed(_database).User(x => x.FirstName = "Maria");

            using ApiDbContext db = new ApiDbContextFactory(_database.Configuration).CreateDbContext();

            db.Users.Single().FirstName.ShouldBe("Maria");
        }

        [Fact]
        public void Errors_say_which_column_they_are_about()
        {
            using ApiDbContext db = new ApiDbContextFactory(_database.Configuration).CreateDbContext();

            ApiDbContextTests.CoreOptionsOf(db).DetailedErrorsEnabled.ShouldBeTrue();
        }

        // No connection string at all is only noticed when the database is first reached for.
        [Fact]
        public void A_configuration_with_no_database_makes_a_context_that_cannot_be_used()
        {
            IConfiguration empty = new ConfigurationBuilder().Build();

            using ApiDbContext db = new ApiDbContextFactory(empty).CreateDbContext();

            Should.Throw<Exception>(() => db.Users.Count());
        }
    }
}
