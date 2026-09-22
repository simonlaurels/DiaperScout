using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class RetailerProductDiscovery : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "retailer_product_listings",
            schema: "diaperscout",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                PackTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                ListingUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                DiscoveryProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                ExternalListingId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                DiscoveredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastCheckedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_retailer_product_listings", x => x.Id);
                table.ForeignKey(
                    name: "FK_retailer_product_listings_pack_types_PackTypeId",
                    column: x => x.PackTypeId,
                    principalSchema: "diaperscout",
                    principalTable: "pack_types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_retailer_product_listings_retailers_RetailerId",
                    column: x => x.RetailerId,
                    principalSchema: "diaperscout",
                    principalTable: "retailers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_retailer_product_listings_PackTypeId_RetailerId_ListingUrl",
            schema: "diaperscout",
            table: "retailer_product_listings",
            columns: new[] { "PackTypeId", "RetailerId", "ListingUrl" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_retailer_product_listings_RetailerId_Status",
            schema: "diaperscout",
            table: "retailer_product_listings",
            columns: new[] { "RetailerId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "retailer_product_listings",
            schema: "diaperscout");
    }
}
