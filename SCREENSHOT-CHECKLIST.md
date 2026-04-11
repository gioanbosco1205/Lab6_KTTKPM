# 📸 CHECKLIST CHỤP ẢNH - OUTBOX PATTERN

## 🎯 Mục tiêu
Chụp đầy đủ ảnh để chứng minh Outbox Pattern hoạt động theo yêu cầu:
- ✅ Mô tả Outbox Pattern
- ✅ Kiến trúc hệ thống
- ✅ Code triển khai
- ✅ Ảnh chụp database outbox
- ✅ Ảnh chụp RabbitMQ message
- ✅ Kết quả chạy hệ thống

---

## 📋 DANH SÁCH ẢNH CẦN CHỤP

### PHẦN 1: DATABASE (4 ảnh)

#### ✅ Ảnh 1: Cấu trúc bảng Messages (Event Store)
**Lệnh:**
```bash
docker exec postgres psql -U postgres -d ChatServiceDb -c "\d \"Messages\""
```
**Nội dung cần thấy:**
- Các cột: Id, Type, Payload, CreatedAt, ProcessedAt, RetryCount, LastRetryAt
- Kiểu dữ liệu của từng cột
- Primary key, indexes

---

#### ✅ Ảnh 2: Messages chưa xử lý (ProcessedAt = NULL)
**Lệnh:**
```bash
# Tạo policy trước
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test Outbox"}'

# Đợi 1 giây
sleep 1

# Xem messages (NHANH - trước khi bị xóa!)
docker exec postgres psql -U postgres -d ChatServiceDb -c \
  "SELECT \"Id\", \"Type\", \"CreatedAt\", \"ProcessedAt\", \"RetryCount\" 
   FROM \"Messages\" 
   WHERE \"ProcessedAt\" IS NULL 
   ORDER BY \"Id\" DESC LIMIT 5;"
```
**Nội dung cần thấy:**
- Messages với ProcessedAt = NULL
- Type = "PolicyService.Events.PolicyCreated"
- RetryCount = 0
- CreatedAt timestamp

---

#### ✅ Ảnh 3: ChatService Logs - OutboxSendingService
**Lệnh:**
```bash
docker logs chatservice --tail 30 | grep -i "outbox\|published\|deleted"
```
**Nội dung cần thấy:**
- "OutboxSendingService started"
- "Published message X"
- "Deleted message X"
- "✅ SUCCESS"

---

#### ✅ Ảnh 4: Messages table sau khi xử lý (empty)
**Lệnh:**
```bash
# Đợi 3 giây để OutboxSendingService xử lý
sleep 3

# Kiểm tra lại
docker exec postgres psql -U postgres -d ChatServiceDb -c \
  "SELECT COUNT(*) as total_messages, 
          COUNT(CASE WHEN \"ProcessedAt\" IS NULL THEN 1 END) as unprocessed,
          COUNT(CASE WHEN \"ProcessedAt\" IS NOT NULL THEN 1 END) as processed
   FROM \"Messages\";"
```
**Nội dung cần thấy:**
- total_messages = 0 (hoặc rất ít)
- unprocessed = 0
- Chứng minh messages đã được xóa sau khi publish

---

### PHẦN 2: RABBITMQ (4 ảnh)

#### ✅ Ảnh 5: RabbitMQ Dashboard
**URL:** http://localhost:15672
**Login:** guest / guest

**Nội dung cần thấy:**
- Overview dashboard
- Connections, Channels, Queues
- Message rates

---

#### ✅ Ảnh 6: Queues List
**URL:** http://localhost:15672/#/queues

**Nội dung cần thấy:**
- policy.created.chatservice
- policy.terminated.chatservice
- product.activated.chatservice
- Message counts

---

#### ✅ Ảnh 7: Queue Details
**URL:** http://localhost:15672/#/queues/%2F/policy.created.chatservice

**Nội dung cần thấy:**
- Queue name
- Messages ready/unacknowledged
- Message rate
- Consumers

---

#### ✅ Ảnh 8: Message Content (JSON)
**Cách chụp:**
1. Vào queue "policy.created.chatservice"
2. Click tab "Get messages"
3. Click "Get Message(s)"
4. Xem JSON payload

**Nội dung cần thấy:**
```json
{
  "policyNumber": "POL-XXXXXXXX",
  "customerName": "Test Outbox",
  "premiumAmount": 1000.00,
  "createdAt": "2026-04-11T..."
}
```

---

### PHẦN 3: SYSTEM (4 ảnh)

#### ✅ Ảnh 9: Docker Containers
**Lệnh:**
```bash
docker compose ps
```
**Nội dung cần thấy:**
- Tất cả containers đang chạy (Up/healthy)
- eureka, rabbitmq, postgres, redis
- authservice, policyservice, chatservice, paymentservice, pricingservice
- apigateway

---

#### ✅ Ảnh 10: ChatService Logs - Full Flow
**Lệnh:**
```bash
docker logs chatservice --tail 50
```
**Nội dung cần thấy:**
- "Received PolicyCreated event"
- "OutboxSendingService started"
- "Published and deleted message"
- "SignalR notifications sent"

---

#### ✅ Ảnh 11: Giao diện Chat
**Cách mở:**
```bash
open client-app/index.html  # macOS
start client-app/index.html # Windows
```

**Nội dung cần thấy:**
- Agent Chat interface
- Policy Management buttons
- Chat history
- Connection status

---

#### ✅ Ảnh 12: Real-time Notifications
**Cách test:**
1. Mở client-app/index.html
2. Connect với Agent ID (vd: agent1)
3. Tạo policy:
```bash
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test Notification"}'
```
4. Xem toast notification xuất hiện

**Nội dung cần thấy:**
- Toast notification: "Policy Created: POL-XXXXXXXX"
- Real-time update trong chat

---

### PHẦN 4: CODE (4 ảnh)

#### ✅ Ảnh 13: OutboxMessage.cs
**File:** `ChatService/Models/OutboxMessage.cs`

**Nội dung cần thấy:**
- Class definition
- Properties: Id, Type, JsonPayload, CreatedAt, ProcessedAt
- Constructor
- RecreateEvent() method

---

#### ✅ Ảnh 14: Outbox.cs
**File:** `ChatService/Services/Outbox.cs`

**Nội dung cần thấy:**
- ReadMessagesAsync() - Đọc messages từ database
- PublishMessageAsync() - Publish to RabbitMQ
- DeleteMessageAsync() - Xóa message sau khi publish
- Retry mechanism

---

#### ✅ Ảnh 15: OutboxSendingService.cs
**File:** `ChatService/Services/OutboxSendingService.cs`

**Nội dung cần thấy:**
- IHostedService implementation
- Timer chạy mỗi 1 giây
- PushMessages() method
- Error handling và retry logic

---

#### ✅ Ảnh 16: Program.cs - Registration
**File:** `ChatService/Program.cs`

**Nội dung cần thấy:**
```csharp
builder.Services.AddScoped<IOutbox, Outbox>();
builder.Services.AddHostedService<OutboxSendingService>();
```

---

## 🚀 CÁCH CHẠY TEST

### Bước 1: Khởi động hệ thống
```bash
docker compose up -d
```

### Bước 2: Chạy test script
```bash
./test-outbox-complete.sh
```

### Bước 3: Chụp ảnh theo checklist trên

### Bước 4: Tổng hợp vào báo cáo

---

## 📊 SUMMARY

Tổng cộng cần chụp: **16 ảnh**

- **Database:** 4 ảnh
- **RabbitMQ:** 4 ảnh
- **System:** 4 ảnh
- **Code:** 4 ảnh

---

## ✅ VERIFICATION

Sau khi chụp xong, kiểm tra:

- [ ] Tất cả 16 ảnh đã được chụp
- [ ] Ảnh rõ nét, đọc được text
- [ ] Có timestamp/date trong ảnh
- [ ] Có thể thấy flow: Save → Process → Publish → Delete
- [ ] Logs cho thấy "SUCCESS" và "Deleted"
- [ ] RabbitMQ có messages
- [ ] Giao diện chat có notifications

---

**Ngày tạo:** 2026-04-11  
**Trạng thái:** ✅ Ready to use
