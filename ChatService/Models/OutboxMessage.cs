using Newtonsoft.Json;

namespace ChatService.Models
{
    public class OutboxMessage
    {
        public virtual long Id { get; protected set; }
        public virtual string Type { get; protected set; } = string.Empty;
        public virtual string JsonPayload { get; protected set; } = string.Empty;
        public virtual DateTime CreatedAt { get; protected set; }
        public virtual bool IsProcessed { get; protected set; }
        public virtual DateTime? ProcessedAt { get; protected set; }

        // Parameterless constructor for EF Core
        protected OutboxMessage() { }

        public OutboxMessage(object eventData)
        {
            Type = eventData.GetType().FullName ?? string.Empty;
            JsonPayload = JsonConvert.SerializeObject(eventData);
            CreatedAt = DateTime.UtcNow;
            IsProcessed = false;
        }

        public virtual object? RecreateEvent()
        {
            var type = System.Type.GetType(Type);
            if (type == null) return null;
            
            return JsonConvert.DeserializeObject(JsonPayload, type);
        }

        public virtual void MarkAsProcessed()
        {
            IsProcessed = true;
            ProcessedAt = DateTime.UtcNow;
        }
    }
}