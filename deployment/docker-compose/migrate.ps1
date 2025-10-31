param(
    [string]$ConnectionString = "postgresql://codingagent:devPassword123!@postgres:5432/codingagent"
)

Write-Host "Running Phase 5 migration scripts..."
$ErrorActionPreference = 'Stop'

$migrations = @(
    'migrations/001-migrate-users.sql',
    'migrations/002-migrate-conversations.sql',
    'migrations/003-migrate-tasks.sql'
)

foreach ($script in $migrations) {
    Write-Host "Applying $script"
    docker compose exec -T postgres psql $ConnectionString -f "/docker-entrypoint-initdb.d/$script"
}

Write-Host "Migration complete."
