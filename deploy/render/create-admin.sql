-- Run this after migrations are applied to create the admin user
-- Password: Admin@BotPulse2024! (Argon2id hash)
-- Generate new hash with: dotnet run --project src/BotPulse.Api -- hash-password "YourPassword"

INSERT INTO users (id, external_id, user_name, email, role, auth_provider, password_hash, is_active, created_at_utc, updated_at_utc)
VALUES (
  gen_random_uuid(),
  'admin-local-001',
  'admin',
  'admin@botpulse.local',
  'Administrator',
  'Local',
  -- This is a placeholder hash — generate the real one from the running app
  -- or use the create-admin-user.ps1 script against the deployed API
  '$argon2id$v=19$m=65536,t=3,p=1$placeholder',
  true,
  NOW(),
  NOW()
) ON CONFLICT DO NOTHING;
