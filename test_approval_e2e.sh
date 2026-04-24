#!/bin/bash
# Comprehensive end-to-end test for approval flow system
# Tests SuperAdmin self-approval capability and full approval workflow

BASE_URL="http://localhost"
TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6IjkwYjhkZGUwLTVjMjktNGQ4Zi1hNzQ3LWM0ZjQ0MWQ0MTVjNSIsInN1YiI6IjkwYjhkZGUwLTVjMjktNGQ4Zi1hNzQ3LWM0ZjQ0MWQ0MTVjNSIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWUiOiJtYXNpYW1pc3pjeiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL2VtYWlsYWRkcmVzcyI6Im1hc2lhbWlzemN6QGdtYWlsLmNvbSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6IkFkbWluIiwidXNlcklkIjoiOTBiOGRkZTAtNWMyOS00ZDhmLWE3NDctYzRmNDQxZDQxNWM1IiwiaXNfc3VwZXJfYWRtaW4iOiJ0cnVlIiwiZXhwIjoxNzc3MDI3NzYwLCJpc3MiOiJUcmFkaW5nUGxhdGZvcm0iLCJhdWQiOiJUcmFkaW5nUGxhdGZvcm1Vc2VycyJ9.OD7FPqHzr0rZmQr8BfuPwFy6piVsPcDQuSkSG6tqthY"
SUPERADMIN_ID="90b8dde0-5c29-4d8f-a747-c4f441d4115c5"

echo "========================================================================"
echo "STARTING APPROVAL FLOW E2E TEST"
echo "========================================================================"

# Test 1: Get pending requests baseline
echo ""
echo "TEST 1: Get Pending Requests (Baseline)"
echo "========================================================================"
RESPONSE=$(curl -s -X GET "$BASE_URL/api/approvals/pending" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")
echo "Response: $RESPONSE"
PENDING_COUNT=$(echo "$RESPONSE" | grep -o '"id"' | wc -l)
echo "Found $PENDING_COUNT pending requests"

# Test 2: Create test instrument
echo ""
echo "TEST 2: Create Test Instrument (generates Create request)"
echo "========================================================================"
CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/instruments" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "TEST_CREATE_001",
    "name": "Test Create Instrument",
    "description": "Test instrument for approval flow"
  }')
echo "Response: $CREATE_RESPONSE"
INSTRUMENT_ID=$(echo "$CREATE_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "Created instrument: $INSTRUMENT_ID"

# Test 3: Get pending requests to find the Create request
echo ""
echo "TEST 3: Get Pending Requests (Find Create Request)"
echo "========================================================================"
sleep 2
PENDING_RESPONSE=$(curl -s -X GET "$BASE_URL/api/approvals/pending" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")
echo "Response: $PENDING_RESPONSE" | head -100
CREATE_REQUEST_ID=$(echo "$PENDING_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
echo "Create request ID: $CREATE_REQUEST_ID"

# Test 4: SuperAdmin approves own Create request
if [ -n "$CREATE_REQUEST_ID" ]; then
  echo ""
  echo "TEST 4: SuperAdmin Approves Own Create Request"
  echo "========================================================================"
  APPROVE_RESPONSE=$(curl -s -X POST "$BASE_URL/api/approvals/$CREATE_REQUEST_ID/approve" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{}')
  echo "Response: $APPROVE_RESPONSE"
  
  # Check if approved
  sleep 1
  CHECK_RESPONSE=$(curl -s -X GET "$BASE_URL/api/approvals/$CREATE_REQUEST_ID" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json")
  echo "Approval Status Check: $CHECK_RESPONSE"
fi

echo ""
echo "========================================================================"
echo "TEST COMPLETED"
echo "========================================================================"
