// DevTools.cs — RemzDNB 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using Path = System.IO.Path;

namespace RZCustomEconomy;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class DevTools(ILogger<DevTools> logger, DatabaseService databaseService, ConfigLoader configLoader ) : IOnLoad
{
    private static readonly string _devDir = Path.Combine(
        AppContext.BaseDirectory, "user", "mods", "RZCustomEconomy", "dev"
    );

    public Task OnLoad()
    {
        var devConfig = configLoader.Load<DevConfig>(DevConfig.FileName, Assembly.GetExecutingAssembly());

        if (devConfig.DumpEnable)
            DumpItems(devConfig);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DumpItems
    // ─────────────────────────────────────────────────────────────────────────

    private void DumpItems(DevConfig config)
    {
        var handbook = databaseService.GetTables().Templates?.Handbook;
        if (handbook is null)
        {
            logger.LogWarning("[RZCustomEconomy] DevTools: handbook is null, cannot dump items.");
            return;
        }

        // Build vanilla TPL set if needed
        HashSet<string>? vanillaTpls = null;
        if (config.DumpModdedItemsOnly)
        {
            vanillaTpls = LoadVanillaTpls();
            if (vanillaTpls is null) return;
        }

        // Build category set if needed
        HashSet<string>? categoryIds = null;
        if (config.DumpOnlyFromCategories)
        {
            var enabledCategories = config.DumpCategories.Where(c => c.Enabled).ToList();
            if (enabledCategories.Count == 0)
            {
                logger.LogWarning("[RZCustomEconomy] DevTools: DumpOnlyFromCategories is true but no enabled category found.");
                return;
            }
            categoryIds = BuildCategorySet(config.DumpCategories, handbook);
        }

        Dictionary<string, string>? enLocale = null;
        if (databaseService.GetTables().Locales?.Global.TryGetValue("en", out var enLocaleRaw) == true)
            enLocale = enLocaleRaw?.Value;

        var items = handbook.Items
            .Where(h =>
            {
                var tpl = h.Id.ToString();
                if (vanillaTpls is not null && vanillaTpls.Contains(tpl)) return false;
                if (categoryIds is not null && !categoryIds.Contains(h.ParentId.ToString())) return false;
                return true;
            })
            .OrderBy(h => h.Id.ToString())
            .ToList();

        if (items.Count == 0)
        {
            logger.LogInformation("[RZCustomEconomy] DevTools: no items matched the dump criteria.");
            return;
        }

        var lines = items.Select(h =>
        {
            var tpl  = h.Id.ToString();
            var name = "?";
            if (enLocale is not null)
            {
                enLocale.TryGetValue($"{tpl} Name", out name);
                name ??= "?";
            }

            return config.DumpHandbookPrice
                ? $"{tpl}  {name}  ({h.Price})"
                : $"{tpl}  {name}";
        });

        var outputPath = Path.Combine(_devDir, "item_dump.txt");
        try
        {
            Directory.CreateDirectory(_devDir);
            File.WriteAllLines(outputPath, lines);
            logger.LogInformation("\e[1;32m[RZCustomEconomy] DevTools: {Count} item(s) dumped to dev/item_dump.txt.\e[0m", items.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[RZCustomEconomy] DevTools: failed to write item_dump.txt: {Err}", ex.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private HashSet<string>? LoadVanillaTpls()
    {
        var vanillaPath = Path.Combine(AppContext.BaseDirectory, "user", "mods", "RZCustomEconomy", "vanilla_handbook.json");
        if (!File.Exists(vanillaPath))
        {
            logger.LogWarning("[RZCustomEconomy] DevTools: vanilla_handbook.json not found.");
            return null;
        }

        try
        {
            var raw       = File.ReadAllText(vanillaPath);
            using var doc = System.Text.Json.JsonDocument.Parse(raw);

            var tpls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (doc.RootElement.TryGetProperty("Items", out var itemsEl))
                foreach (var item in itemsEl.EnumerateArray())
                    if (item.TryGetProperty("Id", out var idEl))
                        tpls.Add(idEl.GetString() ?? "");

            return tpls;
        }
        catch (Exception ex)
        {
            logger.LogWarning("[RZCustomEconomy] DevTools: failed to parse vanilla_handbook.json: {Err}", ex.Message);
            return null;
        }
    }

    private HashSet<string> BuildCategorySet(List<CategoryRoute> roots, HandbookBase handbook)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue  = new Queue<string>(roots.Where(r => r.Enabled).Select(r => r.CategoryId));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current)) continue;

            foreach (var child in handbook.Categories.Where(c => c.ParentId?.ToString() == current))
                queue.Enqueue(child.Id.ToString());
        }

        return result;
    }
}
