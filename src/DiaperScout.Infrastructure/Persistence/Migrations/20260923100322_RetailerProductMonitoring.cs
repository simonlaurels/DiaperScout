using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RetailerProductMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "retailer_product_observations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerProductListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PriceCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Availability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retailer_product_observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_retailer_product_observations_retailer_product_listings_Ret~",
                        column: x => x.RetailerProductListingId,
                        principalSchema: "diaperscout",
                        principalTable: "retailer_product_listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_retailer_product_observations_RetailerProductListingId_Obse~",
                schema: "diaperscout",
                table: "retailer_product_observations",
                columns: new[] { "RetailerProductListingId", "ObservedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retailer_product_observations",
                schema: "diaperscout");
        }
    }
}
