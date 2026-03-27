# 🏢 Insurance System - Microservices với Chat System

## 🎯 Tính năng chính:

### ✅ **Microservices Architecture**
- API Gateway (Ocelot)
- Auth Service (JWT)
- Policy Service (Business Logic)
- Payment Service (EF Core + PostgreSQL)
- Pricing Service
- Chat Service (SignalR + PostgreSQL)

### ✅ **Real-time Chat System**
- **Group Chat**: Public messages tới tất cả agents
- **Private Messages**: 1-on-1 chat giữa agents
- **Agent Status**: Online/Offline tracking
- **Policy Notifications**: Real-time events từ PolicyService

### ✅ **Chat History Database**
- **Message Persistence**: Lưu tất cả messages vào PostgreSQL
- **Chat History API**: RESTful endpoints để lấy lịch sử
- **Unread Tracking**: Đếm messages chưa đọc
- **Agent Management**: Track agent status và thông tin

### ✅ **Event-Driven Architecture**
- **RabbitMQ**: Message broker cho inter-service communication
- **Policy Events**: Khi tạo policy → broadcast tới ChatService
- **Real-time Notifications**: SignalR push tới connected clients

## 🚀 **Quick Start:**

### 1. Khởi động hệ thống:
```bash
./start-system.sh
```

### 2. Test Chat System:
```bash
open client-app/index.html
```

### 3. Xem hướng dẫn chi tiết:
- **[CHAT-SYSTEM-TEST-GUIDE.md](CHAT-SYSTEM-TEST-GUIDE.md)** - Hướng dẫn test đầy đủ
- **[test-chat-history.http](test-chat-history.http)** - Test API endpoints

## 📊 **Services & Ports:**

| Service | Port | Description |
|---------|------|-------------|
| API Gateway | 8080 | Entry point, routing |
| Auth Service | 5001 | JWT authentication |
| Policy Service | 5002 | Business logic, events |
| Chat Service | 5003 | SignalR, chat history |
| Payment Service | 5004 | EF Core, PostgreSQL |
| Pricing Service | 5005 | Pricing calculations |
| PostgreSQL | 5432 | Database |
| RabbitMQ | 5672, 15672 | Message broker |
| Redis | 6379 | Caching |

## 🎮 **Demo Flow:**

1. **Connect Agents**: Mở 2 browser tabs, connect 2 agents khác nhau
2. **Group Chat**: Gửi public messages, thấy real-time ở cả 2 tabs
3. **Private Messages**: Gửi private messages giữa 2 agents
4. **Policy Events**: Tạo policy → thấy notification real-time
5. **Chat History**: Load messages từ database
6. **Database**: Kiểm tra PostgreSQL có lưu messages

## 🔧 **Tech Stack:**

- **.NET 8**: Microservices framework
- **SignalR**: Real-time communication
- **Entity Framework Core**: ORM cho database
- **PostgreSQL**: Primary database
- **RabbitMQ**: Message broker
- **Redis**: Caching layer
- **Docker**: Containerization
- **Ocelot**: API Gateway
- **JWT**: Authentication

## 📁 **Project Structure:**

```
├── ApiGateway/          # API Gateway với Ocelot
├── AuthService/         # JWT Authentication
├── PolicyService/       # Business logic + RabbitMQ publisher
├── ChatService/         # SignalR + Chat History + RabbitMQ subscriber
├── PaymentService/      # EF Core + PostgreSQL
├── PricingService/      # Pricing calculations
├── client-app/          # Web UI cho testing
├── docker-compose.yml   # Container orchestration
└── start-system.sh      # Quick start script
```

## 🎯 **Key Features Implemented:**

### ✅ **Chat System:**
- [x] Real-time group chat
- [x] Private messages 1-on-1
- [x] Agent status tracking
- [x] Message persistence
- [x] Chat history API
- [x] Unread message tracking

### ✅ **Microservices:**
- [x] Service discovery
- [x] API Gateway routing
- [x] JWT authentication
- [x] Event-driven communication
- [x] Database per service
- [x] Docker containerization

### ✅ **Real-time Features:**
- [x] SignalR WebSocket connections
- [x] Policy event notifications
- [x] Agent online/offline status
- [x] Instant message delivery

---

## 🎉 **Hệ thống hoàn chỉnh và sẵn sàng sử dụng!**

**Xem [CHAT-SYSTEM-TEST-GUIDE.md](CHAT-SYSTEM-TEST-GUIDE.md) để test đầy đủ tất cả tính năng.**
