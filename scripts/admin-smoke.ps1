param(
    [string]$Email = "admin@example.com",
    [string]$Password = "Admin@1234",
    [string]$GatewayBaseUrl = "http://localhost:5000"
)

Write-Host "Seeding admin user..."
./seed-admin-user.ps1 -Email $Email -Password $Password | Out-Null

$loginBody = @{ username = 'admin'; password = $Password } | ConvertTo-Json
$login = Invoke-RestMethod -Method Post -Uri "$GatewayBaseUrl/api/auth/login" -Body $loginBody -ContentType 'application/json'
$token = $login.accessToken

if (-not $token) { throw "Failed to obtain JWT token" }

$headers = @{ Authorization = "Bearer $token" }

Write-Host "Checking admin-only endpoint via Gateway..."
Invoke-RestMethod -Method Get -Uri "$GatewayBaseUrl/api/auth/admin/users?page=1&pageSize=5" -Headers $headers | Out-Null

Write-Host "Admin smoke check passed."
