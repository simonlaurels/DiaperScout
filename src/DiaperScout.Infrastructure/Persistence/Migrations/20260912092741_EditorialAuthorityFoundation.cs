using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EditorialAuthorityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "catalogue_audit_records",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActingUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedPayloadJson = table.Column<string>(type: "text", nullable: false),
                    AffectedCanonicalIdsJson = table.Column<string>(type: "text", nullable: false),
                    SourceSummary = table.Column<string>(type: "text", nullable: false),
                    SourceReferencesJson = table.Column<string>(type: "text", nullable: false),
                    EditorialRationale = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_catalogue_audit_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_catalogue_audit_records_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_catalogue_audit_records_users_ActingUserId",
                        column: x => x.ActingUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "privileged_role_assignment_audits",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActingUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privileged_role_assignment_audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_privileged_role_assignment_audits_users_ActingUserId",
                        column: x => x.ActingUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_privileged_role_assignment_audits_users_SubjectUserId",
                        column: x => x.SubjectUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "privileged_role_assignments",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    GrantedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_privileged_role_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_privileged_role_assignments_users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_privileged_role_assignments_users_RevokedByUserId",
                        column: x => x.RevokedByUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_privileged_role_assignments_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_audit_records_ActingUserId",
                schema: "diaperscout",
                table: "catalogue_audit_records",
                column: "ActingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_catalogue_audit_records_ProductId_OccurredAtUtc",
                schema: "diaperscout",
                table: "catalogue_audit_records",
                columns: new[] { "ProductId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_privileged_role_assignment_audits_ActingUserId",
                schema: "diaperscout",
                table: "privileged_role_assignment_audits",
                column: "ActingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_privileged_role_assignment_audits_SubjectUserId_OccurredAtU~",
                schema: "diaperscout",
                table: "privileged_role_assignment_audits",
                columns: new[] { "SubjectUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_privileged_role_assignments_GrantedByUserId",
                schema: "diaperscout",
                table: "privileged_role_assignments",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_privileged_role_assignments_RevokedByUserId",
                schema: "diaperscout",
                table: "privileged_role_assignments",
                column: "RevokedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_privileged_role_assignments_UserId_Role",
                schema: "diaperscout",
                table: "privileged_role_assignments",
                columns: new[] { "UserId", "Role" },
                unique: true,
                filter: "\"RevokedAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "catalogue_audit_records",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "privileged_role_assignment_audits",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "privileged_role_assignments",
                schema: "diaperscout");
        }
    }
}
