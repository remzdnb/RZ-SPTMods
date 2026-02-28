using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace RZCustomLoot;

public record LootConfig
{
    public const string FileName = "lootConfig.json";

    public bool Enabled { get; set; } = true;
    public List<string> BossRoles { get; set; } = [];
    public List<string> FollowerRoles { get; set; } = [];

    public BossLootSection? Bosses { get; set; }
    public BotLootSection? Followers { get; set; }
    public BotLootSection? Pmc { get; set; }
    public BotLootSection? Scav { get; set; }

    public OverwriteBlacklist? OverwriteBlacklist { get; set; }

    public bool EnableVerboseLogging { get; set; } = false;
}

public record BossLootSection
{
    public bool Enabled { get; set; } = true;
    public bool UseGlobalConfig { get; set; } = true;
    public BotLootSection? Global { get; set; }
    public Dictionary<string, BotLootSection>? PerBoss { get; set; }
}

public record BotLootSection
{
    public bool Enabled { get; set; } = true;
    public List<LootEntry> Pockets { get; set; } = [];
}

public record LootEntry
{
    public string Tpl { get; set; } = "";
    public double Chance { get; set; } = 100;
}

public class OverwriteBlacklist
{
    public HashSet<string> Tpls { get; set; } = new();
    public HashSet<string> Categories { get; set; } = new();
}

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
        if (_cachedConfigs.TryGetValue(typeof(T), out var cached)) {
            return (T)cached;
        }

        var path = Path.Combine(_configDir, filename);
        if (!File.Exists(path)) {
            logger.LogError("[RZCustomLoot] {File} not found : using default config.", filename);
            var def = new T();
            _cachedConfigs[typeof(T)] = def;
            return def;
        }

        var result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _options) ?? new T();
        _cachedConfigs[typeof(T)] = result;

        return result;
    }
}

