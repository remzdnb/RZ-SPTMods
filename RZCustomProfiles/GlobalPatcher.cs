// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace RZCustomProfiles;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class UnlockAllOutfits(ILogger<UnlockAllOutfits> logger, DatabaseService databaseService) : IOnLoad
{
    public Task OnLoad()
    {
        return Task.CompletedTask;

        var ragman = databaseService.GetTraders().GetValueOrDefault(Traders.RAGMAN);
        var suits = ragman?.Suits;
        var storage = databaseService.GetTemplates().CustomisationStorage;

        if (suits is null)
        {
            logger.LogWarning("[RZFreeTarkov] Ragman suits or customisation storage is null.");
            return Task.CompletedTask;
        }

        foreach (var suit in suits)
        {
            if (storage.Any(s => s.Id == suit.SuiteId))
            {
                continue;
            }

            storage.Add(
                new CustomisationStorage
                {
                    Id = suit.SuiteId,
                    Source = CustomisationSource.UNLOCKED_IN_GAME,
                    Type = CustomisationType.SUITE,
                }
            );
        }

        return Task.CompletedTask;
    }
}



