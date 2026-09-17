using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionSizeVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_submission_size_variants",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerSize = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WaistMinimumCm = table.Column<int>(type: "integer", nullable: true),
                    WaistMaximumCm = table.Column<int>(type: "integer", nullable: true),
                    HipMinimumCm = table.Column<int>(type: "integer", nullable: true),
                    HipMaximumCm = table.Column<int>(type: "integer", nullable: true),
                    CapacityMl = table.Column<int>(type: "integer", nullable: true),
                    LengthMm = table.Column<int>(type: "integer", nullable: true),
                    WidthMm = table.Column<int>(type: "integer", nullable: true),
                    WeightGrams = table.Column<int>(type: "integer", nullable: true),
                    ManufacturerPackQuantity = table.Column<int>(type: "integer", nullable: true),
                    Gtin = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submission_size_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_size_variants_catalogue_submission_var~",
                        column: x => x.VariantId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submission_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_size_variants_VariantId",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_size_variants_VariantId_ManufacturerSi~",
                schema: "diaperscout",
                table: "catalogue_submission_size_variants",
                columns: new[] { "VariantId", "ManufacturerSize" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submission_size_variants",
                schema: "diaperscout");
        }
    }
}
