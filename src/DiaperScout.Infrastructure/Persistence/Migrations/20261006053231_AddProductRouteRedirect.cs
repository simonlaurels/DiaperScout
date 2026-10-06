using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRouteRedirect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_route_redirects",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefaultVariantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_route_redirects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_route_redirects_product_variants_DefaultVariantId",
                        column: x => x.DefaultVariantId,
                        principalSchema: "diaperscout",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_route_redirects_products_SourceProductId",
                        column: x => x.SourceProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_route_redirects_products_TargetProductId",
                        column: x => x.TargetProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_route_redirects_DefaultVariantId",
                schema: "diaperscout",
                table: "product_route_redirects",
                column: "DefaultVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_route_redirects_SourceProductId",
                schema: "diaperscout",
                table: "product_route_redirects",
                column: "SourceProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_route_redirects_TargetProductId",
                schema: "diaperscout",
                table: "product_route_redirects",
                column: "TargetProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_route_redirects",
                schema: "diaperscout");
        }
    }
}
