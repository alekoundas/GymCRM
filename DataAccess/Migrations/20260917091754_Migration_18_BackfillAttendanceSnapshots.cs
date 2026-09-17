using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_18_BackfillAttendanceSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data only. The previous migration gave an attendance its own copy of the
            // session it belongs to, but rows written before that are still blank, so a
            // week that has already happened would read as an untitled entry. This fills
            // them in from the group each row still points at.
            //
            // Written by hand because the ef tooling only diffs the model and cannot
            // produce a data migration.
            //
            // Rows whose group has already been deleted cannot be recovered - the old
            // cascade removed them along with the group - and rows the new code wrote
            // already carry a snapshot, so both are left alone. That last part is what
            // makes this safe to run twice, and stops it overwriting a snapshot whose
            // group has since been renamed.
            migrationBuilder.Sql(@"
                UPDATE TrainGroupΑttendances
                SET
                    TrainGroupTitle = COALESCE((
                        SELECT g.Title FROM TrainGroups g
                        WHERE g.Id = TrainGroupΑttendances.TrainGroupId), ''),

                    TrainGroupDescription = COALESCE((
                        SELECT g.Description FROM TrainGroups g
                        WHERE g.Id = TrainGroupΑttendances.TrainGroupId), ''),

                    TrainGroupStartOn = COALESCE((
                        SELECT g.StartOn FROM TrainGroups g
                        WHERE g.Id = TrainGroupΑttendances.TrainGroupId), TrainGroupStartOn),

                    TrainGroupDuration = COALESCE((
                        SELECT g.Duration FROM TrainGroups g
                        WHERE g.Id = TrainGroupΑttendances.TrainGroupId), TrainGroupDuration),

                    TrainerId = (
                        SELECT g.TrainerId FROM TrainGroups g
                        WHERE g.Id = TrainGroupΑttendances.TrainGroupId),

                    TrainerFullName = COALESCE((
                        SELECT TRIM(COALESCE(u.FirstName, '') || ' ' || COALESCE(u.LastName, ''))
                        FROM TrainGroups g
                        JOIN AspNetUsers u ON u.Id = g.TrainerId
                        WHERE g.Id = TrainGroupΑttendances.TrainGroupId), '')
                WHERE TrainGroupId IS NOT NULL
                  AND (TrainGroupTitle IS NULL OR TrainGroupTitle = '');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing. Undoing this would blank the only copy of what a past session was,
            // which is the very thing it exists to keep.
        }
    }
}
