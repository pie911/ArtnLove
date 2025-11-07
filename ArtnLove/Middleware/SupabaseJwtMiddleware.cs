using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace ArtnLove.Middleware;

/// <summary>
/// Middleware to parse Supabase JWT from Authorization header and attach ClaimsPrincipal to HttpContext.User.
/// This middleware intentionally tolerates missing or invalid tokens (does not block requests);
/// protected endpoints should check HttpContext.User.Identity.IsAuthenticated.
/// </summary>
public class SupabaseJwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SupabaseJwtMiddleware> _logger;
    private readonly ArtnLove.Services.SupabaseAuthService _authService;

    public SupabaseJwtMiddleware(RequestDelegate next, ILogger<SupabaseJwtMiddleware> logger, ArtnLove.Services.SupabaseAuthService authService)
    {
        _next = next;
        _logger = logger;
        _authService = authService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var header = context.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(header))
            {
                // Expect Bearer <token>
                var m = Regex.Match(header, "Bearer\\s+(.*)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var token = m.Groups[1].Value.Trim();
                    var principal = await _authService.ValidateTokenAsync(token);
                    if (principal != null)
                    {
                        context.User = principal;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in SupabaseJwtMiddleware");
            // swallow to not block request pipeline; endpoint-level auth should handle unauthorized responses
        }

        await _next(context);
    }
}
