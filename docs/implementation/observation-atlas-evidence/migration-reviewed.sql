START TRANSACTION;
DROP INDEX diaperscout."IX_catalogue_submissions_SubmittedByUserId";

ALTER TABLE diaperscout.observations ADD "ContributionId" uuid;

ALTER TABLE diaperscout.observations ADD "PackTypeId" uuid;

ALTER TABLE diaperscout.locations ALTER COLUMN "RetailerId" DROP NOT NULL;

ALTER TABLE diaperscout.locations ADD "CreatedAtUtc" timestamp with time zone;

ALTER TABLE diaperscout.locations ADD "CreatedByUserId" uuid;

ALTER TABLE diaperscout.locations ADD "IsPublicCommercialPlace" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE diaperscout.locations ADD "PlaceIdentity" character varying(64);

ALTER TABLE diaperscout.catalogue_submissions ADD "ProposedPackQuantity" integer;

ALTER TABLE diaperscout.catalogue_submissions ADD "PublicContributionId" uuid;

ALTER TABLE diaperscout.catalogue_submissions ADD "ResolvedPackTypeId" uuid;

CREATE UNIQUE INDEX "IX_observations_AuthorUserId_ContributionId" ON diaperscout.observations ("AuthorUserId", "ContributionId");

CREATE INDEX "IX_observations_PackTypeId" ON diaperscout.observations ("PackTypeId");

CREATE INDEX "IX_locations_CreatedByUserId" ON diaperscout.locations ("CreatedByUserId");

CREATE UNIQUE INDEX "IX_locations_PlaceIdentity" ON diaperscout.locations ("PlaceIdentity");

CREATE INDEX "IX_catalogue_submissions_ResolvedPackTypeId" ON diaperscout.catalogue_submissions ("ResolvedPackTypeId");

CREATE UNIQUE INDEX "IX_catalogue_submissions_SubmittedByUserId_PublicContributionId" ON diaperscout.catalogue_submissions ("SubmittedByUserId", "PublicContributionId");

ALTER TABLE diaperscout.catalogue_submissions ADD CONSTRAINT "FK_catalogue_submissions_pack_types_ResolvedPackTypeId" FOREIGN KEY ("ResolvedPackTypeId") REFERENCES diaperscout.pack_types ("Id") ON DELETE RESTRICT;

ALTER TABLE diaperscout.locations ADD CONSTRAINT "FK_locations_users_CreatedByUserId" FOREIGN KEY ("CreatedByUserId") REFERENCES diaperscout.users ("Id") ON DELETE RESTRICT;

ALTER TABLE diaperscout.observations ADD CONSTRAINT "FK_observations_pack_types_PackTypeId" FOREIGN KEY ("PackTypeId") REFERENCES diaperscout.pack_types ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261001141156_PhysicalObservationsAndPublicProposals', '10.0.0');

COMMIT;

