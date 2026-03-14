#!/bin/bash

echo "=========================================="
echo "Simple Rate Limiting Test"
echo "=========================================="

# Get token
echo "Getting JWT token..."
TOKEN_RESPONSE=$(curl -s -X POST "http://localhost:5050/auth/generate-token" \
    -H "Content-Type: application/json" \
    -H "ClientId: test-client" \
    -d '{"username":"testuser"}')

echo "Token response: $TOKEN_RESPONSE"

# Extract token manually (simple approach)
TOKEN=$(echo "$TOKEN_RESPONSE" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')

if [ -z "$TOKEN" ]; then
    echo "Failed to extract token. Exiting."
    exit 1
fi

echo "Token extracted: ${TOKEN:0:50}..."

# Test rate limiting for Pricing GET (5 per second)
echo ""
echo "Testing Pricing GET rate limiting (5 requests/second)..."
echo "Sending 6 requests quickly - the 6th should be rate limited (429)"

for i in {1..6}; do
    echo -n "Request $i: "
    response=$(curl -s -w "%{http_code}" \
        -H "Authorization: Bearer $TOKEN" \
        -H "ClientId: test-pricing" \
        "http://localhost:5050/pricing")
    
    status_code="${response: -3}"
    
    if [ "$status_code" = "200" ]; then
        echo "✓ Success (200)"
    elif [ "$status_code" = "401" ]; then
        echo "✗ Unauthorized (401)"
    elif [ "$status_code" = "404" ]; then
        echo "? Not Found (404) - Service may not be running"
    elif [ "$status_code" = "429" ]; then
        echo "🚫 Rate Limited (429) - SUCCESS!"
    else
        echo "? Status: $status_code"
    fi
    
    sleep 0.1
done

echo ""
echo "Testing Policy GET rate limiting (2 requests/second)..."
echo "Sending 3 requests quickly - the 3rd should be rate limited (429)"

for i in {1..3}; do
    echo -n "Request $i: "
    response=$(curl -s -w "%{http_code}" \
        -H "Authorization: Bearer $TOKEN" \
        -H "ClientId: test-policy" \
        "http://localhost:5050/policy")
    
    status_code="${response: -3}"
    
    if [ "$status_code" = "200" ]; then
        echo "✓ Success (200)"
    elif [ "$status_code" = "401" ]; then
        echo "✗ Unauthorized (401)"
    elif [ "$status_code" = "404" ]; then
        echo "? Not Found (404) - Service may not be running"
    elif [ "$status_code" = "429" ]; then
        echo "🚫 Rate Limited (429) - SUCCESS!"
    else
        echo "? Status: $status_code"
    fi
    
    sleep 0.1
done

echo ""
echo "=========================================="
echo "Rate Limiting Test Complete!"
echo "=========================================="
echo "Expected results:"
echo "- 404 errors are OK (services not running)"
echo "- 429 errors show rate limiting is working"
echo "- The important thing is seeing 429 responses"