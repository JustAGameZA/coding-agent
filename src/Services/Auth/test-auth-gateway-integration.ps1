# Auth Service Gateway Integration - Test Script
# Run after Auth Service implementation is complete

param(
    [string]$GatewayUrl = "http://localhost:5000",
    [string]$AuthServiceDirectUrl = "http://localhost:5008"
)

$ErrorActionPreference = "Continue"
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Auth Service Gateway Integration Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test data
$testUser = @{
    username = "testuser_$(Get-Random -Maximum 9999)"
    email = "test_$(Get-Random -Maximum 9999)@example.com"
    password = "SecureP@ssw0rd123!"
    fullName = "Test User"
}

# Test 1: Health check (direct)
Write-Host "[Test 1] Health check (direct to Auth Service)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$AuthServiceDirectUrl/health" -Method Get -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ PASS: Auth Service health check OK (Status: $($response.StatusCode))" -ForegroundColor Green
    } else {
        Write-Host "❌ FAIL: Expected 200, got $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 2: Health check (via Gateway)
Write-Host "[Test 2] Health check (via Gateway)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$GatewayUrl/api/auth/health" -Method Get -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ PASS: Gateway routing to Auth Service OK (Status: $($response.StatusCode))" -ForegroundColor Green
        Write-Host "  Headers:" -ForegroundColor Gray
        $response.Headers.GetEnumerator() | Where-Object { $_.Key -like "X-*" } | ForEach-Object {
            Write-Host "    $($_.Key): $($_.Value)" -ForegroundColor Gray
        }
    } else {
        Write-Host "❌ FAIL: Expected 200, got $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Register user (anonymous access)
Write-Host "[Test 3] Register user (anonymous access)..." -ForegroundColor Yellow
try {
    $body = $testUser | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$GatewayUrl/api/auth/register" `
        -Method Post `
        -ContentType "application/json" `
        -Body $body `
        -ErrorAction Stop
    
    Write-Host "✅ PASS: User registration successful" -ForegroundColor Green
    Write-Host "  User ID: $($response.userId)" -ForegroundColor Gray
    Write-Host "  Username: $($response.username)" -ForegroundColor Gray
    Write-Host "  Email: $($response.email)" -ForegroundColor Gray
} catch {
    if ($_.Exception.Response.StatusCode -eq 409) {
        Write-Host "⚠️  SKIP: User already exists (expected if running multiple times)" -ForegroundColor Yellow
    } else {
        Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
    }
}
Write-Host ""

# Test 4: Login (anonymous access)
Write-Host "[Test 4] Login (anonymous access)..." -ForegroundColor Yellow
try {
    $loginBody = @{
        username = $testUser.username
        password = $testUser.password
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$GatewayUrl/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $global:accessToken = $response.accessToken
    $global:refreshToken = $response.refreshToken
    
    Write-Host "✅ PASS: Login successful" -ForegroundColor Green
    Write-Host "  Access Token: $($response.accessToken.Substring(0, 30))..." -ForegroundColor Gray
    Write-Host "  Refresh Token: $($response.refreshToken)" -ForegroundColor Gray
    Write-Host "  Expires In: $($response.expiresIn) seconds" -ForegroundColor Gray
    Write-Host "  Token Type: $($response.tokenType)" -ForegroundColor Gray
} catch {
    Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 5: Get current user (requires JWT)
Write-Host "[Test 5] Get current user (requires JWT)..." -ForegroundColor Yellow
if ($global:accessToken) {
    try {
        $headers = @{
            "Authorization" = "Bearer $global:accessToken"
        }
        $response = Invoke-RestMethod -Uri "$GatewayUrl/api/auth/me" `
            -Method Get `
            -Headers $headers `
            -ErrorAction Stop
        
        Write-Host "✅ PASS: JWT authentication successful" -ForegroundColor Green
        Write-Host "  User ID: $($response.userId)" -ForegroundColor Gray
        Write-Host "  Username: $($response.username)" -ForegroundColor Gray
        Write-Host "  Email: $($response.email)" -ForegroundColor Gray
        Write-Host "  Roles: $($response.roles -join ', ')" -ForegroundColor Gray
    } catch {
        Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  SKIP: No access token from login test" -ForegroundColor Yellow
}
Write-Host ""

# Test 6: Unauthorized access (no token)
Write-Host "[Test 6] Unauthorized access (no token)..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$GatewayUrl/api/auth/me" -Method Get -ErrorAction Stop
    Write-Host "❌ FAIL: Expected 401, got $($response.StatusCode)" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ PASS: Correctly rejected unauthorized request (401 Unauthorized)" -ForegroundColor Green
    } else {
        Write-Host "❌ FAIL: Expected 401, got $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    }
}
Write-Host ""

# Test 7: Refresh token (anonymous access)
Write-Host "[Test 7] Refresh token (anonymous access)..." -ForegroundColor Yellow
if ($global:refreshToken) {
    try {
        $refreshBody = @{
            refreshToken = $global:refreshToken
        } | ConvertTo-Json
        
        $response = Invoke-RestMethod -Uri "$GatewayUrl/api/auth/refresh" `
            -Method Post `
            -ContentType "application/json" `
            -Body $refreshBody `
            -ErrorAction Stop
        
        Write-Host "✅ PASS: Token refresh successful" -ForegroundColor Green
        Write-Host "  New Access Token: $($response.accessToken.Substring(0, 30))..." -ForegroundColor Gray
        Write-Host "  New Refresh Token: $($response.refreshToken)" -ForegroundColor Gray
    } catch {
        Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "⚠️  SKIP: No refresh token from login test" -ForegroundColor Yellow
}
Write-Host ""

# Test 8: Rate limiting headers
Write-Host "[Test 8] Rate limiting headers..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$GatewayUrl/api/auth/health" -Method Get -ErrorAction Stop
    $rateLimitHeaders = $response.Headers.GetEnumerator() | Where-Object { $_.Key -like "X-RateLimit-*" }
    
    if ($rateLimitHeaders.Count -gt 0) {
        Write-Host "✅ PASS: Rate limit headers present" -ForegroundColor Green
        $rateLimitHeaders | ForEach-Object {
            Write-Host "  $($_.Key): $($_.Value)" -ForegroundColor Gray
        }
    } else {
        Write-Host "⚠️  WARN: No rate limit headers found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 9: CORS headers
Write-Host "[Test 9] CORS headers..." -ForegroundColor Yellow
try {
    $headers = @{
        "Origin" = "http://localhost:4200"
    }
    $response = Invoke-WebRequest -Uri "$GatewayUrl/api/auth/health" -Method Get -Headers $headers -ErrorAction Stop
    
    $corsHeader = $response.Headers["Access-Control-Allow-Origin"]
    if ($corsHeader) {
        Write-Host "✅ PASS: CORS headers configured" -ForegroundColor Green
        Write-Host "  Access-Control-Allow-Origin: $corsHeader" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  WARN: No CORS headers found" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# Test 10: OpenTelemetry correlation ID
Write-Host "[Test 10] Correlation ID propagation..." -ForegroundColor Yellow
try {
    $correlationId = [Guid]::NewGuid().ToString()
    $headers = @{
        "X-Correlation-Id" = $correlationId
    }
    $response = Invoke-WebRequest -Uri "$GatewayUrl/api/auth/health" -Method Get -Headers $headers -ErrorAction Stop
    
    $responseCorrelationId = $response.Headers["X-Correlation-Id"]
    if ($responseCorrelationId -eq $correlationId) {
        Write-Host "✅ PASS: Correlation ID propagated correctly" -ForegroundColor Green
        Write-Host "  Request ID: $correlationId" -ForegroundColor Gray
        Write-Host "  Response ID: $responseCorrelationId" -ForegroundColor Gray
    } else {
        Write-Host "⚠️  WARN: Correlation ID mismatch or missing" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Tests Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "1. Verify all tests pass after Auth Service implementation" -ForegroundColor White
Write-Host "2. Check Jaeger for traces: http://localhost:16686" -ForegroundColor White
Write-Host "3. Check Grafana for metrics: http://localhost:3000" -ForegroundColor White
Write-Host "4. Check Seq for logs: http://localhost:5341" -ForegroundColor White
