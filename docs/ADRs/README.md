# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records documenting key technical decisions made during the project.

## ADRs

| ADR | Title | Status |
|-----|-------|--------|
| ADR-004 | ML Classification Strategy | ✅ Complete |
| ADR-005 | Ollama Service Integration | ✅ Complete |
| ADR-006 | Chat Agent Interaction | ✅ Complete |

## ADR Status

### ✅ Complete ADRs

1. **ADR-004: ML Classification Strategy** (`04-ML-AND-ORCHESTRATION-ADR.md`)
   - Documented ML classifier architecture
   - Decision: Python FastAPI service with scikit-learn
   - Hybrid routing strategy (heuristic → ML → LLM)

2. **ADR-005: Ollama Service Integration** (`05-OLLAMA-SERVICE-ADR.md`)
   - Documented Ollama integration approach
   - Decision: .NET service wrapping Ollama API
   - Support for both Ollama-compatible and OpenAI-compatible endpoints

3. **ADR-006: Chat Agent Interaction** (`06-CHAT-AGENT-INTERACTION-ADR.md`)
   - Documented chat agent flow
   - Decision: Event-driven architecture with RabbitMQ
   - Real-time updates via SignalR

## ADR Template

When creating a new ADR, use this template:

```markdown
# ADR-XXX: [Title]

## Status
[Proposed | Accepted | Rejected | Deprecated | Superseded]

## Context
[Describe the issue motivating this decision]

## Decision
[Describe the decision that was made]

## Consequences
[Describe the consequences of this decision]
```

## Location

ADRs are located in the root `docs/` directory:
- `docs/04-ML-AND-ORCHESTRATION-ADR.md`
- `docs/05-OLLAMA-SERVICE-ADR.md`
- `docs/06-CHAT-AGENT-INTERACTION-ADR.md`

---

**Last Updated**: December 2025

