using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DiagnoseCatalogueModelMismatch2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFall~",
                schema: "diaperscout",
                table: "catalogue_submission_variants");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFall~",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                columns: new[] { "SubmissionId", "IsStructuralFallback" },
                unique: true,
                filter: "\"IsStructuralFallback\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFall~",
                schema: "diaperscout",
                table: "catalogue_submission_variants");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFall~",
                schema: "diaperscout",
                table: "catalogue_submission_variants",
                columns: new[] { "SubmissionId", "IsStructuralFallback" },
                filter: "\"IsStructuralFallback\" = true");
        }
    }
}
