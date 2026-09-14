using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionSpecificationsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProposedBackingType",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedFastenerType",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedFragranceType",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedManufacturerSize",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedPackagingType",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedProductType",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedQuantityPerPack",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedWaistMaximumCm",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedWaistMinimumCm",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProposedWaistbandStyle",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProposedBackingType",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedFastenerType",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedFragranceType",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedManufacturerSize",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedPackagingType",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedProductType",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedQuantityPerPack",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedWaistMaximumCm",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedWaistMinimumCm",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedWaistbandStyle",
                schema: "diaperscout",
                table: "catalogue_submissions");
        }
    }
}
