using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;

var builder = WebApplication.CreateBuilder(args);

// Load ocelot config
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add CacheManager
builder.Services.AddCacheManager();

// Add Ocelot + Eureka
builder.Services
    .AddOcelot(builder.Configuration)
    .AddEureka();

// CORS
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Run Gateway
await app.UseOcelot();

app.Run();