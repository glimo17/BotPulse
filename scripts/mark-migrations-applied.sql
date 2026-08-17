-- Mark the new RBAC migrations as applied without running them
-- (because we applied the schema changes manually via seed-rbac.sql)

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
  ('20260727173000_SeedDefaultRoles',   '8.0.11'),
  ('20260728000001_AddRbacColumns',     '8.0.11'),
  ('20260728000002_SeedSystemRoles',    '8.0.11'),
  ('20260728000003_MigrateUserRoles',   '8.0.11')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verify
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
