using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionOriginalVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProposedVariantName",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            // Older submissions used a synthetic "Single version" structural
            // variant. Preserve those submissions as the real base variant
            // before removing the obsolete structural-fallback flag.
            migrationBuilder.Sql(
                """
                UPDATE diaperscout.catalogue_submission_variants
                SET "Name" = NULL
                WHERE "IsStructuralFallback" = TRUE;
                """);

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFallback",
                schema: "diaperscout",
                table: "catalogue_submission_variants");

            migrationBuilder.DropColumn(
                name: "IsStructuralFallback",
                schema: "diaperscout",
                table: "catalogue_submission_variants");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_variants_SubmissionId",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                column: "SubmissionId",
                unique: true,
                filter: "\"Name\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_catalogue_submission_variants_SubmissionId",
                schema: "diaperscout",
                table: "catalogue_submission_variants");

            migrationBuilder.AddColumn<bool>(
                name: "IsStructuralFallback",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFallback",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                columns: new[] { "SubmissionId", "IsStructuralFallback" },
                unique: true,
                filter: "\"IsStructuralFallback\" = true");

            migrationBuilder.Sql(
                """
                UPDATE diaperscout.catalogue_submission_variants
                SET "Name" = 'Single version',
                    "IsStructuralFallback" = TRUE
                WHERE "Name" IS NULL;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ProposedVariantName",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
