# Strategy Selector Implementation

## Overview

The Strategy Selector intelligently chooses the optimal execution strategy for coding tasks based on their complexity. It integrates with the ML Classifier service to make data-driven decisions with fallback to heuristic analysis.

## Architecture

### Components

1. **IStrategySelector** - Domain service interface
2. **StrategySelector** - Implementation with ML integration
3. **IMLClassifierClient** - Client interface for ML service
4. **MLClassifierClient** - HTTP client with resilience policies

### Strategy Selection Flow

```
Task → StrategySelector → ML Classifier Service → Complexity → Strategy
                      ↓ (if unavailable)
                   Heuristic Analysis → Complexity → Strategy
```

## Usage Examples

### Basic Usage

```csharp
// Inject IStrategySelector via DI
public class TaskOrchestrator
{
    private readonly IStrategySelector _strategySelector;

    public TaskOrchestrator(IStrategySelector strategySelector)
    {
        _strategySelector = strategySelector;
    }

    public async Task ExecuteTaskAsync(CodingTask task)
    {
        // Automatically select strategy based on task complexity
        var strategy = await _strategySelector.SelectStrategyAsync(task);
        
        // Execute with selected strategy
        var context = new TaskExecutionContext { /* ... */ };
        var result = await strategy.ExecuteAsync(task, context);
    }
}
```

### Manual Override

```csharp
// Force a specific strategy regardless of complexity
var strategy = await _strategySelector.SelectStrategyAsync(
    task, 
    strategyName: "Iterative");
```

## Configuration

### appsettings.json

```json
{
  "MLClassifier": {
    "BaseUrl": "http://localhost:8000"
  }
}
```

### Environment Variables (for Docker/Kubernetes)

```bash
ML_CLASSIFIER_URL=http://ml-classifier-service:8000
```

## Complexity to Strategy Mapping

| Complexity | Strategy      | Use Case                          | LLM Model       |
|------------|---------------|-----------------------------------|-----------------|
| Simple     | SingleShot    | Bug fixes, small changes (<50 LOC)| gpt-4o-mini     |
| Medium     | Iterative     | Features, moderate changes (50-200)| gpt-4o         |
| Complex    | MultiAgent    | Refactors, large changes (200+)   | gpt-4o + claude |
| Epic       | MultiAgent    | System-wide changes (1000+)       | Ensemble        |

## Resilience Features

### Retry Policy
- 2 retries with 50ms delay
- Handles transient HTTP errors

### Circuit Breaker
- Opens after 3 consecutive failures
- 30 second break duration
- Prevents cascade failures

### Timeout
- 100ms timeout for fast failure
- Meets performance SLA (<100ms)

### Fallback
- Heuristic classification when ML service unavailable
- Keyword matching + description length analysis

## Heuristic Classification Rules

The fallback heuristic uses the following rules:

### Simple Tasks
- Keywords: "fix", "typo", "small", "minor", "quick", "simple"
- Description: < 20 words

### Complex Tasks
- Keywords: "architecture", "refactor", "rewrite", "migration", "complex"
- Description: > 100 words

### Medium Tasks
- Everything else (default)

## Monitoring & Telemetry

### OpenTelemetry Metrics

```csharp
// Strategy selection events are automatically tracked
Activity.Current?.SetTag("strategy.selected", strategy.Name);
Activity.Current?.SetTag("classification.source", "ml"); // or "heuristic"
Activity.Current?.SetTag("classification.confidence", 0.95);
Activity.Current?.SetTag("duration.ms", 45);
```

### Prometheus Metrics (via OpenTelemetry)

- `strategy_selection_duration_ms` - Time to select strategy
- `strategy_selection_total{strategy="SingleShot"}` - Count by strategy
- `ml_classifier_requests_total{status="success|failure"}` - ML client calls
- `ml_classifier_fallback_total` - Heuristic fallback count

### Log Examples

```
INFO: Calling ML Classifier for task {TaskId}
INFO: ML Classification for task {TaskId}: complexity=Simple, confidence=0.95
INFO: Strategy selected: SingleShot for task {TaskId} (complexity=Simple, source=ml, duration=45ms)

WARN: ML Classifier unavailable for task {TaskId}, falling back to heuristic
INFO: Heuristic classification for task {TaskId}: complexity=Simple
INFO: Strategy selected: SingleShot for task {TaskId} (complexity=Simple, source=heuristic, duration=2ms)

WARN: Strategy selection exceeded 100ms target: 125ms for task {TaskId}
```

## Testing

### Unit Tests
- 31 StrategySelector tests (all paths covered)
- 11 MLClassifierClient tests (with mock HTTP handler)
- All constructor validation
- Manual override scenarios
- Fallback behavior

### Integration Tests
- 6 integration tests with real ML service
- Performance validation (<100ms)
- Health check verification
- Manual override without ML call

Run tests:
```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests (requires ML service running)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## API Contract with ML Classifier

### Request
```json
POST /classify
{
  "task_description": "Fix the login bug where users can't authenticate",
  "context": {
    "repository": "backend-api"
  },
  "files_changed": ["src/auth/login.py"]
}
```

### Response
```json
{
  "task_type": "bug_fix",
  "complexity": "simple",
  "confidence": 0.95,
  "reasoning": "Matched 3 keywords for bug_fix",
  "suggested_strategy": "SingleShot",
  "estimated_tokens": 2000,
  "classifier_used": "heuristic"
}
```

## Future Enhancements

1. **Adaptive Learning**: Feed execution results back to ML service for model training
2. **Cost Optimization**: Factor in estimated token cost when selecting strategies
3. **User Preferences**: Allow per-user strategy preferences
4. **A/B Testing**: Compare strategy effectiveness over time
5. **Batch Classification**: Classify multiple tasks in single API call

## Troubleshooting

### ML Service Connection Issues

```bash
# Check ML service health
curl http://localhost:8000/health

# View logs
docker logs ml-classifier-service

# Test classification endpoint
curl -X POST http://localhost:8000/classify \
  -H "Content-Type: application/json" \
  -d '{"task_description": "Fix login bug"}'
```

### High Latency

If strategy selection exceeds 100ms target:
1. Check ML service response time
2. Verify network latency to ML service
3. Consider increasing timeout (trade-off: slower fallback)
4. Review circuit breaker configuration

### Incorrect Strategy Selection

1. Review ML classification confidence scores in logs
2. Check if heuristic fallback is being used too frequently
3. Provide feedback to ML team for model retraining
4. Use manual override for critical tasks

## References

- [ML Classifier Service Documentation](../../MLClassifier/ml_classifier_service/README.md)
- [Execution Strategies](./ITERATIVE_STRATEGY_IMPLEMENTATION.md)
- [Architecture Decision Records](../../../docs/04-ML-AND-ORCHESTRATION-ADR.md)
