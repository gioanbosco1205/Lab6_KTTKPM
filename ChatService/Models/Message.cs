using Newtonsoft.Json;

namespace ChatService.Models
{
    public class Message
    {
        public virtual long? Id { get; protected set; }
        public virtual string Type { get; protected set; } = string.Empty;
        public virtual string Payload { get; protected set; } = string.Empty;

        // Parameterless constructor for EF Core
        protected Message() { }

        public Message(object message)
        {
            Type = message.GetType().FullName ?? string.Empty;
            Payload = JsonConvert.SerializeObject(message);
        }

        public virtual object? RecreateMessage()
        {
            var type = System.Type.GetType(Type);
            if (type == null) return null;
            
            return JsonConvert.DeserializeObject(Payload, type);
        }
    }
}