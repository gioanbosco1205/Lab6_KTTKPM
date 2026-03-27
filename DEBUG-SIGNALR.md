# 🔧 DEBUG SIGNALR CONNECTION

## 🎯 Bước debug từng bước:

### 1. Mở Test SignalR đơn giản
```
http://localhost:3000/../test-signalr.html
```

### 2. Click "Test Connection" và xem logs

### 3. Nếu có lỗi, kiểm tra Browser Console (F12)

### 4. Kiểm tra ChatService logs:
```bash
docker compose logs -f chatservice
```

## 🔍 Các lỗi thường gặp:

### Lỗi CORS:
```
Access to fetch at 'http://localhost:5003/api/auth/agent-login' from origin 'http://localhost:3000' has been blocked by CORS policy
```
**Giải pháp**: Đã fix CORS trong ChatService

### Lỗi WebSocket:
```
WebSocket connection to 'ws://localhost:5003/chathub' failed
```
**Giải pháp**: Kiểm tra SignalR hub endpoint

### Lỗi Authentication:
```
Failed to authenticate agent: 401
```
**Giải pháp**: Kiểm tra JWT token

## 🧪 Test từng bước:

### Bước 1: Test Auth API
```bash
curl -X POST http://localhost:5003/api/auth/agent-login \
  -H "Content-Type: application/json" \
  -d '{"agentId":"test","agentName":"Test"}'
```

### Bước 2: Test SignalR Hub
Mở browser console và chạy:
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5003/chathub")
    .build();

connection.start().then(() => {
    console.log("Connected!");
}).catch(err => {
    console.error("Connection failed:", err);
});
```

### Bước 3: Test với Authentication
```javascript
// Get token first
const response = await fetch('http://localhost:5003/api/auth/agent-login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({agentId: 'test', agentName: 'Test'})
});
const data = await response.json();

// Connect with token
const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5003/chathub", {
        accessTokenFactory: () => data.token
    })
    .build();

await connection.start();
await connection.invoke("JoinAgentGroup", "test");
```

## 📊 Monitoring

### ChatService logs:
```bash
docker compose logs -f chatservice
```

### All services status:
```bash
docker compose ps
```

### Network connectivity:
```bash
curl -v http://localhost:5003/api/auth/agent-login
```

## 🎯 Expected Success Flow:

1. ✅ Auth API returns JWT token
2. ✅ SignalR connection established
3. ✅ JoinAgentGroup method called successfully
4. ✅ Client receives "Joined as agent X" notification
5. ✅ Status shows "Connected as Agent Name"

## 🚨 Nếu vẫn lỗi:

1. **Restart ChatService:**
   ```bash
   docker compose restart chatservice
   ```

2. **Check all services:**
   ```bash
   docker compose ps
   ```

3. **View detailed logs:**
   ```bash
   docker compose logs chatservice | tail -50
   ```

4. **Test with simple curl:**
   ```bash
   curl http://localhost:5003/api/auth/agent-login \
     -X POST \
     -H "Content-Type: application/json" \
     -d '{"agentId":"debug","agentName":"Debug Agent"}'
   ```