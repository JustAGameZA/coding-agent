# Playwright Browsers in CI

Install browsers before running E2E or Browser service tests:

```bash
npx playwright install --with-deps chromium
# optionally
npx playwright install firefox
```

- Run in a pre-step of your CI job
- For Docker images, install in the build stage or as an init step
