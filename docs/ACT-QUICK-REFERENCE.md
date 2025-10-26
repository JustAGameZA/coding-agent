# Act Quick Reference

## Essential Commands

```powershell
# Dry run (validate syntax only - FAST)
gh act --workflows ".github/workflows/browser.yml" --dryrun

# Run specific workflow
gh act --workflows ".github/workflows/browser.yml"

# List jobs in workflow
gh act --workflows ".github/workflows/browser.yml" --list

# Run specific job
gh act --workflows ".github/workflows/browser.yml" --job build-test
```

## All Service Workflows

```powershell
gh act --workflows ".github/workflows/browser.yml"
gh act --workflows ".github/workflows/chat.yml"
gh act --workflows ".github/workflows/gateway.yml"
gh act --workflows ".github/workflows/orchestration.yml"
gh act --workflows ".github/workflows/ml-classifier.yml"
gh act --workflows ".github/workflows/github-service.yml"
gh act --workflows ".github/workflows/cicd-monitor.yml"
gh act --workflows ".github/workflows/dashboard-bff.yml"
```

## Configuration Files

- **`.actrc`** - Global act configuration (platform images, environment)
- **`.github/workflows/.actrc`** - Workflow-specific overrides
- **`docs/ACT-LOCAL-TESTING.md`** - Complete guide

## Quick Tips

✅ **DO**: Use `--dryrun` first to validate  
✅ **DO**: Test locally with `dotnet test` for speed  
✅ **DO**: Use act before pushing to catch issues  
❌ **DON'T**: Use `--bind` unless needed (slow)  
❌ **DON'T**: Run all workflows at once (test specific ones)

## Speed Comparison

| Method | Time | Use Case |
|--------|------|----------|
| `dotnet test` locally | ~10s | Fast iteration |
| `gh act --dryrun` | ~1s | Syntax validation |
| `gh act` (first run) | ~5-10min | Image download + full test |
| `gh act` (cached) | ~1-2min | Subsequent runs |
