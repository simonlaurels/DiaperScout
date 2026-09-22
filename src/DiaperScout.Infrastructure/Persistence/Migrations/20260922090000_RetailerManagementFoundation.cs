using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations;

public partial class RetailerManagementFoundation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAtUtc",
            schema: "diaperscout",
            table: "retailers",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "IdentityVerifiedAtUtc",
            schema: "diaperscout",
            table: "retailers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IdentitySourceUrl",
            schema: "diaperscout",
            table: "retailers",
            type: "character varying(2048)",
            maxLength: 2048,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Status",
            schema: "diaperscout",
            table: "retailers",
            type: "character varying(32)",
            maxLength: 32,
            nullable: false,
            defaultValue: "Discovered");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "UpdatedAtUtc",
            schema: "diaperscout",
            table: "retailers",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP");

        migrationBuilder.CreateIndex(
            name: "IX_retailers_Status",
            schema: "diaperscout",
            table: "retailers",
            column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_retailers_Status",
            schema: "diaperscout",
            table: "retailers");

        migrationBuilder.DropColumn(
            name: "CreatedAtUtc",
            schema: "diaperscout",
            table: "retailers");

        migrationBuilder.DropColumn(
            name: "IdentityVerifiedAtUtc",
            schema: "diaperscout",
            table: "retailers");

        migrationBuilder.DropColumn(
            name: "IdentitySourceUrl",
            schema: "diaperscout",
            table: "retailers");

        migrationBuilder.DropColumn(
            name: "Status",
            schema: "diaperscout",
            table: "retailers");

        migrationBuilder.DropColumn(
            name: "UpdatedAtUtc",
            schema: "diaperscout",
            table: "retailers");
    }
}
