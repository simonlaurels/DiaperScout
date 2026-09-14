using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionRetailDestinationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_submission_retail_destinations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ListingUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    AddedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submission_retail_destinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_retail_destinations_catalogue_submissi~",
                        column: x => x.SubmissionId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_retail_destinations_retailers_Retailer~",
                        column: x => x.RetailerId,
                        principalSchema: "diaperscout",
                        principalTable: "retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_retail_destinations_RetailerId",
                schema: "diaperscout",
                table: "catalogue_submission_retail_destinations",
                column: "RetailerId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_retail_destinations_SubmissionId_Retai~",
                schema: "diaperscout",
                table: "catalogue_submission_retail_destinations",
                columns: new[] { "SubmissionId", "RetailerId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submission_retail_destinations",
                schema: "diaperscout");
        }
    }
}
