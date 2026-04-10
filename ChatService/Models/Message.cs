using Newtonsoft.Json;

namespace ChatService.Models
{
    /// <summary>
    /// Message table - Event Store cho Outbox Pattern
    /// Lưu trữ events để background job xử lý
    /// </summary>
    public class Message
    {
        public virtual long? Id { get; protected set; }
        public virtual string Type { get; protected set; } = string.Empty;
        public virtual string Payload { get; protected set; } = string.Empty;
        
        // Tracking fields
        public virtual DateTime CreatedAt { get; protected set; }
        public virtual DateTime? ProcessedAt { get; protected set; }
        
        // ⭐ Retry mechanism fields (max retry = 5)
        public virtual int RetryCount { get; protected set; }
        public virtual DateTime? LastRetryAt { get; protected set; }

        // Parameterless constructor for EF Core
        protected Message() 
        {
            CreatedAt = DateTime.UtcNow;
            RetryCount = 0;
        }

        public Message(object message)
        {
            Type = message.GetType().FullName ?? string.Empty;
            Payload = JsonConvert.SerializeObject(message);
            CreatedAt = DateTime.UtcNow;
            ProcessedAt = null;
            RetryCount = 0;
            LastRetryAt = null;
        }

        public virtual object? RecreateMessage()
        {
            var type = System.Type.GetType(Type);
            if (type == null) return null;
            
            return JsonConvert.DeserializeObject(Payload, type);
        }

        /// <summary>
        /// Mark message as processed
        /// </summary>
        public virtual void MarkAsProcessed()
        {
            ProcessedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// ⭐ Increment retry count khi publish thất bại
        /// </summary>
        public virtual void IncrementRetryCount()
        {
            RetryCount++;
            LastRetryAt = DateTime.UtcNow;
        }

        /// <summary>
        /// ⭐ Check if message has exceeded max retry attempts
        /// </summary>
        public virtual bool HasExceededMaxRetries(int maxRetries = 5)
        {
            return RetryCount >= maxRetries;
        }
    }
}
