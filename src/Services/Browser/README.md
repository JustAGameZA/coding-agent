# Browser Service

Playwright-based browser automation service with pool management and concurrent request limiting.

## Features

- **Browser Pool**: Manages up to 5 concurrent browser pages using semaphore-based concurrency control
- **Multi-Browser Support**: Chromium and Firefox browsers
- **Headless Mode**: Configurable headless/headed operation
- **Request Validation**: FluentValidation for input validation
- **OpenTelemetry**: Full observability with tracing and metrics
- **Timeout Control**: Configurable timeout per request (default: 30 seconds, max: 120 seconds)

## API Endpoints

### POST /browse

Navigate to a URL and retrieve page content.

**Request Body:**
```json
{
  "url": "https://example.com",
  "browserType": "chromium",
  "timeoutMs": 30000,
  "waitForNetworkIdle": true
}
```

**Response:**
```json
{
  "content": "<html>...</html>",
  "url": "https://example.com/",
  "title": "Example Domain",
  "statusCode": 200,
  "loadTimeMs": 1234,
  "browserType": "chromium"
}
```

**Supported Browser Types:**
- `chromium` (default)
- `firefox`

## Configuration

Configuration is in `appsettings.json`:

```json
{
  "Browser": {
    "Headless": true,
    "Timeout": 30000,
    "UserAgent": "CodingAgent/2.0",
    "BlockImages": false,
    "BlockCSS": false,
    "MaxConcurrentPages": 5
  }
}
```

## Local Development

### Installing Playwright Browsers

Before running the service or integration tests, install Playwright browsers:

```bash
# From the project directory
pwsh bin/Debug/playwright.ps1 install chromium firefox

# Or using the Playwright CLI
playwright install chromium firefox --with-deps
```

### Running the Service

```bash
dotnet run --project src/Services/Browser/CodingAgent.Services.Browser
```

The service will be available at `http://localhost:5004`.

### Running Tests

```bash
# Unit tests only (no browser required)
dotnet test --filter "Category=Unit"

# Integration tests (requires browsers installed)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## Docker

The Dockerfile includes all necessary dependencies for Playwright:

```dockerfile
# Install Playwright dependencies for Chromium
RUN apt-get update && apt-get install -y \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxkbcommon0 \
    libxrandr2 \
    && rm -rf /var/lib/apt/lists/*
```

Browsers are installed at runtime using the Playwright CLI.

## Architecture

### Browser Pool

The `BrowserPool` class manages browser instances and enforces concurrency limits:

- Maximum 5 concurrent pages (configurable)
- Lazy browser initialization
- Automatic cleanup on disposal
- Thread-safe semaphore-based access control

### Service Flow

1. Client sends POST /browse request
2. Request is validated using FluentValidation
3. BrowserPool acquires a page (blocks if limit reached)
4. PlaywrightBrowserService navigates to URL
5. Page content and metadata extracted
6. Page released back to pool
7. Result returned to client

## Testing Strategy

### Unit Tests (27 tests, 100% passing)

- **BrowseRequestValidator**: 22 tests for validation rules
- **PlaywrightBrowserService**: 5 tests with mocked dependencies
- No actual browsers required
- Fast execution (< 1 second)

### Integration Tests (14 tests)

- **BrowserPoolTests**: 8 tests for pool behavior with real browsers
- **BrowserEndpointsTests**: 6 tests for end-to-end scenarios
- Requires Playwright browsers installed
- Tests concurrent requests, timeouts, and both browser types

## Performance Considerations

- **Concurrency Limit**: 5 concurrent pages prevents resource exhaustion
- **Headless Mode**: Faster than headed mode, recommended for production
- **Network Idle**: Optional, can be disabled for faster page loads
- **Timeout**: Default 30s, configurable up to 120s

## Observability

The service exports:

- **Traces**: Via OpenTelemetry to Jaeger
- **Metrics**: Via Prometheus endpoint at `/metrics`
- **Logs**: Structured logging with request/response details

Key metrics:
- Browse request duration
- Browser pool utilization
- HTTP status codes
- Error rates

## Troubleshooting

### Browsers Not Installing

If `playwright install` fails, try:

```bash
# Install system dependencies first
playwright install-deps

# Then install browsers
playwright install chromium firefox
```

### Integration Tests Failing

Ensure browsers are installed before running integration tests:

```bash
# Check if browsers are installed
ls ~/.cache/ms-playwright/

# Reinstall if needed
playwright install chromium firefox
```

### Docker Build Issues

If browsers fail to install in Docker, check:
- Network connectivity to https://playwright.azureedge.net
- System dependencies are installed
- Sufficient disk space
