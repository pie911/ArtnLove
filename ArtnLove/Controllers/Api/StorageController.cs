using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/storage")]
public class StorageController : ControllerBase
{
    private readonly ILogger<StorageController> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ArtnLove.Services.SupabaseService _supabase;
    private readonly IOptions<ArtnLove.Services.UploadOptions> _uploadOptions;

    private static readonly ConcurrentDictionary<string, (int Count, DateTimeOffset WindowStart)> _rateLimits = new();

    public StorageController(ILogger<StorageController> logger, IHttpClientFactory httpFactory, ArtnLove.Services.SupabaseService supabase, IOptions<ArtnLove.Services.UploadOptions> uploadOptions)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        _supabase = supabase;
        _uploadOptions = uploadOptions;
    }

    public record PresignRequest(string bucket, string path, int expiresInSeconds, string? contentType = null, long? contentLength = null);

    [HttpPost("presign")]
    public async Task<IActionResult> CreatePresignedUrl([FromBody] PresignRequest req)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(req.bucket) || string.IsNullOrWhiteSpace(req.path))
            return BadRequest(new { message = "bucket and path are required" });

        // Prevent simple path traversal
        if (req.path.Contains("..")) return BadRequest(new { message = "Invalid path" });

        // Rate limiting per IP (simple sliding window)
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTimeOffset.UtcNow;
        var windowKey = remoteIp;
        var limit = _uploadOptions.Value.RateLimitPerMinute;
        var windowSpan = TimeSpan.FromMinutes(1);

        var entry = _rateLimits.GetOrAdd(windowKey, _ => (Count: 0, WindowStart: now));
        lock (_rateLimits)
        {
            var current = _rateLimits[windowKey];
            if (now - current.WindowStart > windowSpan)
            {
                current = (Count: 0, WindowStart: now);
            }
            if (current.Count >= limit)
            {
                return StatusCode(429, new { message = "Rate limit exceeded" });
            }
            current.Count += 1;
            _rateLimits[windowKey] = current;
        }

        if (!_supabase.HasServiceRoleKey)
            return StatusCode(403, new { message = "Service role key not configured on server" });

        // Validate content type and length if provided
        if (!string.IsNullOrEmpty(req.contentType))
        {
            var allowed = _uploadOptions.Value.AllowedMimeTypes;
            if (!allowed.Contains(req.contentType, StringComparer.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Content type not allowed" });
            }
        }
        if (req.contentLength.HasValue)
        {
            if (req.contentLength.Value > _uploadOptions.Value.MaxFileSizeBytes)
            {
                return BadRequest(new { message = "File too large" });
            }
        }

        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(_supabase.ProjectUrl);

        // Use the service role key server-side to request signed URLs.
        var roleKey = _supabase.ServiceRoleKey;
        if (string.IsNullOrEmpty(roleKey))
        {
            _logger.LogWarning("ServiceRoleKey not configured; cannot create signed URLs securely.");
            return StatusCode(403, new { message = "Service role key not configured on server" });
        }
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", roleKey);

        // Supabase Storage signed URL endpoint is: /storage/v1/object/sign/{bucket}/{path}
        var signEndpoint = $"/storage/v1/object/sign/{Uri.EscapeDataString(req.bucket)}/{Uri.EscapeDataString(req.path)}";
        var body = new { expires_in = req.expiresInSeconds > 0 ? req.expiresInSeconds : 3600 };

        try
        {
            var resp = await client.PostAsJsonAsync(signEndpoint, body);
            if (!resp.IsSuccessStatusCode)
            {
                var text = await resp.Content.ReadAsStringAsync();
                _logger.LogWarning("Supabase sign endpoint returned {Status}: {Body}", resp.StatusCode, text);
                return StatusCode((int)resp.StatusCode, new { message = "Failed to create signed URL", detail = text });
            }

            // Supabase sign endpoint returns either a JSON string or JSON object; return raw content to the client
            var raw = await resp.Content.ReadAsStringAsync();
            // Try parse JSON
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<object>(raw);
                return Ok(parsed);
            }
            catch
            {
                // return raw string if not JSON
                return Ok(new { signed = raw });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Supabase sign endpoint");
            return StatusCode(500, new { message = "Internal error" });
        }
    }
}
