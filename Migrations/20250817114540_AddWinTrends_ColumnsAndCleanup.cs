using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class AddWinTrends_ColumnsAndCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayRecord",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "HomeRecord",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "OverallRecord",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "WinPercentage",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "WinLossRecord",
                table: "TeamStats",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "WinPct",
                table: "TeamStats",
                type: "real",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WinLossRecord",
                table: "TeamStats");

            migrationBuilder.DropColumn(
                name: "WinPct",
                table: "TeamStats");

            migrationBuilder.AddColumn<string>(
                name: "AwayRecord",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeRecord",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OverallRecord",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "WinPercentage",
                table: "Teams",
                type: "real",
                nullable: true);
        }
    }
}
