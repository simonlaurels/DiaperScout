using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_submissions",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProposedManufacturerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProposedBrandName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProposedProductName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    ProposedVariantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PublishedProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submissions_products_PublishedProductId",
                        column: x => x.PublishedProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalogue_submissions_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_CreatedAtUtc",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_PublishedProductId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "PublishedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_Status",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_SubmittedByUserId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "SubmittedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submissions",
                schema: "diaperscout");
        }
    }
}
