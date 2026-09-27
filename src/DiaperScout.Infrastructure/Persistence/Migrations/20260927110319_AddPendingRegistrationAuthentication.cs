using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRegistrationAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_registrations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_registrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pending_registration_tokens",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingRegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pending_registration_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pending_registration_tokens_pending_registrations_PendingRe~",
                        column: x => x.PendingRegistrationId,
                        principalSchema: "diaperscout",
                        principalTable: "pending_registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_pending_registration_tokens_PendingRegistrationId_ExpiresAt~",
                schema: "diaperscout",
                table: "pending_registration_tokens",
                columns: new[] { "PendingRegistrationId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_pending_registration_tokens_TokenHash",
                schema: "diaperscout",
                table: "pending_registration_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pending_registrations_Email",
                schema: "diaperscout",
                table: "pending_registrations",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_registration_tokens",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "pending_registrations",
                schema: "diaperscout");
        }
    }
}
