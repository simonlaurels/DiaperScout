using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DataForSeoIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dataforseo_integration_settings",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(
                        type: "uuid",
                        nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false),
                    Enabled = table.Column<bool>(
                        type: "boolean",
                        nullable: false),
                    LanguageCode = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false),
                    LocationName = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false),
                    Login = table.Column<string>(
                        type: "character varying(320)",
                        maxLength: 320,
                        nullable: false),
                    ProtectedPassword = table.Column<string>(
                        type: "text",
                        nullable: true),
                    ProviderKey = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false),
                    SearchDomain = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_dataforseo_integration_settings",
                        x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dataforseo_integration_settings_ProviderKey",
                schema: "diaperscout",
                table: "dataforseo_integration_settings",
                column: "ProviderKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dataforseo_integration_settings",
                schema: "diaperscout");
        }
    }
}
