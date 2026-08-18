using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbookManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDuplicateExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DuplicateExclusions",
                columns: table => new
                {
                    FirstBookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecondBookId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuplicateExclusions", x => new { x.FirstBookId, x.SecondBookId });
                    table.ForeignKey(
                        name: "FK_DuplicateExclusions_Books_FirstBookId",
                        column: x => x.FirstBookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DuplicateExclusions_Books_SecondBookId",
                        column: x => x.SecondBookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DuplicateExclusions_SecondBookId",
                table: "DuplicateExclusions",
                column: "SecondBookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DuplicateExclusions");
        }
    }
}
