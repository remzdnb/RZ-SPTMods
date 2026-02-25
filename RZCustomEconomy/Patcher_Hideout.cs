// RemzDNB - 2026
// ReSharper disable InvertIf
// ReSharper disable EnforceIfStatementBraces

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class HideoutPatcher(ILogger<HideoutPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);
        if (!masterConfig.EnableHideoutConfig)
            return Task.CompletedTask;

        var hideoutConfig = configLoader.Load<HideoutConfig>(HideoutConfig.FileName);

        ApplyAreaOverrides(hideoutConfig);
        PatchBitcoinFarm(hideoutConfig.BitcoinFarm);

        return Task.CompletedTask;
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Area overrides
    // ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ApplyAreaOverrides(HideoutConfig config)
    {
        var areas = databaseService.GetTables().Hideout?.Areas;
        if (areas is null || areas.Count == 0) {
            logger.LogWarning("[RZFreeMode] No hideout areas found in database — nothing to patch.");
            return;
        }

        // Clear all requirements and construction times SEULEMENT si pas de config custom
        foreach (var area in areas) {
            var areaName = area.Type.ToString();
            var hasCustomRequirements = config.Areas.TryGetValue(areaName, out var areaConfig)
                                        && areaConfig?.LevelRequirements is not null
                                        && areaConfig.LevelRequirements.Count > 0;

            if (!hasCustomRequirements) {
                // Comportement par défaut : clear tout
                area.Requirements?.Clear();
                area.EnableAreaRequirements = false;

                if (area.Stages is not null) {
                    foreach (var (_, stage) in area.Stages) {
                        stage.Requirements?.Clear();
                        stage.ConstructionTime = 0;
                    }
                }
            }
        }

        // Apply per-area overrides from config
        foreach (var (areaName, override_) in config.Areas) {
            if (!Enum.TryParse<HideoutAreas>(areaName, ignoreCase: true, out var areaType)) {
                logger.LogWarning("[RZFreeMode] Unknown hideout area '{Name}' in config — skipping.", areaName);
                continue;
            }

            var area = areas.FirstOrDefault(a => a.Type == areaType);
            if (area is null) {
                logger.LogWarning("[RZFreeMode] Area '{Name}' ({Type}) not found in database — skipping.", areaName, areaType);
                continue;
            }

            if (override_.RemoveFromDb == true) {
                areas.RemoveAll(a => a.Type == areaType);
                continue;
            }

            if (override_.Enabled.HasValue)
                area.IsEnabled = override_.Enabled.Value;

            if (override_.DisplayLevel.HasValue)
                area.DisplayLevel = override_.DisplayLevel.Value;

            // Nouveau : apply custom requirements par level
            if (override_.LevelRequirements is not null && override_.LevelRequirements.Count > 0) {
                ApplyCustomRequirements(area, override_.LevelRequirements, areaName);
            }
        }
    }

    private void ApplyCustomRequirements(
        HideoutArea area,
        Dictionary<string, List<StageRequirement>> levelRequirements,
        string areaName)
    {
        if (area.Stages is null) {
            logger.LogWarning("[RZFreeMode] Area '{Name}' has no stages — cannot apply requirements.", areaName);
            return;
        }

        foreach (var (levelStr, simpleRequirements) in levelRequirements) {
            if (!area.Stages.TryGetValue(levelStr, out var stage)) {
                logger.LogWarning("[RZFreeMode] Area '{Name}' has no stage '{Level}' — skipping.", areaName, levelStr);
                continue;
            }

            // Convertir les SimpleStageRequirement en StageRequirement
            stage.Requirements = simpleRequirements
                .Select(req => ConvertToStageRequirement(req, areaName))
                .Where(req => req is not null)
                .ToList()!;

            stage.ConstructionTime = 0;

            logger.LogInformation(
                "[RZFreeMode] Area '{Name}' level {Level}: {Count} requirement(s) applied.",
                areaName, levelStr, stage.Requirements.Count
            );
        }
    }

    private SPTarkov.Server.Core.Models.Eft.Hideout.StageRequirement? ConvertToStageRequirement(StageRequirement req, string areaName)
    {
        switch (req.Type.ToLowerInvariant())
        {
            case "item":

                if (string.IsNullOrEmpty(req.ItemTpl)) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': Item requirement missing ItemTpl.", areaName);
                    return null;
                }

                return new SPTarkov.Server.Core.Models.Eft.Hideout.StageRequirement {
                    Type = "Item",
                    TemplateId = req.ItemTpl,
                    Count = req.ItemCount,
                    IsFunctional = req.ItemFunctional,
                    IsEncoded = false
                };

            case "area":

                if (string.IsNullOrEmpty(req.AreaName)) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': Area requirement missing AreaName.", areaName);
                    return null;
                }

                if (!Enum.TryParse<HideoutAreas>(req.AreaName, ignoreCase: true, out var requiredArea)) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': Unknown required area '{ReqArea}'.", areaName, req.AreaName);
                    return null;
                }

                return new SPTarkov.Server.Core.Models.Eft.Hideout.StageRequirement {
                    Type = "Area",
                    AreaType = (int) requiredArea,
                    RequiredLevel = req.AreaLevel,
                    IsEncoded = false
                };

            case "skill":
                if (string.IsNullOrEmpty(req.SkillName)) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': Skill requirement missing SkillName.", areaName);
                    return null;
                }

                return new SPTarkov.Server.Core.Models.Eft.Hideout.StageRequirement {
                    Type = "Skill",
                    TemplateId = req.SkillName,
                    Count = req.SkillLevel,
                    IsEncoded = false
                };

            case "traderloyalty":
                if (string.IsNullOrEmpty(req.TraderName)) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': Trader requirement missing TraderName.", areaName);
                    return null;
                }

                var traderId = RZCustomEconomy.TraderIds.FromName(req.TraderName);
                if (traderId is null) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': Unknown trader '{Trader}'.", areaName, req.TraderName);
                    return null;
                }

                return new SPTarkov.Server.Core.Models.Eft.Hideout.StageRequirement {
                    Type = "TraderLoyalty",
                    TraderId = traderId,
                    LoyaltyLevel = req.TraderLoyalty,
                    IsEncoded = false
                };

            case "questcomplete":
                if (string.IsNullOrEmpty(req.QuestId)) {
                    logger.LogWarning("[RZFreeMode] Area '{Area}': QuestComplete requirement missing QuestId.", areaName);
                    return null;
                }

                return new SPTarkov.Server.Core.Models.Eft.Hideout.StageRequirement {
                    Type = "QuestComplete",
                    //QuestId = req.QuestId,
                    IsEncoded = false
                };

            default:
                logger.LogWarning("[RZFreeMode] Area '{Area}': Unknown requirement type '{Type}'.", areaName, req.Type);
                return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Bitcoin farm
    // ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void PatchBitcoinFarm(BitcoinFarmConfig? config)
    {
        if (config is null)
        {
            return;
        }

        var hideout = databaseService.GetHideout();
        var recipes = hideout.Production?.Recipes?.FindAll(r => r.EndProduct == ItemTpl.BARTER_PHYSICAL_BITCOIN);
        if (recipes is null || recipes.Count == 0)
        {
            logger.LogWarning("[RZFreeMode] Bitcoin farm -- no recipes found, skipping.");
            return;
        }

        foreach (var recipe in recipes)
        {
            if (config.ProductionSpeedMultiplier.HasValue)
            {
                recipe.ProductionTime = (int)Math.Round((double)recipe.ProductionTime! / config.ProductionSpeedMultiplier.Value);
            }

            if (config.MaxCapacity.HasValue)
            {
                recipe.ProductionLimitCount = config.MaxCapacity.Value;
            }
        }

        // GPU boost rate.

        if (config.GpuBoostRate.HasValue)
            hideout.Settings.GpuBoostRate = config.GpuBoostRate.Value;

        logger.LogInformation(
            "[RZFreeMode] Bitcoin farm -- {Count} recipe(s) patched (speed x{Mult}, cap {Cap}, gpu {Gpu}).",
            recipes.Count,
            config.ProductionSpeedMultiplier?.ToString() ?? "-",
            config.MaxCapacity?.ToString() ?? "-",
            config.GpuBoostRate?.ToString() ?? "-"
        );
    }
}
