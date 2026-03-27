# ✅ PHẦN 15 - TEST HOÀN CHỈNH

## 🎯 Hệ thống đã sẵn sàng!

Tất cả services đã được setup và chạy thành công:

### 🔧 Services đang chạy:
- ✅ **RabbitMQ**: http://localhost:15672 (guest/guest)
- ✅ **Redis**: localhost:6379
- ✅ **PostgreSQL**: localhost:5432
- ✅ **AuthService**: http://localhost:5001
- ✅ **PolicyService**: http://localhost:5002
- ✅ **ChatService**: http://localhost:5003
- ✅ **PaymentService**: http://localhost:5004
- ✅ **PricingService**: http://localhost:5005
- ✅ **API Gateway**: http://localhost:8080

## 🧪 TEST SEQUENCE - PHẦN 15

### Bước 1: Test RabbitMQ
```bash
# Mở browser: http://localhost:15672
# Login: guest/guest
# ✅ Xác nhận: Dashboard hiển thị thành công
```

### Bước 2: Test PolicyService (Publish Event)
```bash
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Nguyen Van A"}'
```

**Kết quả mong đợi:**
```json
{
  "message": "Policy created successfully",
  "policy": {
    "number": "POL-20260321-XXXX",
    "customerName": "Nguyen Van A",
    "premium": 1500.00,
    "status": "Active",
    "createdAt": "2026-03-21T08:35:46.8427865Z"
  },
  "eventPublished": true
}
```

### Bước 3: Test ChatService (Agent Authentication)
```bash
curl -X POST http://localhost:5003/api/auth/agent-login \
  -H "Content-Type: application/json" \
  -d '{"agentId":"agent1","agentName":"Agent Smith"}'
```

**Kết quả mong đợi:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "agentId": "agent1",
  "agentName": "Agent Smith",
  "expiresAt": "2026-03-21T09:39:16.5776823Z"
}
```

### Bước 4: Test Client App (Real-time Notifications)

1. **Mở Client App:**
   ```bash
   open client-app/index.html
   # Hoặc mở file trong browser
   ```

2. **Connect Agent:**
   - Agent ID: `agent1`
   - Agent Name: `Agent Smith`
   - Click "Connect Agent"
   - ✅ Status hiển thị: "Connected as Agent Smith"

3. **Create Policy:**
   - Customer Name: `Test Customer`
   - Click "Create Policy (Trigger Event)"
   - ✅ Policy được tạo thành công
   - ✅ Notification hiển thị real-time

## 🔄 End-to-End Flow Test

### Test hoàn chỉnh theo thứ tự:

1. **RabbitMQ** ✅ - Message queue sẵn sàng
2. **PolicyService** ✅ - Tạo policy và publish event
3. **ChatService** ✅ - Nhận event từ RabbitMQ
4. **Client App** ✅ - Hiển thị notification real-time

### Kiểm tra RabbitMQ Queues:
1. Vào http://localhost:15672
2. Tab "Queues"
3. Tìm queue: `policy.created.chatservice`
4. ✅ Xác nhận: Queue được tạo và có consumer

## 🎮 Demo Script

```bash
# 1. Khởi động hệ thống
./start-system.sh

# 2. Test PolicyService
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Demo Customer"}'

# 3. Test ChatService
curl -X POST http://localhost:5003/api/auth/agent-login \
  -H "Content-Type: application/json" \
  -d '{"agentId":"demo-agent","agentName":"Demo Agent"}'

# 4. Mở Client App
open client-app/index.html
```

## 📊 Monitoring

### Xem logs:
```bash
# Tất cả services
docker compose logs -f

# Specific service
docker compose logs -f policyservice
docker compose logs -f chatservice
docker compose logs -f rabbitmq
```

### Kiểm tra containers:
```bash
docker compose ps
```

## ✅ Kết quả thành công

Khi tất cả test pass, bạn sẽ thấy:

1. **RabbitMQ**: Management UI accessible, queues hoạt động
2. **PolicyService**: 
   - ✅ API tạo policy thành công
   - ✅ Event được publish lên RabbitMQ
3. **ChatService**: 
   - ✅ Agent authentication thành công
   - ✅ WebSocket connection established
   - ✅ Event subscriber nhận message từ RabbitMQ
4. **Client App**: 
   - ✅ Agent chat real-time
   - ✅ Policy creation notifications real-time
   - ✅ UI responsive và user-friendly

## 🛑 Dừng hệ thống

```bash
docker compose down
```

---

## 🎉 HOÀN THÀNH PHẦN 15!

Hệ thống microservices với:
- ✅ Message Queue (RabbitMQ)
- ✅ Event-driven Architecture
- ✅ Real-time Notifications (SignalR)
- ✅ Agent Chat System
- ✅ Policy Management
- ✅ End-to-end Integration

**Chúc mừng! Bạn đã hoàn thành thành công test Part 15! 🚀**