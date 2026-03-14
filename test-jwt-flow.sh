#!/bin/bash

echo "=========================================="
echo "JWT Authentication Test Script"
echo "=========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test function
test_endpoint() {
    local method=$1
    local url=$2
    local headers=$3
    local data=$4
    local expected_status=$5
    local description=$6
    
    echo -e "\n${YELLOW}Testing: $description${NC}"
    echo "URL: $method $url"
    
    if [ -n "$data" ]; then
        response=$(curl -s -w "\n%{http_code}" -X $method "$url" $headers -d "$data")
    else
        response=$(curl -s -w "\n%{http_code}" -X $method "$url" $headers)
    fi
    
    status_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n -1)
    
    if [ "$status_code" = "$expected_status" ]; then
        echo -e "${GREEN}✓ PASS${NC} - Status: $status_code"
        if [ "$status_code" = "200" ]; then
            echo "Response: $body"
        fi
    else
        echo -e "${RED}✗ FAIL${NC} - Expected: $expected_status, Got: $status_code"
        echo "Response: $body"
    fi
}

echo -e "\n${YELLOW}Step 1: Testing AuthService directly${NC}"

# Test AuthService directly
test_endpoint "POST" "http://localhost:5060/api/auth/generate-token" \
    "-H 'Content-Type: application/json'" \
    '{"username":"testuser"}' \
    "200" \
    "AuthService - Generate Token"

echo -e "\n${YELLOW}Step 2: Getting token via API Gateway${NC}"

# Get token via API Gateway
TOKEN_RESPONSE=$(curl -s -X POST "http://localhost:5050/auth/generate-token" \
    -H "Content-Type: application/json" \
    -d '{"username":"testuser"}')

echo "Token Response: $TOKEN_RESPONSE"

# Extract token (assuming jq is available, otherwise manual extraction)
if command -v jq &> /dev/null; then
    TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.token')
    echo "Extracted Token: ${TOKEN:0:50}..."
else
    echo "jq not found. Please install jq or manually extract token from response above."
    echo "Token should be in format: {\"token\":\"eyJ...\",\"username\":\"testuser\",...}"
    read -p "Please paste the token here: " TOKEN
fi

echo -e "\n${YELLOW}Step 3: Testing protected endpoints WITHOUT token (should fail)${NC}"

test_endpoint "GET" "http://localhost:5050/pricing" "" "" "401" "Pricing without token"
test_endpoint "GET" "http://localhost:5050/policy" "" "" "401" "Policy without token"

echo -e "\n${YELLOW}Step 4: Testing protected endpoints WITH token (should succeed)${NC}"

if [ -n "$TOKEN" ] && [ "$TOKEN" != "null" ]; then
    test_endpoint "GET" "http://localhost:5050/pricing" \
        "-H 'Authorization: Bearer $TOKEN'" \
        "" "200" "Pricing with token"
    
    test_endpoint "GET" "http://localhost:5050/pricing/123" \
        "-H 'Authorization: Bearer $TOKEN'" \
        "" "200" "Pricing by ID with token"
    
    test_endpoint "POST" "http://localhost:5050/pricing" \
        "-H 'Authorization: Bearer $TOKEN' -H 'Content-Type: application/json'" \
        '{"basePrice":1000,"quantity":5,"taxRate":0.1}' \
        "200" "Pricing POST with token"
    
    test_endpoint "GET" "http://localhost:5050/policy" \
        "-H 'Authorization: Bearer $TOKEN'" \
        "" "200" "Policy with token"
else
    echo -e "${RED}No valid token found. Skipping authenticated tests.${NC}"
fi

echo -e "\n${YELLOW}Step 5: Testing with invalid token (should fail)${NC}"

test_endpoint "GET" "http://localhost:5050/pricing" \
    "-H 'Authorization: Bearer invalid_token'" \
    "" "401" "Pricing with invalid token"

echo -e "\n=========================================="
echo -e "${GREEN}JWT Authentication Test Complete!${NC}"
echo "=========================================="