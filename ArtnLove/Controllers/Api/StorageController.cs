using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/storage")]
public class StorageController : ControllerBase
{
    private readonly ILogger<StorageController> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ArtnLove.Services.SupabaseService _supabase;

    public StorageController(ILogger<StorageController> logger, IHttpClientFactory httpFactory, ArtnLove.Services.SupabaseService supabase)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        _supabase = supabase;
    }

    public record PresignRequest(string bucket, string path, int expiresInSeconds);

    [HttpPost("presign")]
    public async Task<IActionResult> CreatePresignedUrl([FromBody] PresignRequest req)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(req.bucket) || string.IsNullOrWhiteSpace(req.path))
            return BadRequest(new { message = "bucket and path are required" });

        if (!_supabase.HasServiceRoleKey)
            return StatusCode(403, new { message = "Service role key not configured on server" });

        // Prevent simple path traversal
        if (req.path.Contains("..")) return BadRequest(new { message = "Invalid path" });

        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(_supabase.ProjectUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _supabase.AnonKey);

        // Supabase Storage signed URL endpoint is: /storage/v1/object/sign/{bucket}/{path}
        var signEndpoint = $"/storage/v1/object/sign/{Uri.EscapeDataString(req.bucket)}/{Uri.EscapeDataString(req.path)}";
        var body = new { expires_in = req.expiresInSeconds > 0 ? req.expiresInSeconds : 3600 };

        // Use Service Role key in header for signing (server-side)
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _supabase.AnonKey);
        // Note: Supabase requires service role key for some operations — ensure you pass correct key in secrets. Here we use AnonKey by default; replace with ServiceRoleKey if required.

        try
        {
            var resp = await client.PostAsJsonAsync(signEndpoint, body);
            if (!resp.IsSuccessStatusCode)
            {
                var text = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("Supabase sign endpoint returned {Status}: {Body}", resp.StatusCode, text);
                return StatusCode((int)resp.StatusCode, new { message = "Failed to create signed URL", detail = text });
            }

            var json = await resp.Content.ReadFromJsonAsync<object>();
            return Ok(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Supabase sign endpoint");
            return StatusCode(500, new { message = "Internal error" });
        }
    }
}
