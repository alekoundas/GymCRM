using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class Migration30 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserStatus_UserStatusId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStatus",
                table: "UserStatus");

            migrationBuilder.RenameTable(
                name: "UserStatus",
                newName: "UserStatuses");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserStatuses_Id",
                table: "UserStatuses",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserStatuses_UserStatusId",
                table: "AspNetUsers",
                column: "UserStatusId",
                principalTable: "UserStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_UserStatuses_UserStatusId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserStatuses",
                table: "UserStatuses");

            migrationBuilder.DropIndex(
                name: "IX_UserStatuses_Id",
                table: "UserStatuses");

            migrationBuilder.RenameTable(
                name: "UserStatuses",
                newName: "UserStatus");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserStatus",
                table: "UserStatus",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_UserStatus_UserStatusId",
                table: "AspNetUsers",
                column: "UserStatusId",
                principalTable: "UserStatus",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
