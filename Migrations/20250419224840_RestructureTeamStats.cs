using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class RestructureTeamStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayPerformance",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "HitsPerGame",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "HomePerformance",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Last3Games",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "LastGame",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RunsPerGame",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SeasonYear",
                table: "Teams");

            migrationBuilder.CreateTable(
                name: "StatTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TeamStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    StatTypeId = table.Column<int>(type: "integer", nullable: false),
                    CurrentSeason = table.Column<int>(type: "integer", nullable: false),
                    Games = table.Column<int>(type: "integer", nullable: true),
                    Total = table.Column<float>(type: "real", nullable: true),
                    Last3Games = table.Column<float>(type: "real", nullable: true),
                    LastGame = table.Column<float>(type: "real", nullable: true),
                    Home = table.Column<float>(type: "real", nullable: true),
                    Away = table.Column<float>(type: "real", nullable: true),
                    PrevSeason = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamStats_StatTypes_StatTypeId",
                        column: x => x.StatTypeId,
                        principalTable: "StatTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamStats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamStats_StatTypeId",
                table: "TeamStats",
                column: "StatTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamStats_TeamId",
                table: "TeamStats",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamStats");

            migrationBuilder.DropTable(
                name: "StatTypes");

            migrationBuilder.AddColumn<float>(
                name: "AwayPerformance",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "HitsPerGame",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "HomePerformance",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "Last3Games",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LastGame",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "RunsPerGame",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeasonYear",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
