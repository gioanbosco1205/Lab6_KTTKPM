# LOGGING GUIDE - Outbox Pattern

## 📋 TỔNG QUAN

Comprehensive logging cho Outbox Pattern với 3 loại chính:
- ✅ **SUCCESS**: Message được publish và delete thành công
- ❌ **FAILURE**: Message publish thất bại
- 🔄 **RETRY**: Message được schedule để retry

---

## 📊 LOG LEVELS

| Level | Sử dụng cho | Icon |
|-------|-------------|------|
| `LogInformation` | Success, batch summary | ✅ |
| `LogWarning` | Retry scheduled | 🔄 |
| `LogError` | Failure, errors | ❌ |
| `LogCritical` | Dead letter queue | ⚠️ |
| `LogDebug` | Detailed operations | 🔍 |

---

## ✅ SUCCESS LOGS

### Format

```
┌─────────────────────────────────────────────────────┐
│ ✅ SUCCESS                                          │
├─────────────────────────────────────────────────────┤
│ Message ID: 123                                     │
│ Type: ChatService.Events.PolicyCreated              │
│ Processing Time: 45.23ms                            │
│ Status: Published & Deleted                         │
└─────────────────────────────────────────────────────┘
```

### Example

```csharp
_logger.LogInformation("┌─────────────────────────────────────────────────────┐");
_logger.LogInformation("│ ✅ SUCCESS                                          │");
_logger.LogInformation("├─────────────────────────────────────────────────────┤");
_logger.LogInformation($"│ Message ID: {message.Id,-38} │");
_logger.LogInformation($"│ Type: {message.Type,-44} │");
_logger.LogInformation($"│ Processing Time: {processingTime:F2}ms              │");
_logger.LogInformation($"│ Status: Published & Deleted                         │");
_logger.LogInformation("└─────────────────────────────────────────────────────┘");
```

### Khi nào log?
- Message được publish lên RabbitMQ thành công
- Message được delete khỏi database thành công
- Toàn bộ flow hoàn tất không có lỗi

---

## ❌ FAILURE LOGS

### Format

```
┌─────────────────────────────────────────────────────┐
│ ❌ FAILURE                                          │
├─────────────────────────────────────────────────────┤
│ Message ID: 123                                     │
│ Type: ChatService.Events.PolicyCreated              │
│ Error: Connection refused                           │
│ Processing Time: 1234.56ms                          │
└─────────────────────────────────────────────────────┘
```

### Example

```csharp
_logger.LogError("┌─────────────────────────────────────────────────────┐");
_logger.LogError("│ ❌ FAILURE                                          │");
_logger.LogError("├─────────────────────────────────────────────────────┤");
_logger.LogError($"│ Message ID: {message.Id,-38} │");
_logger.LogError($"│ Type: {message.Type,-44} │");
_logger.LogError($"│ Error: {ex.Message,-43} │");
_logger.LogError($"│ Processing Time: {processingTime:F2}ms              │");
_logger.LogError("└─────────────────────────────────────────────────────┘");
```

### Khi nào log?
- RabbitMQ connection failed
- Publish operation failed
- Bất kỳ exception nào trong quá trình xử lý

---

## 🔄 RETRY LOGS

### Format

```
┌─────────────────────────────────────────────────────┐
│ 🔄 RETRY SCHEDULED                                  │
├─────────────────────────────────────────────────────┤
│ Message ID: 123                                     │
│ Type: ChatService.Events.PolicyCreated              │
│ Retry Count: 2/5                                    │
│ Last Retry: 2026-04-10 03:15:30                     │
│ Next Retry: In 1 second                             │
└─────────────────────────────────────────────────────┘
```

### Example

```csharp
_logger.LogWarning("┌─────────────────────────────────────────────────────┐");
_logger.LogWarning("│ 🔄 RETRY SCHEDULED                                  │");
_logger.LogWarning("├─────────────────────────────────────────────────────┤");
_logger.LogWarning($"│ Message ID: {message.Id,-38} │");
_logger.LogWarning($"│ Type: {message.Type,-44} │");
_logger.LogWarning($"│ Retry Count: {dbMessage.RetryCount}/5               │");
_logger.LogWarning($"│ Last Retry: {dbMessage.LastRetryAt}                 │");
_logger.LogWarning($"│ Next Retry: In 1 second                             │");
_logger.LogWarning("└─────────────────────────────────────────────────────┘");
```

### Khi nào log?
- Sau khi increment retry count
- Khi retry count < 5
- Message sẽ được retry trong cycle tiếp theo

---

## ⚠️ DEAD LETTER LOGS

### Format

```
┌─────────────────────────────────────────────────────┐
│ ⚠️  DEAD LETTER QUEUE                               │
├─────────────────────────────────────────────────────┤
│ Message ID: 123                                     │
│ Type: ChatService.Events.PolicyCreated              │
│ Retry Count: 5                                      │
│ Last Retry: 2026-04-10 03:20:30                     │
│ Status: Max retries exceeded (5)                    │
└─────────────────────────────────────────────────────┘
```

### Example

```csharp
_logger.LogCritical("┌─────────────────────────────────────────────────────┐");
_logger.LogCritical("│ ⚠️  DEAD LETTER QUEUE                               │");
_logger.LogCritical("├─────────────────────────────────────────────────────┤");
_logger.LogCritical($"│ Message ID: {message.Id,-38} │");
_logger.LogCritical($"│ Type: {message.Type,-44} │");
_logger.LogCritical($"│ Retry Count: {dbMessage.RetryCount,-35} │");
_logger.LogCritical($"│ Last Retry: {dbMessage.LastRetryAt,-36} │");
_logger.LogCritical($"│ Status: Max retries exceeded (5)                    │");
_logger.LogCritical("└─────────────────────────────────────────────────────┘");
```

### Khi nào log?
- Retry count >= 5
- Message được move to dead letter queue
- Cần manual intervention

---

## 📊 BATCH SUMMARY

### Format

```
═══════════════════════════════════════════════════════
│ 📊 BATCH SUMMARY                                    │
├─────────────────────────────────────────────────────┤
│ Total Messages: 10                                  │
│ ✅ Success: 7                                       │
│ ❌ Failures: 2                                      │
│ 🔄 Retries: 2                                       │
│ ⚠️  Dead Letter: 1                                  │
│ Total Time: 1234.56ms                               │
═══════════════════════════════════════════════════════
```

### Example

```csharp
_logger.LogInformation("═══════════════════════════════════════════════════════");
_logger.LogInformation("│ 📊 BATCH SUMMARY                                    │");
_logger.LogInformation("├─────────────────────────────────────────────────────┤");
_logger.LogInformation($"│ Total Messages: {messages.Count,-34} │");
_logger.LogInformation($"│ ✅ Success: {successCount,-38} │");
_logger.LogInformation($"│ ❌ Failures: {failureCount,-37} │");
_logger.LogInformation($"│ 🔄 Retries: {retryCount,-38} │");
_logger.LogInformation($"│ ⚠️  Dead Letter: {deadLetterCount,-34} │");
_logger.LogInformation($"│ Total Time: {totalTime:F2}ms                        │");
_logger.LogInformation("═══════════════════════════════════════════════════════");
```

### Khi nào log?
- Sau khi xử lý xong batch
- Có ít nhất 1 message được xử lý
- Tổng kết statistics

---

## 🔍 DETAILED OPERATION LOGS

### Outbox.cs

```csharp
// Read
_logger.LogDebug($"[Outbox] Read {result.Count} unprocessed messages (retry count < 5)");

// Publish
_logger.LogInformation($"[Outbox] ✅ Published message {message.Id} of type {message.Type} to RabbitMQ");

// Delete
_logger.LogInformation($"[Outbox] 🗑️  Deleted message {messageId} from Messages table");

// Retry increment
_logger.LogWarning($"[Outbox] 🔄 Retry count incremented for message {messageId}: {oldRetryCount} → {message.RetryCount}");

// Dead letter
_logger.LogCritical($"[Outbox] ⚠️  DEAD LETTER: Message {messageId} moved to dead letter queue after {message.RetryCount} retries");
```

---

## 📝 LOG CONFIGURATION

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "ChatService.Services.OutboxSendingService": "Information",
      "ChatService.Services.Outbox": "Information"
    }
  }
}
```

### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "ChatService.Services.OutboxSendingService": "Debug",
      "ChatService.Services.Outbox": "Debug"
    }
  }
}
```

---

## 🎯 LOG FILTERING

### View only SUCCESS logs

```bash
dotnet run | grep "✅ SUCCESS"
```

### View only FAILURE logs

```bash
dotnet run | grep "❌ FAILURE"
```

### View only RETRY logs

```bash
dotnet run | grep "🔄 RETRY"
```

### View only DEAD LETTER logs

```bash
dotnet run | grep "⚠️  DEAD LETTER"
```

### View BATCH SUMMARY

```bash
dotnet run | grep "📊 BATCH SUMMARY" -A 7
```

---

## 📊 LOG ANALYSIS

### Count successes

```bash
cat logs.txt | grep "✅ SUCCESS" | wc -l
```

### Count failures

```bash
cat logs.txt | grep "❌ FAILURE" | wc -l
```

### Count retries

```bash
cat logs.txt | grep "🔄 RETRY" | wc -l
```

### Count dead letters

```bash
cat logs.txt | grep "⚠️  DEAD LETTER" | wc -l
```

---

## 🔧 STRUCTURED LOGGING (Optional)

### Using Serilog

```csharp
Log.Information("Outbox message processed", new
{
    MessageId = message.Id,
    Type = message.Type,
    Status = "Success",
    ProcessingTimeMs = processingTime,
    RetryCount = 0
});

Log.Error(ex, "Outbox message failed", new
{
    MessageId = message.Id,
    Type = message.Type,
    Status = "Failed",
    ProcessingTimeMs = processingTime,
    RetryCount = dbMessage.RetryCount,
    Error = ex.Message
});
```

---

## ✅ BENEFITS

1. **Easy Monitoring**: Visual indicators (✅❌🔄⚠️) dễ nhận biết
2. **Structured Format**: Box format dễ đọc và parse
3. **Complete Information**: Đầy đủ thông tin cho debugging
4. **Statistics**: Batch summary cho monitoring
5. **Filterable**: Dễ dàng filter và analyze logs

---

## 📸 EXAMPLE OUTPUT

```
═══════════════════════════════════════════════════════
[Outbox] Starting batch processing: 3 messages
═══════════════════════════════════════════════════════
[Outbox] Processing message 1 (Type: ChatService.Events.PolicyCreated)
┌─────────────────────────────────────────────────────┐
│ ✅ SUCCESS                                          │
├─────────────────────────────────────────────────────┤
│ Message ID: 1                                       │
│ Type: ChatService.Events.PolicyCreated              │
│ Processing Time: 45.23ms                            │
│ Status: Published & Deleted                         │
└─────────────────────────────────────────────────────┘

[Outbox] Processing message 2 (Type: ChatService.Events.PolicyCreated)
┌─────────────────────────────────────────────────────┐
│ ❌ FAILURE                                          │
├─────────────────────────────────────────────────────┤
│ Message ID: 2                                       │
│ Type: ChatService.Events.PolicyCreated              │
│ Error: Connection refused                           │
│ Processing Time: 1234.56ms                          │
└─────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────┐
│ 🔄 RETRY SCHEDULED                                  │
├─────────────────────────────────────────────────────┤
│ Message ID: 2                                       │
│ Type: ChatService.Events.PolicyCreated              │
│ Retry Count: 1/5                                    │
│ Last Retry: 2026-04-10 03:15:30                     │
│ Next Retry: In 1 second                             │
└─────────────────────────────────────────────────────┘

═══════════════════════════════════════════════════════
│ 📊 BATCH SUMMARY                                    │
├─────────────────────────────────────────────────────┤
│ Total Messages: 3                                   │
│ ✅ Success: 2                                       │
│ ❌ Failures: 1                                      │
│ 🔄 Retries: 1                                       │
│ ⚠️  Dead Letter: 0                                  │
│ Total Time: 1500.45ms                               │
═══════════════════════════════════════════════════════
```

---

## ✅ KẾT LUẬN

Logging đã được implement đầy đủ với:
- ✅ Success logs với processing time
- ❌ Failure logs với error details
- 🔄 Retry logs với retry count
- ⚠️ Dead letter logs cho max retry exceeded
- 📊 Batch summary statistics
- 🔍 Detailed operation logs

Ready for production monitoring! 🎉
