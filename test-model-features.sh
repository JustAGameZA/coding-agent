#!/bin/bash
# Test script for Model Registry, Model Selection, A/B Testing, and Performance Tracking

ORCHESTRATION_URL="http://localhost:5002"
OLLAMA_SERVICE_URL="http://localhost:5003"
ML_CLASSIFIER_URL="http://localhost:8000"

echo "=== Testing Model Registry, Selection, A/B Testing, and Performance Tracking ==="
echo ""

# Test 1: Check Orchestration Service Health
echo "1. Testing Orchestration Service Health..."
curl -s "$ORCHESTRATION_URL/health" | jq '.' || echo "Service not available"
echo ""

# Test 2: Get Available Models (Model Registry)
echo "2. Testing Model Registry - Get Available Models..."
curl -s "$ORCHESTRATION_URL/api/models/" | jq '.' || echo "Models endpoint not available"
echo ""

# Test 3: Refresh Model Registry
echo "3. Testing Model Registry - Refresh Models..."
curl -s -X POST "$ORCHESTRATION_URL/api/models/refresh" | jq '.' || echo "Refresh endpoint not available"
echo ""

# Test 4: Get Models Again After Refresh
echo "4. Testing Model Registry - Get Models After Refresh..."
curl -s "$ORCHESTRATION_URL/api/models/" | jq '. | length' && echo " models found" || echo "No models"
echo ""

# Test 5: Model Selection (ML-based)
echo "5. Testing ML Model Selection..."
curl -s -X POST "$ORCHESTRATION_URL/api/models/select" \
  -H "Content-Type: application/json" \
  -d '{
    "taskDescription": "Create a login page with authentication",
    "taskType": "Feature",
    "complexity": "Medium"
  }' | jq '.' || echo "Model selection not available"
echo ""

# Test 6: Create A/B Test
echo "6. Testing A/B Testing - Create Test..."
AB_TEST_ID=$(curl -s -X POST "$ORCHESTRATION_URL/api/ab-tests/" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test gpt-4o vs gpt-4o-mini",
    "modelA": "gpt-4o",
    "modelB": "gpt-4o-mini",
    "taskTypeFilter": "Feature",
    "trafficPercent": 10,
    "minSamples": 100
  }' | jq -r '.id // empty')

if [ -n "$AB_TEST_ID" ]; then
  echo "Created A/B Test with ID: $AB_TEST_ID"
else
  echo "A/B test creation failed or endpoint not available"
fi
echo ""

# Test 7: Check Active A/B Test
echo "7. Testing A/B Testing - Get Active Test..."
curl -s "$ORCHESTRATION_URL/api/ab-tests/active/Feature" | jq '.' || echo "No active test or endpoint not available"
echo ""

# Test 8: Get Performance Metrics
echo "8. Testing Performance Tracking - Get All Metrics..."
curl -s "$ORCHESTRATION_URL/api/models/metrics" | jq '.' || echo "Metrics endpoint not available"
echo ""

# Test 9: Get Best Model for Task Type/Complexity
echo "9. Testing Performance Tracking - Get Best Model..."
curl -s "$ORCHESTRATION_URL/api/models/best/Feature/Medium" | jq -r '.' || echo "No best model found or endpoint not available"
echo ""

# Test 10: Test Ollama Service (Model Discovery)
echo "10. Testing Ollama Service - Hardware Detection..."
curl -s "$OLLAMA_SERVICE_URL/api/hardware" | jq '{tier, hasGpu, vramGB}' || echo "Ollama service not available"
echo ""

# Test 11: Test ML Classifier
echo "11. Testing ML Classifier Service..."
curl -s -X POST "$ML_CLASSIFIER_URL/classify/" \
  -H "Content-Type: application/json" \
  -d '{
    "task_description": "Create a login page with authentication"
  }' | jq '{task_type, complexity, confidence, classifier_used}' || echo "ML Classifier not available"
echo ""

echo "=== Testing Complete ==="

