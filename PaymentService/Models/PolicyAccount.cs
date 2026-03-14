namespace PaymentService.Models;

public class PolicyAccount
{
    public Guid Id { get; set; }
    public string PolicyNumber { get; set; } = string.Empty;
    public string PolicyAccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}