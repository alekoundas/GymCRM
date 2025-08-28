using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_CancellationSubscriberId",
                table: "TrainGroupCancellationSubscribers");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipants_AspNetUsers_ParticipantId",
                table: "TrainGroupParticipants");

            migrationBuilder.DropTable(
                name: "TrainGroupRepeatingParticipants");

            migrationBuilder.RenameColumn(
                name: "ParticipantId",
                table: "TrainGroupParticipants",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipants_ParticipantId",
                table: "TrainGroupParticipants",
                newName: "IX_TrainGroupParticipants_UserId");

            migrationBuilder.RenameColumn(
                name: "CancellationSubscriberId",
                table: "TrainGroupCancellationSubscribers",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupCancellationSubscribers_CancellationSubscriberId",
                table: "TrainGroupCancellationSubscribers",
                newName: "IX_TrainGroupCancellationSubscribers_UserId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SelectedDate",
                table: "TrainGroupParticipants",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "TEXT");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_UserId",
                table: "TrainGroupCancellationSubscribers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipants_AspNetUsers_UserId",
                table: "TrainGroupParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_UserId",
                table: "TrainGroupCancellationSubscribers");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipants_AspNetUsers_UserId",
                table: "TrainGroupParticipants");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TrainGroupParticipants",
                newName: "ParticipantId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupParticipants_UserId",
                table: "TrainGroupParticipants",
                newName: "IX_TrainGroupParticipants_ParticipantId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TrainGroupCancellationSubscribers",
                newName: "CancellationSubscriberId");

            migrationBuilder.RenameIndex(
                name: "IX_TrainGroupCancellationSubscribers_UserId",
                table: "TrainGroupCancellationSubscribers",
                newName: "IX_TrainGroupCancellationSubscribers_CancellationSubscriberId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SelectedDate",
                table: "TrainGroupParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "TEXT",
                oldNullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupRepeatingParticipants_RepeatingTrainGroupsId",
                table: "TrainGroupRepeatingParticipants",
                column: "RepeatingTrainGroupsId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupCancellationSubscribers_AspNetUsers_CancellationSubscriberId",
                table: "TrainGroupCancellationSubscribers",
                column: "CancellationSubscriberId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipants_AspNetUsers_ParticipantId",
                table: "TrainGroupParticipants",
                column: "ParticipantId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
