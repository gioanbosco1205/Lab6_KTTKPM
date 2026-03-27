namespace PolicyService.Events;

public class PolicyTerminated
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime TerminatedAt { get; set; } = DateTime.UtcNow;
    public string TerminationReason { get; set; } = string.Empty;
    public decimal FinalPremium { get; set; }
    public string TerminatedBy { get; set; } = string.Empty;
}