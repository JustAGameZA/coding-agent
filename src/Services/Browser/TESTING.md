# Browser Service Testing Guide

This guide provides examples for testing the Browser Service locally and in different environments.

## Prerequisites

1. Install Playwright browsers:
   ```bash
   cd src/Services/Browser
   ./install-browsers.sh
   ```

2. Or manually:
   ```bash
   pwsh CodingAgent.Services.Browser/bin/Debug/playwright.ps1 install chromium firefox
   ```

## Running the Service

```bash
cd src/Services/Browser/CodingAgent.Services.Browser
dotnet run
```

The service will start on `http://localhost:5004`.

## API Testing

### Health Check

```bash
curl http://localhost:5004/health
# Response: Healthy

curl http://localhost:5004/ping
# Response: {"service":"BrowserService","status":"healthy","version":"2.0.0","timestamp":"..."}
```

### Browse Endpoint

#### Basic Request (Chromium)

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "browserType": "chromium"
  }'
```

#### With Custom Timeout

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "browserType": "chromium",
    "timeoutMs": 60000
  }'
```

#### Using Firefox

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "browserType": "firefox"
  }'
```

#### Skip Network Idle Wait

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://example.com",
    "browserType": "chromium",
    "waitForNetworkIdle": false
  }'
```

### Expected Response

```json
{
  "content": "<!doctype html>\n<html>\n<head>...</head>\n<body>...</body>\n</html>",
  "url": "https://example.com/",
  "title": "Example Domain",
  "statusCode": 200,
  "loadTimeMs": 1234,
  "browserType": "chromium"
}
```

### Error Responses

#### Invalid URL

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{
    "url": "not-a-valid-url",
    "browserType": "chromium"
  }'
```

Response (400 Bad Request):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Url": ["URL must be a valid HTTP or HTTPS URL"]
  }
}
```

#### Timeout

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://httpbin.org/delay/120",
    "browserType": "chromium",
    "timeoutMs": 1000
  }'
```

Response (408 Request Timeout):
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.9",
  "title": "Request Timeout",
  "detail": "The request to https://httpbin.org/delay/120 timed out",
  "status": 408
}
```

## Testing Concurrent Requests

Test browser pool concurrency limiting:

```bash
# Send 10 requests concurrently (max 5 will run at once)
for i in {1..10}; do
  curl -X POST http://localhost:5004/browse \
    -H "Content-Type: application/json" \
    -d '{"url":"https://example.com","browserType":"chromium"}' &
done
wait
```

## Running Tests

### Unit Tests Only (Fast)

```bash
dotnet test --filter "Category=Unit"
```

### Integration Tests (Requires Browsers)

```bash
dotnet test --filter "Category=Integration"
```

### All Tests

```bash
dotnet test
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Docker Testing

### Build Image

```bash
# From repository root
docker build -f src/Services/Browser/CodingAgent.Services.Browser/Dockerfile -t browser-service .
```

### Run Container

```bash
docker run -p 5004:5004 browser-service
```

### Test in Container

```bash
curl -X POST http://localhost:5004/browse \
  -H "Content-Type: application/json" \
  -d '{"url":"https://example.com","browserType":"chromium"}'
```

## Observability

### Metrics

View Prometheus metrics:
```bash
curl http://localhost:5004/metrics
```

### Traces

Traces are exported to Jaeger at `http://jaeger:4317` (configurable via `OpenTelemetry:Endpoint`).

## Performance Testing

### Simple Load Test

```bash
# Install Apache Bench
sudo apt-get install apache2-utils

# Run 100 requests with concurrency of 10
ab -n 100 -c 10 -p browse_request.json -T application/json http://localhost:5004/browse
```

Where `browse_request.json` contains:
```json
{"url":"https://example.com","browserType":"chromium","waitForNetworkIdle":false}
```

### Expected Performance

- **Concurrency**: Max 5 concurrent pages
- **Response Time**: 500-2000ms (depending on page complexity)
- **Throughput**: ~2-3 requests/second (limited by browser pool)

## Troubleshooting

### Browsers Not Found

If you get "Executable doesn't exist" errors:

```bash
# Check browser installation
ls ~/.cache/ms-playwright/

# Reinstall browsers
cd src/Services/Browser
./install-browsers.sh
```

### Port Already in Use

```bash
# Find process using port 5004
sudo lsof -i :5004

# Kill process
kill -9 <PID>

# Or use a different port
dotnet run --urls http://localhost:5005
```

### High Memory Usage

The browser pool limits concurrent pages to prevent resource exhaustion. If you need more concurrency, adjust `MaxConcurrentPages` in `appsettings.json`:

```json
{
  "Browser": {
    "MaxConcurrentPages": 10
  }
}
```

**Note**: Each browser page uses ~50-100MB of memory. Monitor system resources when increasing concurrency.
