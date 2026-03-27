namespace ChatService.Events;

public class ProductActivated
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public decimal ProductPremium { get; set; }
    public string ActivatedBy { get; set; } = string.Empty;
    public Dictionary<string, object> ProductFeatures { get; set; } = new();
}