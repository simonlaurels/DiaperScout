using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordlessAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_emails",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_emails_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "magic_link_tokens",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserEmailId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_magic_link_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_magic_link_tokens_user_emails_UserEmailId",
                        column: x => x.UserEmailId,
                        principalSchema: "diaperscout",
                        principalTable: "user_emails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_magic_link_tokens_TokenHash",
                schema: "diaperscout",
                table: "magic_link_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_magic_link_tokens_UserEmailId_ExpiresAtUtc",
                schema: "diaperscout",
                table: "magic_link_tokens",
                columns: new[] { "UserEmailId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_user_emails_Email",
                schema: "diaperscout",
                table: "user_emails",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_emails_UserId",
                schema: "diaperscout",
                table: "user_emails",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "magic_link_tokens",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "user_emails",
                schema: "diaperscout");
        }
    }
}
