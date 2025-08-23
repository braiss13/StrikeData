using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchesConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayErrors",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayHits",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayLosses",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AwayPct",
                table: "Matches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayRuns",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayWins",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GamePk",
                table: "Matches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "HomeErrors",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeHits",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeLosses",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HomePct",
                table: "Matches",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeRuns",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeWins",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MatchInning",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchId = table.Column<int>(type: "integer", nullable: false),
                    InningNumber = table.Column<int>(type: "integer", nullable: false),
                    HomeRuns = table.Column<int>(type: "integer", nullable: true),
                    HomeHits = table.Column<int>(type: "integer", nullable: true),
                    HomeErrors = table.Column<int>(type: "integer", nullable: true),
                    AwayRuns = table.Column<int>(type: "integer", nullable: true),
                    AwayHits = table.Column<int>(type: "integer", nullable: true),
                    AwayErrors = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchInning", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchInning_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_GamePk",
                table: "Matches",
                column: "GamePk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchInning_MatchId_InningNumber",
                table: "MatchInning",
                columns: new[] { "MatchId", "InningNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchInning");

            migrationBuilder.DropIndex(
                name: "IX_Matches_GamePk",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayErrors",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayHits",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayLosses",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayPct",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayRuns",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "AwayWins",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GamePk",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeErrors",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeHits",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeLosses",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomePct",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeRuns",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeWins",
                table: "Matches");
        }
    }
}
