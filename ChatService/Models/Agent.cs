using System.ComponentModel.DataAnnotations;

namespace ChatService.Models;

public class Agent
{
    public int Id { get; set; }
    
    [Required]
    public string AgentId { get; set; } = string.Empty;
    
    [Required]
    public string AgentName { get; set; } = string.Empty;
    
    public AgentStatus Status { get; set; } = AgentStatus.Offline;
    
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<ChatMessage> SentMessages { get; set; } = new List<ChatMessage>();
    public ICollection<ChatMessage> ReceivedMessages { get; set; } = new List<ChatMessage>();
}

public enum AgentStatus
{
    Offline = 0,
    Online = 1,
    Away = 2,
    Busy = 3
}