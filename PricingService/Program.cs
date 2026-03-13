using Steeltoe.Discovery.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddDiscoveryClient(builder.Configuration);

var app = builder.Build();

app.UseDiscoveryClient();

app.MapControllers();
app.MapHealthChecks("/health");
app.Run();