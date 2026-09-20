using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class CatalogueRightsAndImportFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DescriptionVisibility",
            schema: "diaperscout",
            table: "products",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Public");

        migrationBuilder.AddColumn<string>(
            name: "ProposedDescriptionVisibility",
            schema: "diaperscout",
            table: "catalogue_submissions",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Public");

        migrationBuilder.AddColumn<Guid>(
            name: "ProductId",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Visibility",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "ModeratorOnly");

        migrationBuilder.CreateIndex(
            name: "IX_catalogue_submission_images_ProductId_Role",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            columns: new[] { "ProductId", "Role" },
            unique: true,
            filter: "\"ProductId\" IS NOT NULL AND \"Role\" <> 'Other'");

        migrationBuilder.AddForeignKey(
            name: "FK_catalogue_submission_images_products_ProductId",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            column: "ProductId",
            principalSchema: "diaperscout",
            principalTable: "products",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_catalogue_submission_images_products_ProductId",
            schema: "diaperscout",
            table: "catalogue_submission_images");

        migrationBuilder.DropIndex(
            name: "IX_catalogue_submission_images_ProductId_Role",
            schema: "diaperscout",
            table: "catalogue_submission_images");

        migrationBuilder.DropColumn(
            name: "DescriptionVisibility",
            schema: "diaperscout",
            table: "products");

        migrationBuilder.DropColumn(
            name: "ProposedDescriptionVisibility",
            schema: "diaperscout",
            table: "catalogue_submissions");

        migrationBuilder.DropColumn(
            name: "ProductId",
            schema: "diaperscout",
            table: "catalogue_submission_images");

        migrationBuilder.DropColumn(
            name: "Visibility",
            schema: "diaperscout",
            table: "catalogue_submission_images");
    }
}
