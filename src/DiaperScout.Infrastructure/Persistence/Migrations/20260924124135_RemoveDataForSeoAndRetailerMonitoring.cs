using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDataForSeoAndRetailerMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dataforseo_integration_settings",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "retailer_product_observations",
                schema: "diaperscout");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dataforseo_integration_settings",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LanguageCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LocationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Login = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ProtectedPassword = table.Column<string>(type: "text", nullable: true),
                    ProviderKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SearchDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dataforseo_integration_settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "retailer_product_observations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Availability = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PriceCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    RetailerProductListingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
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
                name: "IX_dataforseo_integration_settings_ProviderKey",
                schema: "diaperscout",
                table: "dataforseo_integration_settings",
                column: "ProviderKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_retailer_product_observations_RetailerProductListingId_Obse~",
                schema: "diaperscout",
                table: "retailer_product_observations",
                columns: new[] { "RetailerProductListingId", "ObservedAtUtc" });
        }
    }
}
