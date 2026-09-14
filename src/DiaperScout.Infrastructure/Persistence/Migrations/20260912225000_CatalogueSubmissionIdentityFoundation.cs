using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class CatalogueSubmissionIdentityFoundation : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IdentitySourceUrl",
            schema: "diaperscout",
            table: "catalogue_submissions",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProposedGtin",
            schema: "diaperscout",
            table: "catalogue_submissions",
            type: "character varying(14)",
            maxLength: 14,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProposedSku",
            schema: "diaperscout",
            table: "catalogue_submissions",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IdentitySourceUrl",
            schema: "diaperscout",
            table: "catalogue_submissions");

        migrationBuilder.DropColumn(
            name: "ProposedGtin",
            schema: "diaperscout",
            table: "catalogue_submissions");

        migrationBuilder.DropColumn(
            name: "ProposedSku",
            schema: "diaperscout",
            table: "catalogue_submissions");
    }
}
