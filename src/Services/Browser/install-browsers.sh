#!/bin/bash
# Install Playwright browsers for Browser Service tests

set -e

echo "🎭 Installing Playwright browsers for Browser Service..."

# Ensure we're in the correct directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Build the project first to get the playwright.ps1 script
echo "📦 Building Browser Service project..."
dotnet build CodingAgent.Services.Browser/CodingAgent.Services.Browser.csproj

# Find the playwright.ps1 script
PLAYWRIGHT_SCRIPT=$(find . -name "playwright.ps1" | head -n 1)

if [ -z "$PLAYWRIGHT_SCRIPT" ]; then
    echo "❌ Error: playwright.ps1 not found. Make sure the project is built."
    exit 1
fi

echo "🔧 Found Playwright script: $PLAYWRIGHT_SCRIPT"

# Install system dependencies (requires sudo on Linux)
if command -v playwright &> /dev/null; then
    echo "📥 Installing system dependencies..."
    if [ "$EUID" -eq 0 ]; then
        playwright install-deps chromium firefox || echo "⚠️  Warning: Failed to install system dependencies. You may need to install them manually."
    else
        echo "⚠️  Skipping system dependencies (requires root). Run 'sudo playwright install-deps' if needed."
    fi
fi

# Install browsers using PowerShell script
echo "🌐 Installing Chromium and Firefox browsers..."
pwsh "$PLAYWRIGHT_SCRIPT" install chromium firefox

if [ $? -eq 0 ]; then
    echo "✅ Playwright browsers installed successfully!"
    echo ""
    echo "You can now run integration tests with:"
    echo "  dotnet test --filter \"Category=Integration\""
else
    echo "❌ Failed to install Playwright browsers."
    echo ""
    echo "Try installing manually:"
    echo "  pwsh $PLAYWRIGHT_SCRIPT install chromium firefox"
    echo ""
    echo "Or using the Playwright CLI:"
    echo "  playwright install chromium firefox"
    exit 1
fi
