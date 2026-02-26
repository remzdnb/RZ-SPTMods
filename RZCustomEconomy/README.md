# RZCustomEconomy {.tabset}

Economy toolkit — full control over trader assorts, buyback policies, hideout, and crafting through config files.

## 🔎 Overview

CustomEconomy is the successor to AutoAssort. As I kept working on it, I realized it had grown well beyond just rerouting handbook items and injecting custom trades — so I extracted everything economy-related from my upcoming total overhaul mod into its own standalone package. The result is a much more coherent and solid foundation. Compared to AutoAssort, the config has been split into separate files to keep things clear — a master config handles the main feature toggles, while each major feature has its own dedicated file.

Think of this less as a mod and more as a tool. It's aimed at server owners who want full control over their economy and are willing to spend time configuring it. If you're looking for a balanced, ready-to-play experience, stay tuned for an upcoming release that wraps around all of this.

> ⚠️ If you were using RZAutoAssort, remove it before installing this mod. RZCustomEconomy is its direct replacement — running both will cause conflicts.

> ⚠️ Out of the box, `ForceRouteAll` from autoRoutingConfig.json is `true` as a demo mode. For a real playthrough set it to `false` and configure your `CategoryRoutes` and `Overrides` manually.  The default config is tuned for my own economy overhaul project and is not meant to be playable immediatly. Use it as a reference and starting point.

### Global Flags

**Config file :** `masterConfig.json`

- **`ClearDefaultAssorts`** — clears all vanilla trader assorts before injecting custom ones - disable if you want to add offers on top of existing ones
- **`ClearFenceAssorts`** — same as above but for Fence specifically


- **`EnableAutoRouting`** — master switch for automatic handbook → trader routing
- **`EnableManualOffers`** — master switch for manual offers
- **`EnableBuybackConfig`** — master switch for trader buyback rules
- **`EnableHideouConfigt`** — master switch for hideout patches (requirements, construction times, bitcoin farm)
- **`EnableCraftingConfig`** — master switch for crafting recipe overrides


- **`DisableFleaMarket`** —  disables flea market dynamic offers - both blocks new offer generation and purges any existing ones - trader offers are not affected
- **`UnlockAllTraders`** — sets all traders as unlocked by default
- **`AllItemsExamined`** — marks every item as examined on all profile templatesmarks every item as examined on all profile templates. A small number of items (mostly broken ones) are not affected, not sure why.
- **`HandbookPrices`** — overrides the handbook price of any item by TPL. Affects auto-routing prices and any SPT system that reads handbook prices (flea market base prices, trader sell values, etc.)


- **`EnableDevMode`** — forces all assort prices to 1 rouble, ignoring all price config
- **`EnableDevLogs`** — enables verbose logging

## 🔀 Auto Routing

Reads every item in the handbook at runtime and automatically assigns it to a trader based on a category map you define in config. The routing system follows the handbook's category hierarchy — define a route for "Weapons" and every sub-category inherits it automatically. Ships with a full pre-built map covering all vanilla item categories.

**For modded content** — `RouteModdedItemsOnly` detects items not present in the vanilla handbook and routes only those to traders. Every item added by every mod you have installed shows up at a trader immediately, at handbook price. Requires a `vanilla_handbook.json` file (copy of the vanilla SPT `handbook.json`) placed in the mod's root folder.

**For visibility** — `ForceRouteAll` bypasses all filters and routes every item in the handbook. Combined with `AllItemsExamined`, nothing is hidden.

**For economy overhauls** — instead of hand-editing dozens of assort JSON files, you maintain a single config that describes the intent and the mod handles the rest.

### Configuration : `autoRoutingConfig.json`

- **`ForceRouteAll`** — Route every handbook item regardless of category routes and blacklists.
- **`EnableOverrides`** — Enable overrides.
- **`RouteModdedItemsOnly`** — Only route items not present in vanilla_handbook.json (modded items). Mutually exclusive with RouteVanillaItemsOnly.
- **`RouteVanillaItemsOnly`** — Only route items present in vanilla_handbook.json (vanilla items). Mutually exclusive with RouteModdedItemsOnly.
- **`UseStaticBlacklist`** — Apply the StaticBlacklist (broken/non-functional items).
- **`UseUserBlacklist`** — Apply the UserBlacklist (items you want to hide from traders).
- **`FallbackTrader`** — When ForceRouteAll is true, items with no matching category route go here.

> ❔ Each category route entry maps a handbook category ID to a trader. Sub-categories inherit the parent route automatically.

> ❔ Per-TPL overrides take precedence over category routes.

> ❔ The two blacklists serve different purposes. The static blacklist is for broken or non-functional items — anything listed there will never be examined even if `AllItemsExamined` is enabled, which prevents them from showing up in weapon modding menus. The user blacklist is simply for items you don't want routed to any trader. (both will prevent auto routing)

## 🛒 Manual Offers

Completely independent from auto-routing. Define specific trades for specific traders with full control over every parameter. Manual offers are always injected first, before auto-routing runs.

Each offer supports:
- Rouble price or barter payment (or both combined)
- Stack count (`-1` for unlimited)
- Loyalty level requirement
- Durability for weapons and armor
- Manual children (explicit attachments)
- Auto-resolved required children — for items with required slots (armor plates etc.), the mod automatically injects the correct child items by reading the template from the DB, recursively

### Configuration : `manualOffersConfig.json`

> ❔ Offers are grouped by trader ID.

## 🏪 Buyback

Controls what each trader will accept from the player. Each trader can be configured independently — or left untouched if you want to keep vanilla behavior for specific ones.

### Configuration : `buybackConfig.json`

Each trader can be set to one of four modes:

- **Default** — leaves the vanilla buy policy untouched
- **Disabled** — trader refuses to buy anything from the player
- **Categories** — trader only accepts items from the specified handbook category IDs
- **AllWithBlacklist** — trader accepts all handbook items except those explicitly blacklisted

## 🏠 Hideout

> ⚠️ New and experimental

Controls hideout area requirements, construction times, and the bitcoin farm. By default all requirements and construction times are cleared — individual areas can be overridden with custom requirements per level if needed.

### Configuration : `hideoutConfig.json`

Each area supports the following fields:

- **`RemoveFromDb`** — removes the area from the database entirely
- **`Enabled`** — whether the area is available to the player
- **`DisplayLevel`** — whether the current level is displayed in the UI
- **`StartingLevel`** — level the area starts at on a new profile
- **`LevelRequirements`** — optional per-level requirements (trader loyalty, quest completion). If not defined, all requirements for that area are cleared

Bitcoin farm has its own dedicated block:

- **`ProductionSpeedMultiplier`** — multiplies production speed (e.g. `2.0` = twice as fast)
- **`MaxCapacity`** — maximum number of bitcoins that can accumulate
- **`GpuBoostRate`** — GPU boost rate

## ⚗️ Crafting

Define custom crafting recipes per hideout area, and optionally clear all existing recipes for specific areas before injecting your own.

### Configuration : `craftingConfig.json`

- **`ClearAreas`** — list of hideout areas whose vanilla recipes will be wiped before injection
- **`Recipes`** — recipes grouped by hideout area. Each recipe supports area level requirement, end product, count, production time, fuel requirement, and a list of input requirements (items or area levels)

## 🔌 Mod Compatibility

Most features work out of the box with modded content. Auto-routing, manual offers, buyback rules, `AllItemsExamined`, and handbook price overrides all operate on TPLs — as long as you know the correct TPL for a modded item, you can reference it anywhere in the config exactly like a vanilla one. I haven't done extensive compatibility testing though, and this is something I'll look to improve in future updates.

**Tested compatible mods:**
- [More Energy Drinks](https://forge.sp-tarkov.com/mod/1688/more-energy-drinks) by Hood
- [WTT - Content Backport](https://forge.sp-tarkov.com/mod/2512/wtt-content-backport) by GrooveypenguinX

> ⚠️ Any mod that touches the same systems as RZCustomEconomy will conflict. This includes mods that modify trader assorts, flea market offers, hideout requirements or production times, crafting recipes, or trader buyback policies.

{.endtabset}
