using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCircular",
                table: "Exercises");

            migrationBuilder.AddColumn<bool>(
                name: "IsCircular",
                table: "WorkoutPlans",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCircular",
                table: "WorkoutPlans");

            migrationBuilder.AddColumn<bool>(
                name: "IsCircular",
                table: "Exercises",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
