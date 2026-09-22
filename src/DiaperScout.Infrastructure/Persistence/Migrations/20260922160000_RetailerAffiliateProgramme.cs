using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class RetailerAffiliateProgramme : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "retailer_affiliate_programmes",
            schema: "diaperscout",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                Network = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                ProgrammeId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ProgrammeName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                ProgrammeUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                TermsUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                ReferralTerms = table.Column<string>(type: "text", nullable: true),
                CookieDurationDays = table.Column<int>(type: "integer", nullable: true),
                DeepLinksAllowed = table.Column<bool>(type: "boolean", nullable: true),
                ApplicationRequired = table.Column<bool>(type: "boolean", nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                DiscoveredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                LastCheckedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                IsPreferred = table.Column<bool>(type: "boolean", nullable: false),
                PreferredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_retailer_affiliate_programmes", x => x.Id);
                table.ForeignKey(
                    name: "FK_retailer_affiliate_programmes_retailers_RetailerId",
                    column: x => x.RetailerId,
                    principalSchema: "diaperscout",
                    principalTable: "retailers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_retailer_affiliate_programmes_RetailerId_IsPreferred",
            schema: "diaperscout",
            table: "retailer_affiliate_programmes",
            columns: new[] { "RetailerId", "IsPreferred" });

        migrationBuilder.CreateIndex(
            name: "IX_retailer_affiliate_programmes_RetailerId_Network_ProgrammeId",
            schema: "diaperscout",
            table: "retailer_affiliate_programmes",
            columns: new[] { "RetailerId", "Network", "ProgrammeId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "retailer_affiliate_programmes",
            schema: "diaperscout");
    }
}
