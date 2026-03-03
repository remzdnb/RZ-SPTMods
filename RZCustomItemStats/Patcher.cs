// RemzDNB - 2026

using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomItemStats;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class ItemPatcher(ILogger<ItemPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    private static readonly Dictionary<string, PropertyInfo> _propCache =
        typeof(TemplateItemProperties)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

    public Task OnLoad()
    {
        var config = configLoader.Load<ItemsConfig>(ItemsConfig.FileName);

        if (config.Overrides.Count == 0)
        {
            logger.LogInformation("[RZCustomItemStats] No item overrides defined, skipping.");
            return Task.CompletedTask;
        }

        var items = databaseService.GetTables().Templates?.Items;
        if (items is null)
        {
            logger.LogError("[RZCustomItemStats] Templates.Items is null.");
            return Task.CompletedTask;
        }

        var patchedCount = 0;
        foreach ((string tpl, ItemOverride ov) in config.Overrides)
        {
            if (!items.TryGetValue(tpl, out var item))
            {
                logger.LogWarning("[RZCustomItemStats] TPL '{Tpl}' not found, skipping.", tpl);
                continue;
            }

            if (item.Properties is null)
            {
                logger.LogWarning("[RZCustomItemStats] TPL '{Tpl}' has no properties, skipping.", tpl);
                continue;
            }

            var patchedAny = false;
            patchedAny |= ApplyFlatProps(tpl, item.Properties, ov);
            patchedAny |= ApplyGridOverrides(tpl, item.Properties, ov);

            if (patchedAny) {
                patchedCount++;
            }
        }

        //logger.LogInformation("[RZCustomItemStats] {Count} item(s) patched.", patchedCount);
        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Flat props via reflection
    // ─────────────────────────────────────────────────────────────────────────

    private bool ApplyFlatProps(string tpl, TemplateItemProperties props, ItemOverride ov)
    {
        if (ov.Props is null || ov.Props.Count == 0)
            return false;

        var applied = false;

        foreach (var (key, element) in ov.Props)
        {
            if (!_propCache.TryGetValue(key, out var propInfo))
            {
                logger.LogWarning("[RZCustomItemStats] '{Tpl}': unknown property '{Key}' — skipped.", tpl, key);
                continue;
            }

            try
            {
                var value = ConvertElement(element, propInfo.PropertyType);
                if (value is null)
                {
                    logger.LogWarning(
                        "[RZCustomItemStats] '{Tpl}': could not convert '{Key}' ({Kind}) — skipped.", tpl, key, element.ValueKind
                    );
                    continue;
                }

                propInfo.SetValue(props, value);
                applied = true;
            }
            catch (Exception ex)
            {
                logger.LogWarning("[RZCustomItemStats] '{Tpl}': failed to set '{Key}': {Msg}", tpl, key, ex.Message);
            }
        }

        return applied;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Grid overrides
    // ─────────────────────────────────────────────────────────────────────────

    private bool ApplyGridOverrides(string tpl, TemplateItemProperties props, ItemOverride ov)
    {
        if (ov.Grids is null || ov.Grids.Count == 0)
            return false;

        var grids = props.Grids?.ToList();
        if (grids is null || grids.Count == 0)
        {
            logger.LogWarning("[RZCustomItemStats] '{Tpl}': no grids found — grid overrides skipped.", tpl);
            return false;
        }

        var applied = false;

        foreach (var (indexStr, gridOv) in ov.Grids)
        {
            if (!int.TryParse(indexStr, out var index) || index < 0 || index >= grids.Count)
            {
                logger.LogWarning("[RZCustomItemStats] '{Tpl}': invalid grid index '{Index}' — skipped.", tpl, indexStr);
                continue;
            }

            var grid = grids[index];
            if (grid.Properties is null)
            {
                logger.LogWarning("[RZCustomItemStats] '{Tpl}': grid[{Index}] has no properties — skipped.", tpl, index);
                continue;
            }

            if (gridOv.CellsH.HasValue)    grid.Properties.CellsH   = gridOv.CellsH.Value;
            if (gridOv.CellsV.HasValue)    grid.Properties.CellsV   = gridOv.CellsV.Value;
            if (gridOv.MaxWeight.HasValue) grid.Properties.MaxWeight = gridOv.MaxWeight.Value;

            applied = true;
        }

        if (applied)
            props.Grids = grids;

        return applied;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JsonElement → typed value converter
    // ─────────────────────────────────────────────────────────────────────────

    private static object? ConvertElement(JsonElement el, Type targetType)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            return underlying switch
            {
                _ when underlying == typeof(double)  => el.ValueKind == JsonValueKind.Number
                                                            ? el.GetDouble()
                                                            : null,
                _ when underlying == typeof(float)   => el.ValueKind == JsonValueKind.Number
                                                            ? (float)el.GetDouble()
                                                            : null,
                _ when underlying == typeof(int)     => el.ValueKind == JsonValueKind.Number
                                                            ? el.GetInt32()
                                                            : null,
                _ when underlying == typeof(long)    => el.ValueKind == JsonValueKind.Number
                                                            ? el.GetInt64()
                                                            : null,
                _ when underlying == typeof(bool)    => el.ValueKind is JsonValueKind.True or JsonValueKind.False
                                                            ? el.GetBoolean()
                                                            : null,
                _ when underlying == typeof(string)  => el.ValueKind == JsonValueKind.String
                                                            ? el.GetString()
                                                            : null,
                _ when underlying == typeof(MongoId) => el.ValueKind == JsonValueKind.String
                                                            ? new MongoId(el.GetString()!)
                                                            : null,
                _                                    => null,
            };
        }
        catch
        {
            return null;
        }
    }
}
