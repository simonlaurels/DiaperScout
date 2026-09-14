using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiaperScout.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDataFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "diaperscout");

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsoCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manufacturers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "retailers",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retailers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "brands",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_brands_manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalSchema: "diaperscout",
                        principalTable: "manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RetailerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CountryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    AddressLine2 = table.Column<string>(type: "text", nullable: true),
                    Locality = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Postcode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locations_countries_CountryId",
                        column: x => x.CountryId,
                        principalSchema: "diaperscout",
                        principalTable: "countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_locations_retailers_RetailerId",
                        column: x => x.RetailerId,
                        principalSchema: "diaperscout",
                        principalTable: "retailers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "explorer_profiles",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_explorer_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_explorer_profiles_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Slug = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Family = table.Column<string>(type: "text", nullable: true),
                    ProductType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    OfficialWebsiteUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_brands_BrandId",
                        column: x => x.BrandId,
                        principalSchema: "diaperscout",
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_manufacturers_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalSchema: "diaperscout",
                        principalTable: "manufacturers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "backpacks",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backpacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_backpacks_explorer_profiles_UserId",
                        column: x => x.UserId,
                        principalSchema: "diaperscout",
                        principalTable: "explorer_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_gaps",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_gaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_knowledge_gaps_locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "diaperscout",
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_knowledge_gaps_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "observations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CandidateProductName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    Narrative = table.Column<string>(type: "text", nullable: true),
                    PriceAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    PriceCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observations", x => x.Id);
                    table.CheckConstraint("CK_observations_subject", "\"ProductId\" IS NOT NULL OR \"CandidateProductName\" IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_observations_locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "diaperscout",
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_observations_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_observations_users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BackingType = table.Column<int>(type: "integer", nullable: false),
                    FastenerType = table.Column<int>(type: "integer", nullable: false),
                    PrintDesign = table.Column<string>(type: "text", nullable: true),
                    PrimaryColour = table.Column<string>(type: "text", nullable: true),
                    HasWetnessIndicator = table.Column<bool>(type: "boolean", nullable: true),
                    HasStandingLeakGuards = table.Column<bool>(type: "boolean", nullable: true),
                    HasInnerLeakGuards = table.Column<bool>(type: "boolean", nullable: true),
                    HasElasticWaistbandFront = table.Column<bool>(type: "boolean", nullable: true),
                    HasElasticWaistbandRear = table.Column<bool>(type: "boolean", nullable: true),
                    WaistbandStyle = table.Column<int>(type: "integer", nullable: false),
                    Fragrance = table.Column<int>(type: "integer", nullable: false),
                    IsLatexFree = table.Column<bool>(type: "boolean", nullable: true),
                    IsChlorineFree = table.Column<bool>(type: "boolean", nullable: true),
                    FastenerCount = table.Column<int>(type: "integer", nullable: true),
                    ConstructionNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "saved_locations",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BackpackId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_locations_backpacks_BackpackId",
                        column: x => x.BackpackId,
                        principalSchema: "diaperscout",
                        principalTable: "backpacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_locations_locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "diaperscout",
                        principalTable: "locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "saved_products",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BackpackId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_saved_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_saved_products_backpacks_BackpackId",
                        column: x => x.BackpackId,
                        principalSchema: "diaperscout",
                        principalTable: "backpacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_saved_products_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "diaperscout",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "discovery_tasks",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeGapId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResultingObservationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_discovery_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_discovery_tasks_knowledge_gaps_KnowledgeGapId",
                        column: x => x.KnowledgeGapId,
                        principalSchema: "diaperscout",
                        principalTable: "knowledge_gaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discovery_tasks_observations_ResultingObservationId",
                        column: x => x.ResultingObservationId,
                        principalSchema: "diaperscout",
                        principalTable: "observations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_discovery_tasks_users_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "editorial_decisions",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeratorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    Rationale = table.Column<string>(type: "text", nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_editorial_decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_editorial_decisions_observations_ObservationId",
                        column: x => x.ObservationId,
                        principalSchema: "diaperscout",
                        principalTable: "observations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_editorial_decisions_users_ModeratorUserId",
                        column: x => x.ModeratorUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidence_items",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evidence_items_observations_ObservationId",
                        column: x => x.ObservationId,
                        principalSchema: "diaperscout",
                        principalTable: "observations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_evidence_items_users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalSchema: "diaperscout",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "size_variants",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManufacturerSize = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    WaistMinimumCm = table.Column<int>(type: "integer", nullable: true),
                    WaistMaximumCm = table.Column<int>(type: "integer", nullable: true),
                    HipMinimumCm = table.Column<int>(type: "integer", nullable: true),
                    HipMaximumCm = table.Column<int>(type: "integer", nullable: true),
                    CapacityMl = table.Column<int>(type: "integer", nullable: true),
                    LengthMm = table.Column<int>(type: "integer", nullable: true),
                    WidthMm = table.Column<int>(type: "integer", nullable: true),
                    WeightGrams = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_size_variants", x => x.Id);
                    table.CheckConstraint("CK_size_variants_waist_range", "\"WaistMinimumCm\" IS NULL OR \"WaistMaximumCm\" IS NULL OR \"WaistMinimumCm\" <= \"WaistMaximumCm\"");
                    table.ForeignKey(
                        name: "FK_size_variants_product_variants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalSchema: "diaperscout",
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pack_types",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeVariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityPerPack = table.Column<int>(type: "integer", nullable: false),
                    PackagingType = table.Column<int>(type: "integer", nullable: false),
                    CaseQuantity = table.Column<int>(type: "integer", nullable: true),
                    PackagingNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pack_types", x => x.Id);
                    table.CheckConstraint("CK_pack_types_quantity", "\"QuantityPerPack\" > 0");
                    table.ForeignKey(
                        name: "FK_pack_types_size_variants_SizeVariantId",
                        column: x => x.SizeVariantId,
                        principalSchema: "diaperscout",
                        principalTable: "size_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_identifiers",
                schema: "diaperscout",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_identifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_identifiers_pack_types_PackTypeId",
                        column: x => x.PackTypeId,
                        principalSchema: "diaperscout",
                        principalTable: "pack_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_backpacks_UserId",
                schema: "diaperscout",
                table: "backpacks",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_brands_ManufacturerId",
                schema: "diaperscout",
                table: "brands",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_brands_Slug",
                schema: "diaperscout",
                table: "brands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_countries_IsoCode",
                schema: "diaperscout",
                table: "countries",
                column: "IsoCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_discovery_tasks_AcceptedByUserId",
                schema: "diaperscout",
                table: "discovery_tasks",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_discovery_tasks_KnowledgeGapId",
                schema: "diaperscout",
                table: "discovery_tasks",
                column: "KnowledgeGapId");

            migrationBuilder.CreateIndex(
                name: "IX_discovery_tasks_ResultingObservationId",
                schema: "diaperscout",
                table: "discovery_tasks",
                column: "ResultingObservationId");

            migrationBuilder.CreateIndex(
                name: "IX_discovery_tasks_State",
                schema: "diaperscout",
                table: "discovery_tasks",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_editorial_decisions_ModeratorUserId",
                schema: "diaperscout",
                table: "editorial_decisions",
                column: "ModeratorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_editorial_decisions_ObservationId_DecidedAtUtc",
                schema: "diaperscout",
                table: "editorial_decisions",
                columns: new[] { "ObservationId", "DecidedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_ObservationId_SubmittedAtUtc",
                schema: "diaperscout",
                table: "evidence_items",
                columns: new[] { "ObservationId", "SubmittedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_evidence_items_SubmittedByUserId",
                schema: "diaperscout",
                table: "evidence_items",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_explorer_profiles_DisplayName",
                schema: "diaperscout",
                table: "explorer_profiles",
                column: "DisplayName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_explorer_profiles_UserId",
                schema: "diaperscout",
                table: "explorer_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_gaps_LocationId",
                schema: "diaperscout",
                table: "knowledge_gaps",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_knowledge_gaps_ProductId",
                schema: "diaperscout",
                table: "knowledge_gaps",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_CountryId",
                schema: "diaperscout",
                table: "locations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_locations_RetailerId_Postcode_Name",
                schema: "diaperscout",
                table: "locations",
                columns: new[] { "RetailerId", "Postcode", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_manufacturers_Slug",
                schema: "diaperscout",
                table: "manufacturers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_observations_AuthorUserId_CreatedAtUtc",
                schema: "diaperscout",
                table: "observations",
                columns: new[] { "AuthorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_observations_LocationId_ObservedAtUtc",
                schema: "diaperscout",
                table: "observations",
                columns: new[] { "LocationId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_observations_ProductId_ObservedAtUtc",
                schema: "diaperscout",
                table: "observations",
                columns: new[] { "ProductId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_pack_types_SizeVariantId_QuantityPerPack_PackagingType",
                schema: "diaperscout",
                table: "pack_types",
                columns: new[] { "SizeVariantId", "QuantityPerPack", "PackagingType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_identifiers_PackTypeId",
                schema: "diaperscout",
                table: "product_identifiers",
                column: "PackTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_product_identifiers_Type_Value",
                schema: "diaperscout",
                table: "product_identifiers",
                columns: new[] { "Type", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId_Name",
                schema: "diaperscout",
                table: "product_variants",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_BrandId",
                schema: "diaperscout",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_products_ManufacturerId_Name",
                schema: "diaperscout",
                table: "products",
                columns: new[] { "ManufacturerId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_products_Slug",
                schema: "diaperscout",
                table: "products",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_retailers_Slug",
                schema: "diaperscout",
                table: "retailers",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_locations_BackpackId_LocationId",
                schema: "diaperscout",
                table: "saved_locations",
                columns: new[] { "BackpackId", "LocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_locations_LocationId",
                schema: "diaperscout",
                table: "saved_locations",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_saved_products_BackpackId_ProductId",
                schema: "diaperscout",
                table: "saved_products",
                columns: new[] { "BackpackId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_saved_products_ProductId",
                schema: "diaperscout",
                table: "saved_products",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_size_variants_ProductVariantId_ManufacturerSize",
                schema: "diaperscout",
                table: "size_variants",
                columns: new[] { "ProductVariantId", "ManufacturerSize" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Subject",
                schema: "diaperscout",
                table: "users",
                column: "Subject",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discovery_tasks",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "editorial_decisions",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "evidence_items",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "product_identifiers",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "saved_locations",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "saved_products",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "knowledge_gaps",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "observations",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "pack_types",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "backpacks",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "locations",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "size_variants",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "explorer_profiles",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "retailers",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "users",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "products",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "brands",
                schema: "diaperscout");

            migrationBuilder.DropTable(
                name: "manufacturers",
                schema: "diaperscout");
        }
    }
}
