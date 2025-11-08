 using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Json;

namespace ArtnLove.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ArtnLove.Services.SupabaseService _supabase;

    public AuthController(ILogger<AuthController> logger, IHttpClientFactory httpFactory, ArtnLove.Services.SupabaseService supabase)
    {
        _logger = logger;
        _httpFactory = httpFactory;
        _supabase = supabase;
    }

    public record RegisterRequest(string Email, string Password, string? DisplayName, string? Role, string? Bio, string? Location, string? Website);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string AccessToken, string RefreshToken, User User);
    public record User(string Id, string Email, string? DisplayName, string? Role, string? Bio, string? Location, string? Website, string? Sub);
    public record UpdateProfileRequest(string? DisplayName, string? Bio, string? Location, string? Website);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required" });

        if (req.Role != null && !new[] { "buyer", "seller" }.Contains(req.Role))
            return BadRequest(new { message = "Role must be either 'buyer' or 'seller'" });

        try
        {
            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_supabase.ProjectUrl);
            client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);
            // Note: Service role key should not be used for user signup operations
            // Only anon key is needed for public registration

            var authPayload = new
            {
                email = req.Email,
                password = req.Password
            };

            var response = await client.PostAsJsonAsync("/auth/v1/signup", authPayload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase signup failed: {Status} {Content}", response.StatusCode, content);
                return StatusCode((int)response.StatusCode, new { message = "Registration failed", details = content });
            }

            // Supabase signup returns different response structure - check for user data directly
            var signupResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (signupResponse == null || !signupResponse.ContainsKey("user"))
            {
                _logger.LogWarning("Supabase signup returned success but no user data: {Content}", content);
                return BadRequest(new { message = "Registration failed: invalid response from auth service" });
            }

            // Extract user data from the signup response
            var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(signupResponse["user"].ToString()!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (userData == null)
            {
                return BadRequest(new { message = "Registration failed: invalid user data" });
            }
            return Ok(new
            {
                message = "Registration successful. Please check your email to confirm your account before logging in.",
                user = new
                {
                    id = userData["id"]?.ToString(),
                    email = userData["email"]?.ToString(),
                    display_name = req.DisplayName,
                    role = req.Role ?? "buyer",
                    bio = req.Bio,
                    location = req.Location,
                    website = req.Website
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            return StatusCode(500, new { message = "Internal server error during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required" });

        try
        {
            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_supabase.ProjectUrl);
            client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);

            var authPayload = new
            {
                email = req.Email,
                password = req.Password
            };

            var response = await client.PostAsJsonAsync("/auth/v1/token?grant_type=password", authPayload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase login failed: {Status} {Content}", response.StatusCode, content);
                return StatusCode((int)response.StatusCode, new { message = "Login failed", details = content });
            }

            var authResult = JsonSerializer.Deserialize<AuthResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Ok(new
            {
                access_token = authResult?.AccessToken,
                refresh_token = authResult?.RefreshToken,
                user = authResult?.User
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error");
            return StatusCode(500, new { message = "Internal server error during login" });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // Get the JWT token from Authorization header
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return BadRequest(new { message = "Authorization token required" });

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_supabase.ProjectUrl);
            client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync("/auth/v1/logout", null);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase logout failed: {Status} {Content}", response.StatusCode, content);
                // Don't fail the request, just log it
            }

            return Ok(new { message = "Logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, new { message = "Internal server error during logout" });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        // Get the JWT token from Authorization header
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { message = "Authorization token required" });

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_supabase.ProjectUrl);
            client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync("/auth/v1/user");
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Supabase get user failed: {Status} {Content}", response.StatusCode, content);
                return StatusCode((int)response.StatusCode, new { message = "Failed to get user info", details = content });
            }

            var user = JsonSerializer.Deserialize<User>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return Ok(new { user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get current user error");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            return Unauthorized(new { message = "Authorization token required" });

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            using var client = _httpFactory.CreateClient();
            client.BaseAddress = new Uri(_supabase.ProjectUrl);
            client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                data = new
                {
                    display_name = req.DisplayName,
                    bio = req.Bio,
                    location = req.Location,
                    website = req.Website
                }
            };

            var response = await client.PutAsJsonAsync("/auth/v1/user", body);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to update profile: {Status} {Content}", response.StatusCode, content);
                return StatusCode((int)response.StatusCode, new { message = "Failed to update profile", details = content });
            }

            return Ok(new { message = "Profile updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update profile error");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
