using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TrainGroups",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TrainGroupDateId",
                table: "TrainGroupParticipants",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "TrainGroupId",
                table: "TrainGroupParticipants",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupParticipants_TrainGroupId",
                table: "TrainGroupParticipants",
                column: "TrainGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupParticipants_TrainGroups_TrainGroupId",
                table: "TrainGroupParticipants",
                column: "TrainGroupId",
                principalTable: "TrainGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupParticipants_TrainGroups_TrainGroupId",
                table: "TrainGroupParticipants");

            migrationBuilder.DropIndex(
                name: "IX_TrainGroupParticipants_TrainGroupId",
                table: "TrainGroupParticipants");

            migrationBuilder.DropColumn(
                name: "TrainGroupId",
                table: "TrainGroupParticipants");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "TrainGroups",
                type: "TEXT",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "TrainGroupDateId",
                table: "TrainGroupParticipants",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
