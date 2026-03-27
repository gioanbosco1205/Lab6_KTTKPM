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

    [HttpPost("terminate")]
    public async Task<IActionResult> TerminatePolicy([FromBody] TerminatePolicyRequest request)
    {
        try
        {
            // Simulate policy termination logic
            var terminatedPolicy = new
            {
                PolicyId = request.PolicyId,
                PolicyNumber = request.PolicyNumber,
                CustomerName = request.CustomerName,
                Status = "Terminated",
                TerminatedAt = DateTime.UtcNow,
                TerminationReason = request.TerminationReason,
                FinalPremium = request.FinalPremium,
                TerminatedBy = request.TerminatedBy ?? "System"
            };

            // ⭐ PUBLISH POLICY TERMINATED EVENT
            await _eventPublisher.PublishMessage(new PolicyTerminated
            {
                PolicyId = terminatedPolicy.PolicyId,
                PolicyNumber = terminatedPolicy.PolicyNumber,
                CustomerName = terminatedPolicy.CustomerName,
                TerminatedAt = terminatedPolicy.TerminatedAt,
                TerminationReason = terminatedPolicy.TerminationReason,
                FinalPremium = terminatedPolicy.FinalPremium,
                TerminatedBy = terminatedPolicy.TerminatedBy
            });

            return Ok(new
            {
                message = "Policy terminated successfully",
                policy = terminatedPolicy,
                eventPublished = true
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("activate-product")]
    public async Task<IActionResult> ActivateProduct([FromBody] ActivateProductRequest request)
    {
        try
        {
            // Simulate product activation logic
            var activatedProduct = new
            {
                ProductId = $"PROD-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 10000:D4}",
                ProductName = request.ProductName,
                ProductType = request.ProductType,
                PolicyId = request.PolicyId,
                PolicyNumber = request.PolicyNumber,
                CustomerName = request.CustomerName,
                Status = "Active",
                ActivatedAt = DateTime.UtcNow,
                ProductPremium = request.ProductPremium,
                ActivatedBy = request.ActivatedBy ?? "System",
                ProductFeatures = request.ProductFeatures ?? new Dictionary<string, object>()
            };

            // ⭐ PUBLISH PRODUCT ACTIVATED EVENT
            await _eventPublisher.PublishMessage(new ProductActivated
            {
                ProductId = activatedProduct.ProductId,
                ProductName = activatedProduct.ProductName,
                ProductType = activatedProduct.ProductType,
                PolicyId = activatedProduct.PolicyId,
                PolicyNumber = activatedProduct.PolicyNumber,
                CustomerName = activatedProduct.CustomerName,
                ActivatedAt = activatedProduct.ActivatedAt,
                ProductPremium = activatedProduct.ProductPremium,
                ActivatedBy = activatedProduct.ActivatedBy,
                ProductFeatures = activatedProduct.ProductFeatures
            });

            return Ok(new
            {
                message = "Product activated successfully",
                product = activatedProduct,
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

    [HttpGet("test-terminate")]
    public async Task<IActionResult> TestTerminatePolicy()
    {
        // Test endpoint với dữ liệu mẫu
        var request = new TerminatePolicyRequest 
        { 
            PolicyId = "POL-001",
            PolicyNumber = "POL-20241227-0001",
            CustomerName = "Test Customer",
            TerminationReason = "Customer Request",
            FinalPremium = 1200.00m,
            TerminatedBy = "Agent Smith"
        };
        return await TerminatePolicy(request);
    }

    [HttpGet("test-activate-product")]
    public async Task<IActionResult> TestActivateProduct()
    {
        // Test endpoint với dữ liệu mẫu
        var request = new ActivateProductRequest 
        { 
            ProductName = "Premium Health Insurance",
            ProductType = "Health",
            PolicyId = "POL-001",
            PolicyNumber = "POL-20241227-0001",
            CustomerName = "Test Customer",
            ProductPremium = 2500.00m,
            ActivatedBy = "Agent Johnson",
            ProductFeatures = new Dictionary<string, object>
            {
                { "Coverage", "Full Coverage" },
                { "Deductible", 500 },
                { "MaxBenefit", 100000 }
            }
        };
        return await ActivateProduct(request);
    }
}

public class CreatePolicyRequest
{
    public string CustomerName { get; set; } = string.Empty;
}

public class TerminatePolicyRequest
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string TerminationReason { get; set; } = string.Empty;
    public decimal FinalPremium { get; set; }
    public string? TerminatedBy { get; set; }
}

public class ActivateProductRequest
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal ProductPremium { get; set; }
    public string? ActivatedBy { get; set; }
    public Dictionary<string, object>? ProductFeatures { get; set; }
}