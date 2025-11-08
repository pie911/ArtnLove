using Microsoft.AspNetCore.Mvc;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/config")]
public class ConfigController : ControllerBase
{
    private readonly ArtnLove.Services.SupabaseService _supabase;

    public ConfigController(ArtnLove.Services.SupabaseService supabase)
    {
        _supabase = supabase;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Expose only non-sensitive config to clients
        return Ok(new
        {
            supabaseUrl = _supabase.ProjectUrl,
            anonKey = _supabase.AnonKey
        });
    }
}
