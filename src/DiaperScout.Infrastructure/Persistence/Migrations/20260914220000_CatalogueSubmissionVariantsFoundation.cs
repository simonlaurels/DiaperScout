using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class CatalogueSubmissionVariantsFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "catalogue_submission_variants",
            schema: "diaperscout",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                IsStructuralFallback = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_catalogue_submission_variants", x => x.Id);
                table.ForeignKey(
                    name: "FK_catalogue_submission_variants_catalogue_submissions_SubmissionId",
                    column: x => x.SubmissionId,
                    principalSchema: "diaperscout",
                    principalTable: "catalogue_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_catalogue_submission_variants_SubmissionId_Name",
            schema: "diaperscout",
            table: "catalogue_submission_variants",
            columns: new[] { "SubmissionId", "Name" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_catalogue_submission_variants_SubmissionId_IsStructuralFallback",
            schema: "diaperscout",
            table: "catalogue_submission_variants",
            columns: new[] { "SubmissionId", "IsStructuralFallback" },
            unique: true,
            filter: "\"IsStructuralFallback\" = true");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "catalogue_submission_variants",
            schema: "diaperscout");
    }
}
