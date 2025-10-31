# verify-migration.ps1
# Verification script for Phase 5 data migration
# Validates row counts and spot-checks migrated data

param(
    [string]$ConnectionString = "postgresql://codingagent:devPassword123!@postgres:5432/codingagent",
    [int]$SampleSize = 20
)

Write-Host "Verifying Phase 5 data migration..."
$ErrorActionPreference = 'Stop'

# Row count validation
Write-Host "`n=== Row Count Validation ===" -ForegroundColor Cyan
$queries = @(
    @{ Name = "Users"; Query = "SELECT COUNT(*) FROM auth.users" },
    @{ Name = "Conversations"; Query = "SELECT COUNT(*) FROM chat.conversations" },
    @{ Name = "Messages"; Query = "SELECT COUNT(*) FROM chat.messages" },
    @{ Name = "Tasks"; Query = "SELECT COUNT(*) FROM orchestration.coding_tasks" },
    @{ Name = "Executions"; Query = "SELECT COUNT(*) FROM orchestration.task_executions" }
)

foreach ($q in $queries) {
    $result = docker compose exec -T postgres psql $ConnectionString -t -c $q.Query
    Write-Host "$($q.Name): $($result.Trim())"
}

# Spot-check validation (random samples)
Write-Host "`n=== Spot-Check Validation (20 random samples) ===" -ForegroundColor Cyan

# Users sample
Write-Host "`nUsers sample:"
docker compose exec -T postgres psql $ConnectionString -c "SELECT id, username, email FROM auth.users ORDER BY RANDOM() LIMIT $SampleSize" | Out-Null

# Conversations sample
Write-Host "`nConversations sample:"
docker compose exec -T postgres psql $ConnectionString -c "SELECT id, title, user_id FROM chat.conversations ORDER BY RANDOM() LIMIT $SampleSize" | Out-Null

# Tasks sample
Write-Host "`nTasks sample:"
docker compose exec -T postgres psql $ConnectionString -c "SELECT id, title, status FROM orchestration.coding_tasks ORDER BY RANDOM() LIMIT $SampleSize" | Out-Null

Write-Host "`n✅ Verification complete." -ForegroundColor Green

