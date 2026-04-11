# 🚀 HỆ THỐNG MICROSERVICES - HƯỚNG DẪN CHẠY

## 📋 Yêu cầu
- Docker Desktop đã cài đặt và đang chạy
- Port 5001-5005, 8080, 8761, 5672, 15672, 5432, 6379 chưa bị sử dụng

---

## ⚡ CÁCH CHẠY (3 BƯỚC)

### Bước 1: Khởi động hệ thống
```bash
docker compose up -d
```

Đợi khoảng 30-60 giây để tất cả services khởi động.

### Bước 2: Kiểm tra trạng thái
```bash
docker compose ps
```

Tất cả services phải có status "Up" hoặc "Up (healthy)".

### Bước 3: Chạy test tự động
```bash
./FINAL-DOCKER-TEST.sh
```

---

## 🌐 GIAO DIỆN WEB

Mở các URL sau trong trình duyệt:

### 1. Giao diện Chat (Client App) ⭐
```
Mở file: client-app/index.html
```
Hoặc chạy:
```bash
open client-app/index.html  # macOS
start client-app/index.html # Windows
xdg-open client-app/index.html # Linux
```

**Chức năng:**
- 👥 Agent Chat - Chat giữa các agents
- 📋 Policy Management - Tạo/Terminate/Activate policies
- 💬 Private Messages - Tin nhắn riêng tư
- 📚 Chat History - Lịch sử chat từ database
- 🔔 Real-time Notifications - Thông báo real-time qua SignalR

**Cách sử dụng:**
1. Nhập Agent ID (vd: agent1) và Agent Name
2. Click "Connect Agent"
3. Tạo policy để xem notifications real-time
4. Chat với agents khác
5. Xem chat history

### 2. Eureka Server (Service Discovery)
```
http://localhost:8761
```
- Xem danh sách các services đã đăng ký
- Theo dõi trạng thái services

### 3. RabbitMQ Management
```
http://localhost:15672
```
- Username: `guest`
- Password: `guest`
- Xem queues, exchanges, messages
- Theo dõi message flow

### 4. API Gateway
```
http://localhost:8080
```
- Gateway chính để gọi các services

---

## 🧪 TEST NHANH

### ⭐ Test Outbox Pattern (Đầy đủ)
```bash
./test-outbox-complete.sh
```
Xem hướng dẫn chi tiết: `OUTBOX-PATTERN-TEST-GUIDE.md`  
Checklist chụp ảnh: `SCREENSHOT-CHECKLIST.md`

### Test PolicyService qua Gateway
```bash
curl -X POST http://localhost:8080/policy-service/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Nguyen Van A"}'
```

### Test trực tiếp PolicyService
```bash
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Nguyen Van B"}'
```

### Test ChatService

#### 1. Kiểm tra ChatService nhận event từ RabbitMQ
```bash
docker logs chatservice --tail 20
```

Bạn sẽ thấy log:
```
Received PolicyCreated event: POL-XXXXXXXX
SignalR notifications sent for policy: POL-XXXXXXXX
```

#### 2. Test Outbox Pattern
```bash
# Publish event qua ChatService
curl -X POST http://localhost:5003/api/event/publish-policy-created \
  -H "Content-Type: application/json" \
  -d '{"policyNumber": "POL-TEST-001", "premiumAmount": 1500.00}'

# Đợi 2 giây để outbox xử lý
sleep 2

# Kiểm tra trạng thái outbox (should be 0 unprocessed)
curl http://localhost:5003/api/event/outbox/status
```

#### 3. Test SignalR Hub
Mở file HTML sau trong trình duyệt để test real-time notifications:

```html
<!DOCTYPE html>
<html>
<head>
    <title>ChatService SignalR Test</title>
    <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
</head>
<body>
    <h1>ChatService SignalR Test</h1>
    <div id="messages"></div>
    
    <script>
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5003/hubs/agentchat")
            .build();

        connection.on("ReceivePolicyCreated", (message) => {
            const div = document.getElementById("messages");
            div.innerHTML += `<p>Policy Created: ${JSON.stringify(message)}</p>`;
        });

        connection.start()
            .then(() => console.log("Connected to SignalR"))
            .catch(err => console.error(err));
    </script>
</body>
</html>
```

Sau đó tạo policy mới, bạn sẽ thấy notification real-time trong trình duyệt.

#### 4. Test Event Flow hoàn chỉnh
```bash
# 1. Tạo policy từ PolicyService
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test Event Flow"}'

# 2. Đợi 2 giây
sleep 2

# 3. Kiểm tra ChatService logs
docker logs chatservice --tail 10 | grep -i "policy\|received"

# 4. Kiểm tra RabbitMQ queues
docker exec rabbitmq rabbitmqctl list_queues name messages
```

---

## 📊 DANH SÁCH SERVICES

| Service | Port | URL | Mô tả |
|---------|------|-----|-------|
| Eureka Server | 8761 | http://localhost:8761 | Service Discovery UI |
| RabbitMQ UI | 15672 | http://localhost:15672 | Message Queue Management |
| API Gateway | 8080 | http://localhost:8080 | Ocelot Gateway |
| AuthService | 5001 | http://localhost:5001 | Authentication |
| PolicyService | 5002 | http://localhost:5002 | Policy Management |
| ChatService | 5003 | http://localhost:5003 | Chat + SignalR |
| PaymentService | 5004 | http://localhost:5004 | Payment Processing |
| PricingService | 5005 | http://localhost:5005 | Pricing Calculation |

---

## 🔍 XEM LOGS

### Xem logs của một service
```bash
docker logs chatservice -f
docker logs policyservice -f
docker logs apigateway -f
```

### Xem logs tất cả services
```bash
docker compose logs -f
```

---

## 🛑 DỪNG HỆ THỐNG

### Dừng tất cả services
```bash
docker compose down
```

### Dừng và xóa volumes (xóa data)
```bash
docker compose down -v
```

---

## 🔄 KHỞI ĐỘNG LẠI

### Khởi động lại một service
```bash
docker compose restart chatservice
```

### Khởi động lại tất cả
```bash
docker compose restart
```

---

## 📝 KIẾN TRÚC HỆ THỐNG

```
┌─────────────────────────────────────────────────────┐
│                  API Gateway (8080)                  │
│                    Ocelot + Redis                    │
└──────────────────┬──────────────────────────────────┘
                   │
        ┌──────────┼──────────┬──────────┬──────────┐
        │          │          │          │          │
   ┌────▼───┐ ┌───▼────┐ ┌──▼─────┐ ┌──▼──────┐ ┌─▼────────┐
   │ Auth   │ │ Policy │ │  Chat  │ │ Payment │ │ Pricing  │
   │ :5001  │ │ :5002  │ │ :5003  │ │ :5004   │ │ :5005    │
   └────────┘ └───┬────┘ └───┬────┘ └─────────┘ └──────────┘
                  │           │
                  │    ┌──────▼──────┐
                  │    │  RabbitMQ   │
                  │    │   :5672     │
                  │    └─────────────┘
                  │
           ┌──────▼──────┐
           │ PostgreSQL  │
           │   :5432     │
           └─────────────┘

┌─────────────────┐
│ Eureka Server   │
│     :8761       │
└─────────────────┘
```

---

## ✅ KIỂM TRA HỆ THỐNG HOẠT ĐỘNG

Chạy script test tự động:
```bash
./FINAL-DOCKER-TEST.sh
```

Kết quả mong đợi:
- ✅ Eureka Server running
- ✅ RabbitMQ queues created
- ✅ PolicyService creates policy
- ✅ ChatService receives event
- ✅ SignalR notifications sent
- ✅ API Gateway routing works

---

## 🆘 TROUBLESHOOTING

### Lỗi: Port already in use
```bash
# Kiểm tra port đang được sử dụng
lsof -i :8080
lsof -i :5672

# Dừng process đang dùng port hoặc đổi port trong docker-compose.yml
```

### Lỗi: Service không khởi động
```bash
# Xem logs để tìm lỗi
docker logs <service-name>

# Ví dụ:
docker logs chatservice
docker logs policyservice
```

### Lỗi: Cannot connect to Docker daemon
```bash
# Khởi động Docker Desktop
# Hoặc chạy:
sudo systemctl start docker  # Linux
open -a Docker              # macOS
```

---

## 📞 SUPPORT

Nếu gặp vấn đề, kiểm tra:
1. Docker Desktop đang chạy
2. Tất cả ports chưa bị sử dụng
3. Đủ RAM (tối thiểu 4GB)
4. Logs của services: `docker compose logs`

---

**Ngày cập nhật**: 2026-04-11  
**Trạng thái**: ✅ Production Ready
