using Microsoft.Extensions.Options;

namespace ArtnLove.Services;

/// <summary>
/// Lightweight wrapper for reading Supabase configuration.
/// Real project: replace with HTTP client or Supabase SDK calls.
/// </summary>
public class SupabaseService
{
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseService> _logger;

    public SupabaseService(IOptions<SupabaseOptions> options, ILogger<SupabaseService> logger)
    {
        _options = options?.Value ?? new SupabaseOptions();
        _logger = logger;
    }

    public string ProjectUrl => _options.Url ?? string.Empty;
    public string AnonKey => _options.AnonKey ?? string.Empty;
    public bool HasServiceRoleKey => !string.IsNullOrEmpty(_options.ServiceRoleKey);

    public void LogConfig()
    {
        _logger.LogDebug("Supabase URL: {Url}; HasServiceRoleKey: {HasKey}", ProjectUrl, HasServiceRoleKey);
    }
}
