# PHẦN 7 – REGISTER SERVICES - MINH CHỨNG

## 📋 YÊU CẦU
```csharp
services.AddScoped<IEventPublisher, OutboxEventPublisher>();
services.AddSingleton<Outbox>();
services.AddHostedService<OutboxSendingService>();
```

---

## ✅ IMPLEMENTATION

### File: `ChatService/Program.cs` (dòng 87-98)

```csharp
// ⭐ PHẦN 7 - REGISTER SERVICES (Outbox Pattern)
// Theo yêu cầu: services.AddScoped<IEventPublisher, OutboxEventPublisher>();
//               services.AddSingleton<Outbox>();
//               services.AddHostedService<OutboxSendingService>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();  
builder.Services.AddSingleton<Outbox>();                              
builder.Services.AddSingleton<IOutbox>(sp => sp.GetRequiredService<Outbox>()); // Alias cho IOutbox interface
builder.Services.AddHostedService<OutboxSendingService>();
```

---

## 📝 GIẢI THÍCH

### 1. ✅ `AddScoped<IEventPublisher, OutboxEventPublisher>()`
- **Scoped lifetime**: Mỗi HTTP request có 1 instance riêng
- **Mục đích**: Khi controller/service cần publish event, sẽ lưu vào database thay vì gửi trực tiếp lên RabbitMQ
- **File implementation**: `ChatService/Services/OutboxEventPublisher.cs`

### 2. ✅ `AddSingleton<Outbox>()`
- **Singleton lifetime**: 1 instance duy nhất cho toàn bộ application
- **Mục đích**: Class Outbox xử lý read/publish/delete messages từ Message table
- **Lưu ý**: Vì Outbox là Singleton nhưng cần DbContext (Scoped), nên Outbox inject `IServiceProvider` và tạo scope mới cho mỗi operation
- **File implementation**: `ChatService/Services/Outbox.cs`

### 3. ✅ `AddHostedService<OutboxSendingService>()`
- **Background service**: Chạy ngay khi application start
- **Mục đích**: Chạy mỗi 1 giây để xử lý messages trong Message table (read → publish → delete)
- **File implementation**: `ChatService/Services/OutboxSendingService.cs`

### 4. ✅ Bonus: `AddSingleton<IOutbox>`
- **Alias registration**: Cho phép inject `IOutbox` interface thay vì `Outbox` class trực tiếp
- **Mục đích**: Tương thích với code hiện có (OutboxSendingService đang dùng `IOutbox`)

---

## 🔍 TẠI SAO OUTBOX LÀ SINGLETON?

Trong yêu cầu gốc, họ dùng NHibernate với pattern:
- Outbox là Singleton để tái sử dụng
- Mỗi lần operation, tạo session mới

Với EF Core, chúng ta áp dụng tương tự:
- Outbox là Singleton
- Inject `IServiceProvider` thay vì `ChatDbContext`
- Mỗi operation tạo scope mới: `using var scope = _serviceProvider.CreateScope()`

### Ví dụ trong Outbox.cs:

```csharp
public class Outbox : IOutbox
{
    private readonly IServiceProvider _serviceProvider;  // ⭐ Inject ServiceProvider
    
    public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10)
    {
        // ⭐ Tạo scope mới để lấy DbContext
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        
        // Sử dụng context...
        var messages = await context.Messages
            .OrderBy(m => m.Id)
            .Take(batchSize)
            .ToListAsync();
            
        return result;
    }
}
```

---

## 🎯 FILE CẦN CHỤP MÀN HÌNH

| STT | File | Nội dung chụp |
|-----|------|---------------|
| 1 | `ChatService/Program.cs` | Dòng 87-98 (PHẦN 7 - REGISTER SERVICES) |
| 2 | `ChatService/Services/Outbox.cs` | Constructor (dòng 22-29) - inject IServiceProvider |
| 3 | `ChatService/Services/Outbox.cs` | Method ReadMessagesAsync (dòng 35-60) - tạo scope mới |
| 4 | `ChatService/Services/OutboxEventPublisher.cs` | Toàn bộ class |
| 5 | `ChatService/Services/OutboxSendingService.cs` | Toàn bộ class |

---

## ✅ KẾT LUẬN

Đã hoàn thành PHẦN 7 - REGISTER SERVICES theo đúng yêu cầu:
- ✅ `IEventPublisher` → Scoped
- ✅ `Outbox` → Singleton (với IServiceProvider pattern)
- ✅ `OutboxSendingService` → HostedService

Tất cả 3 services đã được đăng ký đúng lifetime và hoạt động cùng nhau để implement Outbox Pattern.
