# Dependency Injection Configuration Summary

## ✅ Cấu hình đã hoàn thành trong Program.cs

```csharp
// ⭐ PHẦN 4 - CONFIGURE DEPENDENCY INJECTION
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
```

## Toàn bộ cấu hình Outbox Pattern Services

```csharp
// Database Context
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Port=5432;Database=ChatServiceDb;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString);
});

// ⭐ Outbox Pattern Services
builder.Services.AddScoped<ChatService.Services.ISession, DatabaseSession>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();           // ← CHÍNH
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<ITransactionalEventService, TransactionalEventService>();
builder.Services.AddHostedService<OutboxProcessorService>();

// RabbitMQ Services (cho background processing)
builder.Services.AddSingleton<IRabbitEventPublisher, RabbitEventPublisher>();
builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(sp =>
{
    return new RabbitMQ.Client.ConnectionFactory()
    {
        HostName = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost",
        Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        UserName = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
        Password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest",
        AutomaticRecoveryEnabled = true,
        NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
    };
});
```

## Service Lifetimes Explained

| Service | Lifetime | Lý do |
|---------|----------|-------|
| `IEventPublisher` | **Scoped** | Mỗi request có instance riêng, share DbContext |
| `ISession` | **Scoped** | Database session per request |
| `IOutboxService` | **Scoped** | Database operations per request |
| `ITransactionalEventService` | **Scoped** | Transaction management per request |
| `IRabbitEventPublisher` | **Singleton** | Shared connection pool, thread-safe |
| `IConnectionFactory` | **Singleton** | Connection factory configuration |
| `OutboxProcessorService` | **Hosted** | Background service, application lifetime |

## Cách sử dụng trong Controllers

### 1. Constructor Injection
```csharp
public class PolicyController : ControllerBase
{
    private readonly IEventPublisher _eventPublisher;

    // ✅ DI Container tự động inject OutboxEventPublisher
    public PolicyController(IEventPublisher eventPublisher)
    {
        _eventPublisher = eventPublisher;
    }
}
```

### 2. Sử dụng trong Action Methods
```csharp
[HttpPost]
public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
{
    // Business logic
    var policy = new Policy(request);
    
    // ✅ Publish event - sẽ lưu vào outbox thay vì gửi RabbitMQ trực tiếp
    await _eventPublisher.PublishMessage(new PolicyCreated
    {
        PolicyNumber = policy.Number,
        Premium = policy.Premium,
        Status = "Created",
        CreatedAt = DateTime.UtcNow
    });

    return Ok(policy);
}
```

## Luồng hoạt động với DI

1. **Request đến Controller**
2. **DI Container** tạo instance của:
   - `OutboxEventPublisher` (implements `IEventPublisher`)
   - `DatabaseSession` (implements `ISession`)
   - `ChatDbContext`
3. **Controller** gọi `_eventPublisher.PublishMessage(event)`
4. **OutboxEventPublisher** lưu event vào database
5. **OutboxProcessorService** (background) xử lý events từ outbox

## Testing với DI

### Unit Test
```csharp
[Test]
public async Task CreatePolicy_ShouldPublishEvent()
{
    // Arrange
    var mockEventPublisher = new Mock<IEventPublisher>();
    var controller = new PolicyController(mockEventPublisher.Object);

    // Act
    await controller.CreatePolicy(new CreatePolicyRequest());

    // Assert
    mockEventPublisher.Verify(x => x.PublishMessage(It.IsAny<PolicyCreated>()), Times.Once);
}
```

### Integration Test
```csharp
[Test]
public async Task CreatePolicy_ShouldSaveToOutbox()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    // Act
    var response = await client.PostAsJsonAsync("/api/policy", new CreatePolicyRequest());

    // Assert - Verify event saved to outbox
    using var scope = factory.Services.CreateScope();
    var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
    var unprocessedCount = await outboxService.GetUnprocessedCountAsync();
    
    Assert.That(unprocessedCount, Is.GreaterThan(0));
}
```

## Configuration Options

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ChatServiceDb;Username=postgres;Password=postgres"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

## Troubleshooting

### 1. Service Not Registered
```
System.InvalidOperationException: Unable to resolve service for type 'IEventPublisher'
```
**Solution**: Đảm bảo có dòng này trong Program.cs:
```csharp
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
```

### 2. Circular Dependency
```
System.InvalidOperationException: A circular dependency was detected
```
**Solution**: Kiểm tra constructor dependencies, có thể cần sử dụng factory pattern.

### 3. DbContext Disposed
```
System.ObjectDisposedException: Cannot access a disposed context instance
```
**Solution**: Đảm bảo tất cả database-related services đều là Scoped lifetime.

## Best Practices ✅

1. **Interface Segregation**: Sử dụng interface thay vì concrete class
2. **Appropriate Lifetimes**: Scoped cho database, Singleton cho connections
3. **Constructor Injection**: Prefer constructor injection over service locator
4. **Explicit Registration**: Đăng ký tất cả dependencies explicitly
5. **Testing**: Mock interfaces để unit test dễ dàng

## Demo APIs

Sử dụng các API sau để test DI configuration:

- `POST /api/policydemo/create-policy-simple` - Demo IEventPublisher
- `POST /api/policydemo/create-policy-transactional` - Demo ITransactionalEventService  
- `GET /api/policydemo/outbox-status` - Monitor outbox

File test: `test-dependency-injection-demo.http`