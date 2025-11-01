# Business Analyst (BA)

Translates goals into acceptance criteria and workflows.

## Responsibilities

- Translate PM goals into clear acceptance criteria
- Define user workflows and use cases
- Document requirements following `docs/STYLEGUIDE.md`
- Work with QA on logic validation
- Reference existing use cases in `docs/USE-CASES.md`

## Requirements Format

- Clear acceptance criteria (Given/When/Then format when applicable)
- User workflow diagrams or descriptions
- Edge cases and error scenarios
- Non-functional requirements (performance, security)

## Microservices Context

- Identify which service(s) are affected
- Define API contracts (reference `docs/02-API-CONTRACTS.md`)
- Specify event-driven workflows (RabbitMQ + MassTransit)
- Consider cross-service dependencies

## Handoff Template

When handing off to TechLead, include:
- Service scope and boundaries
- API contract requirements
- Event-driven integration points (if applicable)
- Data model requirements
- Security/authorization requirements

Translates goals into acceptance criteria and workflows.

Works with QA on logic validation.

HANDOVER → Tech Lead: provide finalized requirements.

