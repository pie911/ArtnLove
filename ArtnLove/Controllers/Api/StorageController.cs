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

        // Note: Bucket creation is handled manually in Supabase dashboard
        // The bucket should be created in the Supabase Storage section before using the app
        _logger.LogInformation("Attempting to create signed URL for bucket: {Bucket}", req.bucket);

        // Skip bucket creation for now as Supabase API endpoints are not available
        // await EnsureBucketExistsAsync(client, req.bucket);

        // Supabase Storage signed URL endpoint is: /storage/v1/object/sign/{bucket}/{path}
        var signEndpoint = $"/storage/v1/object/sign/{Uri.EscapeDataString(req.bucket)}/{Uri.EscapeDataString(req.path)}";
        // Supabase expects the property name `expiresIn` (camelCase)
        var body = new { expiresIn = req.expiresInSeconds > 0 ? req.expiresInSeconds : 3600 };

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

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile()
    {
        if (!Request.HasFormContentType) return BadRequest(new { message = "Expected multipart/form-data" });
        var form = await Request.ReadFormAsync();
        var file = form.Files.FirstOrDefault();
        var bucket = form["bucket"].FirstOrDefault() ?? "public";
        var path = form["path"].FirstOrDefault();

        if (file == null) return BadRequest(new { message = "No file uploaded" });
        if (string.IsNullOrEmpty(path))
        {
            var ext = Path.GetExtension(file.FileName);
            path = $"uploads/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Guid.NewGuid().ToString().Substring(0,6)}{ext}";
        }

        if (!_supabase.HasServiceRoleKey) return StatusCode(403, new { message = "Service role key not configured on server" });

        var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(_supabase.ProjectUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _supabase.ServiceRoleKey);

        // Build multipart content
        using var content = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);

        // Upload to Supabase Storage: POST /storage/v1/object/{bucket}
        var uploadResp = await client.PostAsync($"/storage/v1/object/{Uri.EscapeDataString(bucket)}?path={Uri.EscapeDataString(path)}", content);
        var respText = await uploadResp.Content.ReadAsStringAsync();
        if (!uploadResp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Server upload failed: {Status} {Body}", uploadResp.StatusCode, respText);
            return StatusCode((int)uploadResp.StatusCode, new { message = "Upload failed", detail = respText });
        }

        // Construct public URL
        var publicUrl = _supabase.ProjectUrl.TrimEnd('/') + $"/storage/v1/object/public/{bucket}/{Uri.EscapeDataString(path)}";
        return Ok(new { url = publicUrl, raw = respText });
    }

    private async Task EnsureBucketExistsAsync(HttpClient client, string bucket)
    {
        // List buckets and check
        var listResp = await client.GetAsync("/storage/v1/buckets");
        if (!listResp.IsSuccessStatusCode)
        {
            var txt = await listResp.Content.ReadAsStringAsync();
            _logger.LogWarning("Could not list buckets: {Status} {Body}", listResp.StatusCode, txt);
            // continue and try to create
        }
        else
        {
            var body = await listResp.Content.ReadAsStringAsync();
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(body);
                if (arr.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        if (el.TryGetProperty("name", out var nameEl) && nameEl.GetString() == bucket)
                            return; // exists
                    }
                }
            }
            catch { /* ignore parse errors and try create */ }
        }

        // Create bucket (public by default)
    var createBody = new { name = bucket, @public = true };
        var createResp = await client.PostAsJsonAsync("/storage/v1/buckets", createBody);
        if (!createResp.IsSuccessStatusCode)
        {
            var txt = await createResp.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to create bucket {Bucket}: {Status} {Body}", bucket, createResp.StatusCode, txt);
            // Throw to surface failure to caller
            throw new InvalidOperationException($"Failed to create bucket: {txt}");
        }
    }
}
