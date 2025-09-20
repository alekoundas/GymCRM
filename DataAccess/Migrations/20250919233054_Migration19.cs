using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneNumber_AspNetUsers_UserId",
                table: "PhoneNumber");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroup_AspNetUsers_TrainerId",
                table: "TrainGroup");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupDate_TrainGroup_TrainGroupId",
                table: "TrainGroupDate");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupDateCancellationSubscriber_AspNetUsers_UserId",
                table: "TrainGroupDateCancellationSubscriber");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupDateCancellationSubscriber_TrainGroupDate_TrainGroupDateId",
                table: "TrainGroupDateCancellationSubscriber");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipant_AspNetUsers_UserId",
                table: "TrainGroupParticipant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipant_TrainGroupDate_TrainGroupDateId",
                table: "TrainGroupParticipant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipant_TrainGroup_TrainGroupId",
                table: "TrainGroupParticipant");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipantUnavailableDate_TrainGroupParticipant_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDate");

            migrationBuilder.DropTable(
                name: "EmailHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupParticipantUnavailableDate",
                table: "TrainGroupParticipantUnavailableDate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupParticipant",
                table: "TrainGroupParticipant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupDateCancellationSubscriber",
                table: "TrainGroupDateCancellationSubscriber");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupDate",
                table: "TrainGroupDate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroup",
                table: "TrainGroup");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneNumber",
                table: "PhoneNumber");

            migrationBuilder.RenameTable(
                name: "TrainGroupParticipantUnavailableDate",
                newName: "TrainGroupParticipantUnavailableDates");

            migrationBuilder.RenameTable(
                name: "TrainGroupParticipant",
                newName: "TrainGroupParticipants");

            migrationBuilder.RenameTable(
                name: "TrainGroupDateCancellationSubscriber",
                newName: "TrainGroupCancellationSubscribers");

            migrationBuilder.RenameTable(
                name: "TrainGroupDate",
                newName: "TrainGroupDates");

            migrationBuilder.RenameTable(
                name: "TrainGroup",
                newName: "TrainGroups");

            migrationBuilder.RenameTable(
                name: "PhoneNumber",
                newName: "PhoneNumbers");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipantUnavailableDate_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDates",
                newName: "IX_TrainGroupParticipantUnavailableDates_TrainGroupParticipantId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipantUnavailableDate_Id",
                table: "TrainGroupParticipantUnavailableDates",
                newName: "IX_TrainGroupParticipantUnavailableDates_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipant_UserId",
                table: "TrainGroupParticipants",
                newName: "IX_TrainGroupParticipants_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipant_TrainGroupId",
                table: "TrainGroupParticipants",
                newName: "IX_TrainGroupParticipants_TrainGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipant_TrainGroupDateId",
                table: "TrainGroupParticipants",
                newName: "IX_TrainGroupParticipants_TrainGroupDateId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipant_Id",
                table: "TrainGroupParticipants",
                newName: "IX_TrainGroupParticipants_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDateCancellationSubscriber_UserId",
                table: "TrainGroupCancellationSubscribers",
                newName: "IX_TrainGroupCancellationSubscribers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDateCancellationSubscriber_TrainGroupDateId",
                table: "TrainGroupCancellationSubscribers",
                newName: "IX_TrainGroupCancellationSubscribers_TrainGroupDateId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDateCancellationSubscriber_Id",
                table: "TrainGroupCancellationSubscribers",
                newName: "IX_TrainGroupCancellationSubscribers_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDate_TrainGroupId",
                table: "TrainGroupDates",
                newName: "IX_TrainGroupDates_TrainGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDate_Id",
                table: "TrainGroupDates",
                newName: "IX_TrainGroupDates_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroup_TrainerId",
                table: "TrainGroups",
                newName: "IX_TrainGroups_TrainerId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroup_Id",
                table: "TrainGroups",
                newName: "IX_TrainGroups_Id");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneNumber_UserId",
                table: "PhoneNumbers",
                newName: "IX_PhoneNumbers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneNumber_Id",
                table: "PhoneNumbers",
                newName: "IX_PhoneNumbers_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupParticipantUnavailableDates",
                table: "TrainGroupParticipantUnavailableDates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupParticipants",
                table: "TrainGroupParticipants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupCancellationSubscribers",
                table: "TrainGroupCancellationSubscribers",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupDates",
                table: "TrainGroupDates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroups",
                table: "TrainGroups",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneNumbers",
                table: "PhoneNumbers",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Mails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Subject = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mails_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mails_Id",
                table: "Mails",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mails_UserId",
                table: "Mails",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneNumbers_AspNetUsers_UserId",
                table: "PhoneNumbers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_UserId",
                table: "TrainGroupCancellationSubscribers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_TrainGroupDates_TrainGroupDateId",
                table: "TrainGroupCancellationSubscribers",
                column: "TrainGroupDateId",
                principalTable: "TrainGroupDates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupDates_TrainGroups_TrainGroupId",
                table: "TrainGroupDates",
                column: "TrainGroupId",
                principalTable: "TrainGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipants_AspNetUsers_UserId",
                table: "TrainGroupParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipants_TrainGroupDates_TrainGroupDateId",
                table: "TrainGroupParticipants",
                column: "TrainGroupDateId",
                principalTable: "TrainGroupDates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipants_TrainGroups_TrainGroupId",
                table: "TrainGroupParticipants",
                column: "TrainGroupId",
                principalTable: "TrainGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipantUnavailableDates_TrainGroupParticipants_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDates",
                column: "TrainGroupParticipantId",
                principalTable: "TrainGroupParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroups_AspNetUsers_TrainerId",
                table: "TrainGroups",
                column: "TrainerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhoneNumbers_AspNetUsers_UserId",
                table: "PhoneNumbers");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_UserId",
                table: "TrainGroupCancellationSubscribers");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_TrainGroupDates_TrainGroupDateId",
                table: "TrainGroupCancellationSubscribers");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupDates_TrainGroups_TrainGroupId",
                table: "TrainGroupDates");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipants_AspNetUsers_UserId",
                table: "TrainGroupParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipants_TrainGroupDates_TrainGroupDateId",
                table: "TrainGroupParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipants_TrainGroups_TrainGroupId",
                table: "TrainGroupParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipantUnavailableDates_TrainGroupParticipants_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDates");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroups_AspNetUsers_TrainerId",
                table: "TrainGroups");

            migrationBuilder.DropTable(
                name: "Mails");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroups",
                table: "TrainGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupParticipantUnavailableDates",
                table: "TrainGroupParticipantUnavailableDates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupParticipants",
                table: "TrainGroupParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupDates",
                table: "TrainGroupDates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TrainGroupCancellationSubscribers",
                table: "TrainGroupCancellationSubscribers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PhoneNumbers",
                table: "PhoneNumbers");

            migrationBuilder.RenameTable(
                name: "TrainGroups",
                newName: "TrainGroup");

            migrationBuilder.RenameTable(
                name: "TrainGroupParticipantUnavailableDates",
                newName: "TrainGroupParticipantUnavailableDate");

            migrationBuilder.RenameTable(
                name: "TrainGroupParticipants",
                newName: "TrainGroupParticipant");

            migrationBuilder.RenameTable(
                name: "TrainGroupDates",
                newName: "TrainGroupDate");

            migrationBuilder.RenameTable(
                name: "TrainGroupCancellationSubscribers",
                newName: "TrainGroupDateCancellationSubscriber");

            migrationBuilder.RenameTable(
                name: "PhoneNumbers",
                newName: "PhoneNumber");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroups_TrainerId",
                table: "TrainGroup",
                newName: "IX_TrainGroup_TrainerId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroups_Id",
                table: "TrainGroup",
                newName: "IX_TrainGroup_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipantUnavailableDates_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDate",
                newName: "IX_TrainGroupParticipantUnavailableDate_TrainGroupParticipantId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipantUnavailableDates_Id",
                table: "TrainGroupParticipantUnavailableDate",
                newName: "IX_TrainGroupParticipantUnavailableDate_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipants_UserId",
                table: "TrainGroupParticipant",
                newName: "IX_TrainGroupParticipant_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipants_TrainGroupId",
                table: "TrainGroupParticipant",
                newName: "IX_TrainGroupParticipant_TrainGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipants_TrainGroupDateId",
                table: "TrainGroupParticipant",
                newName: "IX_TrainGroupParticipant_TrainGroupDateId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipants_Id",
                table: "TrainGroupParticipant",
                newName: "IX_TrainGroupParticipant_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDates_TrainGroupId",
                table: "TrainGroupDate",
                newName: "IX_TrainGroupDate_TrainGroupId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupDates_Id",
                table: "TrainGroupDate",
                newName: "IX_TrainGroupDate_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupCancellationSubscribers_UserId",
                table: "TrainGroupDateCancellationSubscriber",
                newName: "IX_TrainGroupDateCancellationSubscriber_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupCancellationSubscribers_TrainGroupDateId",
                table: "TrainGroupDateCancellationSubscriber",
                newName: "IX_TrainGroupDateCancellationSubscriber_TrainGroupDateId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupCancellationSubscribers_Id",
                table: "TrainGroupDateCancellationSubscriber",
                newName: "IX_TrainGroupDateCancellationSubscriber_Id");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneNumbers_UserId",
                table: "PhoneNumber",
                newName: "IX_PhoneNumber_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_PhoneNumbers_Id",
                table: "PhoneNumber",
                newName: "IX_PhoneNumber_Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroup",
                table: "TrainGroup",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupParticipantUnavailableDate",
                table: "TrainGroupParticipantUnavailableDate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupParticipant",
                table: "TrainGroupParticipant",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupDate",
                table: "TrainGroupDate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TrainGroupDateCancellationSubscriber",
                table: "TrainGroupDateCancellationSubscriber",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PhoneNumber",
                table: "PhoneNumber",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "EmailHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_Id",
                table: "EmailHistory",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailHistory_UserId",
                table: "EmailHistory",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhoneNumber_AspNetUsers_UserId",
                table: "PhoneNumber",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroup_AspNetUsers_TrainerId",
                table: "TrainGroup",
                column: "TrainerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupDate_TrainGroup_TrainGroupId",
                table: "TrainGroupDate",
                column: "TrainGroupId",
                principalTable: "TrainGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupDateCancellationSubscriber_AspNetUsers_UserId",
                table: "TrainGroupDateCancellationSubscriber",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupDateCancellationSubscriber_TrainGroupDate_TrainGroupDateId",
                table: "TrainGroupDateCancellationSubscriber",
                column: "TrainGroupDateId",
                principalTable: "TrainGroupDate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipant_AspNetUsers_UserId",
                table: "TrainGroupParticipant",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipant_TrainGroupDate_TrainGroupDateId",
                table: "TrainGroupParticipant",
                column: "TrainGroupDateId",
                principalTable: "TrainGroupDate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipant_TrainGroup_TrainGroupId",
                table: "TrainGroupParticipant",
                column: "TrainGroupId",
                principalTable: "TrainGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipantUnavailableDate_TrainGroupParticipant_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDate",
                column: "TrainGroupParticipantId",
                principalTable: "TrainGroupParticipant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
