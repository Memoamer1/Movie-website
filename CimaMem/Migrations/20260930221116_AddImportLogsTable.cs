using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CimaMem.Migrations
{
    /// <inheritdoc />
    public partial class AddImportLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Seasons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Rating",
                table: "Episodes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TmdbId",
                table: "Episodes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImportLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Processed = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<int>(type: "int", nullable: false),
                    Updated = table.Column<int>(type: "int", nullable: false),
                    Skipped = table.Column<int>(type: "int", nullable: false),
                    Failed = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Series_FirstAirDate",
                table: "Series",
                column: "FirstAirDate");

            migrationBuilder.CreateIndex(
                name: "IX_Series_Popularity",
                table: "Series",
                column: "Popularity");

            migrationBuilder.CreateIndex(
                name: "IX_Series_Rating",
                table: "Series",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_TmdbId",
                table: "Seasons",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Popularity",
                table: "Movies",
                column: "Popularity");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Rating",
                table: "Movies",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ReleaseDate",
                table: "Movies",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Episodes_TmdbId",
                table: "Episodes",
                column: "TmdbId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportLogs");

            migrationBuilder.DropIndex(
                name: "IX_Series_FirstAirDate",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_Popularity",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_Rating",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Seasons_TmdbId",
                table: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Movies_Popularity",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_Rating",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_ReleaseDate",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Episodes_TmdbId",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Episodes");

            migrationBuilder.DropColumn(
                name: "TmdbId",
                table: "Episodes");
        }
    }
}
