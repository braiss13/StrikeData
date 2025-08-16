using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class AddCuriousFacts_PerspectiveOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamStats_TeamId",
                table: "TeamStats");

            migrationBuilder.AddColumn<byte>(
                name: "Perspective",
                table: "TeamStats",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.CreateIndex(
                name: "UX_TeamStat_Team_Type_Persp",
                table: "TeamStats",
                columns: new[] { "TeamId", "StatTypeId", "Perspective" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_TeamStat_Team_Type_Persp",
                table: "TeamStats");

            migrationBuilder.DropColumn(
                name: "Perspective",
                table: "TeamStats");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStats_TeamId",
                table: "TeamStats",
                column: "TeamId");
        }
    }
}
