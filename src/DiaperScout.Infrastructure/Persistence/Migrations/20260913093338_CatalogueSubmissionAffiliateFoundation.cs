using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionAffiliateFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_submission_retail_affiliates",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailDestinationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Network = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TrackingConfiguration = table.Column<string>(type: "text", nullable: true),
                    DeepLinkMechanism = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TermsUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ApplicationReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastVerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submission_retail_affiliates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_retail_affiliates_catalogue_submission~",
                        column: x => x.RetailDestinationId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submission_retail_destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_retail_affiliates_catalogue_submissio~1",
                        column: x => x.SubmissionId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_retail_affiliates_RetailDestinationId_~",
                schema: "diaperscout",
                table: "catalogue_submission_retail_affiliates",
                columns: new[] { "RetailDestinationId", "LastVerifiedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_retail_affiliates_SubmissionId_RetailD~",
                schema: "diaperscout",
                table: "catalogue_submission_retail_affiliates",
                columns: new[] { "SubmissionId", "RetailDestinationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submission_retail_affiliates",
                schema: "diaperscout");
        }
    }
}
