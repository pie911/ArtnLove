using Microsoft.AspNetCore.Mvc;
using ArtnLove.Services;
using ArtnLove.Data;
using System.Text.Json;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/artworks")]
public class ArtworksController : ControllerBase
{
    private readonly ILogger<ArtworksController> _logger;
    private readonly SupabaseService _supabase;
    private readonly ArtnLove.Data.IArtRepository _repo;

    public ArtworksController(ILogger<ArtworksController> logger, SupabaseService supabase, ArtnLove.Data.IArtRepository repo)
    {
        _logger = logger;
        _supabase = supabase;
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int limit = 20)
    {
        var items = await _repo.ListAsync();
        return Ok(items.Take(limit));
    }

    public record CreateArtworkDto(string title, string? description, string[]? mediaUrls);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArtworkDto payload)
    {
        if (string.IsNullOrWhiteSpace(payload.title)) return BadRequest(new { message = "title is required" });

        var obj = new
        {
            title = payload.title,
            description = payload.description,
            mediaUrls = payload.mediaUrls ?? Array.Empty<string>()
        };

        var id = await _repo.AddAsync(obj);
        return CreatedAtAction(nameof(Get), new { id }, new { id, obj });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var items = await _repo.ListAsync();
        var found = items.FirstOrDefault(e => e.TryGetProperty("id", out var idp) && idp.GetGuid() == id);
        if (found.ValueKind == JsonValueKind.Undefined) return NotFound();
        return Ok(found);
    }
}
