using Microsoft.AspNetCore.Mvc;
using ArtnLove.Services;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/artworks")]
public class ArtworksController : ControllerBase
{
    private readonly ILogger<ArtworksController> _logger;
    private readonly SupabaseService _supabase;

    public ArtworksController(ILogger<ArtworksController> logger, SupabaseService supabase)
    {
        _logger = logger;
        _supabase = supabase;
    }

    [HttpGet]
    public IActionResult List([FromQuery] int limit = 20)
    {
        var sample = new[]
        {
            new { id = Guid.NewGuid(), title = "Sunset", owner = "sampleuser", price = 100.0 }
        };
        return Ok(sample);
    }

    [HttpPost]
    public IActionResult Create([FromBody] object payload)
    {
        // Placeholder - validate and persist
        var id = Guid.NewGuid();
        return CreatedAtAction(nameof(Get), new { id }, payload);
    }

    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id)
    {
        return Ok(new { id, title = "Sunset", description = "Sample artwork", mediaUrls = new string[] { } });
    }
}
