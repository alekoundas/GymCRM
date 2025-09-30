using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainGroupCancellationSubscribers");

            migrationBuilder.AlterColumn<int>(
                name: "RecurrenceDayOfWeek",
                table: "TrainGroupDates",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RecurrenceDayOfMonth",
                table: "TrainGroupDates",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "RecurrenceDayOfWeek",
                table: "TrainGroupDates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "RecurrenceDayOfMonth",
                table: "TrainGroupDates",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TrainGroupCancellationSubscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainGroupDateId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SelectedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupCancellationSubscribers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainGroupCancellationSubscribers_TrainGroupDates_TrainGroupDateId",
                        column: x => x.TrainGroupDateId,
                        principalTable: "TrainGroupDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupCancellationSubscribers_Id",
                table: "TrainGroupCancellationSubscribers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupCancellationSubscribers_TrainGroupDateId",
                table: "TrainGroupCancellationSubscribers",
                column: "TrainGroupDateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupCancellationSubscribers_UserId",
                table: "TrainGroupCancellationSubscribers",
                column: "UserId");
        }
    }
}
