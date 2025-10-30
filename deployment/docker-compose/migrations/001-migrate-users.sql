-- 001-migrate-users.sql
-- Migrate legacy users into auth.users (idempotent insert)

-- Example assumes a legacy table legacy.users exists in same DB or dblink
-- Adjust SELECT source as needed.

INSERT INTO auth.users (id, username, email, password_hash, is_active, is_verified, created_at, updated_at)
SELECT u.id, u.username, u.email, u.password_hash, COALESCE(u.is_active, true), COALESCE(u.is_verified, false), COALESCE(u.created_at, NOW()), COALESCE(u.updated_at, NOW())
FROM legacy.users u
LEFT JOIN auth.users au ON au.id = u.id
WHERE au.id IS NULL;
