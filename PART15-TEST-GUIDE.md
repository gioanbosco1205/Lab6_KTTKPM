# PHẦN 15 - TEST HƯỚNG DẪN CHI TIẾT

## 🎯 Mục tiêu Test
Test toàn bộ hệ thống theo thứ tự:
1. **RabbitMQ** - Message Queue
2. **PolicyService** - Tạo policy và publish event
3. **ChatService** - Nhận event và thông báo real-time
4. **Client App** - Hiển thị notification real-time

## 🚀 Bước 1: Khởi động hệ thống

```bash
# Chạy script tự động
./start-system.sh

# Hoặc chạy thủ công
docker-compose up --build -d
```

## 🔍 Bước 2: Kiểm tra các service

### 2.1 RabbitMQ Management
- URL: http://localhost:15672
- Username: `guest`
- Password: `guest`
- ✅ Kiểm tra: Có thể đăng nhập và thấy dashboard

### 2.2 Các API Services
```bash
# Auth Service
curl http://localhost:5001/health

# Policy Service  
curl http://localhost:5002/health

# Chat Service
curl http://localhost:5003/health

# Payment Service
curl http://localhost:5004/health

# Pricing Service
curl http://localhost:5005/health

# API Gateway
curl http://localhost:5000/health
```

## 🧪 Bước 3: Test theo thứ tự Part 15

### 3.1 Test RabbitMQ
1. Mở RabbitMQ Management: http://localhost:15672
2. Đăng nhập với `guest/guest`
3. ✅ Xác nhận: RabbitMQ đang chạy và có thể truy cập

### 3.2 Test PolicyService
```http
POST http://localhost:5002/api/policy
Content-Type: application/json

{
  "policyNumber": "POL-2024-001",
  "customerName": "Nguyen Van A",
  "policyType": "Life Insurance",
  "premium": 1000000,
  "startDate": "2024-01-01",
  "endDate": "2025-01-01"
}
```
✅ Xác nhận: 
- Policy được tạo thành công
- Event được publish lên RabbitMQ
- Kiểm tra trong RabbitMQ Management có queue và message

### 3.3 Test ChatService
```http
POST http://localhost:5003/api/auth/agent-login
Content-Type: application/json

{
  "agentId": "agent1",
  "agentName": "Agent Smith"
}
```
✅ Xác nhận:
- Agent đăng nhập thành công
- Nhận được JWT token
- ChatService đã subscribe RabbitMQ queue

### 3.4 Test Client App
1. Mở file `client-app/index.html` trong browser
2. Nhập Agent ID: `agent1`, Agent Name: `Agent Smith`
3. Click "Connect Agent"
4. ✅ Xác nhận: Status hiển thị "Connected"

## 🎮 Bước 4: Test End-to-End Flow

### 4.1 Agent Chat với nhau
1. Mở 2 tab browser với `client-app/index.html`
2. Tab 1: Connect với `agent1` / `Agent Smith`
3. Tab 2: Connect với `agent2` / `Agent Johnson`
4. Gửi message từ agent1 đến agent2
5. ✅ Xác nhận: Message hiển thị real-time ở cả 2 tab

### 4.2 Bán Policy (Trigger Event)
1. Trong Client App, điền thông tin policy:
   - Policy Number: `POL-2024-002`
   - Customer Name: `Test Customer`
   - Policy Type: `Health Insurance`
   - Premium: `2000000`
2. Click "Create Policy (Trigger Event)"
3. ✅ Xác nhận: 
   - Policy được tạo thành công
   - Thông báo hiển thị trong "Policy Management"

### 4.3 ChatService nhận Event
1. Sau khi tạo policy, kiểm tra RabbitMQ Management
2. ✅ Xác nhận:
   - Message được consume từ queue
   - ChatService log hiển thị đã nhận event

### 4.4 Client nhận Notification Real-time
1. Sau khi tạo policy, kiểm tra "Real-time Notifications" panel
2. ✅ Xác nhận:
   - Notification hiển thị ngay lập tức
   - Format: "Policy Event: Policy POL-2024-002 created for Test Customer"

## 📊 Bước 5: Monitoring và Debug

### 5.1 Xem Logs
```bash
# Xem tất cả logs
docker-compose logs -f

# Xem log specific service
docker-compose logs -f policyservice
docker-compose logs -f chatservice
docker-compose logs -f rabbitmq
```

### 5.2 Kiểm tra RabbitMQ Queues
1. Vào RabbitMQ Management: http://localhost:15672
2. Tab "Queues" - kiểm tra:
   - `policy.created` queue
   - Message count
   - Consumer count

### 5.3 Test API với HTTP file
Sử dụng `test-part15.http` để test từng endpoint:
```bash
# Nếu dùng VS Code với REST Client extension
# Mở test-part15.http và click "Send Request"
```

## ✅ Kết quả mong đợi

### Thành công khi:
1. **RabbitMQ**: Management UI accessible, queues created
2. **PolicyService**: 
   - API tạo policy thành công
   - Event published to RabbitMQ
3. **ChatService**: 
   - Agent authentication thành công
   - WebSocket connection established
   - Event subscriber nhận được message từ RabbitMQ
4. **Client App**: 
   - Agent chat real-time
   - Policy creation notifications real-time
   - UI responsive và user-friendly

### Troubleshooting

#### Lỗi thường gặp:
1. **Port conflicts**: Đổi port trong docker-compose.yml
2. **Service not ready**: Tăng thời gian wait trong start-system.sh
3. **CORS issues**: Kiểm tra CORS config trong ChatService
4. **WebSocket connection failed**: Kiểm tra JWT token và SignalR config

#### Debug commands:
```bash
# Kiểm tra container status
docker-compose ps

# Restart specific service
docker-compose restart chatservice

# Rebuild và restart
docker-compose up --build -d chatservice

# Xem network connectivity
docker network ls
docker network inspect kttkpm_microservices-network
```

## 🎉 Hoàn thành Test Part 15

Khi tất cả các bước trên đều pass, bạn đã hoàn thành thành công test Part 15:
- ✅ RabbitMQ message queue hoạt động
- ✅ PolicyService publish events
- ✅ ChatService consume events  
- ✅ Client App hiển thị real-time notifications
- ✅ Agent chat system hoạt động
- ✅ End-to-end flow từ policy creation đến notification

## 🛑 Dừng hệ thống

```bash
# Dừng tất cả services
docker-compose down

# Dừng và xóa volumes (reset data)
docker-compose down -v
```