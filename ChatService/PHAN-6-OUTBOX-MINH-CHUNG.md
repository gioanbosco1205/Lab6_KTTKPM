# PHẦN 6 – OUTBOX PROCESSOR - MINH CHỨNG

## 📋 YÊU CẦU
Class Outbox chịu trách nhiệm:
1. **Read message**: `session.Query<Message>().OrderBy(m => m.Id).Take(50).ToList()`
2. **Publish message**: `await busClient.BasicPublishAsync(message, cfg => {...})`
3. **Delete message**: `session.CreateQuery("delete Message where id=:id").ExecuteUpdate()`

---

## ✅ 1. READ MESSAGE
**File**: `ChatService/Services/Outbox.cs` (dòng 35-60)

```csharp
public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10)
{
    // ⭐ Tạo scope mới để lấy DbContext (vì Outbox là Singleton)
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    
    // ⭐ FETCH từ Message table - đúng theo yêu cầu
    var messages = await context.Messages
        .OrderBy(m => m.Id)
        .Take(batchSize)
        .ToListAsync();
    
    // Convert sang OutboxMessage format
    var result = new List<OutboxMessage>();
    foreach (var msg in messages)
    {
        if (msg.Id.HasValue)
        {
            result.Add(new OutboxMessage(msg.Id.Value, msg.Type, msg.Payload));
        }
    }
    return result;
}
```

---

## ✅ 2. PUBLISH MESSAGE
**File**: `ChatService/Services/Outbox.cs` (dòng 62-77)

```csharp
public async Task PublishMessageAsync(OutboxMessage message)
{
    var eventData = message.RecreateEvent();
    if (eventData != null)
    {
        // ⭐ PUBLISH to RabbitMQ - đúng theo yêu cầu
        await _rabbitPublisher.PublishAsync(eventData);
        _logger.LogInformation($"[Outbox] Published message {message.Id}");
    }
}
```

**RabbitMQ Implementation**: `ChatService/Services/RabbitEventPublisher.cs` (dòng 70-95)

```csharp
public async Task PublishAsync<T>(T message)
{
    var exchangeName = "policy.events";
    var routingKey = typeof(T).Name.ToLower();
    var json = JsonConvert.SerializeObject(message);
    var body = Encoding.UTF8.GetBytes(json);
    
    // ⭐ BasicPublish to RabbitMQ
    _channel!.BasicPublish(exchange: exchangeName, routingKey: routingKey, 
                          basicProperties: null, body: body);
}
```

---

## ✅ 3. DELETE MESSAGE
**File**: `ChatService/Services/Outbox.cs` (dòng 79-94)

```csharp
public async Task DeleteMessageAsync(long messageId)
{
    // ⭐ Tạo scope mới để lấy DbContext
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    
    // ⭐ DELETE từ Message table - đúng theo yêu cầu
    // ExecuteDeleteAsync tương đương ExecuteUpdate() trong NHibernate
    await context.Messages
        .Where(m => m.Id == messageId)
        .ExecuteDeleteAsync();
    
    _logger.LogInformation($"[Outbox] Deleted message {messageId}");
}
```

---

## ✅ OUTBOX CONSTRUCTOR
**File**: `ChatService/Services/Outbox.cs` (dòng 22-29)

```csharp
public Outbox(
    IServiceProvider serviceProvider,  // ⭐ Inject ServiceProvider (vì Outbox là Singleton)
    IRabbitEventPublisher rabbitPublisher,
    ILogger<Outbox> logger)
{
    _serviceProvider = serviceProvider;
    _rabbitPublisher = rabbitPublisher;
    _logger = logger;
}
```

**Lý do**: Outbox được register là Singleton (PHẦN 7), nhưng cần DbContext (Scoped). 
Giải pháp: Inject `IServiceProvider` và tạo scope mới cho mỗi operation.

---

## ✅ BACKGROUND SERVICE
**File**: `ChatService/Services/OutboxSendingService.cs` (dòng 47-88)

```csharp
private async void PushMessages(object? state)
{
    var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

    // 1. Read messages
    var messages = await outbox.ReadMessagesAsync(5);

    foreach (var message in messages)
    {
        try
        {
            // 2. Publish message
            await outbox.PublishMessageAsync(message);
            
            // 3. Delete message (sau khi gửi thành công)
            await outbox.DeleteMessageAsync(message.Id);
        }
        catch (Exception ex)
        {
            // Không delete để retry lần sau
        }
    }
}
```

---

## 🎯 CÁC FILE CẦN CHỤP

| STT | File | Nội dung chụp |
|-----|------|---------------|
| 1 | `ChatService/Services/Outbox.cs` | Constructor (dòng 22-29) |
| 2 | `ChatService/Services/Outbox.cs` | Method `ReadMessagesAsync()` (dòng 35-60) |
| 3 | `ChatService/Services/Outbox.cs` | Method `PublishMessageAsync()` (dòng 62-77) |
| 4 | `ChatService/Services/Outbox.cs` | Method `DeleteMessageAsync()` (dòng 79-94) |
| 5 | `ChatService/Services/OutboxSendingService.cs` | Method `PushMessages()` (dòng 47-88) |
| 6 | `ChatService/Services/RabbitEventPublisher.cs` | Method `PublishAsync()` (dòng 70-95) |

---

## ✅ KẾT LUẬN

Đã hoàn thành đầy đủ PHẦN 6 - OUTBOX PROCESSOR với 3 nhiệm vụ:
- ✅ **Read**: Fetch từ `Messages` table với `OrderBy().Take()`
- ✅ **Publish**: Gửi lên RabbitMQ bằng `BasicPublish()`
- ✅ **Delete**: Xóa từ `Messages` table bằng `ExecuteDeleteAsync()`

Background service chạy mỗi 1 giây, xử lý theo flow: Read → Publish → Delete

**Lưu ý**: Outbox sử dụng IServiceProvider pattern để tương thích với Singleton lifetime (PHẦN 7)
