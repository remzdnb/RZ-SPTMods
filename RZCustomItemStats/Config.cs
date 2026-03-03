using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace RZCustomItemStats;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// itemsConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public record ItemsConfig
{
    public const string FileName = "itemsConfig.json";

    public Dictionary<string, ItemOverride> Overrides { get; set; } = new();
}

public class ItemOverride
{
    /// <summary>
    /// Flat property overrides. Key = C# property name on TemplateItemProperties (case-insensitive).
    /// Supports double?, int?, bool?, string?
    /// </summary>
    public Dictionary<string, JsonElement>? Props { get; set; }

    /// <summary>
    /// Grid overrides. Key = grid index as string ("0", "1", …).
    /// </summary>
    public Dictionary<string, GridOverride>? Grids { get; set; }
}

public class GridOverride
{
    public int? CellsH    { get; set; }
    public int? CellsV    { get; set; }
    public int? MaxWeight { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// ConfigLoader
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable]
public class ConfigLoader(ILogger<ConfigLoader> logger)
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly string _configDir = Path.Combine(
        AppContext.BaseDirectory, "user", "mods", typeof(ConfigLoader).Assembly.GetName().Name ?? "ModName", "config"
    );

    private readonly Dictionary<Type, object> _cachedConfigs = new();

    public T Load<T>(string filename) where T : new()
    {
        if (_cachedConfigs.TryGetValue(typeof(T), out var cached))
            return (T)cached;

        var path = Path.Combine(_configDir, filename);
        if (!File.Exists(path))
        {
            logger.LogWarning("[RZCustomItemStats] {File} not found — using default config.", filename);
            var def = new T();
            _cachedConfigs[typeof(T)] = def;
            return def;
        }

        var result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _options) ?? new T();
        _cachedConfigs[typeof(T)] = result;
        return result;
    }
}
