START TRANSACTION;
ALTER TABLE diaperscout.locations ADD "Category" integer;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261003102616_AddPlaceCategory', '10.0.0');

COMMIT;
