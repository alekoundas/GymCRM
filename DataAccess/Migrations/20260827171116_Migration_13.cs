using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentWeek",
                table: "WorkoutPlans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutPlanRuleId",
                table: "WorkoutPlans",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkoutPlanRecordings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DurationSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkoutPlanId = table.Column<int>(type: "INTEGER", nullable: true),
                    WorkoutPlanTitle = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlanRecordings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanRecordings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanRecordings_WorkoutPlans_WorkoutPlanId",
                        column: x => x.WorkoutPlanId,
                        principalTable: "WorkoutPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPlanRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlanRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutPlanRuleWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    MaxRecordings = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkoutPlanRuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutPlanRuleWeeks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutPlanRuleWeeks_WorkoutPlanRules_WorkoutPlanRuleId",
                        column: x => x.WorkoutPlanRuleId,
                        principalTable: "WorkoutPlanRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlans_WorkoutPlanRuleId",
                table: "WorkoutPlans",
                column: "WorkoutPlanRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanRecordings_UserId",
                table: "WorkoutPlanRecordings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanRecordings_WorkoutPlanId_StartedOn",
                table: "WorkoutPlanRecordings",
                columns: new[] { "WorkoutPlanId", "StartedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanRules_Name",
                table: "WorkoutPlanRules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutPlanRuleWeeks_WorkoutPlanRuleId_WeekNumber",
                table: "WorkoutPlanRuleWeeks",
                columns: new[] { "WorkoutPlanRuleId", "WeekNumber" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkoutPlans_WorkoutPlanRules_WorkoutPlanRuleId",
                table: "WorkoutPlans",
                column: "WorkoutPlanRuleId",
                principalTable: "WorkoutPlanRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkoutPlans_WorkoutPlanRules_WorkoutPlanRuleId",
                table: "WorkoutPlans");

            migrationBuilder.DropTable(
                name: "WorkoutPlanRecordings");

            migrationBuilder.DropTable(
                name: "WorkoutPlanRuleWeeks");

            migrationBuilder.DropTable(
                name: "WorkoutPlanRules");

            migrationBuilder.DropIndex(
                name: "IX_WorkoutPlans_WorkoutPlanRuleId",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "CurrentWeek",
                table: "WorkoutPlans");

            migrationBuilder.DropColumn(
                name: "WorkoutPlanRuleId",
                table: "WorkoutPlans");
        }
    }
}
