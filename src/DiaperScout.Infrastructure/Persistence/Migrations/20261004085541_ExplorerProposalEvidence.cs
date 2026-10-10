using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExplorerProposalEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingCurrencyCode",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PendingLocationId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PendingObservedAtUtc",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PendingPriceAmount",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResultingObservationId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuggestedExistingProductId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceContentHash",
                schema: "diaperscout",
                table: "catalogue_submission_images",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EvidenceUploadId",
                schema: "diaperscout",
                table: "catalogue_submission_images",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsExplorerEvidence",
                schema: "diaperscout",
                table: "catalogue_submission_images",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_PendingLocationId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "PendingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_ResultingObservationId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "ResultingObservationId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_SuggestedExistingProductId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "SuggestedExistingProductId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_images_SubmissionId_EvidenceUploadId",
                schema: "diaperscout",
                table: "catalogue_submission_images",
                columns: new[] { "SubmissionId", "EvidenceUploadId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogue_submissions_locations_PendingLocationId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "PendingLocationId",
                principalSchema: "diaperscout",
                principalTable: "locations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogue_submissions_observations_ResultingObservationId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "ResultingObservationId",
                principalSchema: "diaperscout",
                principalTable: "observations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogue_submissions_products_SuggestedExistingProductId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "SuggestedExistingProductId",
                principalSchema: "diaperscout",
                principalTable: "products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalogue_submissions_locations_PendingLocationId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_catalogue_submissions_observations_ResultingObservationId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_catalogue_submissions_products_SuggestedExistingProductId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submissions_PendingLocationId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submissions_ResultingObservationId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submissions_SuggestedExistingProductId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submission_images_SubmissionId_EvidenceUploadId",
                schema: "diaperscout",
                table: "catalogue_submission_images");

            migrationBuilder.DropColumn(
                name: "PendingCurrencyCode",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "PendingLocationId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "PendingObservedAtUtc",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "PendingPriceAmount",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ResultingObservationId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "SuggestedExistingProductId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "EvidenceContentHash",
                schema: "diaperscout",
                table: "catalogue_submission_images");

            migrationBuilder.DropColumn(
                name: "EvidenceUploadId",
                schema: "diaperscout",
                table: "catalogue_submission_images");

            migrationBuilder.DropColumn(
                name: "IsExplorerEvidence",
                schema: "diaperscout",
                table: "catalogue_submission_images");
        }
    }
}
