# Feature Flags for Cutover (Gateway)

Configure feature flags in `src/Gateway/CodingAgent.Gateway/appsettings.json`:

```json
{
  "Features": {
    "UseLegacyChat": false,
    "UseLegacyOrchestration": false
  }
}
```

Intended usage (Phase 5):
- Gradually route traffic to new services by toggling flags
- Combine with staged traffic rollout (10% → 50% → 100%)

Implementation note: flags are documented and configured, routing logic will be added alongside cutover work.
