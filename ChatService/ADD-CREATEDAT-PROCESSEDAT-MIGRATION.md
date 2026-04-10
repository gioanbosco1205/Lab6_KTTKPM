# MIGRATION - Thêm CreatedAt và ProcessedAt cho Message Table

## 📋 YÊU CẦU

Thêm 2 trường mới cho bảng `Messages`:
- `CreatedAt` (DateTime, NOT NULL) - Thời điểm message được tạo
- `ProcessedAt` (DateTime, NULL) - Thời điểm message được xử lý

---

## ✅ THAY ĐỔI ĐÃ THỰC HIỆN

### 1. Cập nhật Model

**File**: `ChatService/Models/Message.cs`

```csharp
public class Message
{
    public virtual long? Id { get; protected set; }
    public virtual string Type { get; protected set; } = string.Empty;
    public virtual string Payload { get; protected set; } = string.Empty;
    
    // ⭐ Thêm mới
    public virtual DateTime CreatedAt { get; protected set; }
    public virtual DateTime? ProcessedAt { get; protected set; }

    protected Message() 
    {
        CreatedAt = DateTime.UtcNow;
    }

    public Message(object message)
    {
        Type = message.GetType().FullName ?? string.Empty;
        Payload = JsonConvert.SerializeObject(message);
        CreatedAt = DateTime.UtcNow;  // ⭐ Set khi tạo
        ProcessedAt = null;
    }

    public virtual void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }
}
```

---

### 2. Cập nhật DbContext Configuration

**File**: `ChatService/Data/ChatDbContext.cs`

```csharp
modelBuilder.Entity<Message>(entity =>
{
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Type).IsRequired().HasMaxLength(500);
    entity.Property(e => e.Payload).IsRequired();
    
    // ⭐ Thêm configuration mới
    entity.Property(e => e.CreatedAt).IsRequired();
    entity.Property(e => e.ProcessedAt);

    // Indexes for better query performance
    entity.HasIndex(e => e.Type);
    entity.HasIndex(e => e.CreatedAt);
    entity.HasIndex(e => e.ProcessedAt);
});
```

---

### 3. Cập nhật Outbox.ReadMessagesAsync()

**File**: `ChatService/Services/Outbox.cs`

```csharp
public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10)
{
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    
    // ⭐ Chỉ lấy messages chưa được processed
    var messages = await context.Messages
        .Where(m => m.ProcessedAt == null)  // ⭐ Filter by ProcessedAt
        .OrderBy(m => m.Id)
        .Take(batchSize)
        .ToListAsync();
    
    // ... rest of code
}
```

---

## 🔧 TẠO MIGRATION

### Bước 1: Tạo Migration

```bash
cd ChatService
dotnet ef migrations add AddCreatedAtProcessedAtToMessages
```

### Bước 2: Xem SQL Script (Optional)

```bash
dotnet ef migrations script
```

### Bước 3: Apply Migration

```bash
dotnet ef database update
```

---

## 📝 SQL MIGRATION SCRIPT (Manual)

Nếu không dùng EF migrations, có thể chạy SQL trực tiếp:

### PostgreSQL:

```sql
-- Add CreatedAt column (NOT NULL with default)
ALTER TABLE "Messages" 
ADD COLUMN "CreatedAt" timestamp without time zone NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC');

-- Add ProcessedAt column (NULL)
ALTER TABLE "Messages" 
ADD COLUMN "ProcessedAt" timestamp without time zone NULL;

-- Create indexes for better performance
CREATE INDEX "IX_Messages_CreatedAt" ON "Messages" ("CreatedAt");
CREATE INDEX "IX_Messages_ProcessedAt" ON "Messages" ("ProcessedAt");

-- Update existing records to have CreatedAt
UPDATE "Messages" 
SET "CreatedAt" = NOW() AT TIME ZONE 'UTC' 
WHERE "CreatedAt" IS NULL;
```

### SQL Server:

```sql
-- Add CreatedAt column (NOT NULL with default)
ALTER TABLE Messages 
ADD CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE();

-- Add ProcessedAt column (NULL)
ALTER TABLE Messages 
ADD ProcessedAt datetime2 NULL;

-- Create indexes for better performance
CREATE INDEX IX_Messages_CreatedAt ON Messages (CreatedAt);
CREATE INDEX IX_Messages_ProcessedAt ON Messages (ProcessedAt);
```

### MySQL:

```sql
-- Add CreatedAt column (NOT NULL with default)
ALTER TABLE Messages 
ADD COLUMN CreatedAt datetime NOT NULL DEFAULT (UTC_TIMESTAMP());

-- Add ProcessedAt column (NULL)
ALTER TABLE Messages 
ADD COLUMN ProcessedAt datetime NULL;

-- Create indexes for better performance
CREATE INDEX IX_Messages_CreatedAt ON Messages (CreatedAt);
CREATE INDEX IX_Messages_ProcessedAt ON Messages (ProcessedAt);
```

---

## 🎯 LỢI ÍCH CỦA THAY ĐỔI

### 1. Tracking và Monitoring

```csharp
// Có thể query messages theo thời gian
var oldMessages = await context.Messages
    .Where(m => m.CreatedAt < DateTime.UtcNow.AddHours(-24))
    .ToListAsync();

// Có thể tính processing time
var processingTime = message.ProcessedAt - message.CreatedAt;
```

### 2. Soft Delete Option

Thay vì xóa message ngay, có thể mark as processed:

```csharp
// Option 1: Hard delete (hiện tại)
await context.Messages
    .Where(m => m.Id == messageId)
    .ExecuteDeleteAsync();

// Option 2: Soft delete (mark as processed)
var message = await context.Messages.FindAsync(messageId);
if (message != null)
{
    message.MarkAsProcessed();
    await context.SaveChangesAsync();
}
```

### 3. Query Optimization

```csharp
// Chỉ lấy unprocessed messages
var unprocessed = await context.Messages
    .Where(m => m.ProcessedAt == null)
    .OrderBy(m => m.CreatedAt)  // Oldest first
    .Take(50)
    .ToListAsync();

// Cleanup old processed messages (nếu dùng soft delete)
var cutoffDate = DateTime.UtcNow.AddDays(-30);
await context.Messages
    .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffDate)
    .ExecuteDeleteAsync();
```

### 4. Monitoring Dashboard

```csharp
// Statistics
var stats = new
{
    TotalMessages = await context.Messages.CountAsync(),
    UnprocessedCount = await context.Messages.CountAsync(m => m.ProcessedAt == null),
    ProcessedCount = await context.Messages.CountAsync(m => m.ProcessedAt != null),
    OldestUnprocessed = await context.Messages
        .Where(m => m.ProcessedAt == null)
        .OrderBy(m => m.CreatedAt)
        .Select(m => m.CreatedAt)
        .FirstOrDefaultAsync(),
    AverageProcessingTime = await context.Messages
        .Where(m => m.ProcessedAt != null)
        .Select(m => EF.Functions.DateDiffSecond(m.CreatedAt, m.ProcessedAt.Value))
        .AverageAsync()
};
```

---

## ✅ VERIFICATION

### Test 1: Tạo Message Mới

```csharp
var message = new Message(new PolicyCreated { ... });
await context.Messages.AddAsync(message);
await context.SaveChangesAsync();

// Verify
Assert.NotNull(message.CreatedAt);
Assert.Null(message.ProcessedAt);
Assert.True(message.CreatedAt <= DateTime.UtcNow);
```

### Test 2: Mark as Processed

```csharp
var message = await context.Messages.FirstAsync();
message.MarkAsProcessed();
await context.SaveChangesAsync();

// Verify
Assert.NotNull(message.ProcessedAt);
Assert.True(message.ProcessedAt >= message.CreatedAt);
```

### Test 3: Query Unprocessed

```csharp
var unprocessed = await context.Messages
    .Where(m => m.ProcessedAt == null)
    .ToListAsync();

// All should have null ProcessedAt
Assert.All(unprocessed, m => Assert.Null(m.ProcessedAt));
```

---

## 📸 CHỤP MÀN HÌNH

1. **Message.cs** - Properties CreatedAt và ProcessedAt
2. **ChatDbContext.cs** - Configuration cho 2 fields mới
3. **Outbox.cs** - Query với filter `ProcessedAt == null`
4. **Migration file** - Generated migration code
5. **Database** - Table structure với 2 columns mới

---

## ✅ KẾT LUẬN

Đã thêm thành công 2 trường:
- ✅ `CreatedAt` - Track thời điểm tạo message
- ✅ `ProcessedAt` - Track thời điểm xử lý message

Lợi ích:
- Monitoring và tracking tốt hơn
- Option để soft delete thay vì hard delete
- Query optimization với index
- Statistics và reporting

**Lưu ý**: Cần chạy migration để update database schema!
