# Network Access Setup Script
# Automates configuration for accessing development environment from another PC

param(
    [switch]$ConfigureFirewall,
    [switch]$UpdateConfig,
    [string]$CustomIP
)

Write-Host "`n╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Coding Agent - Network Access Setup         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

# Get host machine's IP address
if ($CustomIP) {
    $hostIP = $CustomIP
    Write-Host "📌 Using custom IP: $hostIP" -ForegroundColor Yellow
} else {
    $hostIP = (Get-NetIPAddress -AddressFamily IPv4 | 
               Where-Object { $_.InterfaceAlias -notlike "*Loopback*" -and $_.IPAddress -notlike "169.254.*" } | 
               Select-Object -First 1).IPAddress
    
    if (-not $hostIP) {
        Write-Host "❌ Could not determine host IP address" -ForegroundColor Red
        Write-Host "   Run with -CustomIP parameter: .\setup-network-access.ps1 -CustomIP 192.168.1.100" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "📍 Detected host IP: $hostIP" -ForegroundColor Green
}

# Confirm with user
Write-Host "`n⚠️  This will configure network access for:" -ForegroundColor Yellow
Write-Host "   • Angular Dashboard: http://${hostIP}:4200" -ForegroundColor White
Write-Host "   • Gateway API: http://${hostIP}:5000" -ForegroundColor White
Write-Host "   • Dashboard BFF: http://${hostIP}:5007" -ForegroundColor White

$confirm = Read-Host "`nContinue? (y/n)"
if ($confirm -ne 'y') {
    Write-Host "Aborted." -ForegroundColor Yellow
    exit 0
}

# Update Angular environment file
if ($UpdateConfig) {
    Write-Host "`n📝 Updating Angular environment configuration..." -ForegroundColor Cyan
    
    $envFile = "src/Frontend/coding-agent-dashboard/src/environments/environment.network.ts"
    
    if (Test-Path $envFile) {
        $content = Get-Content $envFile -Raw
        $content = $content -replace 'YOUR_HOST_IP', $hostIP
        Set-Content $envFile $content -NoNewline
        Write-Host "   ✅ Updated $envFile" -ForegroundColor Green
    } else {
        Write-Host "   ❌ File not found: $envFile" -ForegroundColor Red
    }
    
    # Update Gateway appsettings
    Write-Host "`n📝 Updating Gateway CORS configuration..." -ForegroundColor Cyan
    $gatewaySettings = "src/Gateway/CodingAgent.Gateway/appsettings.json"
    
    if (Test-Path $gatewaySettings) {
        $json = Get-Content $gatewaySettings | ConvertFrom-Json
        $newOrigin = "http://${hostIP}:4200"
        
        if ($json.Frontend.Origins -notcontains $newOrigin) {
            $json.Frontend.Origins += $newOrigin
            $json | ConvertTo-Json -Depth 10 | Set-Content $gatewaySettings
            Write-Host "   ✅ Added $newOrigin to Gateway CORS" -ForegroundColor Green
        } else {
            Write-Host "   ℹ️  Origin already configured" -ForegroundColor Yellow
        }
    }
    
    # Update Dashboard BFF appsettings
    Write-Host "`n📝 Updating Dashboard BFF CORS configuration..." -ForegroundColor Cyan
    $dashboardSettings = "src/Services/Dashboard/CodingAgent.Services.Dashboard/appsettings.json"
    
    if (Test-Path $dashboardSettings) {
        $json = Get-Content $dashboardSettings | ConvertFrom-Json
        $newOrigin = "http://${hostIP}:4200"
        
        if ($json.Frontend.Origins -notcontains $newOrigin) {
            $json.Frontend.Origins += $newOrigin
            $json | ConvertTo-Json -Depth 10 | Set-Content $dashboardSettings
            Write-Host "   ✅ Added $newOrigin to Dashboard BFF CORS" -ForegroundColor Green
        } else {
            Write-Host "   ℹ️  Origin already configured" -ForegroundColor Yellow
        }
    }
}

# Configure Windows Firewall
if ($ConfigureFirewall) {
    Write-Host "`n🔥 Configuring Windows Firewall..." -ForegroundColor Cyan
    
    $ports = @(
        @{ Port = 4200; Name = "Coding Agent - Angular Dev Server" },
        @{ Port = 5000; Name = "Coding Agent - Gateway" },
        @{ Port = 5007; Name = "Coding Agent - Dashboard BFF" }
    )
    
    foreach ($portConfig in $ports) {
        $existingRule = Get-NetFirewallRule -DisplayName $portConfig.Name -ErrorAction SilentlyContinue
        
        if ($existingRule) {
            Write-Host "   ℹ️  Firewall rule already exists: $($portConfig.Name)" -ForegroundColor Yellow
        } else {
            try {
                New-NetFirewallRule -DisplayName $portConfig.Name `
                                    -Direction Inbound `
                                    -LocalPort $portConfig.Port `
                                    -Protocol TCP `
                                    -Action Allow `
                                    -Profile Any | Out-Null
                Write-Host "   ✅ Created firewall rule: $($portConfig.Name) (Port $($portConfig.Port))" -ForegroundColor Green
            } catch {
                Write-Host "   ❌ Failed to create firewall rule: $($_.Exception.Message)" -ForegroundColor Red
                Write-Host "      Try running as Administrator" -ForegroundColor Yellow
            }
        }
    }
}

# Test connectivity
Write-Host "`n🔍 Testing service availability..." -ForegroundColor Cyan

function Test-ServicePort {
    param($Port, $Service)
    $result = Test-NetConnection -ComputerName $hostIP -Port $Port -WarningAction SilentlyContinue
    if ($result.TcpTestSucceeded) {
        Write-Host "   ✅ $Service (port $Port) is accessible" -ForegroundColor Green
        return $true
    } else {
        Write-Host "   ❌ $Service (port $Port) is not accessible" -ForegroundColor Red
        return $false
    }
}

$angularOk = Test-ServicePort 4200 "Angular"
$gatewayOk = Test-ServicePort 5000 "Gateway"
$dashboardOk = Test-ServicePort 5007 "Dashboard BFF"

# Summary
Write-Host "`n╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  Setup Complete!                              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════╝`n" -ForegroundColor Cyan

Write-Host "📋 Next Steps:`n" -ForegroundColor Yellow

if (-not $UpdateConfig) {
    Write-Host "1. Update configuration files:" -ForegroundColor White
    Write-Host "   Run: .\setup-network-access.ps1 -UpdateConfig`n" -ForegroundColor Gray
}

if (-not $ConfigureFirewall) {
    Write-Host "2. Configure Windows Firewall (requires Admin):" -ForegroundColor White
    Write-Host "   Run: .\setup-network-access.ps1 -ConfigureFirewall`n" -ForegroundColor Gray
}

if (-not $angularOk -or -not $gatewayOk -or -not $dashboardOk) {
    Write-Host "3. Start services:" -ForegroundColor White
    Write-Host "   cd src/Frontend/coding-agent-dashboard" -ForegroundColor Gray
    Write-Host "   npm run start:network`n" -ForegroundColor Gray
}

Write-Host "4. Access from another PC:" -ForegroundColor White
Write-Host "   Open browser to: http://${hostIP}:4200`n" -ForegroundColor Gray

Write-Host "📚 Full documentation: docs/NETWORK-ACCESS-SETUP.md" -ForegroundColor Cyan

Write-Host "`n⚠️  Note: This is for development only. Not for production use!`n" -ForegroundColor Yellow
