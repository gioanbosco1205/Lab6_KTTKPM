namespace PolicyService.Events;

public class PolicyCreated
{
    public string PolicyNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public decimal Premium { get; set; }
    public string Status { get; set; } = "Active";
}