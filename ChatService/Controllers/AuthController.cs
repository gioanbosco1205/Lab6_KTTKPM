using Microsoft.AspNetCore.Mvc;
using ChatService.Services;

namespace ChatService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IJwtService _jwtService;

    public AuthController(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Đây chỉ là demo - trong thực tế bạn cần validate user credentials
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest("Username và password không được để trống");
        }

        // Demo: chấp nhận bất kỳ username/password nào
        var userId = Guid.NewGuid().ToString();
        var token = _jwtService.GenerateToken(userId, request.Username);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = userId,
            Username = request.Username,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }

    [HttpPost("agent-login")]
    public IActionResult AgentLogin([FromBody] AgentLoginRequest request)
    {
        // Đây chỉ là demo - trong thực tế bạn cần validate agent credentials
        if (string.IsNullOrEmpty(request.AgentId) || string.IsNullOrEmpty(request.AgentName))
        {
            return BadRequest("AgentId và AgentName không được để trống");
        }

        // Demo: chấp nhận bất kỳ agent nào
        var token = _jwtService.GenerateToken(request.AgentId, request.AgentName);

        return Ok(new AgentLoginResponse
        {
            Token = token,
            AgentId = request.AgentId,
            AgentName = request.AgentName,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60)
        });
    }
    public IActionResult ValidateToken([FromBody] ValidateTokenRequest request)
    {
        var principal = _jwtService.ValidateToken(request.Token);
        
        if (principal == null)
        {
            return Unauthorized("Token không hợp lệ");
        }

        var userId = principal.FindFirst("userId")?.Value;
        var username = principal.FindFirst("username")?.Value;

        return Ok(new
        {
            Valid = true,
            UserId = userId,
            Username = username
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}
public class AgentLoginRequest
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
}

public class AgentLoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}