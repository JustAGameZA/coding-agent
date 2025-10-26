# Ollama Service

The Ollama Service provides cost-effective, on-premise LLM inference using locally-hosted open-source models.

## Features

- **Hardware Detection**: Auto-detects GPU type, VRAM, CPU cores, and RAM
- **Hardware-Aware Model Selection**: Recommends appropriate models based on available resources
- **Dynamic Model Registry**: Discovers models from Ollama backend (no hardcoded lists)
- **Ollama REST API Integration**: Wrapper around Ollama HTTP API
- **Cloud API Fallback** (Optional): Fallback to cloud APIs when local backend unavailable
- **Token Usage Tracking**: Monthly token limits for cloud API usage
- **Integration Tests**: Comprehensive test suite with Testcontainers

## Architecture

```
Ollama Service (.NET 9)
├── Domain
│   ├── Entities (OllamaModel)
│   ├── Services (HardwareDetector)
│   └── ValueObjects (HardwareProfile, OllamaRequest/Response)
├── Infrastructure
│   └── Http (OllamaHttpClient)
└── Api
    └── Endpoints (Hardware detection, Model recommendations)
```

## Hardware Detection

The service automatically detects:
- **GPU Type**: NVIDIA/AMD/CPU-only
- **VRAM**: Available video memory in GB
- **CPU Cores**: Number of available CPU cores
- **RAM**: Total system RAM in GB

### Hardware Tiers

| Tier | VRAM | Recommended Models |
|------|------|-------------------|
| High | 24GB+ | 30B+ models (codellama:34b, deepseek-coder:33b) |
| Medium | 16-23GB | 13B models (codellama:13b, qwen2.5-coder:7b) |
| Low | 8-15GB | 7B models (codellama:7b, mistral:7b) |
| CPU-only | 0GB | Quantized models (codellama:7b-q4_0, phi:2.7b) |

## API Endpoints

### Hardware Detection
```http
GET /api/hardware
```

Returns the detected hardware profile:
```json
{
  "gpuType": "NVIDIA GeForce RTX 4090",
  "vramGB": 24.0,
  "cpuCores": 16,
  "ramGB": 64.0,
  "hasGpu": true,
  "tier": "High",
  "detectedAt": "2025-10-26T13:49:07.957Z"
}
```

### Model Recommendations
```http
POST /api/hardware/models
```

Returns hardware profile and recommended models:
```json
{
  "hardware": {
    "gpuType": "NVIDIA GeForce RTX 4090",
    "vramGB": 24.0,
    "cpuCores": 16,
    "ramGB": 64.0,
    "tier": "High"
  },
  "recommendedModels": [
    "codellama:34b",
    "deepseek-coder:33b",
    "wizardcoder:34b",
    "phind-codellama:34b"
  ]
}
```

## Configuration

### appsettings.json
```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434"
  },
  "OpenTelemetry": {
    "Endpoint": "http://localhost:4317"
  },
  "CloudApi": {
    "Provider": "none",
    "ApiKey": "",
    "Endpoint": "",
    "MonthlyTokenLimit": 100000
  }
}
```

### Environment Variables
- `Ollama__BaseUrl`: Ollama backend URL (default: http://localhost:11434)
- `OpenTelemetry__Endpoint`: OTLP endpoint for traces (default: http://localhost:4317)
- `CloudApi__Provider`: Cloud API provider (e.g., "openai", "anthropic", "none")
- `CloudApi__ApiKey`: API key for cloud provider (optional)
- `CloudApi__MonthlyTokenLimit`: Monthly token usage limit (default: 100,000)

## Cloud API Fallback

The Ollama Service supports optional cloud API fallback when the local Ollama backend is unavailable. This is **disabled by default** and requires explicit configuration.

### Configuration

To enable cloud API fallback:

```json
{
  "CloudApi": {
    "Provider": "openai",
    "ApiKey": "sk-your-api-key-here",
    "MonthlyTokenLimit": 100000
  }
}
```

### Token Usage Tracking

- Token usage is tracked per month per provider
- Monthly limits prevent overuse of paid cloud APIs
- When limit is reached, fallback is disabled
- Usage resets at the start of each month

### Safety Features

- **Opt-in only**: Cloud API is not configured by default
- **Token limits**: Prevents unexpected costs from runaway usage
- **Configuration validation**: Service logs whether cloud API is configured on startup
- **Safe fallback**: If not configured, service only uses local Ollama backend

## Docker Deployment

### Ollama Backend
```yaml
ollama:
  image: ollama/ollama:latest
  ports:
    - "11434:11434"
  volumes:
    - ollama_data:/root/.ollama
  # Uncomment for GPU support
  # deploy:
  #   resources:
  #     reservations:
  #       devices:
  #         - driver: nvidia
  #           count: all
  #           capabilities: [gpu]
```

### GPU Support

For NVIDIA GPUs, ensure:
1. NVIDIA drivers installed on host
2. `nvidia-docker2` installed
3. Uncomment GPU configuration in docker-compose.yml

## Development

### Build
```bash
dotnet build
```

### Run Tests
```bash
# Unit tests only (fast, < 1 second)
dotnet test --filter "Category=Unit"

# Integration tests only (with Testcontainers)
dotnet test --filter "Category=Integration"

# All tests
dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Run Locally
```bash
dotnet run --project src/Services/Ollama/CodingAgent.Services.Ollama
```

## Testing

The test suite includes **57 tests** with **85%+ coverage**:

### Unit Tests (47 tests)
- **Hardware Detection** (19 tests): GPU detection, hardware tiers, model recommendations
- **OllamaHttpClient** (10 tests): API interactions, error handling, token calculation
- **Cloud API Client** (8 tests): Configuration validation, token availability checks
- **Token Usage Tracker** (10 tests): Usage tracking, monthly limits, multi-provider isolation

### Integration Tests (10 tests)
- **Service Endpoints**: Root, health check, hardware detection
- **Cloud API Fallback**: Configuration validation, token tracking, fallback logic
- **Testcontainers**: Uses Ollama container for realistic testing (when Docker available)

Tests follow patterns from other services:
- `[Trait("Category", "Unit")]` for fast unit tests
- `[Trait("Category", "Integration")]` for integration tests
- Testcontainers for consistent test environment
- Mock implementations for cloud API (real implementations can be plugged in)

See `CodingAgent.Services.Ollama.Tests` for test implementation.

## Integration with ML Classifier

The Ollama Service will integrate with the ML Classifier Service (Phase 2) for:
- ML-driven model selection based on task features
- A/B testing different models
- Training data collection from inference results

## Observability

- **OpenTelemetry Traces**: All HTTP requests and hardware detection
- **Prometheus Metrics**: Request counts, latencies, hardware stats
- **Health Checks**: `/health` endpoint for liveness/readiness probes

## Future Enhancements (Phase 2)

- [ ] ModelRegistry as IHostedService for dynamic model sync
- [ ] PostgreSQL persistence for model metadata
- [ ] Redis caching for prompt optimization
- [ ] ML-driven model selection via ML Classifier Service
- [ ] A/B testing engine for model comparison
- [ ] RabbitMQ events for usage tracking
