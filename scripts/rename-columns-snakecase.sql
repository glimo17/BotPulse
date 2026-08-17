-- Rename PascalCase columns to snake_case to match EF Core configuration
-- NOTE: role_permissions stays PascalCase (RoleId, Permission) — EF default convention

-- roles table
ALTER TABLE roles RENAME COLUMN "Id" TO id;
ALTER TABLE roles RENAME COLUMN "Name" TO name;
ALTER TABLE roles RENAME COLUMN "IsSystem" TO is_system;

-- user_roles table
ALTER TABLE user_roles RENAME COLUMN "UserId" TO user_id;
ALTER TABLE user_roles RENAME COLUMN "RoleId" TO role_id;
