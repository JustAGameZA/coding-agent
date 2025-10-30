# Database Cleanup Script
# Removes test users created during E2E testing
# Run this script when test database has accumulated too many test users

$containerName = "coding-agent-postgres"
$dbUser = "codingagent"
$dbName = "codingagent"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Database Cleanup - Test User Removal" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if container is running
Write-Host "Checking database container status..." -ForegroundColor Yellow
$containerStatus = docker ps --filter "name=$containerName" --format "{{.Status}}"

if (-not $containerStatus) {
    Write-Host "Error: Container '$containerName' is not running!" -ForegroundColor Red
    Write-Host "Start the container first: docker compose up -d postgres" -ForegroundColor Yellow
    exit 1
}

Write-Host "Container is running: $containerStatus" -ForegroundColor Green
Write-Host ""

# Preview test users to delete
Write-Host "Fetching test users to delete..." -ForegroundColor Yellow
$previewQuery = @"
SELECT username, email, roles, is_active, created_at 
FROM auth.users 
WHERE username LIKE '%test%' 
   OR username LIKE 'e2euser_%' 
   OR username LIKE 'e2eadmin_%'
   OR username LIKE 'chatuser_%'
ORDER BY created_at DESC;
"@

Write-Host ""
Write-Host "Test Users Found:" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
docker exec $containerName psql -U $dbUser -d $dbName -c $previewQuery

# Count test users
$userCountQuery = @"
SELECT COUNT(*) FROM auth.users 
WHERE username LIKE '%test%' 
   OR username LIKE 'e2euser_%' 
   OR username LIKE 'e2eadmin_%'
   OR username LIKE 'chatuser_%';
"@

$userCount = docker exec $containerName psql -U $dbUser -d $dbName -t -c $userCountQuery
$userCount = $userCount.Trim()

Write-Host ""
Write-Host "Found $userCount test users to delete." -ForegroundColor Cyan
Write-Host ""

if ($userCount -eq "0") {
    Write-Host "No test users to delete. Database is clean!" -ForegroundColor Green
    exit 0
}

# Confirmation prompt
Write-Host "WARNING: This will permanently delete $userCount test users!" -ForegroundColor Yellow
$confirmation = Read-Host "Do you want to proceed with deletion? Type 'yes' to confirm"

if ($confirmation -ne 'yes') {
    Write-Host ""
    Write-Host "Cleanup cancelled by user." -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "Deleting test users..." -ForegroundColor Red

# Execute deletion
$deleteQuery = @"
DELETE FROM auth.users 
WHERE username LIKE '%test%' 
   OR username LIKE 'e2euser_%' 
   OR username LIKE 'e2eadmin_%'
   OR username LIKE 'chatuser_%';
"@

$result = docker exec $containerName psql -U $dbUser -d $dbName -c $deleteQuery

Write-Host $result -ForegroundColor Gray
Write-Host ""
Write-Host "Test users deleted successfully!" -ForegroundColor Green
Write-Host ""

# Show remaining users count
Write-Host "Fetching remaining user count..." -ForegroundColor Yellow
$remainingCountQuery = "SELECT COUNT(*) FROM auth.users;"
$remainingCount = docker exec $containerName psql -U $dbUser -d $dbName -t -c $remainingCountQuery
$remainingCount = $remainingCount.Trim()

Write-Host "Remaining users in database: $remainingCount" -ForegroundColor Cyan

# Show remaining users (excluding test users)
Write-Host ""
Write-Host "Production Users (preserved):" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
$prodUsersQuery = @"
SELECT username, email, roles, is_active, created_at 
FROM auth.users 
ORDER BY created_at DESC;
"@

docker exec $containerName psql -U $dbUser -d $dbName -c $prodUsersQuery

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Cleanup complete!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
