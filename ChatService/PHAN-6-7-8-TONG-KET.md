# PHẦN 6, 7, 8 - OUTBOX PATTERN - TỔNG KẾT

## 📋 TỔNG QUAN 3 PHẦN

| Phần | Nội dung | File chính |
|------|----------|------------|
| **PHẦN 6** | Outbox Processor (Read, Publish, Delete) | `Outbox.cs` |
| **PHẦN 7** | Register Services (DI Configuration) | `Program.cs` |
| **PHẦN 8** | Luồng hoạt động hoàn chỉnh (End-to-end flow) | Tất cả files |

---

## ✅ PHẦN 6 - OUTBOX PROCESSOR

### Class Outbox chịu trách nhiệm:

```csharp
// 1. READ MESSAGE
public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10)
{
    var messages = await context.Messages
        .OrderBy(m => m.Id)
        .Take(batchSize)
        .ToListAsync();
}

// 2. PUBLISH MESSAGE
public async Task PublishMessageAsync(OutboxMessage message)
{
    await _rabbitPublisher.PublishAsync(eventData);
}

// 3. DELETE MESSAGE
public async Task DeleteMessageAsync(long messageId)
{
    await context.Messages
        .Where(m => m.Id == messageId)
        .ExecuteDeleteAsync();
}
```

📸 **CHỤP**: `ChatService/Services/Outbox.cs` - 3 methods trên

---

## ✅ PHẦN 7 - REGISTER SERVICES

### DI Configuration:

```csharp
// File: ChatService/Program.cs

builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();  
builder.Services.AddSingleton<Outbox>();                              
builder.Services.AddSingleton<IOutbox>(sp => sp.GetRequiredService<Outbox>());
builder.Services.AddHostedService<OutboxSendingService>();
```

📸 **CHỤP**: `ChatService/Program.cs` - Dòng 87-98

---

## ✅ PHẦN 8 - LUỒNG HOẠT ĐỘNG HOÀN CHỈNH

### Complete Flow:

```
1. Controller receives request
   ↓
2. IEventPublisher saves to database (transactional)
   ↓
3. Background job runs every 1 second
   ↓
4. Outbox.ReadMessagesAsync() - Read from DB
   ↓
5. Outbox.PublishMessageAsync() - Send to RabbitMQ
   ↓
6. Outbox.DeleteMessageAsync() - Delete from DB
```

### Code Flow:

```csharp
// STEP 1-2: Controller + Save to DB
[HttpPost("publish-policy-created")]
public async Task<IActionResult> PublishPolicyCreated(...)
{
    await _eventPublisher.PublishMessage(policyCreatedEvent);
    return Ok(...);
}

// STEP 3-6: Background Job
private async void PushMessages(object? state)
{
    var messages = await outbox.ReadMessagesAsync(5);      // STEP 4
    
    foreach (var message in messages)
    {
        await outbox.PublishMessageAsync(message);         // STEP 5
        await outbox.DeleteMessageAsync(message.Id);       // STEP 6
    }
}
```

📸 **CHỤP**: 
- `EventController.cs` - Method `PublishPolicyCreated()`
- `OutboxSendingService.cs` - Method `PushMessages()`

---

## 📁 CẤU TRÚC FILES

```
ChatService/
├── Controllers/
│   └── EventController.cs              ⭐ STEP 1: Entry point
├── Services/
│   ├── OutboxEventPublisher.cs         ⭐ STEP 2: Save to DB
│   ├── Outbox.cs                       ⭐ STEP 4-6: Processor
│   ├── OutboxSendingService.cs         ⭐ STEP 3: Background job
│   └── RabbitEventPublisher.cs         ⭐ STEP 5: RabbitMQ
├── Models/
│   ├── Message.cs                      Event Store table
│   └── OutboxMessage.cs                DTO
├── Program.cs                          ⭐ PHẦN 7: DI Registration
└── PHAN-8-TEST-FLOW.http              Test file
```

---

## 🎯 DANH SÁCH CHỤP MÀN HÌNH (TẤT CẢ 3 PHẦN)

### PHẦN 6 - Outbox Processor

| # | File | Nội dung |
|---|------|----------|
| 1 | `Outbox.cs` | Constructor với IServiceProvider |
| 2 | `Outbox.cs` | Method `ReadMessagesAsync()` |
| 3 | `Outbox.cs` | Method `PublishMessageAsync()` |
| 4 | `Outbox.cs` | Method `DeleteMessageAsync()` |
| 5 | `RabbitEventPublisher.cs` | Method `PublishAsync()` |

### PHẦN 7 - Register Services

| # | File | Nội dung |
|---|------|----------|
| 6 | `Program.cs` | DI Registration (dòng 87-98) |

### PHẦN 8 - Luồng hoạt động

| # | File | Nội dung |
|---|------|----------|
| 7 | `EventController.cs` | Method `PublishPolicyCreated()` |
| 8 | `OutboxEventPublisher.cs` | Method `PublishMessage()` |
| 9 | `OutboxSendingService.cs` | Method `StartAsync()` |
| 10 | `OutboxSendingService.cs` | Method `PushMessages()` |

---

## 🧪 TEST FLOW

### Quick Test:

```http
# 1. Publish event
POST http://localhost:5003/api/event/publish-policy-created
Content-Type: application/json

{
  "policyNumber": "POL-20241227-0001",
  "premiumAmount": 1500.00
}

# 2. Check outbox (immediately)
GET http://localhost:5003/api/event/outbox/status
# Expected: unprocessedCount = 1

# 3. Wait 1-2 seconds, check again
GET http://localhost:5003/api/event/outbox/status
# Expected: unprocessedCount = 0 (processed and deleted)
```

📸 **CHỤP**: Test results showing the flow

---

## ✅ CHECKLIST HOÀN THÀNH

### PHẦN 6 - Outbox Processor
- [x] Read message từ Message table
- [x] Publish message lên RabbitMQ
- [x] Delete message sau khi publish thành công
- [x] Sử dụng IServiceProvider pattern (Singleton + Scoped DbContext)

### PHẦN 7 - Register Services
- [x] `AddScoped<IEventPublisher, OutboxEventPublisher>()`
- [x] `AddSingleton<Outbox>()`
- [x] `AddHostedService<OutboxSendingService>()`

### PHẦN 8 - Luồng hoạt động
- [x] Controller nhận request
- [x] Save event vào database (transactional)
- [x] Background job chạy mỗi 1 giây
- [x] Read → Publish → Delete flow
- [x] Error handling và retry mechanism

---

## 📚 TÀI LIỆU THAM KHẢO

Đã tạo các files hướng dẫn chi tiết:

1. **`PHAN-6-OUTBOX-MINH-CHUNG.md`** - Chi tiết PHẦN 6
2. **`PHAN-7-REGISTER-SERVICES-MINH-CHUNG.md`** - Chi tiết PHẦN 7
3. **`PHAN-8-LUONG-HOAT-DONG-MINH-CHUNG.md`** - Chi tiết PHẦN 8
4. **`PHAN-6-7-TONG-HOP.md`** - Tổng hợp PHẦN 6 & 7
5. **`PHAN-8-TEST-FLOW.http`** - Test cases

---

## 🎓 KIẾN THỨC QUAN TRỌNG

### Tại sao cần Outbox Pattern?

1. **Transactional Consistency**: Event và business data luôn đồng bộ
2. **Guaranteed Delivery**: Event không bị mất khi RabbitMQ down
3. **Retry Mechanism**: Tự động retry khi publish thất bại
4. **Decoupling**: Business logic không phụ thuộc vào message broker

### Singleton + Scoped Pattern

```csharp
// Outbox là Singleton nhưng cần DbContext (Scoped)
public class Outbox : IOutbox
{
    private readonly IServiceProvider _serviceProvider;
    
    public async Task<List<OutboxMessage>> ReadMessagesAsync(...)
    {
        // Tạo scope mới cho mỗi operation
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        // Use context...
    }
}
```

---

## ✅ KẾT LUẬN

Đã hoàn thành đầy đủ cả 3 phần:
- ✅ **PHẦN 6**: Outbox Processor với 3 methods
- ✅ **PHẦN 7**: DI Registration đúng lifetime
- ✅ **PHẦN 8**: End-to-end flow hoàn chỉnh

Tất cả code đã được test và hoạt động đúng! 🎉

**Bạn có thể chụp màn hình theo danh sách trên để làm minh chứng.**
