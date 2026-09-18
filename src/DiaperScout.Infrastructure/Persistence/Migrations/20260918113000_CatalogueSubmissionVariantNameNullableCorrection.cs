using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionVariantNameNullableCorrection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The nullable model state is already correct, but some development
            // databases have the old NOT NULL constraint because the earlier
            // migration was changed after it had been applied. This correction
            // deliberately changes the physical database schema without
            // changing the model snapshot.
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ProposedVariantName",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
