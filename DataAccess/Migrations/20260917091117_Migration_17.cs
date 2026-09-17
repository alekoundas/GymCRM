using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_17 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupΑttendances_TrainGroups_TrainGroupId",
                table: "TrainGroupΑttendances");

            migrationBuilder.AlterColumn<int>(
                name: "TrainGroupId",
                table: "TrainGroupΑttendances",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "TrainGroupDescription",
                table: "TrainGroupΑttendances",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainGroupDuration",
                table: "TrainGroupΑttendances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "TrainGroupStartOn",
                table: "TrainGroupΑttendances",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TrainGroupTitle",
                table: "TrainGroupΑttendances",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrainerFullName",
                table: "TrainGroupΑttendances",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TrainerId",
                table: "TrainGroupΑttendances",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupΑttendances_TrainGroups_TrainGroupId",
                table: "TrainGroupΑttendances",
                column: "TrainGroupId",
                principalTable: "TrainGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroupΑttendances_TrainGroups_TrainGroupId",
                table: "TrainGroupΑttendances");

            migrationBuilder.DropColumn(
                name: "TrainGroupDescription",
                table: "TrainGroupΑttendances");

            migrationBuilder.DropColumn(
                name: "TrainGroupDuration",
                table: "TrainGroupΑttendances");

            migrationBuilder.DropColumn(
                name: "TrainGroupStartOn",
                table: "TrainGroupΑttendances");

            migrationBuilder.DropColumn(
                name: "TrainGroupTitle",
                table: "TrainGroupΑttendances");

            migrationBuilder.DropColumn(
                name: "TrainerFullName",
                table: "TrainGroupΑttendances");

            migrationBuilder.DropColumn(
                name: "TrainerId",
                table: "TrainGroupΑttendances");

            migrationBuilder.AlterColumn<int>(
                name: "TrainGroupId",
                table: "TrainGroupΑttendances",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroupΑttendances_TrainGroups_TrainGroupId",
                table: "TrainGroupΑttendances",
                column: "TrainGroupId",
                principalTable: "TrainGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
