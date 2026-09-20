using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueProductAttributeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasElasticWaistbandFront",
                schema: "diaperscout",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "HasElasticWaistbandRear",
                schema: "diaperscout",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "HasInnerLeakGuards",
                schema: "diaperscout",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "IsChlorineFree",
                schema: "diaperscout",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "SecondaryColours",
                schema: "diaperscout",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "ProposedBackingType",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ProposedFastenerType",
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
                name: "SharedChlorineFree",
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
                name: "SharedInnerLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SharedSecondaryColours",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "HasElasticWaistbandFront",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides");

            migrationBuilder.DropColumn(
                name: "HasElasticWaistbandRear",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides");

            migrationBuilder.DropColumn(
                name: "HasInnerLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides");

            migrationBuilder.DropColumn(
                name: "IsChlorineFree",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides");

            migrationBuilder.DropColumn(
                name: "SecondaryColours",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides");

            migrationBuilder.RenameColumn(
                name: "CapacityMl",
                schema: "diaperscout",
                table: "size_variants",
                newName: "ManufacturerStatedAbsorbencyMl");

            migrationBuilder.RenameColumn(
                name: "ProposedWaistbandStyle",
                schema: "diaperscout",
                table: "catalogue_submissions",
                newName: "SharedWaistbandStyle");

            migrationBuilder.RenameColumn(
                name: "ProposedManufacturerSize",
                schema: "diaperscout",
                table: "catalogue_submissions",
                newName: "SharedDesignedFor");

            migrationBuilder.RenameColumn(
                name: "ProposedFragranceType",
                schema: "diaperscout",
                table: "catalogue_submissions",
                newName: "SharedFragrance");

            migrationBuilder.RenameColumn(
                name: "CapacityMl",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                newName: "ManufacturerStatedAbsorbencyMl");

            migrationBuilder.AddColumn<string>(
                name: "AbsorbencyBasisMethod",
                schema: "diaperscout",
                table: "size_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbsorbencySource",
                schema: "diaperscout",
                table: "size_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FitMeasurementBasis",
                schema: "diaperscout",
                table: "size_variants",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "WaistbandStyle",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "PrintDesign",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColour",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Fragrance",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "FastenerType",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "BackingType",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "DesignedFor",
                schema: "diaperscout",
                table: "product_variants",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignedFor",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbsorbencyBasisMethod",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AbsorbencySource",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FitMeasurementBasis",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsorbencyBasisMethod",
                schema: "diaperscout",
                table: "size_variants");

            migrationBuilder.DropColumn(
                name: "AbsorbencySource",
                schema: "diaperscout",
                table: "size_variants");

            migrationBuilder.DropColumn(
                name: "FitMeasurementBasis",
                schema: "diaperscout",
                table: "size_variants");

            migrationBuilder.DropColumn(
                name: "DesignedFor",
                schema: "diaperscout",
                table: "product_variants");

            migrationBuilder.DropColumn(
                name: "DesignedFor",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides");

            migrationBuilder.DropColumn(
                name: "AbsorbencyBasisMethod",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants");

            migrationBuilder.DropColumn(
                name: "AbsorbencySource",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants");

            migrationBuilder.DropColumn(
                name: "FitMeasurementBasis",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants");

            migrationBuilder.RenameColumn(
                name: "ManufacturerStatedAbsorbencyMl",
                schema: "diaperscout",
                table: "size_variants",
                newName: "CapacityMl");

            migrationBuilder.RenameColumn(
                name: "SharedWaistbandStyle",
                schema: "diaperscout",
                table: "catalogue_submissions",
                newName: "ProposedWaistbandStyle");

            migrationBuilder.RenameColumn(
                name: "SharedFragrance",
                schema: "diaperscout",
                table: "catalogue_submissions",
                newName: "ProposedFragranceType");

            migrationBuilder.RenameColumn(
                name: "SharedDesignedFor",
                schema: "diaperscout",
                table: "catalogue_submissions",
                newName: "ProposedManufacturerSize");

            migrationBuilder.RenameColumn(
                name: "ManufacturerStatedAbsorbencyMl",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                newName: "CapacityMl");

            migrationBuilder.AlterColumn<int>(
                name: "WaistbandStyle",
                schema: "diaperscout",
                table: "product_variants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<string>(
                name: "PrintDesign",
                schema: "diaperscout",
                table: "product_variants",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColour",
                schema: "diaperscout",
                table: "product_variants",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Fragrance",
                schema: "diaperscout",
                table: "product_variants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "FastenerType",
                schema: "diaperscout",
                table: "product_variants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AlterColumn<int>(
                name: "BackingType",
                schema: "diaperscout",
                table: "product_variants",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.AddColumn<bool>(
                name: "HasElasticWaistbandFront",
                schema: "diaperscout",
                table: "product_variants",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasElasticWaistbandRear",
                schema: "diaperscout",
                table: "product_variants",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasInnerLeakGuards",
                schema: "diaperscout",
                table: "product_variants",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsChlorineFree",
                schema: "diaperscout",
                table: "product_variants",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColours",
                schema: "diaperscout",
                table: "product_variants",
                type: "text",
                nullable: true);

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

            migrationBuilder.AddColumn<bool>(
                name: "SharedChlorineFree",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
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

            migrationBuilder.AddColumn<bool>(
                name: "SharedInnerLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SharedSecondaryColours",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasElasticWaistbandFront",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasElasticWaistbandRear",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasInnerLeakGuards",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsChlorineFree",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColours",
                schema: "diaperscout",
                table: "catalogue_submission_variant_overrides",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
