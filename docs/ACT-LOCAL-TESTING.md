# Local Workflow Testing with Act

This guide explains how to test GitHub Actions workflows locally using `act`.

## Prerequisites

- Docker Desktop running
- `gh` CLI with `act` extension installed: `gh extension install https://github.com/nektos/gh-act`

## Configuration

The repository includes `.actrc` configuration file that sets up:
- **Container Image**: `ghcr.io/catthehacker/ubuntu:full-latest` - includes Node.js, .NET, Python, and common tools
- **Architecture**: `linux/amd64`
- **Environment**: Optimized for .NET builds

## Common Commands

### Validate Workflow Syntax (Dry Run)
```powershell
gh act --workflows ".github/workflows/browser.yml" --dryrun
```

### Run Specific Workflow
```powershell
gh act --workflows ".github/workflows/browser.yml"
```

### Run Specific Job
```powershell
gh act --workflows ".github/workflows/browser.yml" --job build-test
```

### List Available Jobs
```powershell
gh act --workflows ".github/workflows/browser.yml" --list
```

### Run All Workflows (Push Event)
```powershell
gh act push
```

### Run All Workflows (Pull Request Event)
```powershell
gh act pull_request
```

### Run with Specific Event
```powershell
gh act workflow_dispatch --workflows ".github/workflows/browser.yml"
```

## Service-Specific Workflows

### Browser Service
```powershell
gh act --workflows ".github/workflows/browser.yml"
```

### Chat Service
```powershell
gh act --workflows ".github/workflows/chat.yml"
```

### Gateway
```powershell
gh act --workflows ".github/workflows/gateway.yml"
```

### Orchestration Service
```powershell
gh act --workflows ".github/workflows/orchestration.yml"
```

### ML Classifier (Python)
```powershell
gh act --workflows ".github/workflows/ml-classifier.yml"
```

## Playwright Setup (Browser Service)

Some Browser integration tests require Playwright-managed browsers (Chromium/Firefox). If browsers are not installed locally, those tests will be skipped with a helpful message; to run them, install browsers as follows.

### Recommended (cross-platform)
- Install via Node.js (non-interactive):
  - Windows/macOS/Linux:
    ```powershell
    npx playwright install --with-deps
    ```
  - On Linux, you may also need system dependencies:
    ```powershell
    npx playwright install-deps
    ```

### .NET-first options
- Using .NET tool (if you have a tool manifest or prefer global CLI):
  ```powershell
  dotnet tool restore
  dotnet tool run playwright install --with-deps
  # If no tool manifest exists, you can install globally instead:
  dotnet tool update -g Microsoft.Playwright.CLI
  playwright install --with-deps
  ```

- Using the project-generated script (no tool manifest needed):
  ```powershell
  # Build once to generate the Playwright script
  dotnet build src/Services/Browser/CodingAgent.Services.Browser/CodingAgent.Services.Browser.csproj -c Debug

  # Windows PowerShell
  pwsh -File src/Services/Browser/CodingAgent.Services.Browser/bin/Debug/playwright.ps1 install --with-deps

  # macOS/Linux
  bash src/Services/Browser/CodingAgent.Services.Browser/bin/Debug/playwright.sh install --with-deps
  ```

### Troubleshooting
- Error mentioning "headless_shell.exe" or missing executable:
  - Run one of the install commands above; prefer `--with-deps` on Linux.
- Corporate proxy or restricted network:
  - Set environment variables for proxy or use an offline mirror per Playwright docs.
- Still stuck?
  - Use the helper script:
    ```powershell
    bash src/Services/Browser/install-browsers.sh
    ```
  - Or open an issue with the exact error message and OS details.

## Playwright Setup (Browser Service)

Some Browser integration tests require Playwright-managed browsers (Chromium/Firefox). If browsers are not installed locally, those tests will be skipped with a helpful message; to run them, install browsers as follows.

### Recommended (cross-platform)
- Install via Node.js (non-interactive):
  - Windows/macOS/Linux:
    ```powershell
    npx playwright install --with-deps
    ```
  - On Linux, you may also need system dependencies:
    ```powershell
    npx playwright install-deps
    ```

### .NET-first options
- Using .NET tool (if you have a tool manifest or prefer global CLI):
  ```powershell
  dotnet tool restore
  dotnet tool run playwright install --with-deps
  # If no tool manifest exists, you can install globally instead:
  dotnet tool update -g Microsoft.Playwright.CLI
  playwright install --with-deps
  ```

- Using the project-generated script (no tool manifest needed):
  ```powershell
  # Build once to generate the Playwright script
  dotnet build src/Services/Browser/CodingAgent.Services.Browser/CodingAgent.Services.Browser.csproj -c Debug

  # Windows PowerShell
  pwsh -File src/Services/Browser/CodingAgent.Services.Browser/bin/Debug/playwright.ps1 install --with-deps

  # macOS/Linux
  bash src/Services/Browser/CodingAgent.Services.Browser/bin/Debug/playwright.sh install --with-deps
  ```

### Troubleshooting
- Error mentioning "headless_shell.exe" or missing executable:
  - Run one of the install commands above; prefer `--with-deps` on Linux.
- Corporate proxy or restricted network:
  - Set environment variables for proxy or use an offline mirror per Playwright docs.
- Still stuck?
  - Use the helper script:
    ```powershell
    bash src/Services/Browser/install-browsers.sh
    ```
  - Or open an issue with the exact error message and OS details.

## Troubleshooting

### Issue: Node.js not found after .NET setup
**Solution**: Use the `full-latest` image (configured in `.actrc`) instead of `runner-latest`

### Issue: Slow performance
**Cause**: Container startup and image pulling take time on first run
**Solutions**:
- Images are cached after first pull
- Use `--reuse` flag to keep containers: `gh act --workflows "..." --reuse`
- For faster iteration, run tests locally: `dotnet test --verbosity quiet --nologo`

### Issue: SSL/TLS errors downloading .NET
**Solution**: The `full-latest` image has .NET pre-installed, avoiding download issues

### Issue: Permission denied errors
**Solution**: The `.actrc` configuration handles permissions automatically

## Comparison: Act vs Local Execution

| Aspect | Act | Local |
|--------|-----|-------|
| **Speed** | Slower (container overhead) | Faster |
| **Accuracy** | Matches GitHub Actions exactly | Close but not identical |
| **Environment** | Isolated container | Your machine |
| **Best for** | Pre-push validation | Fast iteration |

## Best Practices

1. **Use dry-run first** to validate syntax: `--dryrun`
2. **Test specific workflows** instead of running all
3. **Local testing for speed**: Use `dotnet test` directly for rapid iteration
4. **Act for pre-push validation**: Run full workflow before pushing
5. **CI/CD for final check**: GitHub Actions is the source of truth

## Advanced Usage

### Skip Cache Steps (Faster)
Edit workflow temporarily to remove cache actions or use:
```powershell
gh act --workflows ".github/workflows/browser.yml" --env ACT_SKIP_CACHE=true
```

### Bind Workspace (Use Local Files)
```powershell
gh act --workflows ".github/workflows/browser.yml" --bind
```
⚠️ Warning: Slower due to file permission changes

### Use Custom Platform Image
```powershell
gh act -P ubuntu-latest=custom/image:tag --workflows ".github/workflows/browser.yml"
```

### Run with Secrets
```powershell
gh act --workflows ".github/workflows/browser.yml" --secret-file .secrets
```

## Container Images Reference

| Image | Size | Tools | Speed |
|-------|------|-------|-------|
| `act-latest` | Small (~500MB) | Basic | Fast |
| `runner-latest` | Medium (~5GB) | Node, common tools | Medium |
| `full-latest` | Large (~15GB) | Everything | Slow first run, then cached |

**Our Choice**: `full-latest` for reliability and complete tool compatibility

## Integration with VS Code Tasks

You can add act commands to `.vscode/tasks.json`:

```json
{
  "label": "Test Browser Workflow",
  "type": "shell",
  "command": "gh act --workflows '.github/workflows/browser.yml' --job build-test",
  "group": "test",
  "presentation": {
    "reveal": "always",
    "panel": "new"
  }
}
```

## Resources

- [Act Documentation](https://github.com/nektos/act)
- [GitHub Actions Runner Images](https://github.com/catthehacker/docker_images)
- [Repository Workflows](../.github/workflows/)
