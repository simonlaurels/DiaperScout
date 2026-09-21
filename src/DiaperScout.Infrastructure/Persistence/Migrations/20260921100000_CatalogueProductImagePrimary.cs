using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class CatalogueProductImagePrimary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsPrimary",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_catalogue_submission_images_ProductId_IsPrimary",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            column: "ProductId",
            unique: true,
            filter: "\"ProductId\" IS NOT NULL AND \"IsPrimary\" = TRUE");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_catalogue_submission_images_ProductId_IsPrimary",
            schema: "diaperscout",
            table: "catalogue_submission_images");

        migrationBuilder.DropColumn(
            name: "IsPrimary",
            schema: "diaperscout",
            table: "catalogue_submission_images");
    }
}
