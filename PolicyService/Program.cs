using Polly;
using Steeltoe.Discovery.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDiscoveryClient(builder.Configuration);
builder.Services.AddControllers();

builder.Services.AddSingleton<PricingClient>();

var app = builder.Build();

app.UseDiscoveryClient();   // ⭐ BẮT BUỘC PHẢI CÓ

app.MapControllers();

app.Run();