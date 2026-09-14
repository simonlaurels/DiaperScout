CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'diaperscout') THEN
            CREATE SCHEMA diaperscout;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.countries (
        "Id" uuid NOT NULL,
        "IsoCode" character varying(2) NOT NULL,
        "Name" character varying(100) NOT NULL,
        CONSTRAINT "PK_countries" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.manufacturers (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Slug" character varying(200) NOT NULL,
        "WebsiteUrl" text,
        CONSTRAINT "PK_manufacturers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.retailers (
        "Id" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Slug" character varying(200) NOT NULL,
        "WebsiteUrl" text,
        CONSTRAINT "PK_retailers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.users (
        "Id" uuid NOT NULL,
        "Subject" character varying(200) NOT NULL,
        "Status" integer NOT NULL,
        CONSTRAINT "PK_users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.brands (
        "Id" uuid NOT NULL,
        "ManufacturerId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "Slug" character varying(200) NOT NULL,
        CONSTRAINT "PK_brands" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_brands_manufacturers_ManufacturerId" FOREIGN KEY ("ManufacturerId") REFERENCES diaperscout.manufacturers ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.locations (
        "Id" uuid NOT NULL,
        "RetailerId" uuid NOT NULL,
        "CountryId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "AddressLine1" character varying(250) NOT NULL,
        "AddressLine2" text,
        "Locality" character varying(150) NOT NULL,
        "Postcode" character varying(32) NOT NULL,
        "Latitude" numeric(9,6),
        "Longitude" numeric(9,6),
        CONSTRAINT "PK_locations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_locations_countries_CountryId" FOREIGN KEY ("CountryId") REFERENCES diaperscout.countries ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_locations_retailers_RetailerId" FOREIGN KEY ("RetailerId") REFERENCES diaperscout.retailers ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.explorer_profiles (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "DisplayName" character varying(100) NOT NULL,
        CONSTRAINT "PK_explorer_profiles" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_explorer_profiles_users_UserId" FOREIGN KEY ("UserId") REFERENCES diaperscout.users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.products (
        "Id" uuid NOT NULL,
        "ManufacturerId" uuid NOT NULL,
        "BrandId" uuid,
        "Name" character varying(250) NOT NULL,
        "Slug" character varying(250) NOT NULL,
        "Family" text,
        "ProductType" integer NOT NULL,
        "Status" integer NOT NULL,
        "Description" text,
        "OfficialWebsiteUrl" text,
        CONSTRAINT "PK_products" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_products_brands_BrandId" FOREIGN KEY ("BrandId") REFERENCES diaperscout.brands ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_products_manufacturers_ManufacturerId" FOREIGN KEY ("ManufacturerId") REFERENCES diaperscout.manufacturers ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.backpacks (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        CONSTRAINT "PK_backpacks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_backpacks_explorer_profiles_UserId" FOREIGN KEY ("UserId") REFERENCES diaperscout.explorer_profiles ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.knowledge_gaps (
        "Id" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" text,
        "ProductId" uuid,
        "LocationId" uuid,
        CONSTRAINT "PK_knowledge_gaps" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_knowledge_gaps_locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES diaperscout.locations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_knowledge_gaps_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES diaperscout.products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.observations (
        "Id" uuid NOT NULL,
        "AuthorUserId" uuid NOT NULL,
        "ProductId" uuid,
        "CandidateProductName" character varying(250),
        "LocationId" uuid,
        "Type" integer NOT NULL,
        "ObservedAtUtc" timestamp with time zone NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "State" integer NOT NULL,
        "Narrative" text,
        "PriceAmount" numeric(12,2),
        "PriceCurrencyCode" character varying(3),
        CONSTRAINT "PK_observations" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_observations_subject" CHECK ("ProductId" IS NOT NULL OR "CandidateProductName" IS NOT NULL),
        CONSTRAINT "FK_observations_locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES diaperscout.locations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_observations_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES diaperscout.products ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_observations_users_AuthorUserId" FOREIGN KEY ("AuthorUserId") REFERENCES diaperscout.users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.product_variants (
        "Id" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "Name" character varying(200) NOT NULL,
        "BackingType" integer NOT NULL,
        "FastenerType" integer NOT NULL,
        "PrintDesign" text,
        "PrimaryColour" text,
        "HasWetnessIndicator" boolean,
        "HasStandingLeakGuards" boolean,
        "HasInnerLeakGuards" boolean,
        "HasElasticWaistbandFront" boolean,
        "HasElasticWaistbandRear" boolean,
        "WaistbandStyle" integer NOT NULL,
        "Fragrance" integer NOT NULL,
        "IsLatexFree" boolean,
        "IsChlorineFree" boolean,
        "FastenerCount" integer,
        "ConstructionNotes" text,
        CONSTRAINT "PK_product_variants" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_product_variants_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES diaperscout.products ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.saved_locations (
        "Id" uuid NOT NULL,
        "BackpackId" uuid NOT NULL,
        "LocationId" uuid NOT NULL,
        "SavedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_saved_locations" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_saved_locations_backpacks_BackpackId" FOREIGN KEY ("BackpackId") REFERENCES diaperscout.backpacks ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_saved_locations_locations_LocationId" FOREIGN KEY ("LocationId") REFERENCES diaperscout.locations ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.saved_products (
        "Id" uuid NOT NULL,
        "BackpackId" uuid NOT NULL,
        "ProductId" uuid NOT NULL,
        "SavedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_saved_products" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_saved_products_backpacks_BackpackId" FOREIGN KEY ("BackpackId") REFERENCES diaperscout.backpacks ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_saved_products_products_ProductId" FOREIGN KEY ("ProductId") REFERENCES diaperscout.products ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.discovery_tasks (
        "Id" uuid NOT NULL,
        "KnowledgeGapId" uuid NOT NULL,
        "Title" character varying(300) NOT NULL,
        "Description" text,
        "State" integer NOT NULL,
        "AcceptedByUserId" uuid,
        "ResultingObservationId" uuid,
        CONSTRAINT "PK_discovery_tasks" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_discovery_tasks_knowledge_gaps_KnowledgeGapId" FOREIGN KEY ("KnowledgeGapId") REFERENCES diaperscout.knowledge_gaps ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_discovery_tasks_observations_ResultingObservationId" FOREIGN KEY ("ResultingObservationId") REFERENCES diaperscout.observations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_discovery_tasks_users_AcceptedByUserId" FOREIGN KEY ("AcceptedByUserId") REFERENCES diaperscout.users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.editorial_decisions (
        "Id" uuid NOT NULL,
        "ObservationId" uuid NOT NULL,
        "ModeratorUserId" uuid NOT NULL,
        "Outcome" integer NOT NULL,
        "Rationale" text,
        "DecidedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_editorial_decisions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_editorial_decisions_observations_ObservationId" FOREIGN KEY ("ObservationId") REFERENCES diaperscout.observations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_editorial_decisions_users_ModeratorUserId" FOREIGN KEY ("ModeratorUserId") REFERENCES diaperscout.users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.evidence_items (
        "Id" uuid NOT NULL,
        "ObservationId" uuid NOT NULL,
        "SubmittedByUserId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "StorageKey" character varying(1000) NOT NULL,
        "ContentType" text,
        "SubmittedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_evidence_items" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_evidence_items_observations_ObservationId" FOREIGN KEY ("ObservationId") REFERENCES diaperscout.observations ("Id") ON DELETE RESTRICT,
        CONSTRAINT "FK_evidence_items_users_SubmittedByUserId" FOREIGN KEY ("SubmittedByUserId") REFERENCES diaperscout.users ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.size_variants (
        "Id" uuid NOT NULL,
        "ProductVariantId" uuid NOT NULL,
        "ManufacturerSize" character varying(100) NOT NULL,
        "WaistMinimumCm" integer,
        "WaistMaximumCm" integer,
        "HipMinimumCm" integer,
        "HipMaximumCm" integer,
        "CapacityMl" integer,
        "LengthMm" integer,
        "WidthMm" integer,
        "WeightGrams" integer,
        CONSTRAINT "PK_size_variants" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_size_variants_waist_range" CHECK ("WaistMinimumCm" IS NULL OR "WaistMaximumCm" IS NULL OR "WaistMinimumCm" <= "WaistMaximumCm"),
        CONSTRAINT "FK_size_variants_product_variants_ProductVariantId" FOREIGN KEY ("ProductVariantId") REFERENCES diaperscout.product_variants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.pack_types (
        "Id" uuid NOT NULL,
        "SizeVariantId" uuid NOT NULL,
        "QuantityPerPack" integer NOT NULL,
        "PackagingType" integer NOT NULL,
        "CaseQuantity" integer,
        "PackagingNotes" text,
        CONSTRAINT "PK_pack_types" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_pack_types_quantity" CHECK ("QuantityPerPack" > 0),
        CONSTRAINT "FK_pack_types_size_variants_SizeVariantId" FOREIGN KEY ("SizeVariantId") REFERENCES diaperscout.size_variants ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE TABLE diaperscout.product_identifiers (
        "Id" uuid NOT NULL,
        "PackTypeId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Value" character varying(64) NOT NULL,
        CONSTRAINT "PK_product_identifiers" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_product_identifiers_pack_types_PackTypeId" FOREIGN KEY ("PackTypeId") REFERENCES diaperscout.pack_types ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_backpacks_UserId" ON diaperscout.backpacks ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_brands_ManufacturerId" ON diaperscout.brands ("ManufacturerId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_brands_Slug" ON diaperscout.brands ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_countries_IsoCode" ON diaperscout.countries ("IsoCode");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_discovery_tasks_AcceptedByUserId" ON diaperscout.discovery_tasks ("AcceptedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_discovery_tasks_KnowledgeGapId" ON diaperscout.discovery_tasks ("KnowledgeGapId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_discovery_tasks_ResultingObservationId" ON diaperscout.discovery_tasks ("ResultingObservationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_discovery_tasks_State" ON diaperscout.discovery_tasks ("State");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_editorial_decisions_ModeratorUserId" ON diaperscout.editorial_decisions ("ModeratorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_editorial_decisions_ObservationId_DecidedAtUtc" ON diaperscout.editorial_decisions ("ObservationId", "DecidedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_evidence_items_ObservationId_SubmittedAtUtc" ON diaperscout.evidence_items ("ObservationId", "SubmittedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_evidence_items_SubmittedByUserId" ON diaperscout.evidence_items ("SubmittedByUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_explorer_profiles_DisplayName" ON diaperscout.explorer_profiles ("DisplayName");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_explorer_profiles_UserId" ON diaperscout.explorer_profiles ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_knowledge_gaps_LocationId" ON diaperscout.knowledge_gaps ("LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_knowledge_gaps_ProductId" ON diaperscout.knowledge_gaps ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_locations_CountryId" ON diaperscout.locations ("CountryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_locations_RetailerId_Postcode_Name" ON diaperscout.locations ("RetailerId", "Postcode", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_manufacturers_Slug" ON diaperscout.manufacturers ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_observations_AuthorUserId_CreatedAtUtc" ON diaperscout.observations ("AuthorUserId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_observations_LocationId_ObservedAtUtc" ON diaperscout.observations ("LocationId", "ObservedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_observations_ProductId_ObservedAtUtc" ON diaperscout.observations ("ProductId", "ObservedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_pack_types_SizeVariantId_QuantityPerPack_PackagingType" ON diaperscout.pack_types ("SizeVariantId", "QuantityPerPack", "PackagingType");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_product_identifiers_PackTypeId" ON diaperscout.product_identifiers ("PackTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_product_identifiers_Type_Value" ON diaperscout.product_identifiers ("Type", "Value");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_product_variants_ProductId_Name" ON diaperscout.product_variants ("ProductId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_products_BrandId" ON diaperscout.products ("BrandId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_products_ManufacturerId_Name" ON diaperscout.products ("ManufacturerId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_products_Slug" ON diaperscout.products ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_retailers_Slug" ON diaperscout.retailers ("Slug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_saved_locations_BackpackId_LocationId" ON diaperscout.saved_locations ("BackpackId", "LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_saved_locations_LocationId" ON diaperscout.saved_locations ("LocationId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_saved_products_BackpackId_ProductId" ON diaperscout.saved_products ("BackpackId", "ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE INDEX "IX_saved_products_ProductId" ON diaperscout.saved_products ("ProductId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_size_variants_ProductVariantId_ManufacturerSize" ON diaperscout.size_variants ("ProductVariantId", "ManufacturerSize");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    CREATE UNIQUE INDEX "IX_users_Subject" ON diaperscout.users ("Subject");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260830104332_InitialDataFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260830104332_InitialDataFoundation', '10.0.0');
    END IF;
END $EF$;
COMMIT;

