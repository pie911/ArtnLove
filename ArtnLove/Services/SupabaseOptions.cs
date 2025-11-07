namespace ArtnLove.Services;

public class SupabaseOptions
{
    // Supabase project URL, e.g. https://your-project.supabase.co
    public string Url { get; set; } = string.Empty;

    // Public anon key (used client-side)
    public string AnonKey { get; set; } = string.Empty;

    // Service role key (server-side only) - store in secrets
    public string ServiceRoleKey { get; set; } = string.Empty;

    // Optional: expected issuer and audience for JWTs. Set these to tighten validation.
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
}
