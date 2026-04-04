# OutboxEventPublisher Implementation Guide

## Tổng quan

OutboxEventPublisher là implementation của Outbox Pattern, thay thế việc gửi RabbitMQ trực tiếp bằng cách lưu events vào database. Events sẽ được xử lý bởi background service sau đó.

## So sánh Before/After

### Trước đây (Direct Publishing):
```csharp
// Gửi RabbitMQ trực tiếp - có thể mất event nếu có lỗi
await eventPublisher.PublishMessage(event);
```

### Bây giờ (Outbox Pattern):
```csharp
// Lưu event vào database - đảm bảo không mất event
await eventPublisher.PublishMessage(event);
// Event trở thành data trong database
```

## Implementation

### 1. IEventPublisher Interface
```csharp
public interface IEventPublisher
{
    Task PublishMessage<T>(T message);
}
```

### 2. OutboxEventPublisher Class
```csharp
public class OutboxEventPublisher : IEventPublisher
{
    private readonly ISession session;

    public OutboxEventPublisher(ISession session)
    {
        this.session = session;
    }

    public async Task PublishMessage<T>(T msg)
    {
        // Lưu vào Message table (Event Store)
        await session.SaveAsync(new Message(msg));
        
        // Lưu vào OutboxMessage table (để background service xử lý)
        await session.SaveAsync(new OutboxMessage(msg));
    }
}
```

### 3. ISession Pattern
```csharp
public interface ISession
{
    Task SaveAsync<T>(T entity) where T : class;
    Task<T?> GetAsync<T>(object id) where T : class;
    Task DeleteAsync<T>(T entity) where T : class;
    Task FlushAsync();
}
```

## Cách sử dụng

### 1. Inject IEventPublisher
```csharp
public class PolicyController : ControllerBase
{
    private readonly IEventPublisher _eventPublisher;

    public PolicyController(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
}
```

### 2. Publish Events
```csharp
[HttpPost]
public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
{
    // Tạo policy
    var policy = new Policy(request);
    await _policyRepository.SaveAsync(policy);

    // Publish event - lưu vào database thay vì gửi RabbitMQ trực tiếp
    var policyCreatedEvent = new PolicyCreated
    {
        PolicyId = policy.Id,
        PolicyNumber = policy.Number,
        CustomerId = policy.CustomerId,
        CreatedAt = DateTime.UtcNow
    };

    await _eventPublisher.PublishMessage(policyCreatedEvent);

    return Ok(policy);
}
```

## Luồng xử lý

1. **Business Logic**: Tạo/cập nhật dữ liệu chính
2. **Event Publishing**: Gọi `_eventPublisher.PublishMessage(event)`
3. **Database Storage**: Event được lưu vào 2 bảng:
   - `Messages`: Event Store (lưu trữ lâu dài)
   - `OutboxMessages`: Outbox (chờ xử lý)
4. **Background Processing**: OutboxProcessorService xử lý:
   - Lấy events chưa xử lý từ outbox
   - Publish qua RabbitMQ
   - Đánh dấu đã xử lý

## Lợi ích

### 1. Tính nhất quán (Consistency)
- Events không bị mất khi có lỗi network
- Đảm bảo dữ liệu và events luôn đồng bộ

### 2. Độ tin cậy (Reliability)
- Events được lưu trong database
- Tự động retry khi publish thất bại
- Có thể monitor và debug

### 3. Hiệu suất (Performance)
- Không block business logic
- Batch processing trong background
- Giảm tải cho RabbitMQ

### 4. Khả năng mở rộng (Scalability)
- Có thể scale background processors
- Xử lý theo batch để tối ưu hiệu suất

## Testing

### 1. Test API
```bash
# Publish event
POST /api/event/publish-policy-created
{
  "policyId": "POL-001",
  "policyNumber": "POL-2024-001",
  "customerId": "CUST-001",
  "productId": "PROD-001",
  "premiumAmount": 1000000
}

# Check outbox status
GET /api/event/outbox/status
```

### 2. Verify Database
```sql
-- Kiểm tra events đã lưu
SELECT * FROM Messages ORDER BY Id DESC LIMIT 10;

-- Kiểm tra outbox
SELECT * FROM outbox_messages WHERE is_processed = false;
```

## Monitoring

### 1. Outbox Status
```csharp
// Số lượng events chưa xử lý
var unprocessedCount = await _outboxService.GetUnprocessedCountAsync();

// Danh sách events chưa xử lý
var unprocessedMessages = await _outboxService.GetUnprocessedMessagesAsync(100);
```

### 2. Metrics cần theo dõi
- Số lượng events chưa xử lý
- Thời gian xử lý trung bình
- Tỷ lệ thành công/thất bại
- Độ trễ giữa tạo event và publish

## Best Practices

1. **Sử dụng Transaction**: Đảm bảo dữ liệu chính và events được lưu trong cùng transaction
2. **Idempotent Events**: Thiết kế events có thể xử lý nhiều lần
3. **Event Versioning**: Có strategy cho việc thay đổi event schema
4. **Monitoring**: Theo dõi outbox để phát hiện vấn đề sớm
5. **Cleanup**: Định kỳ dọn dẹp events đã xử lý cũ