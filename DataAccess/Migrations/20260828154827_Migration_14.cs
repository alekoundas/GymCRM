using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_14 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Attachments",
                table: "Mails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Mails",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "SentOn",
                table: "Mails",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Mails",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Every row that exists already was written after Gmail accepted it, so it
            // is history and not a queue. Without this the background sender reads the
            // whole archive as pending and sends all of it again. 1 is SENT.
            migrationBuilder.Sql("UPDATE Mails SET Status = 1, SentOn = CreatedOn;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attachments",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "SentOn",
                table: "Mails");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Mails");
        }
    }
}
