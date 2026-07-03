using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbookManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LibraryDbContext))]
    [Migration("20260703094000_AddImportItemDiagnostics")]
    public partial class AddImportItemDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DurationMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Format",
                table: "ImportItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "Format",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "ImportItems");
        }
    }
}
