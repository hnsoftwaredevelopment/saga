using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbookManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LibraryDbContext))]
    [Migration("20260707093000_AddImportItemSuggestions")]
    public partial class AddImportItemSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedBookId",
                table: "ImportItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedAuthors",
                table: "ImportItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestedTitle",
                table: "ImportItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuggestionKind",
                table: "ImportItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuggestedBookId",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "SuggestedAuthors",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "SuggestedTitle",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "SuggestionKind",
                table: "ImportItems");
        }
    }
}
