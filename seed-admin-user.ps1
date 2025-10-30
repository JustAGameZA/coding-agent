# Seed Admin User Script
# Creates an admin user for E2E testing and local development via API registration
# Then elevates the user to Admin role
# Username: admin
# Password: Admin@1234!

$gatewayUrl = "http://localhost:5000"
$containerName = "coding-agent-postgres"
$dbUser = "codingagent"
$dbName = "codingagent"

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Seeding Admin User (via API + DB role elevation)" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Gateway is accessible
Write-Host "Checking Gateway accessibility..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$gatewayUrl/health" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "Gateway is accessible: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "Warning: Gateway not accessible at $gatewayUrl" -ForegroundColor Yellow
    Write-Host "Make sure services are running: docker compose up -d" -ForegroundColor Yellow
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Gray
}

Write-Host ""

# Step 1: Register admin user via API
Write-Host "Step 1: Registering admin user via API..." -ForegroundColor Yellow

$registerBody = @{
    username = "admin"
    email = "admin@codingagent.local"
    password = "Admin@1234!"
    confirmPassword = "Admin@1234!"
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$gatewayUrl/api/auth/register" `
        -Method POST `
        -ContentType "application/json" `
        -Body $registerBody `
        -TimeoutSec 10 `
        -ErrorAction Stop
    
    Write-Host "Admin user registered successfully!" -ForegroundColor Green
    Write-Host "  User ID: $($registerResponse.user.id)" -ForegroundColor Gray
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 409) {
        Write-Host "Admin user already exists (skipping registration)" -ForegroundColor Yellow
    } else {
        Write-Host "Error registering admin user: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Attempting to continue with role elevation..." -ForegroundColor Yellow
    }
}

Write-Host ""

# Step 2: Check database container
Write-Host "Step 2: Checking database container..." -ForegroundColor Yellow
$containerStatus = docker ps --filter "name=$containerName" --format "{{.Status}}"

if (-not $containerStatus) {
    Write-Host "Error: Container '$containerName' is not running!" -ForegroundColor Red
    Write-Host "Start the container first: docker compose up -d postgres" -ForegroundColor Yellow
    exit 1
}

Write-Host "Container is running: $containerStatus" -ForegroundColor Green
Write-Host ""

# Step 3: Elevate user to Admin role
Write-Host "Step 3: Elevating user to Admin role..." -ForegroundColor Yellow

$elevateQuery = @"
UPDATE auth.users 
SET roles = ARRAY['Admin', 'User'],
    updated_at = NOW()
WHERE username = 'admin';
"@

$result = docker exec $containerName psql -U $dbUser -d $dbName -c $elevateQuery

if ($LASTEXITCODE -eq 0) {
    Write-Host "User elevated to Admin role successfully!" -ForegroundColor Green
} else {
    Write-Host "Error: Failed to elevate user to Admin role!" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 4: Verify admin user
Write-Host "Step 4: Verifying admin user..." -ForegroundColor Yellow
$verifyQuery = "SELECT username, email, roles, is_active, created_at FROM auth.users WHERE username = 'admin';"
docker exec $containerName psql -U $dbUser -d $dbName -c $verifyQuery

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Admin User Created Successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Username: admin" -ForegroundColor Cyan
Write-Host "  Password: Admin@1234!" -ForegroundColor Cyan
Write-Host "  Email: admin@codingagent.local" -ForegroundColor Cyan
Write-Host "  Roles: Admin, User" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "You can now login with these credentials!" -ForegroundColor Green
Write-Host "Navigate to: http://localhost:4200/login" -ForegroundColor Cyan
