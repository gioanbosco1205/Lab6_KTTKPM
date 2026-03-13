using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/policy")]
public class PolicyController : ControllerBase
{
    private readonly PricingClient _pricingClient;

    public PolicyController(PricingClient pricingClient)
    {
        _pricingClient = pricingClient;
    }

    [HttpGet]
    public async Task<IActionResult> CreatePolicy()
    {
        var price = await _pricingClient.GetPrice();
        return Ok(new
        {
            message = "Policy created",
            pricing = price
        });
    }
}