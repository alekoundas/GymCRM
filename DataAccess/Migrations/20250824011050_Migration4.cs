using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRepeating",
                table: "TrainGroups");

            migrationBuilder.RenameColumn(
                name: "RepeatingTrainGroupType",
                table: "TrainGroupDates",
                newName: "RecurringTrainGroupType");

            migrationBuilder.AlterColumn<string>(
                name: "RecurrenceDayOfWeek",
                table: "TrainGroupDates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RecurringTrainGroupType",
                table: "TrainGroupDates",
                newName: "RepeatingTrainGroupType");

            migrationBuilder.AddColumn<bool>(
                name: "IsRepeating",
                table: "TrainGroups",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "RecurrenceDayOfWeek",
                table: "TrainGroupDates",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
