# OutboxSendingService - Background Service Guide

## Tổng quan

OutboxSendingService là implementation của IHostedService trong ASP.NET Core để chạy background job. Service này chạy mỗi 1 giây để xử lý outbox messages và gửi chúng qua RabbitMQ.

## Implementation

### 1. IHostedService Pattern
```csharp
public class OutboxSendingService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Timer chạy mỗi 1 giây
        _timer = new Timer(
            PushMessages,
            null,
            TimeSpan.Zero,                    // Start immediately
            TimeSpan.FromSeconds(1)           // Service này chạy mỗi 1 second
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
}
```

### 2. PushMessages Method
```csharp
private async void PushMessages(object? state)
{
    using var scope = _serviceProvider.CreateScope();
    
    var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();
    var rabbitPublisher = scope.ServiceProvider.GetRequiredService<IRabbitEventPublisher>();

    // Lấy messages chưa xử lý từ outbox
    var messages = await outbox.GetUnprocessedMessagesAsync(5);

    foreach (var message in messages)
    {
        try
        {
            // Recreate event từ JSON payload
            var eventData = message.RecreateEvent();
            if (eventData != null)
            {
                // Publish qua RabbitMQ
                await rabbitPublisher.PublishAsync(eventData);
                
                // Mark as processed
                await outbox.MarkAsProcessedAsync(message.Id);
            }
        }
        catch (Exception ex)
        {
            // Log error, retry next time
        }
    }
}
```

## Dependency Injection Configuration

### Program.cs
```csharp
// Đăng ký Outbox services
builder.Services.AddScoped<IOutbox, Outbox>();

// ⭐ PHẦN 5 - BACKGROUND SERVICE
builder.Services.AddHostedService<OutboxSendingService>();  // Chạy mỗi 1 giây
```

## Service Characteristics

### Timing
- **Interval**: 1 giây (theo yêu cầu)
- **Start Delay**: 0 (bắt đầu ngay lập tức)
- **Batch Size**: 5 messages per cycle

### Processing Flow
1. **Timer Trigger**: Mỗi 1 giây
2. **Get Messages**: Lấy 5 messages chưa xử lý từ outbox
3. **Process Each Message**:
   - Recreate event object từ JSON
   - Publish qua RabbitMQ
   - Mark as processed
4. **Error Handling**: Log errors, retry next cycle

### Dependency Injection Scope
- Sử dụng `IServiceProvider.CreateScope()` để tạo scoped services
- Đảm bảo DbContext được dispose đúng cách
- Thread-safe với multiple concurrent operations

## Monitoring APIs

### 1. Outbox Status
```http
GET /api/outboxmonitor/status
```

Response:
```json
{
  "status": "OutboxSendingService running every 1 second",
  "unprocessedCount": 3,
  "recentUnprocessedMessages": [...],
  "serviceInfo": {
    "serviceName": "OutboxSendingService",
    "interval": "1 second",
    "description": "IHostedService running background job to push outbox messages"
  }
}
```

### 2. Performance Stats
```http
GET /api/outboxmonitor/stats
```

Response:
```json
{
  "totalUnprocessed": 3,
  "oldestUnprocessedMessage": "2024-04-04T06:47:26.123Z",
  "messagesByType": [
    { "type": "ChatService.Events.PolicyCreated", "count": 2 },
    { "type": "ChatService.Events.ProductActivated", "count": 1 }
  ],
  "serviceConfiguration": {
    "serviceType": "IHostedService",
    "processingInterval": "1 second",
    "batchSize": 5,
    "autoRetry": true
  }
}
```

## Comparison với OutboxProcessorService

| Feature | OutboxSendingService | OutboxProcessorService |
|---------|---------------------|------------------------|
| **Interval** | 1 giây | 30 giây |
| **Base Class** | IHostedService | BackgroundService |
| **Batch Size** | 5 messages | 50 messages |
| **Use Case** | Real-time processing | Batch processing |
| **Resource Usage** | Higher (frequent runs) | Lower (less frequent) |

## Configuration Options

### appsettings.json
```json
{
  "OutboxSendingService": {
    "IntervalSeconds": 1,
    "BatchSize": 5,
    "MaxRetryAttempts": 3,
    "EnableDetailedLogging": true
  }
}
```

### Environment Variables
```bash
OUTBOX_SENDING_INTERVAL=1
OUTBOX_BATCH_SIZE=5
OUTBOX_ENABLE_LOGGING=true
```

## Performance Considerations

### 1. Frequency vs Resource Usage
- **1 giây interval**: Responsive nhưng sử dụng nhiều CPU/DB connections
- **Batch size 5**: Cân bằng giữa throughput và latency
- **Scoped services**: Đảm bảo proper disposal

### 2. Error Handling
- **Individual message errors**: Không block toàn bộ batch
- **Retry mechanism**: Automatic retry trong cycle tiếp theo
- **Logging**: Chi tiết để debug

### 3. Scalability
- **Single instance**: Tránh duplicate processing
- **Database locking**: Sử dụng database-level locking nếu cần
- **Horizontal scaling**: Cần coordination mechanism

## Testing

### 1. Unit Testing
```csharp
[Test]
public async Task PushMessages_ShouldProcessUnprocessedMessages()
{
    // Arrange
    var mockOutbox = new Mock<IOutbox>();
    var mockPublisher = new Mock<IRabbitEventPublisher>();
    
    mockOutbox.Setup(x => x.GetUnprocessedMessagesAsync(5))
             .ReturnsAsync(new List<OutboxMessage> { /* test data */ });

    // Act & Assert
    // Test PushMessages logic
}
```

### 2. Integration Testing
```csharp
[Test]
public async Task OutboxSendingService_ShouldProcessMessagesEverySecond()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    
    // Create test messages in outbox
    // Wait for processing
    // Verify messages are processed
}
```

### 3. Load Testing
- Tạo nhiều messages trong outbox
- Monitor processing time
- Verify no message loss
- Check resource usage

## Troubleshooting

### 1. Service Not Starting
```
OutboxSendingService not registered or failed to start
```
**Solution**: Kiểm tra đăng ký trong Program.cs:
```csharp
builder.Services.AddHostedService<OutboxSendingService>();
```

### 2. Messages Not Processing
```
Messages remain unprocessed in outbox
```
**Solutions**:
- Kiểm tra RabbitMQ connection
- Verify IOutbox registration
- Check logs for errors

### 3. High CPU Usage
```
OutboxSendingService consuming too much CPU
```
**Solutions**:
- Tăng interval từ 1 giây lên cao hơn
- Giảm batch size
- Optimize database queries

### 4. Memory Leaks
```
Memory usage increasing over time
```
**Solutions**:
- Đảm bảo proper disposal của scoped services
- Check for event handler leaks
- Monitor DbContext lifecycle

## Best Practices

1. **Error Handling**: Always catch và log exceptions
2. **Resource Management**: Sử dụng `using` statements cho scoped services
3. **Monitoring**: Implement health checks và metrics
4. **Configuration**: Make interval và batch size configurable
5. **Graceful Shutdown**: Handle cancellation tokens properly
6. **Database Optimization**: Use appropriate indexes on outbox table
7. **Logging**: Structured logging với correlation IDs

## Production Deployment

### Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<OutboxSendingServiceHealthCheck>("outbox-sending-service");
```

### Metrics
- Messages processed per second
- Average processing time
- Error rate
- Queue depth

### Alerts
- Outbox queue growing too large
- Processing errors exceeding threshold
- Service stopped or crashed