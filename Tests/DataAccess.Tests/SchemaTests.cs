using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tests.Shared;

namespace DataAccess.Tests
{
    /// <summary>
    /// What the migrations build, and that they build what the code expects.
    /// </summary>
    public sealed class SchemaTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase();

        public void Dispose()
        {
            _database.Dispose();
        }

        [Fact]
        public void Every_migration_applies_to_an_empty_database()
        {
            using ApiDbContext db = _database.Open();

            db.Database.GetPendingMigrations().ShouldBeEmpty();
            db.Database.GetAppliedMigrations().ShouldBe(db.Database.GetMigrations());
        }

        // An entity changed and nobody added a migration: the app would start against a schema
        // that is missing a column.
        [Fact]
        public void The_model_has_no_change_without_a_migration()
        {
            using ApiDbContext db = _database.Open();

            db.Database.HasPendingModelChanges().ShouldBeFalse();
        }

        // The order is the database's history: one renamed or put in between would be run again, or
        // never.
        [Fact]
        public void The_migrations_are_these_in_this_order()
        {
            using ApiDbContext db = _database.Open();

            db.Database.GetMigrations().ShouldBe(
            [
                "20251027092426_Migration_1",
                "20251029165706_Migration_2",
                "20251031233134_Migration_3",
                "20251105215609_Migration_4",
                "20251105223744_Migration_5",
                "20251107233557_Migration_6",
                "20251110191149_Migration_7",
                "20251112221614_Migration_8",
                "20251116212356_Migration_9",
                "20251118154734_Migration_10",
                "20251118172032_Migration_11",
                "20260616191729_Migration_12",
                "20260827171116_Migration_13",
                "20260828154827_Migration_14",
                "20260905131901_Migration_15",
                "20260911155233_Migration16",
                "20260917091117_Migration_17",
                "20260917091754_Migration_18_BackfillAttendanceSnapshots",
                "20260926182627_Migration_19",
                "20260926200102_Migration_20",
                "20260926212152_Migration_21",
                "20261123195427_Migration_22"
            ]);
        }

        public static TheoryData<string, string> EveryStep
        {
            get
            {
                TheoryData<string, string> data = new TheoryData<string, string>();

                using TestDatabase database = new TestDatabase(isMigrated: false);
                using ApiDbContext db = database.Open();

                string previous = Migration.InitialDatabase;
                foreach (string migration in db.Database.GetMigrations())
                {
                    data.Add(previous, migration);
                    previous = migration;
                }

                return data;
            }
        }

        private static string SchemaOf(TestDatabase database)
        {
            using SqliteConnection connection = new SqliteConnection(database.ConnectionString);
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "SELECT type || ' ' || name FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%' ORDER BY 1";

            List<string> lines = new List<string>();
            using (SqliteDataReader reader = command.ExecuteReader())
                while (reader.Read())
                    lines.Add(reader.GetString(0));

            foreach (string table in lines.Where(x => x.StartsWith("table ", StringComparison.Ordinal)).Select(x => x["table ".Length..]).ToList())
            {
                command.CommandText = $"SELECT name || ' ' || type || ' ' || \"notnull\" FROM pragma_table_info('{table}') ORDER BY name";

                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                    lines.Add($"{table}.{reader.GetString(0)}");
            }

            return string.Join("\n", lines);
        }

        // One migration back and the same one again, from every point in the history. Nobody runs
        // these on the server, but one that cannot be undone has no way back when it goes wrong.
        [Theory]
        [MemberData(nameof(EveryStep))]
        public void Every_migration_can_be_undone_and_done_again(string before, string migration)
        {
            using TestDatabase database = new TestDatabase(isMigrated: false);
            using ApiDbContext db = database.Open();
            IMigrator migrator = db.GetService<IMigrator>();

            migrator.Migrate(before);
            string schemaBefore = SchemaOf(database);

            migrator.Migrate(migration);
            string schemaAfter = SchemaOf(database);

            migrator.Migrate(before);
            SchemaOf(database).ShouldBe(schemaBefore);
            db.Database.GetAppliedMigrations().ShouldNotContain(migration);

            migrator.Migrate(migration);
            SchemaOf(database).ShouldBe(schemaAfter);
            db.Database.GetAppliedMigrations().ShouldContain(migration);
        }

        [Fact]
        public void Everything_can_be_undone_down_to_nothing()
        {
            using TestDatabase database = new TestDatabase();
            using ApiDbContext db = database.Open();

            db.GetService<IMigrator>().Migrate(Migration.InitialDatabase);

            SchemaOf(database).ShouldBe("");
        }
    }
}
