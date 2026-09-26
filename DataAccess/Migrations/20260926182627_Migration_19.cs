using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RecurringEndOnDate",
                table: "TrainGroupParticipants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RemovedBy_FullName",
                table: "TrainGroupParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RemovedBy_Id",
                table: "TrainGroupParticipants",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedOn",
                table: "TrainGroupParticipants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecurringEndOnDate",
                table: "TrainGroupParticipants");

            migrationBuilder.DropColumn(
                name: "RemovedBy_FullName",
                table: "TrainGroupParticipants");

            migrationBuilder.DropColumn(
                name: "RemovedBy_Id",
                table: "TrainGroupParticipants");

            migrationBuilder.DropColumn(
                name: "RemovedOn",
                table: "TrainGroupParticipants");
        }
    }
}
