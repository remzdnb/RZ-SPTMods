// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class CraftingPatcher(ILogger<CraftingPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

    public Task OnLoad()
    {
        if (!_masterConfig.EnableCraftingConfig)
            return Task.CompletedTask;

        var config = configLoader.Load<CraftsConfig>(CraftsConfig.FileName);
        var recipes = databaseService.GetTables().Hideout?.Production?.Recipes;

        if (recipes is null)
        {
            logger.LogWarning("[RZFreeMode] Hideout recipes is null -- skipping.");
            return Task.CompletedTask;
        }

        // 1. Clear specified areas.

        var toRemove = config.ClearAreas.ToHashSet();
        recipes.RemoveAll(r => r.AreaType.HasValue && toRemove.Contains(r.AreaType.Value));

        // 2. Inject custom recipes.

        if (config.Recipes.Count == 0)
            return Task.CompletedTask;

        var injected = 0;
        foreach (var (areaType, areaRecipes) in config.Recipes)
        {
            foreach (var recipe in areaRecipes)
            {
                recipes.Add(BuildProduction(recipe, areaType));
                injected++;
            }
        }

        if (_masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZFreeMode] {Count} custom recipe(s) injected.", injected);
        }

        return Task.CompletedTask;
    }

    private static HideoutProduction BuildProduction(CraftRecipe r, HideoutAreas areaType)
    {
        return new HideoutProduction
        {
            Id = new MongoId(),
            AreaType = areaType,
            ProductionTime = r.ProductionTime,
            NeedFuelForAllProductionTime = r.NeedFuelForAllProductionTime,
            Locked = r.Locked,
            EndProduct = r.EndProduct,
            Continuous = r.Continuous,
            Count = r.Count,
            ProductionLimitCount = 0,
            IsEncoded = false,
            Requirements = r
                .Requirements.Select(req => new Requirement
                {
                    Type = req.Type,
                    TemplateId = req.TemplateId,
                    AreaType = (int?)req.AreaType,
                    RequiredLevel = req.RequiredLevel,
                    Count = req.Count,
                    IsFunctional = req.IsFunctional,
                    IsEncoded = req.IsEncoded,
                })
                .ToList(),
        };
    }
}
