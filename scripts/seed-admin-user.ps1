# ============================================
# Seed Default Admin User Script
# Creates an admin user in the database via API
# Username: admin
# Password: Admin123! (can be changed later via UI)
# ============================================

param(
    [string]$GatewayUrl = "http://localhost:5000",
    [string]$ContainerName = "coding-agent-postgres",
    [string]$DbUser = "codingagent",
    [string]$DbName = "codingagent",
    [string]$Username = "admin",
    [string]$Password = "Admin123!",
    [string]$Email = "admin@example.com"
)

Write-Host "================================================" -ForegroundColor Cyan
Write-Host "Seeding Default Admin User" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Cyan
Write-Host ""

# Check if Gateway is accessible
Write-Host "Step 1: Checking Gateway accessibility..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$GatewayUrl/health" -Method GET -TimeoutSec 5 -ErrorAction Stop
    Write-Host "✓ Gateway is accessible: $($response.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "✗ Warning: Gateway not accessible at $GatewayUrl" -ForegroundColor Yellow
    Write-Host "  Make sure services are running: docker compose up -d" -ForegroundColor Yellow
    Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Gray
    exit 1
}

Write-Host ""

# Step 2: Register admin user via API (this will hash the password correctly)
Write-Host "Step 2: Registering admin user via API..." -ForegroundColor Yellow

$registerBody = @{
    username = $Username
    email = $Email
    password = $Password
    confirmPassword = $Password
} | ConvertTo-Json

try {
    $registerResponse = Invoke-RestMethod -Uri "$GatewayUrl/api/auth/register" `
        -Method POST `
        -ContentType "application/json" `
        -Body $registerBody `
        -TimeoutSec 10 `
        -ErrorAction Stop
    
    Write-Host "✓ Admin user registered successfully!" -ForegroundColor Green
    Write-Host "  User ID: $($registerResponse.user.id)" -ForegroundColor Gray
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 409) {
        Write-Host "ℹ Admin user already exists (skipping registration)" -ForegroundColor Yellow
    } else {
        $errorBody = $_.ErrorDetails.Message
        Write-Host "✗ Error registering admin user: $($_.Exception.Message)" -ForegroundColor Red
        if ($errorBody) {
            Write-Host "  Error details: $errorBody" -ForegroundColor Gray
        }
        exit 1
    }
}

Write-Host ""

# Step 3: Check database container
Write-Host "Step 3: Checking database container..." -ForegroundColor Yellow
$containerStatus = docker ps --filter "name=$ContainerName" --format "{{.Status}}" 2>&1

if (-not $containerStatus -or $containerStatus -like "*Error*") {
    Write-Host "✗ Warning: Container '$ContainerName' might not be running!" -ForegroundColor Yellow
    Write-Host "  Attempting to continue with role elevation..." -ForegroundColor Yellow
} else {
    Write-Host "✓ Container is running: $containerStatus" -ForegroundColor Green
}

Write-Host ""

# Step 4: Elevate user to Admin role via database
Write-Host "Step 4: Elevating user to Admin role..." -ForegroundColor Yellow

$elevateQuery = @"
UPDATE auth.users 
SET roles = 'Admin,User',
    updated_at = NOW()
WHERE username = '$Username';
"@

try {
    $result = docker exec $ContainerName psql -U $DbUser -d $DbName -c $elevateQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ User elevated to Admin role successfully!" -ForegroundColor Green
    } else {
        Write-Host "✗ Error: Failed to elevate user to Admin role!" -ForegroundColor Red
        Write-Host "  Error: $result" -ForegroundColor Gray
        exit 1
    }
} catch {
    Write-Host "✗ Error executing database query: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 5: Verify admin user
Write-Host "Step 5: Verifying admin user..." -ForegroundColor Yellow
$verifyQuery = "SELECT username, email, roles, is_active, created_at FROM auth.users WHERE username = '$Username';"
try {
    $verifyResult = docker exec $ContainerName psql -U $DbUser -d $DbName -c $verifyQuery 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host $verifyResult
    }
} catch {
    Write-Host "Could not verify user (database query failed)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Green
Write-Host "Admin User Created Successfully!" -ForegroundColor Green
Write-Host "================================================" -ForegroundColor Green
Write-Host "  Username: $Username" -ForegroundColor Cyan
Write-Host "  Password: $Password" -ForegroundColor Cyan
Write-Host "  Email: $Email" -ForegroundColor Cyan
Write-Host "  Roles: Admin, User" -ForegroundColor Cyan
Write-Host "================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Note: You can change the password later via the UI!" -ForegroundColor Yellow
Write-Host "Navigate to: http://localhost:4200/login" -ForegroundColor Cyan


