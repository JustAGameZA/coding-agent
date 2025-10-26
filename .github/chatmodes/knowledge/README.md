# Knowledge Snippets (for assistants)

Purpose: lightweight, human‑readable notes that help LLMs operate efficiently in this repo without re‑digesting everything each time. Keep each file small (≤200 lines), focused, and easy to refresh.

Rules
- No secrets, tokens, or credentials.
- Paraphrase external sources and include short citations/links when relevant.
- Prefer model‑agnostic phrasing; avoid provider‑specific features unless required.
- Update when behavior, APIs, or workflows change.

Suggested files
- `repo-brief.md` — architecture/services summary, build/test commands, gotchas.
- `ci-common-fixes.md` — recurring GitHub Actions issues and minimal patches.
- `observability-notes.md` — OpenTelemetry, metrics, correlation IDs, Prometheus.

Maintenance
- Keep diffs small and targeted.
- When in doubt, favor deleting stale guidance over preserving it.
