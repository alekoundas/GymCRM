﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainGroupParticipantUnavailableDate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UnavailableDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrainGroupParticipantId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupParticipantUnavailableDate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroupParticipantUnavailableDate_TrainGroupParticipants_TrainGroupParticipantId",
                        column: x => x.TrainGroupParticipantId,
                        principalTable: "TrainGroupParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupParticipantUnavailableDate_TrainGroupParticipantId",
                table: "TrainGroupParticipantUnavailableDate",
                column: "TrainGroupParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainGroupParticipantUnavailableDate");
        }
    }
}
