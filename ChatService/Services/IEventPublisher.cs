namespace ChatService.Services
{
    public interface IEventPublisher
    {
        Task PublishMessage<T>(T message);
    }
}