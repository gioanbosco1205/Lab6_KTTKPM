# Dependency Injection Configuration Guide

## Cấu hình trong Program.cs

### Outbox Pattern Services
```csharp
// ⭐ PHẦN MỚI - Đăng ký Outbox Services
builder.Services.AddScoped<ChatService.Services.ISession, DatabaseSession>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<ITransactionalEventService, TransactionalEventService>();
builder.Services.AddHostedService<OutboxProcessorService>();
```

### Các Services liên quan
```csharp
// Entity Framework DbContext
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Port=5432;Database=ChatServiceDb;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString);
});

// RabbitMQ Event Publisher (cho background processing)
builder.Services.AddSingleton<IRabbitEventPublisher, RabbitEventPublisher>();

// RabbitMQ Connection Factory
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

## Service Lifetimes

### Scoped Services (Per Request)
- **IEventPublisher**: Mỗi request có một instance riêng
- **ISession**: Database session per request
- **IOutboxService**: Outbox operations per request
- **ITransactionalEventService**: Transaction management per request

### Singleton Services (Application Lifetime)
- **IRabbitEventPublisher**: Shared connection pool
- **IConnectionFactory**: RabbitMQ connection factory

### Hosted Services (Background)
- **OutboxProcessorService**: Background service xử lý outbox messages

## Cách sử dụng trong Controllers

### 1. Inject IEventPublisher
```csharp
[ApiController]
[Route("api/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PolicyController> _logger;

    public PolicyController(
        IEventPublisher eventPublisher,
        ILogger<PolicyController> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        // Business logic
        var policy = new Policy(request);
        
        // Publish event using Outbox Pattern
        var policyCreatedEvent = new PolicyCreated
        {
            PolicyNumber = policy.Number,
            Premium = policy.Premium,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        await _eventPublisher.PublishMessage(policyCreatedEvent);

        return Ok(policy);
    }
}
```

### 2. Inject ITransactionalEventService (Khuyến nghị)
```csharp
[ApiController]
[Route("api/[controller]")]
public class PolicyController : ControllerBase
{
    private readonly ITransactionalEventService _transactionalEventService;
    private readonly IPolicyRepository _policyRepository;

    public PolicyController(
        ITransactionalEventService transactionalEventService,
        IPolicyRepository policyRepository)
    {
        _transactionalEventService = transactionalEventService;
        _policyRepository = policyRepository;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        var policy = new Policy(request);
        var policyCreatedEvent = new PolicyCreated
        {
            PolicyNumber = policy.Number,
            Premium = policy.Premium,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };

        // Lưu policy và publish event trong cùng transaction
        await _transactionalEventService.SaveDataAndPublishEventAsync(
            async (context) =>
            {
                await _policyRepository.SaveAsync(policy);
            },
            policyCreatedEvent
        );

        return Ok(policy);
    }
}
```

## Cách sử dụng trong Services

### 1. Business Service với Event Publishing
```csharp
public class PolicyService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IPolicyRepository _policyRepository;

    public PolicyService(
        IEventPublisher eventPublisher,
        IPolicyRepository policyRepository)
    {
        _eventPublisher = eventPublisher;
        _policyRepository = policyRepository;
    }

    public async Task<Policy> CreatePolicyAsync(CreatePolicyRequest request)
    {
        var policy = new Policy(request);
        await _policyRepository.SaveAsync(policy);

        // Publish event
        await _eventPublisher.PublishMessage(new PolicyCreated
        {
            PolicyNumber = policy.Number,
            Premium = policy.Premium,
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        });

        return policy;
    }
}
```

### 2. Đăng ký Business Service
```csharp
// Trong Program.cs
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
```

## Testing với Dependency Injection

### 1. Unit Test với Mock
```csharp
[Test]
public async Task CreatePolicy_ShouldPublishEvent()
{
    // Arrange
    var mockEventPublisher = new Mock<IEventPublisher>();
    var mockRepository = new Mock<IPolicyRepository>();
    var service = new PolicyService(mockEventPublisher.Object, mockRepository.Object);

    var request = new CreatePolicyRequest { /* ... */ };

    // Act
    await service.CreatePolicyAsync(request);

    // Assert
    mockEventPublisher.Verify(x => x.PublishMessage(It.IsAny<PolicyCreated>()), Times.Once);
}
```

### 2. Integration Test
```csharp
[Test]
public async Task CreatePolicy_ShouldSaveToOutbox()
{
    // Arrange
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();

    // Act
    var response = await client.PostAsJsonAsync("/api/policy", new CreatePolicyRequest());

    // Assert
    response.EnsureSuccessStatusCode();
    
    // Verify outbox contains the event
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
  },
  "OutboxProcessor": {
    "ProcessingIntervalSeconds": 30,
    "BatchSize": 50
  }
}
```

### Environment-specific Configuration
```csharp
// Development
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
}

// Production với additional monitoring
if (builder.Environment.IsProduction())
{
    builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
    builder.Services.AddScoped<IEventPublisherMonitor, EventPublisherMonitor>();
}
```

## Troubleshooting

### 1. Service Resolution Errors
```
InvalidOperationException: Unable to resolve service for type 'IEventPublisher'
```
**Solution**: Đảm bảo đã đăng ký service trong Program.cs:
```csharp
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
```

### 2. Circular Dependencies
```
InvalidOperationException: A circular dependency was detected
```
**Solution**: Kiểm tra constructor dependencies và sử dụng factory pattern nếu cần.

### 3. Database Context Issues
```
InvalidOperationException: DbContext has been disposed
```
**Solution**: Đảm bảo sử dụng scoped lifetime cho DbContext và các services liên quan.

## Best Practices

1. **Sử dụng Interface**: Luôn inject interface thay vì concrete class
2. **Scoped Lifetime**: Sử dụng scoped cho database-related services
3. **Singleton cho Connections**: Connection factories nên là singleton
4. **Hosted Services**: Background services nên được đăng ký như hosted services
5. **Configuration**: Sử dụng IOptions pattern cho complex configuration
6. **Testing**: Mock interfaces để unit test dễ dàng