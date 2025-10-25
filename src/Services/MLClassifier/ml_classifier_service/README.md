# ML Classifier Service

FastAPI-based microservice for task classification using heuristic and machine learning approaches.

## Overview

The ML Classifier Service is responsible for analyzing coding task descriptions and classifying them into:
- **Task Type**: bug_fix, feature, refactor, test, documentation, deployment
- **Complexity**: simple, medium, complex
- **Execution Strategy**: SingleShot, Iterative, MultiAgent

## Current Implementation (Phase 1)

- ✅ **Heuristic Classifier**: Keyword-based classification (90% accuracy, 5ms latency)
- 🚧 **ML Classifier**: XGBoost-based classification (coming in Phase 2)
- 🚧 **LLM Fallback**: GPT-4 classification for edge cases (coming in Phase 2)

## API Endpoints

### Health Check
```bash
GET /health
```

### Classify Task
```bash
POST /classify/
Content-Type: application/json

{
  "task_description": "Fix the login bug where users can't authenticate",
  "context": {"repository": "backend-api"},
  "files_changed": ["src/auth/login.py"]
}
```

**Response:**
```json
{
  "task_type": "bug_fix",
  "complexity": "simple",
  "confidence": 0.92,
  "reasoning": "Matched 3 keywords for bug_fix: 'fix', 'bug', 'authenticate'",
  "suggested_strategy": "SingleShot",
  "estimated_tokens": 2000,
  "classifier_used": "heuristic"
}
```

### Batch Classification
```bash
POST /classify/batch
Content-Type: application/json

[
  {"task_description": "Fix the login bug"},
  {"task_description": "Add user profile feature"}
]
```

## Local Development

### Prerequisites
- Python 3.12+
- pip or Poetry

### Setup

#### Option 1: Using Poetry (Recommended)

1. **Install Poetry:**
```bash
curl -sSL https://install.python-poetry.org | python3 -
# Or: pip install poetry
```

2. **Install dependencies:**
```bash
poetry install
```

3. **Run the service:**
```bash
# Development with auto-reload
poetry run uvicorn main:app --reload --port 8000

# Or activate poetry shell first
poetry shell
uvicorn main:app --reload --port 8000
```

#### Option 2: Using pip

1. **Create virtual environment:**
```bash
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
```

2. **Install dependencies:**
```bash
pip install -r requirements.txt
```

3. **Run the service:**
```bash
# Development with auto-reload
uvicorn main:app --reload --port 8000

# Or use the main.py directly
python main.py
```

4. **Access the API:**
- API: http://localhost:8000
- Interactive docs: http://localhost:8000/docs
- OpenAPI spec: http://localhost:8000/openapi.json

## Testing

### Run all tests:
```bash
pytest
```

### Run with coverage:
```bash
pytest --cov=ml_classifier_service --cov-report=html
```

### Run specific test types:
```bash
# Unit tests only
pytest tests/unit/

# Integration tests only
pytest tests/integration/
```

## Docker

### Build image:
```bash
docker build -t ml-classifier-service:latest .
```

### Run container:
```bash
docker run -p 8000:8000 ml-classifier-service:latest
```

## Testing with PowerShell

```powershell
# Health check
Invoke-WebRequest -Uri http://localhost:8000/health

# Classify task
$body = @{
    task_description = "Fix the login bug where users can't authenticate"
} | ConvertTo-Json

Invoke-WebRequest -Uri http://localhost:8000/classify/ `
    -Method POST `
    -Body $body `
    -ContentType "application/json"
```

## Architecture

```
ml_classifier_service/
├── main.py                     # FastAPI application entry point
├── pyproject.toml              # Poetry dependencies and config
├── requirements.txt            # pip dependencies (alternative)
├── api/
│   ├── routes/
│   │   ├── classification.py   # Classification endpoints
│   │   └── health.py           # Health check endpoints
│   └── schemas/
│       └── classification.py   # Pydantic models
├── domain/
│   ├── classifiers/
│   │   └── heuristic.py        # Heuristic classifier implementation
│   └── models/
│       └── task_type.py        # Domain enums
├── infrastructure/             # Infrastructure layer (Phase 2 placeholders)
│   ├── database.py             # PostgreSQL connection & repositories
│   ├── cache.py                # Redis caching
│   └── messaging.py            # RabbitMQ event handling
└── tests/
    ├── unit/                   # Unit tests
    └── integration/            # Integration tests
```

## Performance Targets

| Metric | Target | Current |
|--------|--------|---------|
| Latency (p50) | < 20ms | ~5ms |
| Latency (p95) | < 50ms | ~10ms |
| Accuracy | > 90% | ~90% |
| Throughput | 1000 req/s | TBD |

## Future Enhancements (Phase 2+)

- [ ] ML-based classification with XGBoost
- [ ] LLM fallback for edge cases
- [ ] PostgreSQL integration for training data
- [ ] Redis caching for model predictions
- [ ] RabbitMQ event consumption for feedback loop
- [ ] Automated model retraining pipeline
- [ ] A/B testing framework
- [ ] Model versioning and rollback

## Contributing

See the main repository [CONTRIBUTING.md](../../../../CONTRIBUTING.md) for guidelines.

## License

See [LICENSE](../../../../LICENSE)
