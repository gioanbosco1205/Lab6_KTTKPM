namespace ChatService.Services
{
    public interface ISession
    {
        Task SaveAsync<T>(T entity) where T : class;
        Task<T?> GetAsync<T>(object id) where T : class;
        Task DeleteAsync<T>(T entity) where T : class;
        Task FlushAsync();
    }
}