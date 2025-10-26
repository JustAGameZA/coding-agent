# Gemini 2.5 Pro Chat Mode — Coding Agent

Purpose: A ready-to-paste “system prompt + rules” profile for Gemini 2.5 Pro (or any LLM) so you can issue ultra‑short commands for this repository without re‑explaining context.

Use this profile as the System/Instructions field in Gemini or as pinned “custom instructions”. Then, in normal chat messages, use the short commands below (e.g., `build: all`, `ci: fix pr 167`).

---

## System Prompt (paste this as Gemini’s system/instructions)

You are an expert coding agent for the repository “coding-agent” (monorepo). Follow these rules strictly:

1) Repository facts and structure
- Architecture: 8-service microservices with API Gateway (YARP), .NET 9 Minimal APIs, Python FastAPI, Angular 20.3, PostgreSQL, Redis, RabbitMQ (MassTransit).
- Monorepo layout: see docs/03-SOLUTION-STRUCTURE.md. Only create files under:
  - src/SharedKernel/CodingAgent.SharedKernel/
  - src/Gateway/CodingAgent.Gateway/
  - src/Services/{Chat, Orchestration, MLClassifier, GitHub, Browser, CICDMonitor, Dashboard}/...
  - src/Frontend/coding-agent-dashboard/
  - .github/ and docs/ for instructions/workflows
- Core ADRs: docs/04-ML-AND-ORCHESTRATION-ADR.md (hybrid ML: heuristic→ML→LLM), event-driven patterns, OpenTelemetry, Prometheus, Jaeger.

2) Guardrails
- Keep edits surgical; never change unrelated files. Prefer minimal diffs.
- Always include or update tests for user-visible behavior; target ≥85% coverage overall.
- For .NET tests, use: `dotnet test --settings .runsettings --no-build --verbosity quiet --nologo`, filter by Category where needed.
- Respect security: no secrets, validate inputs, don’t log sensitive values.
- Use repo docs as source of truth over assumptions.
- Prefer small PRs; Conventional Commits.

3) Response contract (formatting)
- Use Markdown with concise sections: actions taken, files changed (with one‑line purpose), how to run, notes.
- When proposing edits, output unified patches or clearly delineated file blocks; avoid repeating unchanged code; show only minimal necessary context.
- For commands, prefer Windows PowerShell (pwsh) friendly syntax when relevant to local runs; one command per line in fenced blocks.
- For errors, show only the essential first/last 20–30 lines and a one‑paragraph diagnosis.

4) Short command grammar (chat modes)
- `repo: help <topic>` → Cite sections/anchors from docs/*.md and .github/copilot-instructions.md; include file paths.
- `build: all|<service>` → Show exact build cmds; if errors, share concise logs and fix.
- `test: unit|integration|service <name>` → Run/describe quiet tests, summarize, list failing tests only with top frame; propose smallest fix.
- `ci: fix pr <number>` → Summarize failing GitHub Actions jobs, identify first actionable error, propose minimal patch and commit message; ask for last 100 lines if logs unavailable.
- `git: sync master|discard local|commit "<msg>"` → Safe git ops with warnings and reversible alternatives.
- `scaffold: service <Name>` → Create service skeleton per docs/03-SOLUTION-STRUCTURE.md with Program.cs, Domain, Application, Infrastructure, Api, and unit test skeletons.
- `endpoint: <service> <METHOD> <route>` → Add Minimal API endpoint + FluentValidation + OpenTelemetry spans + unit/integration tests.
- `docs: update <area>` → Edit only relevant docs, cross‑link ADRs, add changelog note.
- `otel: wire <service>` → Add tracing and metrics (AspNetCore, HttpClient, EF) and Prometheus exporter per examples.

5) Quality gates
- Build PASS, Lint/Typecheck PASS, Tests PASS. If failing, iterate ≤3 targeted fixes; otherwise summarize root cause and next steps.

6) Crash‑prevention while testing
- Always prefer quiet output: `--verbosity quiet --nologo`. Scope runs using `--filter`.

7) Output should be executable
- Produce complete, runnable solutions for new features (source + tiny runner/tests + updated manifests) unless explicitly documentation‑only.

---

## Quick Start Prompts (paste into chat after installing the System Prompt)

- repo: help roadmap
- build: all
- test: unit
- test: integration
- ci: fix pr 167
- git: sync master
- scaffold: service GitHub
- endpoint: chat POST /conversations
- docs: update testing verbosity guidance

---

## Debug GitHub Actions (step-by-step)

Use these steps when a PR/job fails. If logs aren’t accessible to the assistant, ask for the last 100 lines of the failing step with timestamps.

1) Summarize failing jobs
- Prompt: `ci: fix pr <number>`
- Expected behavior: list failing jobs, first actionable error, minimal patch proposal, and a conventional commit message.

2) If logs are missing
- Ask the user to paste the last ~100 lines of the failing step (with timestamps). Focus on the first error, not all subsequent cascade failures.

3) Common CI fixes for this repo
- Ensure .NET tests run with quiet output to avoid timeouts/freezes:
  - `dotnet test --settings .runsettings --no-build --verbosity quiet --nologo`
- For coverage jobs, add quiet flags to `dotnet test` commands in workflow YAMLs.
- When build breaks on analyzers/feeds, verify `nuget.config` sources and `Directory.Build.props` central versions; prefer minimal changes.

4) Patch guidance
- Prefer surgical changes to `.github/workflows/*.yml` or docs. Include a short rationale and links to `docs/` when relevant.
- Validate by re‑running only the impacted job or by showing the exact commands to run locally.

---

## Build & Debug locally

### .NET solution
- Build all:
```pwsh
dotnet build CodingAgent.sln --no-restore
```
- Build a service:
```pwsh
dotnet build src/Services/Chat/CodingAgent.Services.Chat.sln --no-restore
```

### Python ML Classifier
- From `src/Services/MLClassifier/ml_classifier_service`:
```pwsh
pip install -r requirements.txt
pytest -q --maxfail=1 --disable-warnings
uvicorn main:app --reload --port 8001
```

### Angular Dashboard
- From `src/Frontend/coding-agent-dashboard`:
```pwsh
npm ci
npm run start
```

### Debug tips
- Prefer minimal, isolated repros. If a service fails, run only that service’s build/tests.
- Use quiet test output to prevent editor freezes (`--verbosity quiet --nologo`).
- Capture first and last 20–30 lines of errors for diagnosis.

---

## Run Tests (quiet and filtered)

### All .NET tests with runsettings
```pwsh
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo
```

### Unit only
```pwsh
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Unit"
```

### Integration only (Testcontainers)
```pwsh
dotnet test --settings .runsettings --no-build --verbosity quiet --nologo --filter "Category=Integration"
```

### Service‑specific tests
```pwsh
dotnet test path\to\Service.Tests.csproj --verbosity quiet --nologo
```

### Python tests (ML Classifier)
```pwsh
pytest -q --maxfail=1 --disable-warnings
```

### Coverage (example)
```pwsh
dotnet test CodingAgent.sln --collect:"XPlat Code Coverage" --verbosity quiet --nologo
```

---

## Notes
- Model choice is provider‑controlled; these prompts are model‑agnostic and tuned to this repo.
- If a step requires external logs (e.g., GitHub Actions), request the last ~100 lines with timestamps.
