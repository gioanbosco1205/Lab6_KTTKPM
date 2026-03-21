using RawRabbit;

namespace ChatService.Services;

public class RabbitEventPublisher
{
    private readonly IBusClient busClient;

    public RabbitEventPublisher(IBusClient busClient)
    {
        this.busClient = busClient;
    }

    public Task PublishMessage<T>(T message)
    {
        return busClient.BasicPublishAsync(message, cfg => cfg.OnExchange("lab-dotnet-micro").WithRoutingKey(typeof(T).Name.ToLower()));
    }
}