using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class MergeStatsWinTrends_AddPlayerMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stats");

            migrationBuilder.DropTable(
                name: "WinTrends");

            migrationBuilder.AddColumn<float>(
                name: "AwayPerformance",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AwayRecord",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<string>(
                name: "HomeRecord",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<string>(
                name: "OverallRecord",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<float>(
                name: "WinPercentage",
                table: "Teams",
                type: "real",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HomeTeamId = table.Column<int>(type: "integer", nullable: true),
                    AwayTeamId = table.Column<int>(type: "integer", nullable: true),
                    Venue = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HomeScore = table.Column<int>(type: "integer", nullable: true),
                    AwayScore = table.Column<int>(type: "integer", nullable: true),
                    Attendance = table.Column<int>(type: "integer", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_AwayTeamId",
                        column: x => x.AwayTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_HomeTeamId",
                        column: x => x.HomeTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Position = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BattingAverage = table.Column<float>(type: "real", nullable: true),
                    HomeRuns = table.Column<int>(type: "integer", nullable: true),
                    RunsBattedIn = table.Column<int>(type: "integer", nullable: true),
                    GamesPlayed = table.Column<int>(type: "integer", nullable: true),
                    StolenBases = table.Column<int>(type: "integer", nullable: true),
                    OnBasePercentage = table.Column<float>(type: "real", nullable: true),
                    SluggingPercentage = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Name",
                table: "Teams",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matches_AwayTeamId",
                table: "Matches",
                column: "AwayTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_HomeTeamId",
                table: "Matches",
                column: "HomeTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropIndex(
                name: "IX_Teams_Name",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "AwayPerformance",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "AwayRecord",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "HitsPerGame",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "HomePerformance",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "HomeRecord",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Last3Games",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "LastGame",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "OverallRecord",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RunsPerGame",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "SeasonYear",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "WinPercentage",
                table: "Teams");

            migrationBuilder.CreateTable(
                name: "Stats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    AwayPerformance = table.Column<float>(type: "real", nullable: true),
                    HitsPerGame = table.Column<float>(type: "real", nullable: true),
                    HomePerformance = table.Column<float>(type: "real", nullable: true),
                    Last3Games = table.Column<float>(type: "real", nullable: true),
                    LastGame = table.Column<float>(type: "real", nullable: true),
                    RunsPerGame = table.Column<float>(type: "real", nullable: true),
                    SeasonYear = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stats_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WinTrends",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    AwayRecord = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HomeRecord = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OverallRecord = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SeasonYear = table.Column<int>(type: "integer", nullable: false),
                    WinPercentage = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WinTrends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WinTrends_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stats_TeamId",
                table: "Stats",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_WinTrends_TeamId",
                table: "WinTrends",
                column: "TeamId");
        }
    }
}
