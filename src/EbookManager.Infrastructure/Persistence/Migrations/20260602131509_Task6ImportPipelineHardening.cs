using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EbookManager.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Task6ImportPipelineHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportItems_ImportRunId",
                table: "ImportItems");

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                table: "ImportItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DuplicateKey",
                table: "Books",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "ImportItems"
                SET "Sequence" = (
                    SELECT COUNT(*)
                    FROM "ImportItems" AS "Earlier"
                    WHERE "Earlier"."ImportRunId" = "ImportItems"."ImportRunId"
                      AND "Earlier"."Id" < "ImportItems"."Id"
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Books"
                SET "DuplicateKey" = (
                    SELECT LOWER(TRIM("Books"."Title")) || '|' || COALESCE((
                        SELECT GROUP_CONCAT("NormalizedName", '|')
                        FROM (
                            SELECT DISTINCT "Authors"."NormalizedName" AS "NormalizedName"
                            FROM "BookAuthors"
                            INNER JOIN "Authors" ON "Authors"."Id" = "BookAuthors"."AuthorId"
                            WHERE "BookAuthors"."BookId" = "Books"."Id"
                            ORDER BY "Authors"."NormalizedName"
                        )
                    ), '')
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE TEMP TABLE "__BooksDuplicateKeyMigrationGuard" (
                    "IsValid" INTEGER NOT NULL
                        CONSTRAINT "REPAIR_REQUIRED_LEGACY_BOOKS_DUPLICATEKEY_COLLISIONS"
                        CHECK ("IsValid" = 1)
                );

                INSERT INTO "__BooksDuplicateKeyMigrationGuard" ("IsValid")
                SELECT 0
                WHERE EXISTS (
                    SELECT 1
                    FROM "Books"
                    GROUP BY "DuplicateKey"
                    HAVING COUNT(*) > 1
                );

                DROP TABLE "__BooksDuplicateKeyMigrationGuard";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ImportItems_ImportRunId_Sequence",
                table: "ImportItems",
                columns: new[] { "ImportRunId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_DuplicateKey",
                table: "Books",
                column: "DuplicateKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ImportItems_ImportRunId_Sequence",
                table: "ImportItems");

            migrationBuilder.DropIndex(
                name: "IX_Books_DuplicateKey",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "ImportItems");

            migrationBuilder.DropColumn(
                name: "DuplicateKey",
                table: "Books");

            migrationBuilder.CreateIndex(
                name: "IX_ImportItems_ImportRunId",
                table: "ImportItems",
                column: "ImportRunId");
        }
    }
}
