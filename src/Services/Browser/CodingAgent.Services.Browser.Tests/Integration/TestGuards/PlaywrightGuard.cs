using System;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace CodingAgent.Services.Browser.Tests.Integration.TestGuards;

public static class PlaywrightGuard
{
    /// <summary>
    /// Ensures Playwright browsers are installed; otherwise throws InvalidOperationException
    /// so integration tests fail with a clear message explaining how to install browsers.
    /// </summary>
    public static async Task EnsureBrowsersInstalledOrSkipAsync()
    {
        try
        {
            using var PlaywrightInstance = await Microsoft.Playwright.Playwright.CreateAsync();
            // Try launching Chromium headless quickly to verify installation
            await using var Browser = await PlaywrightInstance.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                Timeout = 10000
            });
        }
        catch (Exception Ex)
        {
            // Common failure hints include missing browser binaries (e.g., headless_shell.exe)
            var Reason = Ex.Message;
            var Help = "Playwright browsers not installed. Install them locally and re-run tests.\n" +
                       "Windows (PowerShell):\n  dotnet tool restore ; dotnet tool run playwright install --with-deps\n  # or\n  dotnet build src/Services/Browser/CodingAgent.Services.Browser/CodingAgent.Services.Browser.csproj -c Debug\n  pwsh -File src/Services/Browser/CodingAgent.Services.Browser/bin/Debug/playwright.ps1 install --with-deps\n  # or\n  npx playwright install --with-deps\n" +
                       "macOS/Linux:\n  npx playwright install --with-deps\n" +
                       "See docs/ACT-LOCAL-TESTING.md#playwright-setup for details.";
            throw new InvalidOperationException($"Skipping Browser integration tests: {Reason}\n\n{Help}", Ex);
        }
    }

    /// <summary>
    /// Synchronous wrapper for constructors.
    /// </summary>
    public static void EnsureBrowsersInstalledOrSkip() => EnsureBrowsersInstalledOrSkipAsync().GetAwaiter().GetResult();
}
