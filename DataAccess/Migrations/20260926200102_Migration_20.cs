using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainGroupCategoryId",
                table: "TrainGroups",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainGroupCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainGroupCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroups_TrainGroupCategoryId",
                table: "TrainGroups",
                column: "TrainGroupCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainGroupCategories_Id",
                table: "TrainGroupCategories",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainGroups_TrainGroupCategories_TrainGroupCategoryId",
                table: "TrainGroups",
                column: "TrainGroupCategoryId",
                principalTable: "TrainGroupCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrainGroups_TrainGroupCategories_TrainGroupCategoryId",
                table: "TrainGroups");

            migrationBuilder.DropTable(
                name: "TrainGroupCategories");

            migrationBuilder.DropIndex(
                name: "IX_TrainGroups_TrainGroupCategoryId",
                table: "TrainGroups");

            migrationBuilder.DropColumn(
                name: "TrainGroupCategoryId",
                table: "TrainGroups");
        }
    }
}
