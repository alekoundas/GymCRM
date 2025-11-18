using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ΑttendanceDate",
                table: "TrainGroupΑttendances",
                newName: "AttendanceDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AttendanceDate",
                table: "TrainGroupΑttendances",
                newName: "ΑttendanceDate");
        }
    }
}
