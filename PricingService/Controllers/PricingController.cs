using Microsoft.AspNetCore.Mvc;

namespace PricingService.Controllers
{
    [ApiController]
    [Route("api/pricing")]
    public class PricingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            var port = HttpContext.Connection.LocalPort;

            return Ok(new
            {
                price = 1500,
                port = port
            });
        }
    }
}