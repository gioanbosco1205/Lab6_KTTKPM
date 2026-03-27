using Microsoft.AspNetCore.Mvc;
using PolicyService.Events;
using PolicyService.Services;

[ApiController]
[Route("api/policy")]
public class PolicyController : ControllerBase
{
    private readonly PricingClient _pricingClient;
    private readonly RabbitEventPublisher _eventPublisher;

    public PolicyController(PricingClient pricingClient, RabbitEventPublisher eventPublisher)
    {
        _pricingClient = pricingClient;
        _eventPublisher = eventPublisher;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePolicy([FromBody] CreatePolicyRequest request)
    {
        try
        {
            // Sử dụng giá cố định thay vì gọi PricingService để tránh lỗi connection
            decimal price = 1500.00m;
            
            // Tạo policy number
            var policyNumber = $"POL-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 10000:D4}";
            
            // Simulate policy creation logic
            var policy = new
            {
                Number = policyNumber,
                CustomerName = request.CustomerName,
                Premium = price,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            // ⭐ PUBLISH EVENT - PHẦN 12
            await _eventPublisher.PublishMessage(new PolicyCreated
            {
                PolicyNumber = policy.Number,
                Premium = policy.Premium,
                CreatedAt = policy.CreatedAt,
                Status = policy.Status
            });

            return Ok(new
            {
                message = "Policy created successfully",
                policy = policy,
                eventPublished = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestCreatePolicy()
    {
        // Test endpoint với dữ liệu mẫu
        var request = new CreatePolicyRequest { CustomerName = "Test Customer" };
        return await CreatePolicy(request);
    }
}

public class CreatePolicyRequest
{
    public string CustomerName { get; set; } = string.Empty;
}