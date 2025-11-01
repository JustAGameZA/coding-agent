# Script to find and report all remaining mocks in E2E tests
# This helps track progress on removing all mocks

Write-Host "Finding all remaining mocks in E2E tests..." -ForegroundColor Yellow

$testFiles = Get-ChildItem -Path "src/Frontend/coding-agent-dashboard/e2e" -Filter "*.spec.ts" -Recurse

$mockCount = 0
$filesWithMocks = @()

foreach ($file in $testFiles) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match "page\.route\(|route\.fulfill") {
        $count = ([regex]::Matches($content, "page\.route\(")).Count
        $mockCount += $count
        $filesWithMocks += [PSCustomObject]@{
            File = $file.Name
            Mocks = $count
        }
    }
}

Write-Host "`nTotal mocks found: $mockCount" -ForegroundColor Cyan
Write-Host "Files with mocks: $($filesWithMocks.Count)" -ForegroundColor Cyan

if ($filesWithMocks.Count -gt 0) {
    Write-Host "`nFiles needing updates:" -ForegroundColor Yellow
    $filesWithMocks | Format-Table -AutoSize
}

