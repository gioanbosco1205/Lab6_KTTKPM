using RawRabbit;

namespace PolicyService.Services;

public class RabbitEventPublisher
{
    private readonly IBusClient busClient;

    public RabbitEventPublisher(IBusClient busClient)
    {
        this.busClient = busClient;
    }

    public Task PublishMessage<T>(T message)
    {
        return busClient.PublishAsync(message);
    }
}