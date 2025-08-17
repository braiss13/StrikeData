using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class RenameStatTypesToTeamStatTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatTypes_StatCategories_StatCategoryId",
                table: "StatTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamStats_StatTypes_StatTypeId",
                table: "TeamStats");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StatTypes",
                table: "StatTypes");

            migrationBuilder.RenameTable(
                name: "StatTypes",
                newName: "TeamStatTypes");

            migrationBuilder.RenameIndex(
                name: "IX_StatTypes_StatCategoryId",
                table: "TeamStatTypes",
                newName: "IX_TeamStatTypes_StatCategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamStatTypes",
                table: "TeamStatTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamStats_TeamStatTypes_StatTypeId",
                table: "TeamStats",
                column: "StatTypeId",
                principalTable: "TeamStatTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamStatTypes_StatCategories_StatCategoryId",
                table: "TeamStatTypes",
                column: "StatCategoryId",
                principalTable: "StatCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamStats_TeamStatTypes_StatTypeId",
                table: "TeamStats");

            migrationBuilder.DropForeignKey(
                name: "FK_TeamStatTypes_StatCategories_StatCategoryId",
                table: "TeamStatTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamStatTypes",
                table: "TeamStatTypes");

            migrationBuilder.RenameTable(
                name: "TeamStatTypes",
                newName: "StatTypes");

            migrationBuilder.RenameIndex(
                name: "IX_TeamStatTypes_StatCategoryId",
                table: "StatTypes",
                newName: "IX_StatTypes_StatCategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StatTypes",
                table: "StatTypes",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StatTypes_StatCategories_StatCategoryId",
                table: "StatTypes",
                column: "StatCategoryId",
                principalTable: "StatCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TeamStats_StatTypes_StatTypeId",
                table: "TeamStats",
                column: "StatTypeId",
                principalTable: "StatTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
