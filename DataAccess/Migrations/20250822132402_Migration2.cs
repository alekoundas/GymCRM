using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInformations_Customers_CustomerId",
                table: "ContactInformations");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_ContactInformations_CustomerId",
                table: "ContactInformations");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ContactInformations");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "ContactInformations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ContactInformations",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TrainGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsRepeating = table.Column<bool>(type: "INTEGER", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    MaxParticipants = table.Column<int>(type: "INTEGER", nullable: false),
                    TrainerId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroups_AspNetUsers_TrainerId",
                        column: x => x.TrainerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainGroupDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RepeatingTrainGroupType = table.Column<string>(type: "TEXT", nullable: true),
                    StartOn = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    FixedDay = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    RecurrenceDayOfWeek = table.Column<int>(type: "INTEGER", nullable: true),
                    RecurrenceDayOfMonth = table.Column<int>(type: "INTEGER", nullable: true),
                    TrainGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroupDates_TrainGroups_TrainGroupId",
                        column: x => x.TrainGroupId,
                        principalTable: "TrainGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainGroupRepeatingParticipants",
                columns: table => new
                {
                    RepeatingParticipantsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RepeatingTrainGroupsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupRepeatingParticipants", x => new { x.RepeatingParticipantsId, x.RepeatingTrainGroupsId });
                    table.ForeignKey(
                        name: "FK_TrainGroupRepeatingParticipants_AspNetUsers_RepeatingParticipantsId",
                        column: x => x.RepeatingParticipantsId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainGroupRepeatingParticipants_TrainGroups_RepeatingTrainGroupsId",
                        column: x => x.RepeatingTrainGroupsId,
                        principalTable: "TrainGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainGroupCancellationSubscribers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SelectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrainGroupDateId = table.Column<int>(type: "INTEGER", nullable: false),
                    CancellationSubscriberId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupCancellationSubscribers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_CancellationSubscriberId",
                        column: x => x.CancellationSubscriberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainGroupCancellationSubscribers_TrainGroupDates_TrainGroupDateId",
                        column: x => x.TrainGroupDateId,
                        principalTable: "TrainGroupDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainGroupParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SelectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrainGroupDateId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroupParticipants_AspNetUsers_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainGroupParticipants_TrainGroupDates_TrainGroupDateId",
                        column: x => x.TrainGroupDateId,
                        principalTable: "TrainGroupDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInformations_UserId",
                table: "ContactInformations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupCancellationSubscribers_CancellationSubscriberId",
                table: "TrainGroupCancellationSubscribers",
                column: "CancellationSubscriberId");

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
                name: "IX_TrainGroupDates_Id",
                table: "TrainGroupDates",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupDates_TrainGroupId",
                table: "TrainGroupDates",
                column: "TrainGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupParticipants_Id",
                table: "TrainGroupParticipants",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupParticipants_ParticipantId",
                table: "TrainGroupParticipants",
                column: "ParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupParticipants_TrainGroupDateId",
                table: "TrainGroupParticipants",
                column: "TrainGroupDateId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupRepeatingParticipants_RepeatingTrainGroupsId",
                table: "TrainGroupRepeatingParticipants",
                column: "RepeatingTrainGroupsId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroups_Id",
                table: "TrainGroups",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroups_TrainerId",
                table: "TrainGroups",
                column: "TrainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInformations_AspNetUsers_UserId",
                table: "ContactInformations",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ContactInformations_AspNetUsers_UserId",
                table: "ContactInformations");

            migrationBuilder.DropTable(
                name: "TrainGroupCancellationSubscribers");

            migrationBuilder.DropTable(
                name: "TrainGroupParticipants");

            migrationBuilder.DropTable(
                name: "TrainGroupRepeatingParticipants");

            migrationBuilder.DropTable(
                name: "TrainGroupDates");

            migrationBuilder.DropTable(
                name: "TrainGroups");

            migrationBuilder.DropIndex(
                name: "IX_ContactInformations_UserId",
                table: "ContactInformations");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_Email",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ContactInformations");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "ContactInformations",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "ContactInformations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", nullable: false),
                    LastName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInformations_CustomerId",
                table: "ContactInformations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Id",
                table: "Customers",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ContactInformations_Customers_CustomerId",
                table: "ContactInformations",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
