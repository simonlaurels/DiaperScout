using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PhysicalObservationsAndPublicProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_catalogue_submissions_SubmittedByUserId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.AddColumn<Guid>(
                name: "ContributionId",
                schema: "diaperscout",
                table: "observations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PackTypeId",
                schema: "diaperscout",
                table: "observations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "RetailerId",
                schema: "diaperscout",
                table: "locations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                schema: "diaperscout",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                schema: "diaperscout",
                table: "locations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublicCommercialPlace",
                schema: "diaperscout",
                table: "locations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlaceIdentity",
                schema: "diaperscout",
                table: "locations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedPackQuantity",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublicContributionId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResolvedPackTypeId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_observations_AuthorUserId_ContributionId",
                schema: "diaperscout",
                table: "observations",
                columns: new[] { "AuthorUserId", "ContributionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_observations_PackTypeId",
                schema: "diaperscout",
                table: "observations",
                column: "PackTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_CreatedByUserId",
                schema: "diaperscout",
                table: "locations",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_PlaceIdentity",
                schema: "diaperscout",
                table: "locations",
                column: "PlaceIdentity",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_ResolvedPackTypeId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "ResolvedPackTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_SubmittedByUserId_PublicContributionId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                columns: new[] { "SubmittedByUserId", "PublicContributionId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_catalogue_submissions_pack_types_ResolvedPackTypeId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "ResolvedPackTypeId",
                principalSchema: "diaperscout",
                principalTable: "pack_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_locations_users_CreatedByUserId",
                schema: "diaperscout",
                table: "locations",
                column: "CreatedByUserId",
                principalSchema: "diaperscout",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_observations_pack_types_PackTypeId",
                schema: "diaperscout",
                table: "observations",
                column: "PackTypeId",
                principalSchema: "diaperscout",
                principalTable: "pack_types",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_catalogue_submissions_pack_types_ResolvedPackTypeId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_locations_users_CreatedByUserId",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropForeignKey(
                name: "FK_observations_pack_types_PackTypeId",
                schema: "diaperscout",
                table: "observations");

            migrationBuilder.DropIndex(
                name: "IX_observations_AuthorUserId_ContributionId",
                schema: "diaperscout",
                table: "observations");

            migrationBuilder.DropIndex(
                name: "IX_observations_PackTypeId",
                schema: "diaperscout",
                table: "observations");

            migrationBuilder.DropIndex(
                name: "IX_locations_CreatedByUserId",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "IX_locations_PlaceIdentity",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submissions_ResolvedPackTypeId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropIndex(
                name: "IX_catalogue_submissions_SubmittedByUserId_PublicContributionId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ContributionId",
                schema: "diaperscout",
                table: "observations");

            migrationBuilder.DropColumn(
                name: "PackTypeId",
                schema: "diaperscout",
                table: "observations");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "IsPublicCommercialPlace",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "PlaceIdentity",
                schema: "diaperscout",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "ProposedPackQuantity",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "PublicContributionId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.DropColumn(
                name: "ResolvedPackTypeId",
                schema: "diaperscout",
                table: "catalogue_submissions");

            migrationBuilder.AlterColumn<Guid>(
                name: "RetailerId",
                schema: "diaperscout",
                table: "locations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submissions_SubmittedByUserId",
                schema: "diaperscout",
                table: "catalogue_submissions",
                column: "SubmittedByUserId");
        }
    }
}
