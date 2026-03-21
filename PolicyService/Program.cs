using Polly;
using RawRabbit.DependencyInjection.ServiceCollection;
using RawRabbit.Instantiation;
using Steeltoe.Discovery.Client;
using PolicyService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDiscoveryClient(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddSingleton<PricingClient>();

// ⭐ RabbitMQ Configuration
builder.Services.AddRawRabbit(new RawRabbitOptions
{
    ClientConfiguration = new RawRabbit.Configuration.RawRabbitConfiguration
    {
        Username = "guest",
        Password = "guest",
        Port = 5672,
        VirtualHost = "/",
        Hostnames = { "localhost" }
    }
});

builder.Services.AddScoped<RabbitEventPublisher>();

var app = builder.Build();

app.UseDiscoveryClient();   // ⭐ BẮT BUỘC PHẢI CÓ

app.MapControllers();

app.Run();