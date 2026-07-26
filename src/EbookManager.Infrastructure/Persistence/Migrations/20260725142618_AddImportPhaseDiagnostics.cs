using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbookManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImportPhaseDiagnostics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AvailabilityCheckMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CleanupMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DatabaseSaveMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DuplicateCheckMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HashingMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ManagedCopyMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MetadataReadMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SizeReadMilliseconds",
                table: "ImportItems",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailabilityCheckMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "CleanupMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "DatabaseSaveMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "DuplicateCheckMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "HashingMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "ManagedCopyMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "MetadataReadMilliseconds",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "SizeReadMilliseconds",
                table: "ImportItems");
        }
    }
}
