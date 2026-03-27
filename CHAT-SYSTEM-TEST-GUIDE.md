# 💬 HƯỚNG DẪN TEST HỆ THỐNG CHAT HOÀN CHỈNH

## 🎯 Tính năng đã hoàn thành:

### ✅ **Real-time Chat**
- Group chat (public messages)
- Private messages (1-on-1)
- Agent status tracking (Online/Offline)
- Policy notifications

### ✅ **Chat History Database**
- Lưu tất cả messages vào PostgreSQL
- API để lấy lịch sử chat
- Unread message tracking
- Agent status persistence

## 🚀 **CÁCH TEST:**

### **Bước 1: Khởi động hệ thống**
```bash
./start-system.sh
```

**Kết quả mong đợi:**
- ✅ Tất cả 9 services đang chạy
- ✅ Database migration thành công
- ✅ "Ready to test!" message

### **Bước 2: Mở Client App**
```bash
open client-app/index.html
```

**Hoặc mở trực tiếp trong browser:** `file:///.../client-app/index.html`

### **Bước 3: Test Real-time Chat**

#### **3.1 Test Group Chat:**
1. **Tab 1**: 
   - Agent ID: `agent1`
   - Agent Name: `Agent Smith`
   - Click "Connect Agent"

2. **Tab 2** (incognito mode):
   - Agent ID: `agent2` 
   - Agent Name: `Agent Johnson`
   - Click "Connect Agent"

3. **Gửi group messages:**
   - Tab 1: Type "Hello from Agent Smith" → Send
   - Tab 2: Type "Hi from Agent Johnson" → Send
   - **Kết quả**: Messages xuất hiện real-time ở cả 2 tabs

#### **3.2 Test Private Messages:**
1. **Kiểm tra Online Agents:**
   - Cả 2 tabs đều thấy agent khác trong "Online Agents"

2. **Gửi private message:**
   - Tab 1: Select "agent2" → Type "Private hello!" → Send Private
   - Tab 2: Thấy message từ Agent Smith trong "Private Chat"

3. **Reply private message:**
   - Tab 2: Select "agent1" → Type "Private reply!" → Send Private
   - Tab 1: Thấy reply trong "Private Chat"

#### **3.3 Test Policy Notifications:**
1. **Tạo policy:**
   - Customer Name: "Test Customer"
   - Click "Create Policy (Trigger Event)"

2. **Kết quả:**
   - Policy được tạo thành công
   - Notification xuất hiện real-time ở tất cả connected agents

### **Bước 4: Test Chat History Database**

#### **4.1 Test trong Client App:**
1. **Load Group History:**
   - Click "Load Group History"
   - **Kết quả**: Hiển thị tất cả group messages từ database

2. **Load Private History:**
   - Select agent trong dropdown
   - Click "Load Private History"  
   - **Kết quả**: Hiển thị conversation giữa 2 agents

3. **Check Unread Count:**
   - Click "Check Unread"
   - **Kết quả**: Hiển thị số messages chưa đọc

#### **4.2 Test API trực tiếp:**
```bash
# 1. Authenticate
curl -X POST http://localhost:5003/api/auth/agent-login \
  -H "Content-Type: application/json" \
  -d '{"agentId":"agent1","agentName":"Agent Smith"}'

# 2. Get group messages (thay JWT_TOKEN)
curl -X GET "http://localhost:5003/api/chathistory/group-messages?limit=10" \
  -H "Authorization: Bearer JWT_TOKEN"

# 3. Get unread count
curl -X GET "http://localhost:5003/api/chathistory/unread-count" \
  -H "Authorization: Bearer JWT_TOKEN"
```

#### **4.3 Kiểm tra Database trực tiếp:**
```bash
# Xem messages trong database
docker exec -it postgres psql -U postgres -d ChatServiceDb -c "SELECT * FROM \"ChatMessages\";"

# Xem agents trong database  
docker exec -it postgres psql -U postgres -d ChatServiceDb -c "SELECT * FROM \"Agents\";"
```

## 🔍 **KẾT QUẢ MONG ĐỢI:**

### ✅ **Real-time Chat:**
- Messages xuất hiện ngay lập tức
- Online agents list update real-time
- Private messages chỉ người nhận thấy
- Policy notifications broadcast tới tất cả

### ✅ **Chat History:**
- Messages được lưu vào database ngay khi gửi
- API trả về đúng messages với timestamp
- Unread count chính xác
- Agent status được track

### ✅ **Database:**
```sql
-- ChatMessages table có data
Id | SenderId | SenderName | Message | MessageType | CreatedAt | IsRead
1  | agent1   | Agent Smith| Hello   | 1          | 2024-...  | false

-- Agents table có data  
Id | AgentId | AgentName   | Status | LastSeen  | CreatedAt
1  | agent1  | Agent Smith | 1      | 2024-...  | 2024-...
```

## 🎮 **DEMO SCRIPT NHANH:**

```bash
# Terminal 1: Start system
./start-system.sh

# Browser: Mở 2 tabs với client-app/index.html
# Tab 1: Connect agent1
# Tab 2: Connect agent2  
# Gửi messages qua lại
# Test private messages
# Click "Load Group History" để xem database
# Tạo policy để test notifications
```

## 🔧 **Troubleshooting:**

### **Lỗi kết nối:**
```bash
# Check services status
docker compose ps

# Check logs
docker compose logs chatservice
```

### **Database issues:**
```bash
# Check database connection
docker exec -it postgres psql -U postgres -d ChatServiceDb -c "\dt"
```

### **SignalR connection issues:**
- Kiểm tra browser console (F12)
- Đảm bảo JWT token hợp lệ
- Check CORS settings

## 📊 **SERVICES ENDPOINTS:**

- **API Gateway**: http://localhost:8080
- **Auth Service**: http://localhost:5001  
- **Policy Service**: http://localhost:5002
- **Chat Service**: http://localhost:5003
- **Payment Service**: http://localhost:5004
- **Pricing Service**: http://localhost:5005
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## 🎯 **TÍNH NĂNG HOÀN CHỈNH:**

### ✅ **Đã implement:**
- [x] Real-time group chat
- [x] Private messages 1-on-1
- [x] Agent status tracking
- [x] Policy event notifications
- [x] Message persistence in PostgreSQL
- [x] Chat history API
- [x] Unread message tracking
- [x] JWT authentication
- [x] Web UI for testing
- [x] Docker containerization

### 🚀 **Có thể mở rộng:**
- Message encryption
- File sharing
- Group private messages
- Message search
- Read receipts
- Typing indicators

---

## 🎉 **HOÀN THÀNH!**

**Hệ thống chat đã sẵn sàng sử dụng với đầy đủ tính năng real-time và database persistence!**