using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoogleRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    ExpiresIn = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedBy_Id = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy_FullName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleRefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoogleRefreshTokens_Id",
                table: "GoogleRefreshTokens",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleRefreshTokens");
        }
    }
}
