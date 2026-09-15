using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionDetailsAndVariantOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProposedDescription",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedOfficialWebsiteUrl",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedProductFamily",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedProductStatus",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedChlorineFree",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharedConstructionNotes",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedElasticWaistbandFront",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedElasticWaistbandRear",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SharedFastenerCount",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedInnerLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedLatexFree",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharedPrimaryColour",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharedPrintDesign",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharedSecondaryColours",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedStandingLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SharedWetnessIndicator",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "catalogue_submission_variant_overrides",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BackingType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    FastenerType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    PrintDesign = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PrimaryColour = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SecondaryColours = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HasWetnessIndicator = table.Column<bool>(type: "boolean", nullable: true),
                    HasStandingLeakGuards = table.Column<bool>(type: "boolean", nullable: true),
                    HasInnerLeakGuards = table.Column<bool>(type: "boolean", nullable: true),
                    HasElasticWaistbandFront = table.Column<bool>(type: "boolean", nullable: true),
                    HasElasticWaistbandRear = table.Column<bool>(type: "boolean", nullable: true),
                    WaistbandStyle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Fragrance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    IsLatexFree = table.Column<bool>(type: "boolean", nullable: true),
                    IsChlorineFree = table.Column<bool>(type: "boolean", nullable: true),
                    FastenerCount = table.Column<int>(type: "integer", nullable: true),
                    ConstructionNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submission_variant_overrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_variant_overrides_catalogue_submission~",
                        column: x => x.VariantId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submission_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_variant_overrides_VariantId",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                column: "VariantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submission_variant_overrides",
                schema: "diaperscout");

            migrationBuilder.DropColumn(
                name: "ProposedDescription",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedOfficialWebsiteUrl",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedProductFamily",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedProductStatus",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedChlorineFree",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedConstructionNotes",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedElasticWaistbandFront",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedElasticWaistbandRear",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedFastenerCount",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedInnerLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedLatexFree",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedPrimaryColour",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedPrintDesign",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedSecondaryColours",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedStandingLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedWetnessIndicator",
                schema: "diaperscout",
                table: "catalogue_submissions");
        }
    }
}
