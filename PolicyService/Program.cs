using Polly;
using Steeltoe.Discovery.Client;
using PolicyService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDiscoveryClient(builder.Configuration);
builder.Services.AddControllers();

// ⭐ CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<PricingClient>();

// ⭐ RabbitMQ Configuration
builder.Services.AddSingleton<RabbitMQ.Client.IConnectionFactory>(sp =>
{
    return new RabbitMQ.Client.ConnectionFactory()
    {
        HostName = builder.Configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost",
        Port = builder.Configuration.GetValue<int>("RabbitMQ:Port", 5672),
        UserName = builder.Configuration.GetValue<string>("RabbitMQ:Username") ?? "guest",
        Password = builder.Configuration.GetValue<string>("RabbitMQ:Password") ?? "guest"
    };
});

builder.Services.AddScoped<RabbitEventPublisher>();

var app = builder.Build();

// ⭐ Use CORS
app.UseCors("AllowAll");

app.UseDiscoveryClient();   // ⭐ BẮT BUỘC PHẢI CÓ

app.MapControllers();

app.Run();