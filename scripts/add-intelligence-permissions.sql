INSERT INTO role_permissions ("RoleId", "Permission")
VALUES
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Intelligence.View'),
  ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Intelligence.Diagnose'),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Intelligence.View'),
  ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Intelligence.Diagnose')
ON CONFLICT DO NOTHING;

SELECT COUNT(*) AS intelligence_perms
FROM role_permissions
WHERE "Permission" LIKE 'Intelligence%';
