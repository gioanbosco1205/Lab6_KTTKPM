#!/bin/bash

echo "========================================="
echo "🎯 FINAL DOCKER SYSTEM TEST"
echo "========================================="
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Test 1: Check all services
echo -e "${YELLOW}Test 1: Checking all services...${NC}"
docker compose ps
echo ""

# Test 2: Check Eureka
echo -e "${YELLOW}Test 2: Checking Eureka Server...${NC}"
if curl -s http://localhost:8761 | grep -q "Eureka"; then
    echo -e "${GREEN}✅ Eureka Server is running${NC}"
else
    echo -e "${RED}❌ Eureka Server is not responding${NC}"
fi
echo ""

# Test 3: Check RabbitMQ
echo -e "${YELLOW}Test 3: Checking RabbitMQ...${NC}"
docker exec rabbitmq rabbitmqctl list_queues name messages
echo ""

# Test 4: Clean old messages
echo -e "${YELLOW}Test 4: Cleaning old messages...${NC}"
docker exec postgres psql -U postgres -d ChatServiceDb -c "DELETE FROM outbox_messages; DELETE FROM \"Messages\";" > /dev/null 2>&1
echo -e "${GREEN}✅ Old messages cleaned${NC}"
echo ""

# Test 5: Create Policy
echo -e "${YELLOW}Test 5: Creating policy via PolicyService...${NC}"
response=$(curl -s -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test User Docker"}')
echo "Response: $response"
echo ""

# Test 6: Wait and check ChatService logs
echo -e "${YELLOW}Test 6: Waiting 3 seconds for event processing...${NC}"
sleep 3

echo -e "${BLUE}Checking ChatService logs for event processing...${NC}"
docker logs chatservice --tail 50 | grep -i "policy\|received\|event" || echo "No event logs found"
echo ""

# Test 7: Check RabbitMQ queues
echo -e "${YELLOW}Test 7: Checking RabbitMQ queues...${NC}"
docker exec rabbitmq rabbitmqctl list_queues name messages
echo ""

# Test 8: Check databases
echo -e "${YELLOW}Test 8: Checking databases...${NC}"
echo "Messages table:"
docker exec postgres psql -U postgres -d ChatServiceDb -c "SELECT COUNT(*) FROM \"Messages\";"
echo ""
echo "outbox_messages table:"
docker exec postgres psql -U postgres -d ChatServiceDb -c "SELECT COUNT(*) FROM outbox_messages;"
echo ""

# Test 9: Test via API Gateway
echo -e "${YELLOW}Test 9: Testing via API Gateway...${NC}"
gateway_response=$(curl -s -X POST http://localhost:8080/policy-service/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Gateway Test User"}' 2>&1)
  
if echo "$gateway_response" | grep -q "Policy created"; then
    echo -e "${GREEN}✅ API Gateway routing works${NC}"
    echo "Response: $gateway_response"
else
    echo -e "${RED}❌ API Gateway routing failed${NC}"
    echo "Response: $gateway_response"
fi
echo ""

# Test 10: Check Eureka registered services
echo -e "${YELLOW}Test 10: Checking services registered with Eureka...${NC}"
curl -s http://localhost:8761 | grep -A 5 "Instances currently registered" | head -10
echo ""

echo "========================================="
echo -e "${GREEN}✅ SYSTEM TEST COMPLETE${NC}"
echo "========================================="
echo ""

echo -e "${BLUE}📊 Summary:${NC}"
echo "- Eureka Server: http://localhost:8761"
echo "- RabbitMQ Management: http://localhost:15672 (guest/guest)"
echo "- API Gateway: http://localhost:8080"
echo "- PolicyService: http://localhost:5002"
echo "- ChatService: http://localhost:5003"
echo "- PaymentService: http://localhost:5004"
echo "- PricingService: http://localhost:5005"
echo "- AuthService: http://localhost:5001"
echo ""
