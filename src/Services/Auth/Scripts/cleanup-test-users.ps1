# Cleanup test users from Auth database
# This script removes test users created during E2E tests or development

Write-Host "Cleaning up test users from Auth database..." -ForegroundColor Cyan

$result = docker exec -it coding-agent-postgres psql -U codingagent -d codingagent -c @"
DELETE FROM auth.users 
WHERE username LIKE '%test%' 
   OR username LIKE 'e2euser_%' 
   OR username LIKE 'chatuser_%'
   OR username LIKE 'testuser%'
   OR email LIKE '%test%'
   OR email LIKE '%e2e%';

SELECT COUNT(*) as deleted_count FROM auth.users WHERE false;
"@

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Test users cleaned up successfully" -ForegroundColor Green
    
    # Show remaining user count
    Write-Host "`nRemaining users:" -ForegroundColor Yellow
    docker exec -it coding-agent-postgres psql -U codingagent -d codingagent -c @"
SELECT COUNT(*) as total_users, 
       SUM(CASE WHEN is_active THEN 1 ELSE 0 END) as active_users,
       SUM(CASE WHEN roles LIKE '%Admin%' THEN 1 ELSE 0 END) as admin_users
FROM auth.users;
"@
} else {
    Write-Host "✗ Failed to cleanup test users" -ForegroundColor Red
    exit 1
}
