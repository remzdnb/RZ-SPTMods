// RemzDNB - 2026

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;

namespace RZCustomItemTiers;

[Injectable(InjectionType.Singleton)]
public class ConfigLoader(ILogger<ConfigLoader> logger, ModHelper modHelper)
{
    private readonly Dictionary<(Type, string), object> _cachedConfigs = new();

    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public T Load<T>(string filename, Assembly callerAssembly) where T : new()
    {
        var modDir = modHelper.GetAbsolutePathToModFolder(callerAssembly);
        var tag = callerAssembly.GetName().Name ?? "RZ";
        var key = (typeof(T), modDir);

        if (_cachedConfigs.TryGetValue(key, out var cached)) {
            return (T)cached;
        }

        var path = Path.Combine(modDir, "config", filename);
        if (!File.Exists(path))
        {
            logger.LogError("[{Tag}] {File} not found in '{Dir}' : using defaults.", tag, filename, modDir);
            var def = new T();
            _cachedConfigs[key] = def;
            return def;
        }

        var result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _serializerOptions) ?? new T();
        _cachedConfigs[key] = result;
        return result;
    }

    public IEnumerable<T> LoadAll<T>(string subfolder, Assembly callerAssembly, IEnumerable<string>? exclude = null) where T : new()
    {
        var modDir = modHelper.GetAbsolutePathToModFolder(callerAssembly);
        var dir = Path.Combine(modDir, subfolder);

        var tag = callerAssembly.GetName().Name ?? "RZ";

        if (!Directory.Exists(dir))
        {
            logger.LogWarning("[{Tag}] Folder '{Dir}' not found.", tag, dir);
            return [];
        }

        var excludeSet = new HashSet<string>(exclude ?? [], StringComparer.OrdinalIgnoreCase);

        return Directory.GetFiles(dir, "*.json")
            .Where(path => !excludeSet.Contains(Path.GetFileName(path)))
            .OrderBy(path => path)
            .Select<string, T?>(path =>
            {
                try
                {
                    var result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _serializerOptions);
                    if (result is null)
                        logger.LogWarning("[{Tag}] Could not deserialize '{File}' : skipping.", tag, path);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogWarning("[{Tag}] Failed to load '{File}' : {Msg} : skipping.", tag, path, ex.Message);
                    return default;
                }
            })
            .Where(r => r is not null)
            .Cast<T>();
    }
}
