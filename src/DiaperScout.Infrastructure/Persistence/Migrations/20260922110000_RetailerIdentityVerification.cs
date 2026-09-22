using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class RetailerIdentityVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "retailer_identity_verifications",
            schema: "diaperscout",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ObservedRetailerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                ListingUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                WebsiteUrlValid = table.Column<bool>(type: "boolean", nullable: false),
                SourceUrlValid = table.Column<bool>(type: "boolean", nullable: false),
                ListingUrlValid = table.Column<bool>(type: "boolean", nullable: false),
                NameMatches = table.Column<bool>(type: "boolean", nullable: false),
                DomainMatches = table.Column<bool>(type: "boolean", nullable: false),
                Notes = table.Column<string>(type: "text", nullable: true),
                VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_retailer_identity_verifications", x => x.Id);
                table.ForeignKey(
                    name: "FK_retailer_identity_verifications_retailers_RetailerId",
                    column: x => x.RetailerId,
                    principalSchema: "diaperscout",
                    principalTable: "retailers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_retailer_identity_verifications_users_VerifiedByUserId",
                    column: x => x.VerifiedByUserId,
                    principalSchema: "diaperscout",
                    principalTable: "users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_retailer_identity_verifications_RetailerId_VerifiedAtUtc",
            schema: "diaperscout",
            table: "retailer_identity_verifications",
            columns: new[] { "RetailerId", "VerifiedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_retailer_identity_verifications_VerifiedByUserId",
            schema: "diaperscout",
            table: "retailer_identity_verifications",
            column: "VerifiedByUserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "retailer_identity_verifications",
            schema: "diaperscout");
    }
}
