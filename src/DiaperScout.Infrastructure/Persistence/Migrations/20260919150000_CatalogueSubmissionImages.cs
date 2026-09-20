using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class CatalogueSubmissionImages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "catalogue_submission_images",
            schema: "diaperscout",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                PermissionEvidence = table.Column<string>(type: "text", nullable: true),
                PermissionStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                SourceNotes = table.Column<string>(type: "text", nullable: true),
                SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_catalogue_submission_images", x => x.Id);
                table.ForeignKey(
                    name: "FK_catalogue_submission_images_catalogue_submissions_SubmissionId",
                    column: x => x.SubmissionId,
                    principalSchema: "diaperscout",
                    principalTable: "catalogue_submissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_catalogue_submission_images_SubmissionId_Role",
            schema: "diaperscout",
            table: "catalogue_submission_images",
            columns: new[] { "SubmissionId", "Role" },
            unique: true,
            filter: "\"Role\" <> 'Other'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "catalogue_submission_images",
            schema: "diaperscout");
    }
}
