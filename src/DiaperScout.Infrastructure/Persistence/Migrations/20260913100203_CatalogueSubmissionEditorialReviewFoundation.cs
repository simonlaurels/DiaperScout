using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogueSubmissionEditorialReviewFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_submission_editorial_decisions",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeratorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_submission_editorial_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_editorial_decisions_catalogue_submissi~",
                        column: x => x.SubmissionId,
                        principalSchema: "diaperscout",
                        principalTable: "catalogue_submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_catalogue_submission_editorial_decisions_users_ModeratorUse~",
                        column: x => x.ModeratorUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_editorial_decisions_ModeratorUserId",
                schema: "diaperscout",
                table: "catalogue_submission_editorial_decisions",
                column: "ModeratorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_submission_editorial_decisions_SubmissionId_Decid~",
                schema: "diaperscout",
                table: "catalogue_submission_editorial_decisions",
                columns: new[] { "SubmissionId", "DecidedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_submission_editorial_decisions",
                schema: "diaperscout");
        }
    }
}
