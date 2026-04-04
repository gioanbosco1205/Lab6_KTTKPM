using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using ChatService.Services;
using ChatService.Hubs;
using ChatService.Data;
using ChatService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// ⭐ PHẦN MỚI - Cấu hình Entity Framework với PostgreSQL
builder.Services.AddDbContext<ChatDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Host=localhost;Port=5432;Database=ChatServiceDb;Username=postgres;Password=postgres";
    options.UseNpgsql(connectionString);
});

// ⭐ PHẦN MỚI - Đăng ký Repository
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// Cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials()
               .SetIsOriginAllowed(origin => true); // Allow all origins in development
    });
});

// Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
    
    // Cấu hình để SignalR có thể sử dụng JWT từ query string
    x.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Thêm SignalR
builder.Services.AddSignalR();

// Thêm MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Đăng ký JWT Service
builder.Services.AddScoped<ChatService.Services.IJwtService, ChatService.Services.JwtService>();

// Đăng ký RabbitEventPublisher
builder.Services.AddSingleton<IRabbitEventPublisher, RabbitEventPublisher>();

// ⭐ PHẦN MỚI - Đăng ký Outbox Services
builder.Services.AddScoped<ChatService.Services.ISession, DatabaseSession>();
builder.Services.AddScoped<IEventPublisher, OutboxEventPublisher>();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<ITransactionalEventService, TransactionalEventService>();
builder.Services.AddHostedService<OutboxProcessorService>();

// ⭐ PHẦN 13 - Đăng ký Event Subscriber Services
builder.Services.AddScoped<PolicyEventSubscriber>();
builder.Services.AddHostedService<EventSubscriberHostedService>();

// Cấu hình RabbitMQ Connection
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ⭐ PHẦN MỚI - Auto migrate database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
    try
    {
        context.Database.Migrate();
        Console.WriteLine("✅ Database migration completed successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database migration failed: {ex.Message}");
        // Continue running even if migration fails
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Disable HTTPS redirection for development
// app.UseHttpsRedirection();

// Sử dụng CORS
app.UseCors("CorsPolicy");

// Thêm Authentication và Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR Hub
app.MapHub<AgentChatHub>("/chathub");

app.Run();
