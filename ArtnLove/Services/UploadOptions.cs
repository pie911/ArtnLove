namespace ArtnLove.Services;

public class UploadOptions
{
    // Allowed MIME types for uploads
    public string[] AllowedMimeTypes { get; set; } = new[] { "image/jpeg", "image/png", "image/webp", "image/avif" };

    // Max file size in bytes (default 10 MB)
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    // Simple rate limit: max presign requests per IP per minute
    public int RateLimitPerMinute { get; set; } = 30;
}
