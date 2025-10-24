# API Contracts - OpenAPI Index

Status: Draft
Version: 1.0.0
Last Updated: October 24, 2025

---

## Purpose

Central index for OpenAPI specifications for all services. Each service must provide a machine-readable spec and keep it in sync with implementation.

- Style: OpenAPI 3.1
- Naming: kebab-case filenames
- Location: docs/api/

## Specs

- Gateway: docs/api/gateway-openapi.yaml (TBD)
- Chat Service: docs/api/chat-service-openapi.yaml (TBD)
- Orchestration Service: docs/api/orchestration-service-openapi.yaml (TBD)
- ML Classifier: docs/api/ml-classifier-openapi.yaml (TBD)
- GitHub Service: docs/api/github-service-openapi.yaml (TBD)
- Browser Service: docs/api/browser-service-openapi.yaml (TBD)
- CI/CD Monitor: docs/api/cicd-monitor-openapi.yaml (TBD)
- Dashboard Service: docs/api/dashboard-service-openapi.yaml (TBD)

## Requirements

- Contract-first: update spec before implementation
- Backward compatible changes only on minor versions
- Breaking changes require major version and migration notes
- Include examples for all endpoints

## Next Steps

- [ ] Create initial Gateway OpenAPI (routes + health/metrics)
- [ ] Add Chat Service endpoints
- [ ] Add Orchestration Service endpoints
- [ ] Add ML Classifier endpoints
- [ ] Wire CI to validate OpenAPI (spectral)
