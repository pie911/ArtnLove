using Microsoft.AspNetCore.Mvc;
using ArtnLove.Services;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly SupabaseService _supabase;

    public UsersController(ILogger<UsersController> logger, SupabaseService supabase)
    {
        _logger = logger;
        _supabase = supabase;
    }

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        // Sample response. Replace with DB lookup.
        var sample = new
        {
            id,
            email = "user@example.com",
            username = "sampleuser",
            displayName = "Sample User",
            avatarUrl = string.Empty
        };
        return Ok(sample);
    }
}
