using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbookManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomMetadataFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomMetadataFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMetadataFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomMetadataValues",
                columns: table => new
                {
                    BookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TextValue = table.Column<string>(type: "TEXT", nullable: true),
                    NumberValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    DateValue = table.Column<string>(type: "TEXT", nullable: true),
                    BooleanValue = table.Column<bool>(type: "INTEGER", nullable: true),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMetadataValues", x => new { x.BookId, x.FieldId });
                    table.ForeignKey(
                        name: "FK_CustomMetadataValues_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomMetadataValues_CustomMetadataFields_FieldId",
                        column: x => x.FieldId,
                        principalTable: "CustomMetadataFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMetadataFields_Key",
                table: "CustomMetadataFields",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomMetadataFields_NormalizedName",
                table: "CustomMetadataFields",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomMetadataFields_SortOrder_Name_Id",
                table: "CustomMetadataFields",
                columns: new[] { "SortOrder", "Name", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMetadataValues_FieldId",
                table: "CustomMetadataValues",
                column: "FieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomMetadataValues");

            migrationBuilder.DropTable(
                name: "CustomMetadataFields");
        }
    }
}
