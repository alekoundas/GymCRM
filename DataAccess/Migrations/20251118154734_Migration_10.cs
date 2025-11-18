using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TrainGroupΑttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ΑttendanceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TrainGroupId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupΑttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainGroupΑttendances_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainGroupΑttendances_TrainGroups_TrainGroupId",
                        column: x => x.TrainGroupId,
                        principalTable: "TrainGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupΑttendances_Id",
                table: "TrainGroupΑttendances",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupΑttendances_TrainGroupId",
                table: "TrainGroupΑttendances",
                column: "TrainGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupΑttendances_UserId",
                table: "TrainGroupΑttendances",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainGroupΑttendances");
        }
    }
}
