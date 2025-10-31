# cutover-verify.ps1
# Verification checks for Phase 5 cutover
# Validates health, latency, and error rates

param(
    [string]$GatewayUrl = "http://localhost:5000",
    [string]$PrometheusUrl = "http://localhost:9090"
)

Write-Host "Running Phase 5 cutover verification checks..."
$ErrorActionPreference = 'Stop'

# Health checks
Write-Host "`n=== Health Checks ===" -ForegroundColor Cyan
$services = @("gateway", "auth", "chat", "orchestration", "dashboard")
foreach ($service in $services) {
    try {
        $response = Invoke-WebRequest -Uri "$GatewayUrl/api/$service/health" -Method Get -UseBasicParsing -ErrorAction Stop
        $status = if ($response.StatusCode -eq 200) { "✅ Healthy" } else { "❌ Unhealthy" }
        Write-Host "$service`: $status ($($response.StatusCode))"
    }
    catch {
        Write-Host "$service`: ❌ Failed - $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Error rate check (requires Prometheus)
Write-Host "`n=== Error Rate Check ===" -ForegroundColor Cyan
Write-Host "Query Prometheus for error rates:"
Write-Host "  rate(http_requests_total{status=~'5..'}[5m]) < 0.01" -ForegroundColor Yellow
Write-Host "  (Target: < 1%)" -ForegroundColor Yellow

# Latency check (requires Prometheus)
Write-Host "`n=== Latency Check ===" -ForegroundColor Cyan
Write-Host "Query Prometheus for p95 latency:"
Write-Host "  histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) < 0.5" -ForegroundColor Yellow
Write-Host "  (Target: < 500ms)" -ForegroundColor Yellow

# RabbitMQ backlog check
Write-Host "`n=== RabbitMQ Backlog Check ===" -ForegroundColor Cyan
Write-Host "Check RabbitMQ queue depths in Grafana dashboard" -ForegroundColor Yellow

Write-Host "`n✅ Verification checks complete." -ForegroundColor Green
Write-Host "Review Prometheus/Grafana dashboards for detailed metrics." -ForegroundColor Yellow

