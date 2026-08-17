INSERT INTO role_permissions ("RoleId", "Permission")
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Audit.View')
ON CONFLICT DO NOTHING;

SELECT COUNT(*) AS admin_perms
FROM role_permissions
WHERE "RoleId" = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
