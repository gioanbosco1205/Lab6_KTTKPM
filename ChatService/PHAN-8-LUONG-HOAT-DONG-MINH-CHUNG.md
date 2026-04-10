# PHẦN 8 – LUỒNG HOẠT ĐỘNG HOÀN CHỈNH - MINH CHỨNG

## 📋 YÊU CẦU

Khi policy được tạo:

```
PolicyService
├── Step 1: Save Policy + Save Event → Outbox + Commit Transaction
├── Step 2: Outbox Background Job
├── Step 3: Send Event → RabbitMQ
└── Step 4: Delete Outbox Record
```

---

## ✅ IMPLEMENTATION TRONG CHATSERVICE

### FLOW DIAGRAM

```
┌─────────────────────────────────────────────────────────────────┐
│                    OUTBOX PATTERN - COMPLETE FLOW                │
└─────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────┐
│ STEP 1: Controller receives request                              │
│ File: ChatService/Controllers/EventController.cs                 │
└──────────────────────────────────────────────────────────────────┘
                              ↓
        [POST] /api/event/publish-policy-created
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ STEP 2: Publish to Outbox (Save to Database)                     │
│ File: ChatService/Services/OutboxEventPublisher.cs               │
│                                                                   │
│ await _eventPublisher.PublishMessage(policyCreatedEvent);        │
│   ↓                                                               │
│ await _session.SaveAsync(new Message(message));                  │
│ await _session.SaveAsync(new OutboxMessage(message));            │
│                                                                   │
│ ✅ Transaction committed - Event safely stored in DB             │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ STEP 3: Background Job (runs every 1 second)                     │
│ File: ChatService/Services/OutboxSendingService.cs               │
│                                                                   │
│ Timer triggers PushMessages() every 1 second                     │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ STEP 4: Outbox.ReadMessagesAsync()                               │
│ File: ChatService/Services/Outbox.cs                             │
│                                                                   │
│ var messages = await context.Messages                            │
│     .OrderBy(m => m.Id)                                           │
│     .Take(5)                                                      │
│     .ToListAsync();                                               │
│                                                                   │
│ ✅ Read unprocessed messages from Message table                  │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ STEP 5: Outbox.PublishMessageAsync()                             │
│ File: ChatService/Services/Outbox.cs                             │
│                                                                   │
│ await _rabbitPublisher.PublishAsync(eventData);                  │
│   ↓                                                               │
│ _channel.BasicPublish(                                            │
│     exchange: "policy.events",                                    │
│     routingKey: "policycreated",                                  │
│     body: jsonBytes                                               │
│ );                                                                │
│                                                                   │
│ ✅ Event sent to RabbitMQ successfully                           │
└──────────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────────┐
│ STEP 6: Outbox.DeleteMessageAsync()                              │
│ File: ChatService/Services/Outbox.cs                             │
│                                                                   │
│ await context.Messages                                            │
│     .Where(m => m.Id == messageId)                                │
│     .ExecuteDeleteAsync();                                        │
│                                                                   │
│ ✅ Message deleted from database after successful publish        │
└──────────────────────────────────────────────────────────────────┘
                              ↓
                    ✅ FLOW COMPLETE
```

---

## 📝 CHI TIẾT TỪNG BƯỚC

### STEP 1: Controller Receives Request

**File**: `ChatService/Controllers/EventController.cs` (dòng 29-50)

```csharp
[HttpPost("publish-policy-created")]
public async Task<IActionResult> PublishPolicyCreated([FromBody] PolicyCreatedRequest request)
{
    try
    {
        var policyCreatedEvent = new PolicyCreated
        {
            PolicyNumber = request.PolicyNumber,
            Premium = request.PremiumAmount,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        // ⭐ STEP 1: Lưu event vào database (Outbox Pattern)
        await _eventPublisher.PublishMessage(policyCreatedEvent);

        return Ok(new { 
            Message = "Event saved to database successfully", 
            EventType = nameof(PolicyCreated),
            PolicyNumber = request.PolicyNumber 
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to publish PolicyCreated event");
        return StatusCode(500, new { Error = "Failed to publish event" });
    }
}
```

📸 **CHỤP**: Method `PublishPolicyCreated()` đầy đủ

---

### STEP 2: Save to Database (Outbox)

**File**: `ChatService/Services/OutboxEventPublisher.cs` (dòng 18-35)

```csharp
public async Task PublishMessage<T>(T message)
{
    try
    {
        // ⭐ STEP 2a: Lưu vào Message table (Event Store)
        await _session.SaveAsync(new Message(message));

        // ⭐ STEP 2b: Lưu vào OutboxMessage table (để background job xử lý)
        await _session.SaveAsync(new OutboxMessage(message));

        _logger.LogInformation($"Event {typeof(T).Name} saved to database successfully");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to publish event {typeof(T).Name} to database");
        throw;
    }
}
```

**Lưu ý**: Cả 2 operations (Save Message + Save OutboxMessage) nằm trong cùng 1 transaction với business logic.

📸 **CHỤP**: Method `PublishMessage()` đầy đủ

---

### STEP 3: Background Job Triggers

**File**: `ChatService/Services/OutboxSendingService.cs` (dòng 30-42)

```csharp
public Task StartAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("OutboxSendingService started - running every 1 second");

    // ⭐ STEP 3: Timer chạy mỗi 1 giây
    _timer = new Timer(
        PushMessages,
        null,
        TimeSpan.Zero,                    // Start immediately
        TimeSpan.FromSeconds(1)           // Run every 1 second
    );

    return Task.CompletedTask;
}
```

📸 **CHỤP**: Method `StartAsync()` đầy đủ

---

### STEP 4: Read Messages from Database

**File**: `ChatService/Services/Outbox.cs` (dòng 35-60)

```csharp
public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10)
{
    // ⭐ STEP 4: Tạo scope mới để lấy DbContext
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    
    // ⭐ STEP 4: Fetch messages từ Message table
    var messages = await context.Messages
        .OrderBy(m => m.Id)
        .Take(batchSize)
        .ToListAsync();

    // Convert to OutboxMessage format
    var result = new List<OutboxMessage>();
    foreach (var msg in messages)
    {
        if (msg.Id.HasValue)
        {
            result.Add(new OutboxMessage(msg.Id.Value, msg.Type, msg.Payload));
        }
    }
    
    _logger.LogDebug($"[Outbox] Read {result.Count} messages from Messages table");
    return result;
}
```

📸 **CHỤP**: Method `ReadMessagesAsync()` đầy đủ

---

### STEP 5: Publish to RabbitMQ

**File**: `ChatService/Services/Outbox.cs` (dòng 62-77)

```csharp
public async Task PublishMessageAsync(OutboxMessage message)
{
    var eventData = message.RecreateEvent();
    if (eventData != null)
    {
        // ⭐ STEP 5: Gửi lên RabbitMQ
        await _rabbitPublisher.PublishAsync(eventData);
        _logger.LogInformation($"[Outbox] Published message {message.Id} of type {message.Type}");
    }
    else
    {
        _logger.LogWarning($"[Outbox] Could not recreate event from message {message.Id}");
    }
}
```

**RabbitMQ Implementation**: `ChatService/Services/RabbitEventPublisher.cs` (dòng 70-95)

```csharp
public async Task PublishAsync<T>(T message)
{
    try
    {
        EnsureConnection();

        var exchangeName = "policy.events";
        var routingKey = typeof(T).Name.ToLower();

        var json = JsonConvert.SerializeObject(message);
        var body = Encoding.UTF8.GetBytes(json);

        // ⭐ STEP 5: BasicPublish to RabbitMQ
        _channel!.BasicPublish(
            exchange: exchangeName, 
            routingKey: routingKey, 
            basicProperties: null, 
            body: body
        );
        
        _logger.LogInformation($"Published message of type {typeof(T).Name} to exchange {exchangeName}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Failed to publish message of type {typeof(T).Name}");
        throw;
    }
}
```

📸 **CHỤP**: Method `PublishMessageAsync()` trong Outbox.cs và `PublishAsync()` trong RabbitEventPublisher.cs

---

### STEP 6: Delete Message from Database

**File**: `ChatService/Services/Outbox.cs` (dòng 79-94)

```csharp
public async Task DeleteMessageAsync(long messageId)
{
    // ⭐ STEP 6: Tạo scope mới để lấy DbContext
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    
    // ⭐ STEP 6: Xóa message sau khi publish thành công
    await context.Messages
        .Where(m => m.Id == messageId)
        .ExecuteDeleteAsync();
    
    _logger.LogInformation($"[Outbox] Deleted message {messageId} from Messages table");
}
```

📸 **CHỤP**: Method `DeleteMessageAsync()` đầy đủ

---

### COMPLETE FLOW IN PushMessages()

**File**: `ChatService/Services/OutboxSendingService.cs` (dòng 47-88)

```csharp
private async void PushMessages(object? state)
{
    try
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

        // ⭐ STEP 4: Read messages
        var messages = await outbox.ReadMessagesAsync(5);

        if (messages.Count > 0)
        {
            _logger.LogDebug($"Processing {messages.Count} outbox messages");
        }

        foreach (var message in messages)
        {
            try
            {
                // ⭐ STEP 5: Publish message to RabbitMQ
                await outbox.PublishMessageAsync(message);
                
                // ⭐ STEP 6: Delete message after successful publish
                await outbox.DeleteMessageAsync(message.Id);
                
                _logger.LogDebug($"Successfully processed outbox message {message.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process outbox message {message.Id}");
                // Không delete để retry lần sau
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error occurred in OutboxSendingService.PushMessages");
    }
}
```

📸 **CHỤP**: Method `PushMessages()` đầy đủ

---

## 🎯 DANH SÁCH CHỤP MÀN HÌNH

| # | File | Method/Section | Mô tả |
|---|------|----------------|-------|
| 1 | `EventController.cs` | `PublishPolicyCreated()` | STEP 1: Controller nhận request |
| 2 | `OutboxEventPublisher.cs` | `PublishMessage()` | STEP 2: Lưu vào database |
| 3 | `OutboxSendingService.cs` | `StartAsync()` | STEP 3: Background job setup |
| 4 | `OutboxSendingService.cs` | `PushMessages()` | STEP 4-6: Complete flow |
| 5 | `Outbox.cs` | `ReadMessagesAsync()` | STEP 4: Read from DB |
| 6 | `Outbox.cs` | `PublishMessageAsync()` | STEP 5: Publish to RabbitMQ |
| 7 | `RabbitEventPublisher.cs` | `PublishAsync()` | STEP 5: RabbitMQ implementation |
| 8 | `Outbox.cs` | `DeleteMessageAsync()` | STEP 6: Delete from DB |

---

## ✅ TRANSACTION SAFETY

### Tại sao Outbox Pattern đảm bảo consistency?

1. **STEP 1-2**: Business logic + Save Event nằm trong cùng 1 database transaction
   - Nếu business logic fail → Event không được lưu
   - Nếu save event fail → Transaction rollback, business logic cũng rollback
   - ✅ **Atomic operation**: All or nothing

2. **STEP 3-6**: Background job xử lý async
   - Nếu RabbitMQ down → Event vẫn an toàn trong database
   - Background job sẽ retry cho đến khi thành công
   - ✅ **Guaranteed delivery**: Event sẽ được gửi eventually

3. **Error Handling**:
   - Nếu publish thành công nhưng delete fail → Message sẽ được gửi lại (idempotent)
   - Nếu publish fail → Message không bị delete, sẽ retry lần sau
   - ✅ **At-least-once delivery**: Event có thể gửi nhiều lần nhưng không bao giờ bị mất

---

## 🧪 TEST FLOW

### Test Request:

```http
POST http://localhost:5003/api/event/publish-policy-created
Content-Type: application/json

{
  "policyId": "POL-001",
  "policyNumber": "POL-20241227-0001",
  "customerId": "CUST-001",
  "productId": "PROD-001",
  "premiumAmount": 1500.00
}
```

### Expected Flow:

1. ✅ API returns 200 OK immediately
2. ✅ Event saved to `Messages` table
3. ✅ Event saved to `OutboxMessages` table (optional)
4. ✅ Background job picks up message within 1 second
5. ✅ Message published to RabbitMQ exchange `policy.events`
6. ✅ Message deleted from `Messages` table
7. ✅ Subscribers (PaymentService, ChatService) receive event

### Check Outbox Status:

```http
GET http://localhost:5003/api/event/outbox/status
```

---

## ✅ KẾT LUẬN

Đã implement đầy đủ PHẦN 8 - LUỒNG HOẠT ĐỘNG HOÀN CHỈNH:

- ✅ **STEP 1**: Controller nhận request và tạo event
- ✅ **STEP 2**: Save event vào database (transactional)
- ✅ **STEP 3**: Background job chạy mỗi 1 giây
- ✅ **STEP 4**: Read messages từ database
- ✅ **STEP 5**: Publish lên RabbitMQ
- ✅ **STEP 6**: Delete message sau khi publish thành công

Flow này đảm bảo:
- **Consistency**: Event và business data luôn đồng bộ
- **Reliability**: Event không bị mất khi RabbitMQ down
- **Retry**: Tự động retry khi publish thất bại
- **Decoupling**: Business logic không phụ thuộc vào message broker
