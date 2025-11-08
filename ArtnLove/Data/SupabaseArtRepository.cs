using System.Text.Json;
using System.Net.Http.Json;

namespace ArtnLove.Data;

public class SupabaseArtRepository : IArtRepository
{
    private readonly ArtnLove.Services.SupabaseService _supabase;
    private readonly IHttpClientFactory _httpFactory;

    public SupabaseArtRepository(ArtnLove.Services.SupabaseService supabase, IHttpClientFactory httpFactory)
    {
        _supabase = supabase;
        _httpFactory = httpFactory;
    }

    public async Task<Guid> AddAsync(object artwork)
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var json = JsonSerializer.SerializeToElement(artwork).GetRawText();

        var payload = new
        {
            id = id,
            title = GetPropertyOrDefault(artwork, "title"),
            description = GetPropertyOrDefault(artwork, "description"),
            image_url = (string?)null,
            visibility = "public",
            price = (decimal?)null,
            metadata = json,
            created_at = createdAt
        };

        using var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(_supabase.ProjectUrl);
        client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabase.ServiceRoleKey}");

        var response = await client.PostAsJsonAsync("/rest/v1/artworks", payload);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to add artwork");
        }

        return id;
    }

    public async Task<IEnumerable<JsonElement>> ListAsync()
    {
        using var client = _httpFactory.CreateClient();
        client.BaseAddress = new Uri(_supabase.ProjectUrl);
        client.DefaultRequestHeaders.Add("apikey", _supabase.AnonKey);
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabase.ServiceRoleKey}");

        var response = await client.GetAsync("/rest/v1/artworks?select=*&order=created_at.desc&limit=100");
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Failed to list artworks");
        }

        var content = await response.Content.ReadAsStringAsync();
        var arr = JsonSerializer.Deserialize<JsonElement>(content);
        var list = new List<JsonElement>();
        if (arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                list.Add(el);
            }
        }

        return list;
    }

    private static object? GetPropertyOrDefault(object obj, string name)
    {
        var t = obj.GetType();
        var prop = t.GetProperty(name, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (prop != null) return prop.GetValue(obj);

        if (obj is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                if (string.Equals(key?.ToString(), name, StringComparison.OrdinalIgnoreCase)) return dict[key!];
            }
        }

        return null;
    }
}
