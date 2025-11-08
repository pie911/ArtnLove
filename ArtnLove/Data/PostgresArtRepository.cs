using System.Text.Json;
using Dapper;
using Npgsql;

namespace ArtnLove.Data;

public class PostgresArtRepository : IArtRepository
{
    private readonly string _connString;

    public PostgresArtRepository(IConfiguration configuration)
    {
        // Expect DATABASE_URL or ConnectionStrings:Default
        _connString = configuration.GetValue<string>("DATABASE_URL") ?? configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("DATABASE_URL not configured");
    }

    public async Task<Guid> AddAsync(object artwork)
    {
        // artwork is a POCO - serialize to JSON and store in metadata
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var json = JsonSerializer.SerializeToElement(artwork).GetRawText();

        var sql = @"
INSERT INTO artworks (id, title, description, image_url, visibility, price, metadata, created_at)
VALUES (@Id, @Title, @Description, @ImageUrl, @Visibility, @Price, @Metadata::jsonb, @CreatedAt)
";

        // Try to extract common fields if present
        using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();
        var parameters = new
        {
            Id = id,
            Title = GetPropertyOrDefault(artwork, "title"),
            Description = GetPropertyOrDefault(artwork, "description"),
            ImageUrl = (string?)null,
            Visibility = "public",
            Price = (decimal?)null,
            Metadata = json,
            CreatedAt = createdAt
        };

        await conn.ExecuteAsync(sql, parameters);
        return id;
    }

    public async Task<IEnumerable<JsonElement>> ListAsync()
    {
        var sql = @"
SELECT jsonb_build_object(
  'id', id,
  'created_at', created_at,
  'payload', jsonb_build_object(
    'title', title,
    'description', description,
    'image_url', image_url,
    'visibility', visibility,
    'price', price,
    'metadata', metadata
  )
)::text as json
FROM artworks
ORDER BY created_at DESC
LIMIT 100
";

        using var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync();
        var rows = await conn.QueryAsync<string>(sql);
        var list = new List<JsonElement>();
        foreach (var r in rows)
        {
            try
            {
                var el = JsonSerializer.Deserialize<JsonElement>(r);
                list.Add(el);
            }
            catch { }
        }

        return list;
    }

    private static object? GetPropertyOrDefault(object obj, string name)
    {
        // Try to reflectively read property or dictionary key
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
