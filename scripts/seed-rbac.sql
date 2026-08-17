-- Seed system roles and permissions for BotPulse RBAC
-- Run this when migrations haven't applied yet

-- 1. Add missing columns to roles (if they don't exist)
ALTER TABLE roles ADD COLUMN IF NOT EXISTS description TEXT;
ALTER TABLE roles ADD COLUMN IF NOT EXISTS created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW();
ALTER TABLE roles ADD COLUMN IF NOT EXISTS updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW();

-- 2. Add missing columns to user_roles (if they don't exist)
ALTER TABLE user_roles ADD COLUMN IF NOT EXISTS assigned_by_user_id UUID;
ALTER TABLE user_roles ADD COLUMN IF NOT EXISTS assigned_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW();
ALTER TABLE user_roles ADD COLUMN IF NOT EXISTS scope VARCHAR(255);

-- 3. Insert system roles (using PascalCase column names as they exist in DB)
INSERT INTO roles ("Id", "Name", "IsSystem", description, created_at_utc, updated_at_utc)
VALUES
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Administrator',      true, 'Full access to the platform.',               NOW(), NOW()),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Operations Manager', true, 'Monitor and manage RPA operations.',         NOW(), NOW()),
  ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'Viewer',             true, 'Read-only access to platform resources.',   NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

-- 4. Insert Administrator permissions (all 23)
INSERT INTO role_permissions ("RoleId", "Permission")
SELECT 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', p
FROM unnest(ARRAY[
  'Dashboard.View','Dashboard.Export',
  'Jobs.View','Jobs.Execute','Jobs.Cancel','Jobs.Retry',
  'Robots.View','Robots.Restart',
  'Queues.View',
  'Logs.View',
  'Machines.View',
  'Assets.View',
  'Metrics.View',
  'Alerts.View','Alerts.Acknowledge',
  'Users.View','Users.Create','Users.Update','Users.Delete',
  'Roles.View','Roles.Update',
  'Settings.View','Settings.Edit',
  'Integrations.Configure'
]) AS p
ON CONFLICT DO NOTHING;

-- 5. Insert Operations Manager permissions (13)
INSERT INTO role_permissions ("RoleId", "Permission")
SELECT 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', p
FROM unnest(ARRAY[
  'Dashboard.View',
  'Robots.View',
  'Queues.View',
  'Jobs.View','Jobs.Execute','Jobs.Cancel','Jobs.Retry',
  'Logs.View',
  'Machines.View',
  'Assets.View',
  'Metrics.View',
  'Alerts.View','Alerts.Acknowledge'
]) AS p
ON CONFLICT DO NOTHING;

-- 6. Insert Viewer permissions (9)
INSERT INTO role_permissions ("RoleId", "Permission")
SELECT 'cccccccc-cccc-cccc-cccc-cccccccccccc', p
FROM unnest(ARRAY[
  'Dashboard.View',
  'Robots.View',
  'Queues.View',
  'Jobs.View',
  'Metrics.View',
  'Logs.View',
  'Machines.View',
  'Assets.View',
  'Alerts.View'
]) AS p
ON CONFLICT DO NOTHING;

-- 7. Assign Administrator role to existing admin user
INSERT INTO user_roles ("UserId", "RoleId", assigned_at_utc)
SELECT u.id, 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NOW()
FROM users u
WHERE u.user_name = 'admin'
ON CONFLICT ("UserId", "RoleId") DO NOTHING;

-- Verify
SELECT u.user_name, r."Name" as role, COUNT(rp."Permission") as permissions
FROM users u
JOIN user_roles ur ON ur."UserId" = u.id
JOIN roles r ON r."Id" = ur."RoleId"
JOIN role_permissions rp ON rp."RoleId" = r."Id"
GROUP BY u.user_name, r."Name";
