using System.ComponentModel.DataAnnotations;

namespace ChatService.Models;

public class ChatMessage
{
    public int Id { get; set; }
    
    [Required]
    public string SenderId { get; set; } = string.Empty;
    
    [Required]
    public string SenderName { get; set; } = string.Empty;
    
    public string? ReceiverId { get; set; } // null for group messages
    
    public string? ReceiverName { get; set; }
    
    [Required]
    public string Message { get; set; } = string.Empty;
    
    [Required]
    public MessageType MessageType { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsRead { get; set; } = false;
}

public enum MessageType
{
    GroupMessage = 1,
    PrivateMessage = 2,
    SystemMessage = 3,
    PolicyNotification = 4
}