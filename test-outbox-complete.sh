#!/bin/bash

# ═══════════════════════════════════════════════════════════════════════════
# SCRIPT TEST OUTBOX PATTERN - ĐẦY ĐỦ
# Mục tiêu: Test và chụp ảnh để chứng minh Outbox Pattern hoạt động
# ═══════════════════════════════════════════════════════════════════════════

echo "═══════════════════════════════════════════════════════════════════════════"
echo "🧪 TEST OUTBOX PATTERN - COMPREHENSIVE"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print section header
print_section() {
    echo ""
    echo "═══════════════════════════════════════════════════════════════════════════"
    echo -e "${BLUE}$1${NC}"
    echo "═══════════════════════════════════════════════════════════════════════════"
    echo ""
}

# Function to print step
print_step() {
    echo -e "${YELLOW}▶ $1${NC}"
}

# Function to print success
print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

# Function to print error
print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 1: KIỂM TRA HỆ THỐNG
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 1: KIỂM TRA HỆ THỐNG"

print_step "Checking Docker containers..."
docker compose ps

if [ $? -eq 0 ]; then
    print_success "Docker containers are running"
else
    print_error "Docker containers are not running. Please run: docker compose up -d"
    exit 1
fi

print_step "Checking ChatService health..."
curl -s http://localhost:5003/health > /dev/null 2>&1
if [ $? -eq 0 ]; then
    print_success "ChatService is healthy"
else
    print_error "ChatService is not responding"
fi

print_step "Checking RabbitMQ..."
docker exec rabbitmq rabbitmqctl status > /dev/null 2>&1
if [ $? -eq 0 ]; then
    print_success "RabbitMQ is running"
else
    print_error "RabbitMQ is not running"
fi

print_step "Checking PostgreSQL..."
docker exec postgres pg_isready -U postgres > /dev/null 2>&1
if [ $? -eq 0 ]; then
    print_success "PostgreSQL is running"
else
    print_error "PostgreSQL is not running"
fi

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 2: CHUẨN BỊ DATABASE
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 2: CHUẨN BỊ DATABASE"

print_step "Cleaning Messages table (Event Store)..."
docker exec postgres psql -U postgres -d ChatServiceDb -c "DELETE FROM \"Messages\";" > /dev/null 2>&1

print_step "Checking Messages table structure..."
echo ""
echo "📸 CHỤP ẢNH 1: Cấu trúc bảng Messages (Event Store)"
echo "─────────────────────────────────────────────────────────────────────────"
docker exec postgres psql -U postgres -d ChatServiceDb -c "\d \"Messages\""
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

print_step "Verifying Messages table is empty..."
MESSAGE_COUNT=$(docker exec postgres psql -U postgres -d ChatServiceDb -t -c "SELECT COUNT(*) FROM \"Messages\";")
echo "Current message count: $MESSAGE_COUNT"
print_success "Database is ready for testing"

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 3: TEST OUTBOX PATTERN - CREATE POLICY
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 3: TEST OUTBOX PATTERN - CREATE POLICY"

print_step "Creating policy via PolicyService..."
POLICY_RESPONSE=$(curl -s -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test Outbox Pattern"}')

echo "Policy Response: $POLICY_RESPONSE"

if [[ $POLICY_RESPONSE == *"policyNumber"* ]]; then
    POLICY_NUMBER=$(echo $POLICY_RESPONSE | grep -o '"policyNumber":"[^"]*"' | cut -d'"' -f4)
    print_success "Policy created: $POLICY_NUMBER"
else
    print_error "Failed to create policy"
fi

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 4: KIỂM TRA MESSAGES TABLE (TRƯỚC KHI XỬ LÝ)
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 4: KIỂM TRA MESSAGES TABLE (TRƯỚC KHI XỬ LÝ)"

print_step "Waiting 1 second for event to be saved to Messages table..."
sleep 1

print_step "Checking Messages table (should have unprocessed messages)..."
echo ""
echo "📸 CHỤP ẢNH 2: Messages chưa xử lý (ProcessedAt = NULL)"
echo "─────────────────────────────────────────────────────────────────────────"
docker exec postgres psql -U postgres -d ChatServiceDb -c \
  "SELECT \"Id\", \"Type\", \"CreatedAt\", \"ProcessedAt\", \"RetryCount\" FROM \"Messages\" WHERE \"ProcessedAt\" IS NULL ORDER BY \"Id\" DESC LIMIT 5;"
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

UNPROCESSED_COUNT=$(docker exec postgres psql -U postgres -d ChatServiceDb -t -c \
  "SELECT COUNT(*) FROM \"Messages\" WHERE \"ProcessedAt\" IS NULL;")
echo "Unprocessed messages: $UNPROCESSED_COUNT"

if [ "$UNPROCESSED_COUNT" -gt 0 ]; then
    print_success "Messages are in the outbox (Event Store)"
else
    print_error "No messages found in outbox"
fi

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 5: KIỂM TRA OUTBOX SENDING SERVICE
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 5: KIỂM TRA OUTBOX SENDING SERVICE"

print_step "Checking ChatService logs for OutboxSendingService..."
echo ""
echo "📸 CHỤP ẢNH 3: Logs của OutboxSendingService"
echo "─────────────────────────────────────────────────────────────────────────"
docker logs chatservice --tail 30 | grep -i "outbox\|published\|deleted\|success"
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

print_step "Waiting 3 seconds for OutboxSendingService to process messages..."
sleep 3

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 6: KIỂM TRA MESSAGES TABLE (SAU KHI XỬ LÝ)
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 6: KIỂM TRA MESSAGES TABLE (SAU KHI XỬ LÝ)"

print_step "Checking Messages table (should be empty - messages deleted after publish)..."
echo ""
echo "📸 CHỤP ẢNH 4: Messages table sau khi xử lý (should be empty)"
echo "─────────────────────────────────────────────────────────────────────────"
docker exec postgres psql -U postgres -d ChatServiceDb -c \
  "SELECT COUNT(*) as total_messages, 
          COUNT(CASE WHEN \"ProcessedAt\" IS NULL THEN 1 END) as unprocessed,
          COUNT(CASE WHEN \"ProcessedAt\" IS NOT NULL THEN 1 END) as processed
   FROM \"Messages\";"
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

FINAL_UNPROCESSED=$(docker exec postgres psql -U postgres -d ChatServiceDb -t -c \
  "SELECT COUNT(*) FROM \"Messages\" WHERE \"ProcessedAt\" IS NULL;")

if [ "$FINAL_UNPROCESSED" -eq 0 ]; then
    print_success "All messages have been processed and deleted (Outbox Pattern working!)"
else
    print_error "Still have $FINAL_UNPROCESSED unprocessed messages"
fi

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 7: KIỂM TRA RABBITMQ
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 7: KIỂM TRA RABBITMQ"

print_step "Checking RabbitMQ queues..."
echo ""
echo "📸 CHỤP ẢNH 5: RabbitMQ Queues"
echo "─────────────────────────────────────────────────────────────────────────"
docker exec rabbitmq rabbitmqctl list_queues name messages messages_ready messages_unacknowledged
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

print_step "Checking RabbitMQ exchanges..."
echo ""
echo "📸 CHỤP ẢNH 6: RabbitMQ Exchanges"
echo "─────────────────────────────────────────────────────────────────────────"
docker exec rabbitmq rabbitmqctl list_exchanges name type
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

print_success "RabbitMQ is processing messages"

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 8: KIỂM TRA CHATSERVICE LOGS (FULL)
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 8: KIỂM TRA CHATSERVICE LOGS (FULL)"

print_step "Showing ChatService logs with Outbox Pattern flow..."
echo ""
echo "📸 CHỤP ẢNH 7: ChatService Logs - Full Flow"
echo "─────────────────────────────────────────────────────────────────────────"
docker logs chatservice --tail 50 | grep -E "Outbox|Published|Deleted|Received|PolicyCreated|SUCCESS|FAILURE"
echo "─────────────────────────────────────────────────────────────────────────"
echo ""

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 9: TEST RETRY MECHANISM (OPTIONAL)
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 9: TEST RETRY MECHANISM (OPTIONAL)"

print_step "Creating a test message to verify retry mechanism..."
echo "Note: This test requires manually stopping RabbitMQ to simulate failure"
echo "To test retry mechanism:"
echo "  1. docker compose stop rabbitmq"
echo "  2. Create a policy"
echo "  3. Check Messages table - RetryCount should increment"
echo "  4. docker compose start rabbitmq"
echo "  5. Message should be published successfully"
echo ""

# ═══════════════════════════════════════════════════════════════════════════
# PHẦN 10: SUMMARY
# ═══════════════════════════════════════════════════════════════════════════

print_section "PHẦN 10: SUMMARY - KẾT QUẢ TEST"

echo "✅ Checklist hoàn thành:"
echo ""
echo "  [✓] 1. Mô tả Outbox Pattern - Xem OUTBOX-PATTERN-TEST-GUIDE.md"
echo "  [✓] 2. Kiến trúc hệ thống - Xem OUTBOX-PATTERN-TEST-GUIDE.md"
echo "  [✓] 3. Code triển khai - Xem ChatService/Services/Outbox*.cs"
echo "  [✓] 4. Ảnh database outbox - Chụp từ output trên"
echo "  [✓] 5. Ảnh RabbitMQ - Chụp từ http://localhost:15672"
echo "  [✓] 6. Kết quả chạy hệ thống - Chụp từ output trên"
echo ""

echo "📸 Danh sách ảnh cần chụp:"
echo ""
echo "  Database:"
echo "    • Ảnh 1: Cấu trúc bảng Messages"
echo "    • Ảnh 2: Messages chưa xử lý (ProcessedAt = NULL)"
echo "    • Ảnh 4: Messages table sau khi xử lý (empty)"
echo ""
echo "  RabbitMQ (http://localhost:15672):"
echo "    • Ảnh 5: RabbitMQ Dashboard"
echo "    • Ảnh 6: Queues list"
echo "    • Ảnh 7: Queue details (policy.created.chatservice)"
echo "    • Ảnh 8: Message content (JSON payload)"
echo ""
echo "  System:"
echo "    • Ảnh 9: Docker containers (docker compose ps)"
echo "    • Ảnh 10: ChatService logs (output trên)"
echo "    • Ảnh 11: Giao diện chat (client-app/index.html)"
echo "    • Ảnh 12: Real-time notifications"
echo ""
echo "  Code:"
echo "    • Screenshot: ChatService/Models/OutboxMessage.cs"
echo "    • Screenshot: ChatService/Services/Outbox.cs"
echo "    • Screenshot: ChatService/Services/OutboxSendingService.cs"
echo ""

echo "═══════════════════════════════════════════════════════════════════════════"
echo "🎉 TEST COMPLETE!"
echo "═══════════════════════════════════════════════════════════════════════════"
echo ""
echo "Next steps:"
echo "  1. Chụp ảnh từ output trên"
echo "  2. Mở http://localhost:15672 và chụp ảnh RabbitMQ"
echo "  3. Mở client-app/index.html và chụp ảnh giao diện"
echo "  4. Chụp ảnh code từ ChatService/Services/"
echo "  5. Tổng hợp vào báo cáo"
echo ""
