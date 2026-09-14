using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionVerificationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_submission_verifications",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Area = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Scope = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    PermissionTerms = table.Column<string>(type: "text", nullable: true),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submission_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_verifications_catalogue_submissions_Su~",
                        column: x => x.SubmissionId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_verifications_users_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_verifications_SubmissionId_Area_Verifi~",
                schema: "diaperscout",
                table: "catalogue_submission_verifications",
                columns: new[] { "SubmissionId", "Area", "VerifiedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_verifications_VerifiedByUserId",
                schema: "diaperscout",
                table: "catalogue_submission_verifications",
                column: "VerifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submission_verifications",
                schema: "diaperscout");
        }
    }
}
