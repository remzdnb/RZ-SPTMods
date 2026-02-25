# RZCustomEconomy {.tabset}

Economy toolkit — full control over trader assorts, buyback policies, hideout, and crafting through config files.

## Overview

CustomEconomy is the successor to AutoAssort. As I kept working on it, I realized it had grown well beyond just rerouting handbook items and injecting custom trades — so I extracted everything economy-related from my upcoming total overhaul mod into its own standalone package. The result is a much more coherent and solid foundation. Compared to AutoAssort, the config has been split into separate files to keep things clear — a master config handles the main feature toggles, while each major feature has its own dedicated file.

Think of this less as a mod and more as a tool. It's aimed at server owners who want full control over their economy and are willing to spend time configuring it. If you're looking for a balanced, ready-to-play experience, stay tuned for an upcoming release that wraps around all of this.

> ⚠️ If you were using RZAutoAssort, remove it before installing this mod. RZCustomEconomy is its direct replacement — running both will cause conflicts.

> ⚠️ The default config is tuned for my own economy overhaul project and is not meant to be playable out-of-the-box. Use it as a reference and starting point.

### Global Flags

- **`ClearDefaultAssorts`** — clears all vanilla trader assorts before injecting custom ones - disable if you want to add offers on top of existing ones
- **`ClearFenceAssorts`** — same as above but for Fence specifically


- **`EnableAutoRouting`** — master switch for automatic handbook → trader routing
- **`EnableManualOffers`** — master switch for manual offers
- **`EnableBuybackConfig`** — master switch for trader buyback rules
- **`EnableHideouConfigt`** — master switch for hideout patches (requirements, construction times, bitcoin farm)
- **`EnableCraftingConfig`** — master switch for crafting recipe overrides


- **`DisableFleaMarket`** —  disables flea market dynamic offers - both blocks new offer generation and purges any existing ones - trader offers are not affected
- **`UnlockAllTraders`** — sets all traders as unlocked by default
- **`AllItemsExamined`** — marks every item as examined on all profile templates
- **`HandbookPrices`** — overrides the handbook price of any item by TPL. Affects auto-routing prices and any SPT system that reads handbook prices (flea market base prices, trader sell values, etc.)


- **`EnableDevMode`** — forces all assort prices to 1 rouble, ignoring all price config
- **`EnableDevLogs`** — enables verbose logging

## Feature 1 — Auto Routing

Reads every item in the handbook at runtime and automatically assigns it to a trader based on a category map you define in config. The routing system follows the handbook's category hierarchy — define a route for "Weapons" and every sub-category inherits it automatically. Ships with a full pre-built map covering all vanilla item categories.

**For modded content** — `RouteModdedItemsOnly` detects items not present in the vanilla handbook and routes only those to traders. Every item added by every mod you have installed shows up at a trader immediately, at handbook price. Requires a `vanilla_handbook.json` file (copy of the vanilla SPT `handbook.json`) placed in the mod's root folder.

**For visibility** — `ForceRouteAll` bypasses all filters and routes every item in the handbook. Combined with `AllItemsExamined`, nothing is hidden.

**For economy overhauls** — instead of hand-editing dozens of assort JSON files, you maintain a single config that describes the intent and the mod handles the rest.

### Configuration

| Flag | Default | Description |
|------|---------|-------------|
| `ForceRouteAll` | `true` | Routes every handbook item ignoring blacklists and disabled routes |
| `RouteModdedItemsOnly` | `false` | Only routes items not present in `vanilla_handbook.json` |
| `RouteVanillaItemsOnly` | `false` | Only routes items present in `vanilla_handbook.json` |
| `EnableOverrides` | `false` | Enables per-TPL overrides |
| `UseStaticBlacklist` | `true` | Applies the static blacklist (broken/non-functional items) |
| `UseUserBlacklist` | `true` | Applies the user blacklist |
| `FallbackTrader` | `"Arena"` | When `ForceRouteAll` is true, unmatched items go here |

Each category route entry maps a handbook category ID to a trader. Sub-categories inherit the parent route automatically.
```json
{ "Enabled": true, "CategoryId": "5b5f792486f77447ed5636b3", "TraderName": "Peacekeeper", "PriceMultiplier": 1.0, "LoyaltyLevel": 1 }
```

Per-TPL overrides take precedence over category routes. Set `TraderName` to `""` to blacklist an item without touching `UserBlacklist`.
```json
{
  "ItemTpl": "5c093ca986f7740a1867ab12",
  "TraderName": "Jaeger",
  "PriceRoubles": 500000,
  "LoyaltyLevel": 4,
  "StackCount": 1,
  "BarterItems": [
    { "ItemTpl": "5d235b4d86f7742e017bc88a", "Count": 3 }
  ]
}
```

> ⚠️ Out of the box, `ForceRouteAll` is `true` as a demo mode. For a real playthrough set it to `false` and configure your `CategoryRoutes` and `Overrides` manually.

## Feature 2 — Manual Offers

Completely independent from auto-routing. Define specific trades for specific traders with full control over every parameter. Manual offers are always injected first, before auto-routing runs.

Each offer supports:
- Rouble price or barter payment (or both combined)
- Stack count (`-1` for unlimited)
- Loyalty level requirement
- Durability for weapons and armor
- Manual children (explicit attachments)
- Auto-resolved required children — for items with required slots (armor plates etc.), the mod automatically injects the correct child items by reading the template from the DB, recursively

### Configuration

Offers are grouped by trader ID.
```json
{
  "Id": "54cb50c76803fa8b248b4571",
  "Offers": [
    {
      "ItemTpl": "5a16b8a9fcdbcb00165aa6ca",
      "StackCount": -1,
      "LoyaltyLevel": 1,
      "Durability": 100,
      "PriceRoubles": 45000,
      "Children": [],
      "BarterItems": []
    }
  ]
}
```

## Feature 3 — Buyback

**Config file :** `buybackConfig.json`

Controls what each trader will accept from the player. Each trader can be configured independently — or left untouched if you want to keep vanilla behavior for specific ones.
### Configuration

Each trader can be set to one of four modes:

- **Default** — leaves the vanilla buy policy untouched
- **Disabled** — trader refuses to buy anything from the player
- **Categories** — trader only accepts items from the specified handbook category IDs
- **AllWithBlacklist** — trader accepts all handbook items except those explicitly blacklisted
```json
"Fence": {
"Mode": "AllWithBlacklist",
"Blacklist": [
    "59faff1d86f7746c51718c9c",
    "5732ee6a24597719ae0c0281"
  ]
}
```

## About

First SPT mod, early beta. I've been learning the SPT C# server codebase as I go — feedback and bug reports are welcome.

RZCustomEconomy is the first building block of a larger economy conversion mod for SPT. The routing and manual offer system is designed to be the foundation that everything else builds on top of. Stay tuned.

French developer, 35 years old. I've been modding Tarkov for about two weeks, mostly to build a fun server for me and my friends to play on.
I do use Claude AI to help write parts of the code — no point hiding it. C# isn't my primary language, I come from a C++ background. That said, if anyone has doubts about whether I actually know what I'm doing: my day job involves navigating and maintaining Unreal Engine C++ codebases of 150k+ lines where I understand exactly what every line does. So using AI to smooth over some syntax differences on a hobby project that will never make me a single dollar feels pretty reasonable to me.
If you have feedback, suggestions, or just want to talk about the mod — Discord: remzdnb.

**Tested compatible mods:**
- [More Energy Drinks](https://forge.sp-tarkov.com/mod/1688/more-energy-drinks) by Hood
- [WTT - Content Backport](https://forge.sp-tarkov.com/mod/2512/wtt-content-backport) by GrooveypenguinX

Discord: **remzdnb**

{.endtabset}
