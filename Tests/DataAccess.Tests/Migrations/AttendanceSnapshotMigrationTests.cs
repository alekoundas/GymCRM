using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tests.Shared;

namespace DataAccess.Tests.Migrations
{
    /// <summary>
    /// Migration 18 copied each session down onto the attendances from before they kept their own.
    /// </summary>
    public sealed class AttendanceSnapshotMigrationTests : IDisposable
    {
        private const string Before = "20260917091117_Migration_17";
        private const string After = "20260917091754_Migration_18_BackfillAttendanceSnapshots";
        private const string TrainerId = "11111111-2222-3333-4444-555555555555";

        private readonly TestDatabase _database = new TestDatabase(isMigrated: false);

        public AttendanceSnapshotMigrationTests()
        {
            MigrateTo(Before);
        }

        public void Dispose()
        {
            _database.Dispose();
        }

        private void MigrateTo(string migration)
        {
            using ApiDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        private void AddGroup(string firstName = "Νίκος", string lastName = "Παπάς", string description = "'Easy'")
        {
            _database.Execute($"""
                INSERT INTO AspNetUsers (Id, FirstName, LastName, Address, MedicalHistory, Email, UserName, CreatedOn, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
                VALUES ('{TrainerId}', '{firstName}', '{lastName}', '', '', 'a@gym.test', 'a', '2026-01-01 00:00:00', 0, 0, 0, 0, 0);
                INSERT INTO TrainGroups (Id, Title, Description, Duration, StartOn, MaxParticipants, TrainerId, CreatedBy_Id, CreatedBy_FullName, CreatedOn)
                VALUES (7, 'Pilates', {description}, '2000-01-01 01:00:00', '2000-01-01 18:30:00', 5, '{TrainerId}', '', '', '2026-01-01 00:00:00');
                """);
        }

        private void AddAttendance(string groupId, string title)
        {
            _database.Execute($"""
                INSERT INTO TrainGroupΑttendances (AttendanceDate, TrainGroupId, UserId, TrainGroupTitle, TrainGroupDescription, TrainGroupStartOn, TrainGroupDuration, TrainerFullName, TrainerId, CreatedBy_Id, CreatedBy_FullName, CreatedOn)
                VALUES ('2026-02-02 00:00:00', {groupId}, '{TrainerId}', {title}, 'own', '0001-01-01 00:00:00', '0001-01-01 00:00:00', 'own', NULL, '', '', '2026-02-02 00:00:00');
                """);
        }

        private object? Snapshot()
        {
            return _database.Scalar("""
                SELECT TrainGroupTitle || '|' || TrainGroupDescription || '|' || TrainGroupStartOn || '|' || TrainGroupDuration || '|' || TrainerFullName || '|' || COALESCE(TrainerId, 'none')
                FROM TrainGroupΑttendances
                """);
        }

        [Fact]
        public void An_attendance_with_no_title_is_filled_in_from_its_group()
        {
            AddGroup();
            AddAttendance("7", "''");

            MigrateTo(After);

            Snapshot().ShouldBe($"Pilates|Easy|2000-01-01 18:30:00|2000-01-01 01:00:00|Νίκος Παπάς|{TrainerId}");
        }

        [Theory]
        [InlineData("Νίκος", "", "Νίκος")]
        [InlineData("", "Παπάς", "Παπάς")]
        [InlineData("", "", "")]
        public void The_trainers_name_has_no_spare_space(string firstName, string lastName, string expected)
        {
            AddGroup(firstName, lastName);
            AddAttendance("7", "''");

            MigrateTo(After);

            Snapshot().ShouldBe($"Pilates|Easy|2000-01-01 18:30:00|2000-01-01 01:00:00|{expected}|{TrainerId}");
        }

        [Fact]
        public void A_group_with_no_description_leaves_an_empty_one()
        {
            AddGroup(description: "NULL");
            AddAttendance("7", "''");

            MigrateTo(After);

            Snapshot().ShouldBe($"Pilates||2000-01-01 18:30:00|2000-01-01 01:00:00|Νίκος Παπάς|{TrainerId}");
        }

        // It already says what the session was, and the group may have been renamed since.
        [Fact]
        public void An_attendance_with_a_title_is_left_alone()
        {
            AddGroup();
            AddAttendance("7", "'Yoga'");

            MigrateTo(After);

            Snapshot().ShouldBe("Yoga|own|0001-01-01 00:00:00|0001-01-01 00:00:00|own|none");
        }

        [Fact]
        public void An_attendance_with_no_group_is_left_alone()
        {
            AddGroup();
            AddAttendance("NULL", "''");

            MigrateTo(After);

            Snapshot().ShouldBe("|own|0001-01-01 00:00:00|0001-01-01 00:00:00|own|none");
        }

        // Undoing it would blank the only copy of what a past session was.
        [Fact]
        public void Going_back_keeps_what_was_filled_in()
        {
            AddGroup();
            AddAttendance("7", "''");
            MigrateTo(After);

            MigrateTo(Before);

            Snapshot().ShouldBe($"Pilates|Easy|2000-01-01 18:30:00|2000-01-01 01:00:00|Νίκος Παπάς|{TrainerId}");
        }
    }
}
