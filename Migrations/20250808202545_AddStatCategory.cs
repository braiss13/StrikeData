using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace StrikeData.Migrations
{
    /// <inheritdoc />
    public partial class AddStatCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StatCategoryId",
                table: "StatTypes",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "StatCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StatTypes_StatCategoryId",
                table: "StatTypes",
                column: "StatCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_StatTypes_StatCategories_StatCategoryId",
                table: "StatTypes",
                column: "StatCategoryId",
                principalTable: "StatCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StatTypes_StatCategories_StatCategoryId",
                table: "StatTypes");

            migrationBuilder.DropTable(
                name: "StatCategories");

            migrationBuilder.DropIndex(
                name: "IX_StatTypes_StatCategoryId",
                table: "StatTypes");

            migrationBuilder.DropColumn(
                name: "StatCategoryId",
                table: "StatTypes");
        }
    }
}
