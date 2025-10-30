# Gateway Rate Limits and Timeouts

The API Gateway enforces per-IP and per-user rate limits and propagates correlation IDs and rate limit headers.

## Headers Exposed
- X-Correlation-Id: Request correlation ID
- X-RateLimit-Limit: Active limit (IP or User)
- X-RateLimit-Remaining: Remaining for current window
- X-RateLimit-Limit-IP / X-RateLimit-Remaining-IP
- X-RateLimit-Limit-User / X-RateLimit-Remaining-User (when authenticated)
- Retry-After: Seconds until the next window when throttled (429)

## Client Guidance
- Backoff using Retry-After for 429 responses
- Reuse X-Correlation-Id in support tickets
- Prefer batching and caching to reduce call volume

## Timeouts and Resilience
Gateway outbound calls use Polly policies:
- Retries: 3 with exponential backoff + jitter
- Circuit breaker: opens after 5 consecutive failures for 30s

Configuration is centralized in Program.cs (Gateway) and adjustable via environment variables.
