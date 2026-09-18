using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

/// <summary>
/// Adds a database default for the legacy IsStructuralFallback column.
/// The application no longer models this property; Name == null represents the
/// original/base variant. The default keeps the legacy NOT NULL column compatible
/// with inserts from the current domain model.
/// </summary>
[Migration("20260918144500_CatalogueSubmissionVariantLegacyDefaultFix")]
public partial class CatalogueSubmissionVariantLegacyDefaultFix : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE diaperscout.catalogue_submission_variants
            ALTER COLUMN "IsStructuralFallback" SET DEFAULT FALSE;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE diaperscout.catalogue_submission_variants
            ALTER COLUMN "IsStructuralFallback" DROP DEFAULT;
            """);
    }
}
