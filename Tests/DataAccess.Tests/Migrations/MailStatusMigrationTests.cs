using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tests.Shared;

namespace DataAccess.Tests.Migrations
{
    /// <summary>
    /// Migration 14 gave mails a status. Every mail from before it had already been sent, and one
    /// left as pending would be sent to its member a second time.
    /// </summary>
    public sealed class MailStatusMigrationTests : IDisposable
    {
        private readonly TestDatabase _database = new TestDatabase(isMigrated: false);

        public void Dispose()
        {
            _database.Dispose();
        }

        private void MigrateTo(string migration)
        {
            using ApiDbContext db = _database.Open();
            db.GetService<IMigrator>().Migrate(migration);
        }

        [Fact]
        public void Every_mail_from_before_is_marked_as_sent_when_it_was_written()
        {
            MigrateTo("20260827171116_Migration_13");
            _database.Execute("""
                INSERT INTO AspNetUsers (Id, FirstName, LastName, Address, MedicalHistory, Email, UserName, CreatedOn, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
                VALUES ('11111111-2222-3333-4444-555555555555', '', '', '', '', 'a@gym.test', 'a', '2026-01-01 00:00:00', 0, 0, 0, 0, 0);
                INSERT INTO Mails (Subject, Body, UserId, CreatedBy_Id, CreatedBy_FullName, CreatedOn)
                VALUES ('First', 'Body', '11111111-2222-3333-4444-555555555555', '', '', '2026-02-03 04:05:06'),
                       ('Second', 'Body', '11111111-2222-3333-4444-555555555555', '', '', '2026-03-04 05:06:07');
                """);

            MigrateTo("20260828154827_Migration_14");

            _database.Scalar("SELECT group_concat(Subject || '|' || Status || '|' || SentOn || '|' || Error || '|' || Attachments, ';') FROM Mails ORDER BY Id")
                .ShouldBe("First|1|2026-02-03 04:05:06||;Second|1|2026-03-04 05:06:07||");
        }

        [Fact]
        public void A_database_with_no_mails_is_fine()
        {
            MigrateTo("20260828154827_Migration_14");

            _database.Scalar("SELECT COUNT(*) FROM Mails").ShouldBe(0L);
        }
    }
}
