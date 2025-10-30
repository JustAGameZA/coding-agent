-- Seed admin user for Coding Agent Auth Service
-- Password: Admin@1234! (hashed with BCrypt)

-- Create admin user if not exists
INSERT INTO auth.users (id, username, email, password_hash, roles, is_active, created_at, updated_at)
VALUES (
  gen_random_uuid(),
  'admin',
  'admin@codingagent.local',
  -- BCrypt hash for 'Admin@1234!' with work factor 12
  -- Note: This is a placeholder hash. Generate a real one using BCrypt before production use
  '$2a$12$LQv4vKZ9Z9Z9Z9Z9Z9Z9.eZ9Z9Z9Z9Z9Z9Z9Z9Z9Z9Z9Z9Z9Z9Z9Z9',
  'Admin,User',
  true,
  NOW(),
  NOW()
)
ON CONFLICT (username) DO UPDATE 
SET 
  roles = 'Admin,User',
  is_active = true,
  updated_at = NOW();

-- Verify admin user was created
SELECT id, username, email, roles, is_active, created_at 
FROM auth.users 
WHERE username = 'admin';
