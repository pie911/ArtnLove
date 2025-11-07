using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ArtnLove.Services;

/// <summary>
/// Lightweight Supabase JWT helper. This is a scaffold: it does basic token decoding and expiry checks.
/// TODO: Replace with full signature validation using your Supabase JWT secret or JWKS when available.
/// </summary>
public class SupabaseAuthService
{
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAuthService> _logger;

    public SupabaseAuthService(IOptions<SupabaseOptions> options, ILogger<SupabaseAuthService> logger)
    {
        _options = options?.Value ?? new SupabaseOptions();
        _logger = logger;
    }

    /// <summary>
    /// Validate and parse a JWT. If a service role key is configured, performs signature validation (HS256) and lifetime/issuer/audience checks.
    /// If no service role key is available this method will decode the token and perform a lifetime check only (safer than nothing, but not secure).
    /// </summary>
    public Task<ClaimsPrincipal?> ValidateTokenAsync(string jwt)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(jwt)) return Task.FromResult<ClaimsPrincipal?>(null);

            // If server has a service role key, perform full validation (signature, lifetime, optionally issuer/audience)
            if (!string.IsNullOrEmpty(_options.ServiceRoleKey))
            {
                var keyBytes = Encoding.UTF8.GetBytes(_options.ServiceRoleKey);
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = !string.IsNullOrEmpty(_options.JwtIssuer),
                    ValidIssuer = _options.JwtIssuer,
                    ValidateAudience = !string.IsNullOrEmpty(_options.JwtAudience),
                    ValidAudience = _options.JwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                try
                {
                    var principal = handler.ValidateToken(jwt, validationParameters, out var validatedToken);
                    return Task.FromResult<ClaimsPrincipal?>(principal);
                }
                catch (SecurityTokenException ste)
                {
                    _logger.LogInformation(ste, "Token validation failed");
                    return Task.FromResult<ClaimsPrincipal?>(null);
                }
            }

            // Fallback: no service key available — decode token and check expiry only
            var token = handler.ReadJwtToken(jwt);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var exp = token.Payload.Exp;
            if (exp.HasValue && exp.Value < now)
            {
                _logger.LogInformation("JWT expired (fallback)");
                return Task.FromResult<ClaimsPrincipal?>(null);
            }

            var claims = token.Claims.Select(c => new Claim(c.Type, c.Value)).ToList();
            var identity = new ClaimsIdentity(claims, "supabase-jwt-fallback");
            var principalFallback = new ClaimsPrincipal(identity);
            _logger.LogWarning("Token was accepted by fallback (no service role key configured). Enable service role key to validate signatures.");
            return Task.FromResult<ClaimsPrincipal?>(principalFallback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse/validate JWT");
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }
}
