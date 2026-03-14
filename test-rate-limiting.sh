#!/bin/bash

echo "=========================================="
echo "Rate Limiting Test Script"
echo "=========================================="

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get JWT Token first
echo -e "\n${YELLOW}Step 1: Getting JWT Token${NC}"
TOKEN_RESPONSE=$(curl -s -X POST "http://localhost:5050/auth/generate-token" \
    -H "Content-Type: application/json" \
    -H "ClientId: test-script" \
    -d '{"username":"testuser"}')

if command -v jq &> /dev/null; then
    TOKEN=$(echo "$TOKEN_RESPONSE" | jq -r '.token')
    echo "Token obtained: ${TOKEN:0:50}..."
else
    echo "Token Response: $TOKEN_RESPONSE"
    echo "Please install jq for better token extraction"
    exit 1
fi

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
    echo -e "${RED}Failed to get token. Exiting.${NC}"
    exit 1
fi

# Test Rate Limiting for different endpoints
test_rate_limit() {
    local endpoint=$1
    local method=$2
    local limit=$3
    local period=$4
    local client_id=$5
    local description=$6
    
    echo -e "\n${BLUE}Testing: $description${NC}"
    echo "Endpoint: $method $endpoint"
    echo "Rate Limit: $limit requests per $period"
    echo "Client ID: $client_id"
    
    local success_count=0
    local rate_limited_count=0
    
    # Test requests up to limit + 2
    for i in $(seq 1 $((limit + 2))); do
        if [ "$method" = "GET" ]; then
            response=$(curl -s -w "\n%{http_code}" \
                -H "Authorization: Bearer $TOKEN" \
                -H "ClientId: $client_id" \
                "$endpoint")
        elif [ "$method" = "POST" ]; then
            response=$(curl -s -w "\n%{http_code}" -X POST \
                -H "Authorization: Bearer $TOKEN" \
                -H "ClientId: $client_id" \
                -H "Content-Type: application/json" \
                -d '{"basePrice":1000,"quantity":1,"taxRate":0.1}' \
                "$endpoint")
        fi
        
        status_code=$(echo "$response" | tail -n1)
        
        if [ "$status_code" = "200" ] || [ "$status_code" = "201" ]; then
            success_count=$((success_count + 1))
            echo -e "Request $i: ${GREEN}✓ $status_code${NC}"
        elif [ "$status_code" = "429" ]; then
            rate_limited_count=$((rate_limited_count + 1))
            echo -e "Request $i: ${RED}✗ $status_code (Rate Limited)${NC}"
        else
            echo -e "Request $i: ${YELLOW}? $status_code${NC}"
        fi
        
        # Small delay between requests for per-second limits
        if [ "$period" = "second" ]; then
            sleep 0.1
        fi
    done
    
    echo -e "Results: ${GREEN}$success_count successful${NC}, ${RED}$rate_limited_count rate limited${NC}"
    
    # Verify rate limiting worked
    if [ $rate_limited_count -gt 0 ]; then
        echo -e "${GREEN}✓ Rate limiting is working correctly${NC}"
    else
        echo -e "${YELLOW}⚠ No rate limiting detected - check configuration${NC}"
    fi
}

# Test different endpoints with their specific rate limits
echo -e "\n${YELLOW}Step 2: Testing Rate Limits${NC}"

# Test Pricing GET (5 per second)
test_rate_limit "http://localhost:5050/pricing" "GET" 5 "second" "test-pricing-get" "Pricing GET (5/second)"

# Wait a bit before next test
sleep 2

# Test Pricing GET by ID (3 per second)  
test_rate_limit "http://localhost:5050/pricing/123" "GET" 3 "second" "test-pricing-get-id" "Pricing GET by ID (3/second)"

# Wait a bit before next test
sleep 2

# Test Policy GET (2 per second)
test_rate_limit "http://localhost:5050/policy" "GET" 2 "second" "test-policy-get" "Policy GET (2/second)"

# Test POST (20 per minute - we'll test first few)
echo -e "\n${BLUE}Testing: Pricing POST (20/minute)${NC}"
echo "Testing first 5 requests only..."
for i in $(seq 1 5); do
    response=$(curl -s -w "\n%{http_code}" -X POST \
        -H "Authorization: Bearer $TOKEN" \
        -H "ClientId: test-pricing-post" \
        -H "Content-Type: application/json" \
        -d '{"basePrice":1000,"quantity":1,"taxRate":0.1}' \
        "http://localhost:5050/pricing")
    
    status_code=$(echo "$response" | tail -n1)
    
    if [ "$status_code" = "200" ] || [ "$status_code" = "201" ] || [ "$status_code" = "404" ]; then
        echo -e "POST Request $i: ${GREEN}✓ $status_code${NC}"
    elif [ "$status_code" = "429" ]; then
        echo -e "POST Request $i: ${RED}✗ $status_code (Rate Limited)${NC}"
    else
        echo -e "POST Request $i: ${YELLOW}? $status_code${NC}"
    fi
done

echo -e "\n=========================================="
echo -e "${GREEN}Rate Limiting Test Complete!${NC}"
echo "=========================================="
echo -e "${YELLOW}Rate Limit Summary:${NC}"
echo "• Auth endpoints: 10/minute"
echo "• Pricing GET: 5/second"  
echo "• Pricing GET by ID: 3/second"
echo "• Policy GET: 2/second"
echo "• Pricing POST: 20/minute"
echo "• Pricing PUT: 15/minute"
echo "• Pricing DELETE: 5/minute"
echo -e "\n${BLUE}Note: 404 errors are expected if services aren't running${NC}"
echo -e "${BLUE}The important thing is that rate limiting (429) works${NC}"