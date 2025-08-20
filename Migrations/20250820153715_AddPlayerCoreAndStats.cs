using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerCoreAndStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BattingAverage",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GamesPlayed",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "HomeRuns",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "OnBasePercentage",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Position",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "RunsBattedIn",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "SluggingPercentage",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "StolenBases",
                table: "Players",
                newName: "Number");

            migrationBuilder.AddColumn<long>(
                name: "MLB_Player_Id",
                table: "Players",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Players",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlayerStatTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StatCategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStatTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStatTypes_StatCategories_StatCategoryId",
                        column: x => x.StatCategoryId,
                        principalTable: "StatCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    PlayerStatTypeId = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStats_PlayerStatTypes_PlayerStatTypeId",
                        column: x => x.PlayerStatTypeId,
                        principalTable: "PlayerStatTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerStats_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_MLB_Player_Id",
                table: "Players",
                column: "MLB_Player_Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStats_PlayerStatTypeId",
                table: "PlayerStats",
                column: "PlayerStatTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_PlayerStat_Player_Type",
                table: "PlayerStats",
                columns: new[] { "PlayerId", "PlayerStatTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStatTypes_StatCategoryId",
                table: "PlayerStatTypes",
                column: "StatCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerStats");

            migrationBuilder.DropTable(
                name: "PlayerStatTypes");

            migrationBuilder.DropIndex(
                name: "IX_Players_MLB_Player_Id",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "MLB_Player_Id",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Players");

            migrationBuilder.RenameColumn(
                name: "Number",
                table: "Players",
                newName: "StolenBases");

            migrationBuilder.AddColumn<float>(
                name: "BattingAverage",
                table: "Players",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GamesPlayed",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeRuns",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "OnBasePercentage",
                table: "Players",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Position",
                table: "Players",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RunsBattedIn",
                table: "Players",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "SluggingPercentage",
                table: "Players",
                type: "real",
                nullable: true);
        }
    }
}
