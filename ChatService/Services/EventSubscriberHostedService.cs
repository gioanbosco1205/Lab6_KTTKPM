using ChatService.Services;

namespace ChatService.Services;

public class EventSubscriberHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventSubscriberHostedService> _logger;

    public EventSubscriberHostedService(
        IServiceProvider serviceProvider,
        ILogger<EventSubscriberHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventSubscriberHostedService starting...");

        // Đợi một chút để đảm bảo các service khác đã khởi động
        await Task.Delay(2000, stoppingToken);

        using var scope = _serviceProvider.CreateScope();
        var policySubscriber = scope.ServiceProvider.GetRequiredService<PolicyEventSubscriber>();

        try
        {
            await policySubscriber.StartAsync();
            _logger.LogInformation("Event subscribers started successfully");

            // Giữ service chạy
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in EventSubscriberHostedService");
        }
    }
}