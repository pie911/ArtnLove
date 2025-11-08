using System.Text.Json;

namespace ArtnLove.Data;

public interface IArtRepository
{
    Task<Guid> AddAsync(object artwork);
    Task<IEnumerable<JsonElement>> ListAsync();
}
