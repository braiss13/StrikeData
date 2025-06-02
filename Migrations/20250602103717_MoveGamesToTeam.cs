using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class MoveGamesToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Games",
                table: "TeamStats");

            migrationBuilder.AddColumn<int>(
                name: "Games",
                table: "Teams",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Games",
                table: "Teams");

            migrationBuilder.AddColumn<int>(
                name: "Games",
                table: "TeamStats",
                type: "integer",
                nullable: true);
        }
    }
}
