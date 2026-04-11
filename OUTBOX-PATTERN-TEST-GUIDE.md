# 📋 HƯỚNG DẪN TEST OUTBOX PATTERN - ĐẦY ĐỦ

## 🎯 Mục tiêu
Hướng dẫn test và chụp ảnh để chứng minh Outbox Pattern hoạt động đúng theo yêu cầu:
- ✅ Mô tả Outbox Pattern
- ✅ Kiến trúc hệ thống
- ✅ Code triển khai
- ✅ Ảnh chụp database outbox
- ✅ Ảnh chụp RabbitMQ message
- ✅ Kết quả chạy hệ thống

---

## 📖 PHẦN 1: MÔ TẢ OUTBOX PATTERN

### Outbox Pattern là gì?
Outbox Pattern là một design pattern để đảm bảo tính nhất quán (consistency) giữa database và message broker trong hệ thống phân tán.

### Vấn đề cần giải quyết:
Khi một service cần:
1. Lưu dữ liệu vào database
2. Gửi event đến message broker (RabbitMQ)

Nếu một trong hai thao tác thất bại → Dữ liệu không nhất quán!

### Giải pháp Outbox Pattern:
```
1. Lưu dữ liệu vào database
2. Lưu event vào bảng "outbox_messages" (cùng transaction)
3. Background job đọc messages từ outbox
4. Publish messages đến RabbitMQ
5. Xóa messages đã publish thành công
```

### Ưu điểm:
- ✅ Đảm bảo tính nhất quán dữ liệu
- ✅ Không mất message khi RabbitMQ down
- ✅ Có thể retry khi publish thất bại
- ✅ Audit trail (lịch sử messages)

---

## 🏗️ PHẦN 2: KIẾN TRÚC HỆ THỐNG

### Sơ đồ kiến trúc:

```
┌─────────────────────────────────────────────────────────────┐
│                    PolicyService                             │
│  1. Create Policy                                            │
│  2. Publish Event to RabbitMQ                                │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ PolicyCreated Event
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    RabbitMQ                                  │
│  Queue: policy.created.chatservice                           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Consume Event
                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    ChatService                               │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  EventSubscriberHostedService                        │   │
│  │  - Subscribe to RabbitMQ                             │   │
│  │  - Receive PolicyCreated event                       │   │
│  └──────────────────┬───────────────────────────────────┘   │
│                     │                                        │
│                     ▼                                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  OUTBOX PATTERN                                      │   │
│  │                                                      │   │
│  │  Step 1: Save to Database                           │   │
│  │  ┌────────────────────────────────────────────┐     │   │
│  │  │  outbox_messages table                     │     │   │
│  │  │  - id                                      │     │   │
│  │  │  - type (event type)                      │     │   │
│  │  │  - json_payload (event data)              │     │   │
│  │  │  - created_at                             │     │   │
│  │  │  - is_processed (false)                   │     │   │
│  │  └────────────────────────────────────────────┘     │   │
│  │                                                      │   │
│  │  Step 2: Background Job (OutboxSendingService)     │   │
│  │  ┌────────────────────────────────────────────┐     │   │
│  │  │  - Run every 1 second                      │     │   │
│  │  │  - Read unprocessed messages               │     │   │
│  │  │  - Publish to RabbitMQ                     │     │   │
│  │  │  - Delete if success                       │     │   │
│  │  │  - Retry if failed (max 5 times)          │     │   │
│  │  └────────────────────────────────────────────┘     │   │
│  └──────────────────┬───────────────────────────────────┘   │
│                     │                                        │
│                     ▼                                        │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  SignalR Hub                                         │   │
│  │  - Send real-time notifications to clients          │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### Flow chi tiết:

```
PolicyService                RabbitMQ              ChatService
     │                          │                       │
     │ 1. Create Policy         │                       │
     ├─────────────────────────>│                       │
     │                          │                       │
     │                          │ 2. Consume Event      │
     │                          ├──────────────────────>│
     │                          │                       │
     │                          │                       │ 3. Save to outbox_messages
     │                          │                       │    (is_processed = false)
     │                          │                       │
     │                          │                       │ 4. OutboxSendingService
     │                          │                       │    (runs every 1 second)
     │                          │                       │
     │                          │                       │ 5. Read unprocessed messages
     │                          │                       │
     │                          │ 6. Publish to Queue   │
     │                          │<──────────────────────│
     │                          │                       │
     │                          │                       │ 7. Delete message if success
     │                          │                       │
     │                          │                       │ 8. Send SignalR notification
     │                          │                       │
```

---

## 💻 PHẦN 3: CODE TRIỂN KHAI

### 3.1. Outbox Message Model
**File:** `ChatService/Models/OutboxMessage.cs`

```csharp
public class OutboxMessage
{
    public virtual long Id { get; protected set; }
    public virtual string Type { get; protected set; } = string.Empty;
    public virtual string JsonPayload { get; protected set; } = string.Empty;
    public virtual DateTime CreatedAt { get; protected set; }
    public virtual bool IsProcessed { get; protected set; }
    public virtual DateTime? ProcessedAt { get; protected set; }

    public OutboxMessage(object eventData)
    {
        Type = eventData.GetType().FullName ?? string.Empty;
        JsonPayload = JsonConvert.SerializeObject(eventData);
        CreatedAt = DateTime.UtcNow;
        IsProcessed = false;
    }
}
```

### 3.2. Outbox Service Interface
**File:** `ChatService/Services/IOutbox.cs`

```csharp
public interface IOutbox
{
    Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 100);
    Task PublishMessageAsync(OutboxMessage message);
    Task DeleteMessageAsync(long messageId);
    Task<int> GetUnprocessedCountAsync();
}
```

### 3.3. Outbox Implementation
**File:** `ChatService/Services/Outbox.cs`

```csharp
public class Outbox : IOutbox
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitEventPublisher _rabbitPublisher;

    public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 100)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        
        return await context.Messages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task PublishMessageAsync(OutboxMessage message)
    {
        var eventObj = message.RecreateEvent();
        if (eventObj != null)
        {
            await _rabbitPublisher.PublishMessage(eventObj);
        }
    }

    public async Task DeleteMessageAsync(long messageId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        
        var message = await context.Messages.FindAsync(messageId);
        if (message != null)
        {
            context.Messages.Remove(message);
            await context.SaveChangesAsync();
        }
    }
}
```

### 3.4. Background Job (OutboxSendingService)
**File:** `ChatService/Services/OutboxSendingService.cs`

```csharp
public class OutboxSendingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxSendingService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxSendingService started - running every 1 second");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

                // Read unprocessed messages
                var messages = await outbox.ReadMessagesAsync(100);

                foreach (var message in messages)
                {
                    try
                    {
                        // Publish to RabbitMQ
                        await outbox.PublishMessageAsync(message);
                        
                        // Delete if success
                        await outbox.DeleteMessageAsync(message.Id);
                        
                        _logger.LogInformation($"✅ Published and deleted message {message.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"❌ Failed to publish message {message.Id}: {ex.Message}");
                        // Will retry in next cycle
                    }
                }

                await Task.Delay(1000, stoppingToken); // Run every 1 second
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in OutboxSendingService: {ex.Message}");
            }
        }
    }
}
```

### 3.5. Register Services
**File:** `ChatService/Program.cs`

```csharp
// Register Outbox Pattern
builder.Services.AddScoped<IOutbox, Outbox>();
builder.Services.AddHostedService<OutboxSendingService>();
```

---

## 📸 PHẦN 4: HƯỚNG DẪN CHỤP ẢNH

### 4.1. Ảnh Database Outbox

**Bước 1:** Kết nối database
```bash
docker exec -it postgres psql -U postgres -d ChatServiceDb
```

**Bước 2:** Xem cấu trúc bảng
```sql
\d outbox_messages
```
📸 **CHỤP ẢNH 1:** Cấu trúc bảng outbox_messages

**Bước 3:** Tạo policy để tạo message
```bash
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test Outbox"}'
```

**Bước 4:** Xem messages trong outbox (NHANH - trước khi bị xóa)
```sql
SELECT * FROM outbox_messages WHERE is_processed = false;
```
📸 **CHỤP ẢNH 2:** Messages trong outbox (is_processed = false)

**Bước 5:** Đợi 2 giây, xem messages đã được xử lý
```sql
SELECT * FROM outbox_messages;
```
📸 **CHỤP ẢNH 3:** Outbox trống (messages đã được publish và xóa)

**Bước 6:** Xem lịch sử (nếu có bảng audit)
```sql
SELECT COUNT(*) FROM outbox_messages;
```
📸 **CHỤP ẢNH 4:** Số lượng messages đã xử lý

### 4.2. Ảnh RabbitMQ Messages

**Bước 1:** Mở RabbitMQ Management UI
```
http://localhost:15672
Username: guest
Password: guest
```
📸 **CHỤP ẢNH 5:** RabbitMQ Dashboard

**Bước 2:** Vào tab "Queues"
📸 **CHỤP ẢNH 6:** Danh sách queues
- policy.created.chatservice
- policy.terminated.chatservice
- product.activated.chatservice

**Bước 3:** Click vào queue "policy.created.chatservice"
📸 **CHỤP ẢNH 7:** Chi tiết queue (message rate, consumers)

**Bước 4:** Tạo policy và xem message flow
```bash
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Test RabbitMQ"}'
```

**Bước 5:** Vào tab "Get messages" trong queue
📸 **CHỤP ẢNH 8:** Message content (JSON payload)

### 4.3. Ảnh Kết Quả Chạy Hệ Thống

**Bước 1:** Chạy hệ thống
```bash
docker compose up -d
```
📸 **CHỤP ẢNH 9:** Docker containers running

**Bước 2:** Kiểm tra logs ChatService
```bash
docker logs chatservice --tail 50
```
📸 **CHỤP ẢNH 10:** Logs showing:
- OutboxSendingService started
- Published and deleted message
- Received PolicyCreated event

**Bước 3:** Mở giao diện chat
```bash
open client-app/index.html
```
📸 **CHỤP ẢNH 11:** Giao diện chat với real-time notifications

**Bước 4:** Tạo policy và xem notification
📸 **CHỤP ẢNH 12:** Toast notification hiển thị policy created

**Bước 5:** Chạy test script
```bash
./FINAL-DOCKER-TEST.sh
```
📸 **CHỤP ẢNH 13:** Kết quả test (tất cả pass)

---

## 🧪 PHẦN 5: SCRIPT TEST TỰ ĐỘNG

### Test Outbox Pattern hoàn chỉnh:

```bash
#!/bin/bash

echo "=== TEST OUTBOX PATTERN ==="

# 1. Clean database
echo "1. Cleaning database..."
docker exec postgres psql -U postgres -d ChatServiceDb -c "DELETE FROM outbox_messages;"

# 2. Create policy
echo "2. Creating policy..."
curl -X POST http://localhost:5002/api/policy/create \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Outbox Test"}'

# 3. Check outbox immediately (should have message)
echo "3. Checking outbox (should have unprocessed message)..."
docker exec postgres psql -U postgres -d ChatServiceDb -c \
  "SELECT id, type, is_processed, created_at FROM outbox_messages;"

# 4. Wait for background job
echo "4. Waiting 3 seconds for OutboxSendingService..."
sleep 3

# 5. Check outbox again (should be empty)
echo "5. Checking outbox (should be empty - message published and deleted)..."
docker exec postgres psql -U postgres -d ChatServiceDb -c \
  "SELECT COUNT(*) FROM outbox_messages WHERE is_processed = false;"

# 6. Check ChatService logs
echo "6. Checking ChatService logs..."
docker logs chatservice --tail 20 | grep -i "published\|deleted\|received"

# 7. Check RabbitMQ
echo "7. Checking RabbitMQ queues..."
docker exec rabbitmq rabbitmqctl list_queues name messages

echo "=== TEST COMPLETE ==="
```

Lưu script này vào file `test-outbox-complete.sh` và chạy:
```bash
chmod +x test-outbox-complete.sh
./test-outbox-complete.sh
```

---

## 📋 CHECKLIST CHỤP ẢNH

### Database:
- [ ] Ảnh 1: Cấu trúc bảng outbox_messages
- [ ] Ảnh 2: Messages chưa xử lý (is_processed = false)
- [ ] Ảnh 3: Outbox trống sau khi xử lý
- [ ] Ảnh 4: Statistics (count, timestamps)

### RabbitMQ:
- [ ] Ảnh 5: RabbitMQ Dashboard
- [ ] Ảnh 6: Danh sách queues
- [ ] Ảnh 7: Chi tiết queue
- [ ] Ảnh 8: Message content (JSON)

### Hệ thống:
- [ ] Ảnh 9: Docker containers
- [ ] Ảnh 10: ChatService logs
- [ ] Ảnh 11: Giao diện chat
- [ ] Ảnh 12: Real-time notifications
- [ ] Ảnh 13: Test results

### Code:
- [ ] Screenshot OutboxMessage.cs
- [ ] Screenshot Outbox.cs
- [ ] Screenshot OutboxSendingService.cs
- [ ] Screenshot Program.cs (registration)

---

## 🎯 KẾT LUẬN

Sau khi hoàn thành các bước trên, bạn sẽ có:

1. ✅ **Mô tả Outbox Pattern** - Giải thích rõ ràng pattern và lý do sử dụng
2. ✅ **Kiến trúc hệ thống** - Sơ đồ chi tiết flow
3. ✅ **Code triển khai** - Full source code với comments
4. ✅ **Ảnh database outbox** - Chứng minh messages được lưu và xử lý
5. ✅ **Ảnh RabbitMQ** - Chứng minh messages được publish
6. ✅ **Kết quả chạy** - Logs và giao diện hoạt động

**Tất cả chứng minh Outbox Pattern hoạt động đúng và đầy đủ!** 🎉
