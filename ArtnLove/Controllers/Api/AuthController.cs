using Microsoft.AspNetCore.Mvc;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger)
    {
        _logger = logger;
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized(new { message = "No valid JWT provided." });

        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new { authenticated = true, claims });
    }

    [HttpPost("refresh")]
    public IActionResult Refresh()
    {
        // Placeholder: in production, validate refresh token (secure httpOnly cookie), call Supabase token endpoint or rotate tokens.
        return StatusCode(501, new { message = "Refresh token rotation not implemented in scaffold. Implement server-side refresh flow using Supabase admin endpoints." });
    }
}
