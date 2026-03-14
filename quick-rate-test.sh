#!/bin/bash

echo "Quick Rate Limiting Test"
echo "========================"

# Get fresh token
echo "Getting new token..."
TOKEN_RESPONSE=$(curl -s -X POST "http://localhost:5050/auth/generate-token" \
    -H "Content-Type: application/json" \
    -H "ClientId: quick-test" \
    -d '{"username":"testuser"}')

echo "Token response: $TOKEN_RESPONSE"

# Extract token
TOKEN=$(echo "$TOKEN_RESPONSE" | sed -n 's/.*"token":"\([^"]*\)".*/\1/p')

if [ -z "$TOKEN" ]; then
    echo "Failed to get token"
    exit 1
fi

echo "Token: ${TOKEN:0:50}..."
echo ""

# Test rate limiting with rapid requests
echo "Testing rate limiting (5 requests/second limit)..."
echo "Sending 7 requests rapidly..."

for i in {1..7}; do
    echo -n "Request $i: "
    
    response=$(curl -s -w "%{http_code}" \
        -H "Authorization: Bearer $TOKEN" \
        -H "ClientId: rapid-test" \
        "http://localhost:5050/pricing")
    
    status_code="${response: -3}"
    body="${response%???}"
    
    if [ "$status_code" = "200" ]; then
        echo "✓ Success (200)"
    elif [ "$status_code" = "429" ]; then
        echo "🚫 Rate Limited (429) - WORKING!"
    else
        echo "Status: $status_code"
    fi
done

echo ""
echo "If you see 429 responses, rate limiting is working!"
echo "If all are 200, rate limiting may need adjustment."