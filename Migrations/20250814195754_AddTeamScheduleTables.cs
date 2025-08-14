using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamScheduleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    GameNumber = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsHome = table.Column<bool>(type: "boolean", nullable: false),
                    OpponentTeamId = table.Column<int>(type: "integer", nullable: true),
                    OpponentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Score = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Decision = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Record = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamGames", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamGames_Teams_OpponentTeamId",
                        column: x => x.OpponentTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamGames_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMonthlySplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Games = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    WinPercentage = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMonthlySplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMonthlySplits_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamOpponentSplits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    Season = table.Column<int>(type: "integer", nullable: false),
                    OpponentTeamId = table.Column<int>(type: "integer", nullable: true),
                    OpponentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Games = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    WinPercentage = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamOpponentSplits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamOpponentSplits_Teams_OpponentTeamId",
                        column: x => x.OpponentTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamOpponentSplits_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamGames_OpponentTeamId",
                table: "TeamGames",
                column: "OpponentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamGames_TeamId_Season_GameNumber",
                table: "TeamGames",
                columns: new[] { "TeamId", "Season", "GameNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMonthlySplits_TeamId_Season_Month",
                table: "TeamMonthlySplits",
                columns: new[] { "TeamId", "Season", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamOpponentSplits_OpponentTeamId",
                table: "TeamOpponentSplits",
                column: "OpponentTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamOpponentSplits_TeamId_Season_OpponentName",
                table: "TeamOpponentSplits",
                columns: new[] { "TeamId", "Season", "OpponentName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamGames");

            migrationBuilder.DropTable(
                name: "TeamMonthlySplits");

            migrationBuilder.DropTable(
                name: "TeamOpponentSplits");
        }
    }
}
