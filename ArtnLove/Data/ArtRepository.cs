using System.Text.Json;

namespace ArtnLove.Data;

public class ArtRepository : IArtRepository
{
    private readonly string _filePath;
    private readonly object _lock = new object();

    public ArtRepository()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.GetFullPath(Path.Combine(dataDir, "artworks.json"));
        // ensure file exists
        if (!File.Exists(_filePath)) File.WriteAllText(_filePath, "[]");
    }

    public Task<Guid> AddAsync(object artwork)
    {
        lock (_lock)
        {
            var text = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<JsonElement>>(text) ?? new List<JsonElement>();
            // create wrapper with id
            var id = Guid.NewGuid();
            var doc = new Dictionary<string, object?>();
            doc["id"] = id;
            doc["created_at"] = DateTime.UtcNow;
            doc["payload"] = artwork;
            list.Add(JsonSerializer.SerializeToElement(doc));
            File.WriteAllText(_filePath, JsonSerializer.Serialize(list));
            return Task.FromResult(id);
        }
    }

    public Task<IEnumerable<JsonElement>> ListAsync()
    {
        lock (_lock)
        {
            var text = File.ReadAllText(_filePath);
            var list = JsonSerializer.Deserialize<List<JsonElement>>(text) ?? new List<JsonElement>();
            return Task.FromResult(list.AsEnumerable());
        }
    }
}
