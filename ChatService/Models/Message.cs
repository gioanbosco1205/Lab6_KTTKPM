using Newtonsoft.Json;

namespace ChatService.Models
{
   
    public class Message
    {
        public virtual long? Id { get; protected set; }
        public virtual string Type { get; protected set; } = string.Empty;
        public virtual string Payload { get; protected set; } = string.Empty;
        
   
        public virtual DateTime CreatedAt { get; protected set; }
        public virtual DateTime? ProcessedAt { get; protected set; }


        protected Message() 
        {
            CreatedAt = DateTime.UtcNow;
        }

        public Message(object message)
        {
            Type = message.GetType().FullName ?? string.Empty;
            Payload = JsonConvert.SerializeObject(message);
            CreatedAt = DateTime.UtcNow;
            ProcessedAt = null;
        }

        public virtual object? RecreateMessage()
        {
            var type = System.Type.GetType(Type);
            if (type == null) return null;
            
            return JsonConvert.DeserializeObject(Payload, type);
        }

        /// <summary>
        /// Mark message as processed (optional - nếu muốn soft delete thay vì hard delete)
        /// </summary>
        public virtual void MarkAsProcessed()
        {
            ProcessedAt = DateTime.UtcNow;
        }
    }
}