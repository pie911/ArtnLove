using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArtnLove.Middleware;

public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;

    public InputValidationMiddleware(RequestDelegate next, ILogger<InputValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST/PUT/PATCH requests
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            // Validate query parameters
            foreach (var param in context.Request.Query)
            {
                if (!IsSafeInput(param.Value.ToString()!))
                {
                    _logger.LogWarning("Potentially unsafe query parameter detected: {Key}", param.Key);
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid input detected" });
                    return;
                }
            }

            // Validate form data
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                foreach (var field in form)
                {
                    if (!IsSafeInput(field.Value))
                    {
                        _logger.LogWarning("Potentially unsafe form field detected: {Key}", field.Key);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsJsonAsync(new { message = "Invalid input detected" });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }

    private bool IsSafeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
            return true;

        // Check for common XSS patterns
        var dangerousPatterns = new[]
        {
            @"<script[^>]*>.*?</script>",
            @"<iframe[^>]*>.*?</iframe>",
            @"<object[^>]*>.*?</object>",
            @"<embed[^>]*>.*?</embed>",
            @"javascript:",
            @"vbscript:",
            @"data:text/html",
            @"<[^>]*>",
            @"<script",
            @"<iframe"
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }
        }

        // Check for SQL injection patterns
        var sqlPatterns = new[]
        {
            @"(\b(union|select|insert|update|delete|drop|create|alter|exec|execute)\b)",
            @"(--|#|/\*|\*/)",
            @"('|(\\x27)|(\\x2D\\x2D)|(\\x23)|(\\x2F\\x2A)|(\\x2A\\x2F))"
        };

        foreach (var pattern in sqlPatterns)
        {
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
