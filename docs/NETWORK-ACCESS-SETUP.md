# Network Access Setup Guide

This guide explains how to access your development environment from another PC on your local network.

## Quick Setup

### 1. Find Your Host Machine's IP Address

```powershell
# On your development machine (Windows)
Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" }

# Look for your network adapter (usually starts with 192.168.x.x or 10.x.x.x)
# Example: 192.168.1.100
```

### 2. Update Environment Configuration

Edit `src/Frontend/coding-agent-dashboard/src/environments/environment.network.ts`:

```typescript
export const environment = {
  production: false,
  // Replace YOUR_HOST_IP with your actual IP (e.g., 192.168.1.100)
  apiUrl: 'http://192.168.1.100:5000/api',
  signalRUrl: 'http://192.168.1.100:5000/hubs/chat',
  apiBaseUrl: 'http://192.168.1.100:5000/api',
  chatHubUrl: 'http://192.168.1.100:5000/hubs/chat',
  fileBaseUrl: 'http://192.168.1.100:5000/files',
  maxUploadSize: 50 * 1024 * 1024,
  dashboardServiceUrl: 'http://192.168.1.100:5007',
  version: '2.0.0-network'
};
```

### 3. Update CORS Configuration

Edit `src/Gateway/CodingAgent.Gateway/appsettings.json`:

```json
{
  "Frontend": {
    "Origins": [
      "http://localhost:4200",
      "http://192.168.1.100:4200"  // Add your host IP
    ]
  }
}
```

Edit `src/Services/Dashboard/CodingAgent.Services.Dashboard/appsettings.json`:

```json
{
  "Frontend": {
    "Origins": [
      "http://localhost:4200",
      "http://192.168.1.100:4200"  // Add your host IP
    ]
  }
}
```

### 4. Configure Windows Firewall

Allow inbound connections on required ports:

```powershell
# Allow Angular dev server
New-NetFirewallRule -DisplayName "Coding Agent - Angular" -Direction Inbound -LocalPort 4200 -Protocol TCP -Action Allow

# Allow Gateway
New-NetFirewallRule -DisplayName "Coding Agent - Gateway" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow

# Allow Dashboard BFF
New-NetFirewallRule -DisplayName "Coding Agent - Dashboard BFF" -Direction Inbound -LocalPort 5007 -Protocol TCP -Action Allow
```

Or manually in Windows Firewall:
1. Open **Windows Defender Firewall with Advanced Security**
2. Click **Inbound Rules** → **New Rule**
3. Choose **Port** → **TCP** → Enter ports: `4200, 5000, 5007`
4. Allow the connection
5. Apply to all profiles (Domain, Private, Public)

### 5. Start Services with Network Configuration

#### Option A: Using Docker Compose (Recommended)

The Docker services are already configured to listen on `0.0.0.0`, so they're accessible from network:

```powershell
# Start all services
docker compose -f deployment/docker-compose/docker-compose.yml -f deployment/docker-compose/docker-compose.apps.dev.yml up -d

# The Angular container already listens on 0.0.0.0:4200
```

#### Option B: Local Development (Without Docker)

```powershell
# Terminal 1: Start Gateway
cd src/Gateway/CodingAgent.Gateway
dotnet run --urls "http://0.0.0.0:5000"

# Terminal 2: Start Dashboard BFF
cd src/Services/Dashboard/CodingAgent.Services.Dashboard
dotnet run --urls "http://0.0.0.0:5007"

# Terminal 3: Start Angular with network configuration
cd src/Frontend/coding-agent-dashboard
npm run start:network
```

### 6. Access from Another PC

From any device on the same network, open a browser and navigate to:

```
http://192.168.1.100:4200
```

(Replace `192.168.1.100` with your host machine's actual IP)

## Troubleshooting

### Cannot Access from Other PC

1. **Verify IP Address**
   ```powershell
   Test-NetConnection -ComputerName 192.168.1.100 -Port 4200
   ```

2. **Check Firewall Rules**
   ```powershell
   Get-NetFirewallRule -DisplayName "Coding Agent*" | Select-Object DisplayName, Enabled, Direction
   ```

3. **Verify Services Are Listening on 0.0.0.0**
   ```powershell
   netstat -ano | findstr ":4200"
   netstat -ano | findstr ":5000"
   netstat -ano | findstr ":5007"
   ```
   Should show `0.0.0.0:PORT` not `127.0.0.1:PORT`

4. **Check Docker Container Ports**
   ```powershell
   docker ps --format "{{.Names}} - {{.Ports}}" | Select-String -Pattern "(gateway|dashboard|ui)"
   ```

### CORS Errors

If you see CORS errors in the browser console:

1. Verify CORS origins include your IP:
   ```json
   "Frontend": {
     "Origins": ["http://localhost:4200", "http://YOUR_IP:4200"]
   }
   ```

2. Restart Gateway and Dashboard BFF services after changing CORS config

3. Check browser console for the exact origin being blocked

### Connection Refused

- Ensure Windows Firewall rules are active
- Verify antivirus isn't blocking ports
- Check if services are running: `docker ps` or `Get-Process | Where-Object {$_.ProcessName -eq "dotnet"}`

## Architecture

```
Remote PC Browser
   ↓ http://192.168.1.100:4200
Host PC - Angular Dev Server (0.0.0.0:4200)
   ↓ serves Angular SPA
Remote PC Browser executes Angular app
   ↓ http://192.168.1.100:5000 (API calls)
Host PC - Gateway (0.0.0.0:5000)
   ↓ http://192.168.1.100:5007 (BFF calls)
Host PC - Dashboard BFF (0.0.0.0:5007)
```

## Security Considerations

⚠️ **Development Only**: This configuration is for local network development only.

For production:
- Use HTTPS with valid certificates
- Implement proper authentication
- Use environment-specific API endpoints
- Configure restrictive CORS origins
- Use reverse proxy (nginx/IIS) instead of exposing dev servers

## Quick Commands Reference

```powershell
# Find your IP
ipconfig | Select-String "IPv4"

# Start with network access (Docker)
docker compose -f deployment/docker-compose/docker-compose.yml -f deployment/docker-compose/docker-compose.apps.dev.yml up -d

# Start Angular with network config
cd src/Frontend/coding-agent-dashboard
npm run start:network

# Test connectivity from remote PC
# Open browser to: http://YOUR_HOST_IP:4200

# Check service health
curl http://YOUR_HOST_IP:5000/health
curl http://YOUR_HOST_IP:5007/health

# View logs
docker logs coding-agent-gateway-dev --tail 50
docker logs coding-agent-dashboard-bff-dev --tail 50
docker logs coding-agent-dashboard-ui-dev --tail 50
```

## Dynamic IP Handling

If your host machine's IP changes frequently (DHCP):

1. **Reserve IP in Router**: Configure your router to always assign the same IP to your machine based on MAC address

2. **Use Hostname**: Replace IP with hostname in environment files:
   ```typescript
   apiUrl: 'http://YOUR-COMPUTER-NAME:5000/api'
   ```

3. **Use Script**: Create a script to update environment files automatically:
   ```powershell
   $ip = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike "*Loopback*" } | Select-Object -First 1).IPAddress
   
   # Update environment.network.ts with current IP
   (Get-Content src/Frontend/coding-agent-dashboard/src/environments/environment.network.ts) `
     -replace 'YOUR_HOST_IP', $ip | `
     Set-Content src/Frontend/coding-agent-dashboard/src/environments/environment.network.ts
   ```

---

**Last Updated**: October 27, 2025
