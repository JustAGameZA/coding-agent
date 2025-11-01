# Test script for Model Registry, Model Selection, A/B Testing, and Performance Tracking

$ORCHESTRATION_URL = "http://localhost:5002"
$OLLAMA_SERVICE_URL = "http://localhost:5003"
$ML_CLASSIFIER_URL = "http://localhost:8000"

Write-Host "=== Testing Model Registry, Selection, A/B Testing, and Performance Tracking ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check Orchestration Service Health
Write-Host "1. Testing Orchestration Service Health..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/health" -Method Get -ErrorAction Stop
    $response | ConvertTo-Json -Depth 3
} catch {
    Write-Host "Service not available: $_" -ForegroundColor Red
}
Write-Host ""

# Test 2: Get Available Models (Model Registry)
Write-Host "2. Testing Model Registry - Get Available Models..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/models/" -Method Get -ErrorAction Stop
    Write-Host "Found $($response.Count) models:" -ForegroundColor Green
    $response | ForEach-Object { Write-Host "  - $($_.Name) ($($_.Provider))" }
} catch {
    Write-Host "Models endpoint not available: $_" -ForegroundColor Red
}
Write-Host ""

# Test 3: Refresh Model Registry
Write-Host "3. Testing Model Registry - Refresh Models..." -ForegroundColor Yellow
try {
    Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/models/refresh" -Method Post -ErrorAction Stop | Out-Null
    Write-Host "Model registry refreshed successfully" -ForegroundColor Green
} catch {
    Write-Host "Refresh endpoint not available: $_" -ForegroundColor Red
}
Write-Host ""

# Test 4: Model Selection (ML-based)
Write-Host "4. Testing ML Model Selection..." -ForegroundColor Yellow
try {
    $body = @{
        taskDescription = "Create a login page with authentication"
        taskType = "Feature"
        complexity = "Medium"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/models/select" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    Write-Host "Selected Model: $($response.selectedModel)" -ForegroundColor Green
    Write-Host "Reason: $($response.reason)" -ForegroundColor Gray
    Write-Host "Confidence: $($response.confidence)" -ForegroundColor Gray
    if ($response.isABTest) {
        Write-Host "A/B Test: Yes (ID: $($response.abTestId))" -ForegroundColor Magenta
    }
} catch {
    Write-Host "Model selection not available: $_" -ForegroundColor Red
}
Write-Host ""

# Test 5: Create A/B Test
Write-Host "5. Testing A/B Testing - Create Test..." -ForegroundColor Yellow
try {
    $body = @{
        name = "Test gpt-4o vs gpt-4o-mini"
        modelA = "gpt-4o"
        modelB = "gpt-4o-mini"
        taskTypeFilter = "Feature"
        trafficPercent = 10
        minSamples = 100
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/ab-tests/" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    $abTestId = $response.id
    Write-Host "Created A/B Test with ID: $abTestId" -ForegroundColor Green
    Write-Host "Test Name: $($response.name)" -ForegroundColor Gray
    Write-Host "Models: $($response.modelA) vs $($response.modelB)" -ForegroundColor Gray
    Write-Host "Traffic: $($response.trafficPercent)%" -ForegroundColor Gray
} catch {
    Write-Host "A/B test creation failed: $_" -ForegroundColor Red
    $abTestId = $null
}
Write-Host ""

# Test 6: Check Active A/B Test
Write-Host "6. Testing A/B Testing - Get Active Test..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/ab-tests/active/Feature" -Method Get -ErrorAction Stop
    Write-Host "Active Test: $($response.name)" -ForegroundColor Green
} catch {
    Write-Host "No active test or endpoint not available: $_" -ForegroundColor Yellow
}
Write-Host ""

# Test 7: Get Performance Metrics
Write-Host "7. Testing Performance Tracking - Get All Metrics..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/models/metrics" -Method Get -ErrorAction Stop
    $metricsCount = ($response.PSObject.Properties | Measure-Object).Count
    Write-Host "Found metrics for $metricsCount models" -ForegroundColor Green
    $response.PSObject.Properties | ForEach-Object {
        $metrics = $_.Value
        Write-Host "  $($metrics.modelName): Success Rate=$([math]::Round($metrics.successRate, 2)), Executions=$($metrics.executionCount)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Metrics endpoint not available (no metrics yet): $_" -ForegroundColor Yellow
}
Write-Host ""

# Test 8: Get Best Model for Task Type/Complexity
Write-Host "8. Testing Performance Tracking - Get Best Model..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$ORCHESTRATION_URL/api/models/best/Feature/Medium" -Method Get -ErrorAction Stop
    Write-Host "Best Model for Feature/Medium: $response" -ForegroundColor Green
} catch {
    Write-Host "No best model found yet (need performance data): $_" -ForegroundColor Yellow
}
Write-Host ""

# Test 9: Test Ollama Service (Model Discovery)
Write-Host "9. Testing Ollama Service - Hardware Detection..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$OLLAMA_SERVICE_URL/api/hardware" -Method Get -ErrorAction Stop
    Write-Host "Hardware Tier: $($response.tier)" -ForegroundColor Green
    Write-Host "Has GPU: $($response.hasGpu), VRAM: $($response.vramGB)GB" -ForegroundColor Gray
} catch {
    Write-Host "Ollama service not available: $_" -ForegroundColor Red
}
Write-Host ""

# Test 10: Test ML Classifier
Write-Host "10. Testing ML Classifier Service..." -ForegroundColor Yellow
try {
    $body = @{
        task_description = "Create a login page with authentication"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$ML_CLASSIFIER_URL/classify/" -Method Post -Body $body -ContentType "application/json" -ErrorAction Stop
    Write-Host "Classification:" -ForegroundColor Green
    Write-Host "  Type: $($response.task_type)" -ForegroundColor Gray
    Write-Host "  Complexity: $($response.complexity)" -ForegroundColor Gray
    Write-Host "  Confidence: $($response.confidence)" -ForegroundColor Gray
    Write-Host "  Classifier: $($response.classifier_used)" -ForegroundColor Gray
} catch {
    Write-Host "ML Classifier not available: $_" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== Testing Complete ===" -ForegroundColor Cyan

