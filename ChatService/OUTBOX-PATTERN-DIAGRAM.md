# OUTBOX PATTERN - VISUAL DIAGRAM

## 🎯 COMPLETE FLOW VISUALIZATION

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         OUTBOX PATTERN FLOW                              │
│                    (PHẦN 6, 7, 8 - HOÀN CHỈNH)                          │
└─────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────────────────────────────────────────┐
│  CLIENT REQUEST                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ POST /api/event/publish-policy-created
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 1: CONTROLLER (EventController.cs)                                │
│  ─────────────────────────────────────────────────────────────────────  │
│  [HttpPost("publish-policy-created")]                                    │
│  public async Task<IActionResult> PublishPolicyCreated(...)             │
│  {                                                                       │
│      var event = new PolicyCreated { ... };                             │
│      await _eventPublisher.PublishMessage(event);  ◄─── IEventPublisher │
│      return Ok(...);                                                     │
│  }                                                                       │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Inject: IEventPublisher
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 2: SAVE TO DATABASE (OutboxEventPublisher.cs)                     │
│  ─────────────────────────────────────────────────────────────────────  │
│  public async Task PublishMessage<T>(T message)                          │
│  {                                                                       │
│      await _session.SaveAsync(new Message(message));        ◄─── Table 1│
│      await _session.SaveAsync(new OutboxMessage(message));  ◄─── Table 2│
│  }                                                                       │
│                                                                          │
│  ✅ TRANSACTION COMMITTED                                               │
│  ✅ Event safely stored in database                                     │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Data persisted
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  DATABASE                                                                │
│  ─────────────────────────────────────────────────────────────────────  │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │ Messages Table (Event Store)                                 │       │
│  ├────┬──────────────────────────┬─────────────────────────────┤       │
│  │ Id │ Type                     │ Payload                      │       │
│  ├────┼──────────────────────────┼─────────────────────────────┤       │
│  │ 1  │ PolicyCreated            │ {"PolicyNumber":"POL-001"...}│       │
│  │ 2  │ PolicyCreated            │ {"PolicyNumber":"POL-002"...}│       │
│  └────┴──────────────────────────┴─────────────────────────────┘       │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────┐       │
│  │ OutboxMessages Table (Optional tracking)                     │       │
│  ├────┬──────────────────────────┬─────────────┬──────────────┤       │
│  │ Id │ Type                     │ IsProcessed │ CreatedAt     │       │
│  ├────┼──────────────────────────┼─────────────┼──────────────┤       │
│  │ 1  │ PolicyCreated            │ false       │ 10:30:00     │       │
│  └────┴──────────────────────────┴─────────────┴──────────────┘       │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Polling every 1 second
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 3: BACKGROUND JOB (OutboxSendingService.cs)                       │
│  ─────────────────────────────────────────────────────────────────────  │
│  public Task StartAsync(CancellationToken cancellationToken)             │
│  {                                                                       │
│      _timer = new Timer(                                                 │
│          PushMessages,                                                   │
│          null,                                                           │
│          TimeSpan.Zero,              // Start immediately                │
│          TimeSpan.FromSeconds(1)     // ⏰ Run every 1 second           │
│      );                                                                  │
│  }                                                                       │
│                                                                          │
│  ⏰ Timer triggers PushMessages() every 1 second                        │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Timer tick
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 4: READ MESSAGES (Outbox.cs)                                      │
│  ─────────────────────────────────────────────────────────────────────  │
│  public async Task<List<OutboxMessage>> ReadMessagesAsync(int batch)    │
│  {                                                                       │
│      using var scope = _serviceProvider.CreateScope();                  │
│      var context = scope.ServiceProvider.GetRequiredService<...>();     │
│                                                                          │
│      var messages = await context.Messages                              │
│          .OrderBy(m => m.Id)         ◄─── Oldest first                 │
│          .Take(batch)                ◄─── Batch size (5-50)            │
│          .ToListAsync();                                                 │
│                                                                          │
│      return ConvertToOutboxMessages(messages);                           │
│  }                                                                       │
│                                                                          │
│  📖 Read unprocessed messages from database                             │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Messages retrieved
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 5: PUBLISH TO RABBITMQ (Outbox.cs + RabbitEventPublisher.cs)      │
│  ─────────────────────────────────────────────────────────────────────  │
│  public async Task PublishMessageAsync(OutboxMessage message)            │
│  {                                                                       │
│      var eventData = message.RecreateEvent();                            │
│      await _rabbitPublisher.PublishAsync(eventData);  ◄─── RabbitMQ    │
│  }                                                                       │
│                                                                          │
│  RabbitEventPublisher.PublishAsync():                                    │
│  {                                                                       │
│      var json = JsonConvert.SerializeObject(message);                    │
│      var body = Encoding.UTF8.GetBytes(json);                            │
│                                                                          │
│      _channel.BasicPublish(                                              │
│          exchange: "policy.events",      ◄─── Exchange                 │
│          routingKey: "policycreated",    ◄─── Routing key              │
│          body: body                                                      │
│      );                                                                  │
│  }                                                                       │
│                                                                          │
│  📤 Message sent to RabbitMQ                                            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Publish successful
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  RABBITMQ EXCHANGE                                                       │
│  ─────────────────────────────────────────────────────────────────────  │
│  Exchange: policy.events                                                 │
│  Type: Topic                                                             │
│                                                                          │
│  ┌──────────────────────────────────────────────────────────┐          │
│  │ Routing Key: policycreated                                │          │
│  │ Message: {"PolicyNumber":"POL-001","Premium":1500,...}    │          │
│  └──────────────────────────────────────────────────────────┘          │
│                    │                           │                         │
│                    │                           │                         │
│         ┌──────────┴──────────┐    ┌──────────┴──────────┐            │
│         │ Queue: payment       │    │ Queue: chat         │            │
│         │ Binding: policy*     │    │ Binding: policy*    │            │
│         └──────────────────────┘    └─────────────────────┘            │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ After successful publish
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  STEP 6: DELETE MESSAGE (Outbox.cs)                                     │
│  ─────────────────────────────────────────────────────────────────────  │
│  public async Task DeleteMessageAsync(long messageId)                    │
│  {                                                                       │
│      using var scope = _serviceProvider.CreateScope();                  │
│      var context = scope.ServiceProvider.GetRequiredService<...>();     │
│                                                                          │
│      await context.Messages                                              │
│          .Where(m => m.Id == messageId)                                  │
│          .ExecuteDeleteAsync();          ◄─── Delete from DB           │
│                                                                          │
│      _logger.LogInformation($"Deleted message {messageId}");             │
│  }                                                                       │
│                                                                          │
│  🗑️ Message removed from database                                      │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Cleanup complete
                                    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  ✅ FLOW COMPLETE                                                       │
│  ─────────────────────────────────────────────────────────────────────  │
│  • Event published to RabbitMQ                                           │
│  • Message deleted from database                                         │
│  • Subscribers will receive the event                                    │
│  • Ready for next batch                                                  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 ERROR HANDLING FLOW

```
┌─────────────────────────────────────────────────────────────────────────┐
│  WHAT IF RABBITMQ IS DOWN?                                               │
└─────────────────────────────────────────────────────────────────────────┘

STEP 5: Publish to RabbitMQ
    │
    │ ❌ Connection failed
    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  catch (Exception ex)                                                    │
│  {                                                                       │
│      _logger.LogError(ex, "Failed to process message");                  │
│      // ⚠️ DO NOT DELETE MESSAGE                                        │
│  }                                                                       │
└─────────────────────────────────────────────────────────────────────────┘
    │
    │ Message stays in database
    ↓
┌─────────────────────────────────────────────────────────────────────────┐
│  NEXT TIMER TICK (1 second later)                                       │
│  ─────────────────────────────────────────────────────────────────────  │
│  • Read same message again                                               │
│  • Retry publish                                                         │
│  • If successful → Delete                                                │
│  • If failed → Keep for next retry                                       │
│                                                                          │
│  ✅ GUARANTEED DELIVERY                                                 │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 🎯 DEPENDENCY INJECTION (PHẦN 7)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Program.cs - Service Registration                                       │
└─────────────────────────────────────────────────────────────────────────┘

builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
                    │                           │
                    │                           └─── Implementation
                    └─── Interface (Scoped lifetime)

builder.Services.AddSingleton<Outbox>();
                    │            │
                    │            └─── Concrete class
                    └─── Singleton lifetime (1 instance for entire app)

builder.Services.AddSingleton<IOutbox>(sp => sp.GetRequiredService<Outbox>());
                    │            │
                    │            └─── Alias for interface
                    └─── Resolves to same Singleton instance

builder.Services.AddHostedService<OutboxSendingService>();
                    │                    │
                    │                    └─── Background service
                    └─── Starts automatically with app


┌─────────────────────────────────────────────────────────────────────────┐
│  LIFETIME EXPLANATION                                                    │
└─────────────────────────────────────────────────────────────────────────┘

Scoped (IEventPublisher):
• New instance per HTTP request
• Shares DbContext with controller
• Transaction safety

Singleton (Outbox):
• One instance for entire application
• Reused by background job
• Uses IServiceProvider to create scopes for DbContext

HostedService (OutboxSendingService):
• Starts when app starts
• Runs in background
• Independent of HTTP requests
```

---

## 📊 TIMING DIAGRAM

```
Time    Controller          Database            Background Job      RabbitMQ
────────────────────────────────────────────────────────────────────────────
T+0s    POST request
        │
        ├─► Save event ──► [Message saved]
        │                  [Outbox saved]
        │
        └─► 200 OK
                                                                    
T+1s                                            Timer tick
                                                │
                                                ├─► Read messages
                                                │   from DB
                                                │
                                                ├─► Publish ──────► [Received]
                                                │
                                                └─► Delete from DB
                                                
T+2s                                            Timer tick
                                                │
                                                └─► No messages
                                                    (idle)

T+3s    POST request
        │
        ├─► Save event ──► [Message saved]
        │
        └─► 200 OK

T+4s                                            Timer tick
                                                │
                                                ├─► Read messages
                                                │
                                                ├─► Publish ──────► [Received]
                                                │
                                                └─► Delete from DB
```

---

## 🎓 KEY CONCEPTS

### 1. Transactional Outbox Pattern
```
Business Operation + Event Storage = Single Transaction
                                     ↓
                            All or Nothing
```

### 2. At-Least-Once Delivery
```
Event may be sent multiple times (if delete fails)
BUT
Event will NEVER be lost
```

### 3. Eventual Consistency
```
Event stored immediately (synchronous)
Event published later (asynchronous)
                ↓
        Eventually consistent
```

### 4. Decoupling
```
Business Logic ──► Database ──► Background Job ──► RabbitMQ
                                                        ↓
                                                  Subscribers
```

---

## ✅ BENEFITS

1. **Reliability**: Events never lost, even if RabbitMQ is down
2. **Consistency**: Business data and events always in sync
3. **Retry**: Automatic retry on failure
4. **Performance**: Non-blocking for user requests
5. **Monitoring**: Can track unprocessed events in database

---

## 📝 SUMMARY

```
PHẦN 6: Outbox class với 3 methods (Read, Publish, Delete)
PHẦN 7: DI registration (Scoped, Singleton, HostedService)
PHẦN 8: Complete flow (Controller → DB → Background → RabbitMQ)

Result: Reliable, consistent, decoupled event publishing! 🎉
```
