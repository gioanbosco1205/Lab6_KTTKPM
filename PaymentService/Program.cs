using Marten;
using PaymentService.Models;
using PaymentService.Repositories;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Marten
builder.Services.AddMarten(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PgConnection");
    if (connectionString != null)
    {
        options.Connection(connectionString);
    }
});

// Register repositories and services
builder.Services.AddScoped<IPolicyAccountRepository, PolicyAccountRepository>();
builder.Services.AddScoped<IDataStore, DataStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

// Test database connection endpoint
app.MapGet("/test-db", async (IDocumentStore store) =>
{
    try
    {
        // Test connection by opening a session and executing a simple query
        using var session = store.LightweightSession();
        
        // Execute a simple query to test connection
        await session.Query<PolicyAccount>().CountAsync();
        
        var connectionString = builder.Configuration.GetConnectionString("PgConnection");
        
        return Results.Ok(new { 
            status = "Connected", 
            message = "PostgreSQL connection successful!", 
            timestamp = DateTime.UtcNow,
            connectionString = connectionString?.Replace("Password=1234567890aA@", "Password=***")
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database connection failed: {ex.Message}");
    }
})
.WithName("TestDatabase")
.WithOpenApi();

// Force create database schema
app.MapPost("/create-schema", async (IDocumentStore store) =>
{
    try
    {
        // Force tạo schema và tables
        await store.Advanced.Clean.CompletelyRemoveAllAsync();
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        
        return Results.Ok(new { 
            status = "Success", 
            message = "Database schema created successfully!", 
            timestamp = DateTime.UtcNow 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Schema creation failed: {ex.Message}");
    }
})
.WithName("CreateSchema")
.WithOpenApi();

// Check database tables
app.MapGet("/check-tables", async (IDocumentStore store) =>
{
    try
    {
        using var session = store.LightweightSession();
        
        // Thử query để kiểm tra table có tồn tại không
        var count = await session.Query<PolicyAccount>().CountAsync();
        
        return Results.Ok(new { 
            status = "Success", 
            message = "Table mt_doc_policyaccount exists and accessible", 
            totalRecords = count,
            timestamp = DateTime.UtcNow 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Table check failed: {ex.Message}");
    }
})
.WithName("CheckTables")
.WithOpenApi();

// PolicyAccount endpoints
app.MapPost("/policy-accounts", async (PolicyAccount account, IPolicyAccountRepository repository, IDocumentSession session) =>
{
    try
    {
        if (account.Id == Guid.Empty)
            account.Id = Guid.NewGuid();
            
        repository.Add(account);
        await session.SaveChangesAsync();
        
        return Results.Created($"/policy-accounts/{account.PolicyAccountNumber}", account);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error creating policy account: {ex.Message}");
    }
})
.WithName("CreatePolicyAccount")
.WithOpenApi();

app.MapGet("/policy-accounts/{number}", async (string number, IPolicyAccountRepository repository) =>
{
    try
    {
        var account = await repository.FindByNumber(number);
        if (account.Id == Guid.Empty)
            return Results.NotFound($"Policy account with number {number} not found");
            
        return Results.Ok(account);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error finding policy account: {ex.Message}");
    }
})
.WithName("GetPolicyAccountByNumber")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
