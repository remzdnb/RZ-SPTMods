# RZCustomEconomy {.tabset}

Economy toolkit — full control over trader assorts, buyback policies, hideout, and crafting through config files.

## 🔎 Overview

> Full control over SPT's economy through config files : traders, flea market, hideout, crafting. Every feature is independently toggleable and fully configurable : pick what you need, ignore the rest. Whether you want to tweak a few trader stocks or rebuild the economy from scratch, it's all just config.
> If you're looking for a ready-to-play experience, stay tuned for **FreeTarkov** : a full overhaul mod built on top of RZCustomEconomy, currently in development.

---

---

---

### Getting Started

> **⚠️ By default, every feature in this mod is disabled.** Install it as-is and nothing changes in your game - it's a clean slate.

Each feature has a dedicated on/off switch in `masterConfig.json`. Flip it to `true` and the feature is live. That's it. Every switch maps directly to one of the tabs in this description, which contains the full documentation and config reference for that feature.

- 🏷️ **`EnableDefaultTrades`** — 🟢 On by default → see the **Default Trades** tab
- 🔀 **`EnableRoutedTrades`** — 🔴 Off by default → see the **Routed Trades** tab
- 🛒 **`EnableManualTrades`** — 🔴 Off by default → see the **Manual Trades** tab
- 🏪 **`EnableBuybackConfig`** — 🔴 Off by default → see the **Buyback** tab
- 📦 **`EnableSupplyConfig`** — 🔴 Off by default → see the **Supply** tab
- 🏠 **`EnableHideoutConfig`** — 🔴 Off by default → see the **Hideout** tab
- ⚗️ **`EnableCraftingConfig`** — 🔴 Off by default → see the **Crafting** tab

Pick what you need, leave the rest off. Features are fully independent - run any combination you want.

---

The three main features that control what players see at traders are **Default Trades**, **Routed Trades** and **Manual Trades**. It's worth understanding the difference before diving in.

🏷️ **EnableDefaultTrades** keeps vanilla trader assorts intact and applies patches on top of them, for instance replacing barter requirements with a straight cash price. Setting it to false wipes all vanilla assorts entirely and ignores *defaultTradesConfig.json*.

🔀 **EnableRoutedTrades** takes a completely different approach : it rebuilds trader offers from scratch based on the handbook. Every item gets automatically assigned to a trader according to category rules you define. If set to false, *routedTradesConfig.json* will be entirely ignored.

🛒 **EnableManualTrades** lets you define specific offers for specific traders with full control over every parameter : item, price, currency, barter, durability, attachments. Completely independent from the other two. If set to false, *manualTradesConfig.json* will be entirely ignored.

---

---

---

> You can find all the following settings in `masterConfig.json`

---

### *Main Features Switches*

- 🏷️ **`EnableDefaultTrades`** — Master switch for default trader assorts. (`defaultTradesConfig.json`)
- 🔀 **`EnableRoutedTrades`** — Master switch for automatic handbook → trader routing. (`routedTradesConfig.json`)
- 🛒 **`EnableManualTrades`** — Master switch for manual offers. (`manualTradesConfig.json`)
- 🏪 **`EnableBuybackConfig`** — Master switch for trader buyback rules. (`buybackConfig.json`)
- 📦 **`EnableSupplyConfig`** — Master switch for supply configuration. (`supplyConfig.json`)
- 🏠 **`EnableHideoutConfig`** — Master switch for hideout patchess. (`hideoutConfig.json`)
- ⚗️ **`EnableCraftingConfig`** — Master switch for crafting recipe overrides. (`craftingConfig.json`)

---

### *Secondary Features Switches*

- **`EnableFenceTrades`** — Fence is handled differently from other traders internally and hasn't been fully integrated yet. For now this switch only controls whether Fence's dynamic offers are generated or not. Independant from any other feature.
- **`DisableFleaMarket`** — Disables flea market dynamic offers. Both blocks new offer generation and purges any existing ones. Trader offers are not affected.
- **`UnlockAllTraders`** — Sets all traders as unlocked by default.
- **`AllItemsExamined`** — Mark all items as examined/identified on all profile templates. A small number of items are not affected, not sure why.

---

### *Handbook Price Overrides*

Overrides the handbook price of any item by TPL.
This affects auto-routing prices (which are based on handbook price), as well as any system in SPT that reads handbook prices (flea market base prices, trader sell prices, etc.).

---

### *Blacklists*

- **`StaticBlacklist`** — Items that are broken, invisible, or non-functional in-game.
- **`UserBlacklist`** — Items you want to hide for gameplay or balance reasons.

> 💡 The two blacklists serve different purposes. The static blacklist is for broken or non-functional items — anything listed there will never be examined even if `AllItemsExamined` is enabled, which prevents them from showing up in weapon modding menus. The user blacklist is simply for items you don't want routed to any trader, or removed from vanilla assorts — both can be toggled independently.

> 💡 Both blacklists will prevent auto routing if `ApplyToRoutedTrades` is true.

---

### *Dev Options*

- **`EnableDevMode`** — Force all assort prices to 1 rouble, ignoring all price config.
- **`EnableDevLogs`** — Enables verbose logging.

## 🏷️ Default Trades

Patches vanilla trader assorts without replacing them entirely. Useful when you want to keep the default stock but clean up how trades work.

---

### Configuration : `defaultTradesConfig.json`

**`NoBarterTraders`** — replaces barter schemes with a straight cash payment at handbook price, per trader. Each trader entry supports:

- **`Enabled`** — whether this trader's barters are converted
- **`Currency`** — currency to use for the converted price : `"rub"` | `"eur"` | `"usd"`. EUR and USD amounts are calculated from the handbook rouble price using the in-game exchange rate.
- **`ExcludedBarterTpls`** — list of item TPLs to exclude. If every item in a barter scheme belongs to this list, the scheme is left untouched. Useful to preserve GP coin or lega medal trades selectively.

> 📝 Since I don't personally use this mode, features are still limited for now. Feel free to leave a suggestion if there's something specific you'd like to see added.

## 🔀 Routed Trades

Reads every item in the handbook at runtime and automatically assigns it to a trader based on a category map you define in config. The routing system follows the handbook's category hierarchy : define a route for "Weapons" and every sub-category inherits it automatically. Ships with a full pre-built map covering all vanilla item categories.

**For modded content** — `RouteModdedItemsOnly` detects items not present in the vanilla handbook and routes only those to traders. Every item added by every mod you have installed shows up at a trader immediately, at handbook price. The vanilla_handbook.json file in the mod's root folder is used as the reference to determine what's vanilla and what isn't.

**For visibility** — `ForceRouteAll` bypasses all filters and routes every item in the handbook. Combined with `AllItemsExamined`, nothing is hidden.

**For economy overhauls** — instead of hand-editing dozens of assort JSON files, you maintain a single config that describes the intent and the mod handles the rest.

---

### Configuration : `routedTradesConfig.json`

- **`ForceRouteAll`** — Route every handbook item regardless of category routes and blacklists.
- **`EnableOverrides`** — Enable overrides.
- **`RouteModdedItemsOnly`** — Only route items not present in vanilla_handbook.json (modded items). Mutually exclusive with RouteVanillaItemsOnly.
- **`RouteVanillaItemsOnly`** — Only route items present in vanilla_handbook.json (vanilla items). Mutually exclusive with RouteModdedItemsOnly.
- **`FallbackTrader`** — When ForceRouteAll is true, items with no matching category route go here.

> 💡 Each category route entry maps a handbook category ID to a trader. Sub-categories inherit the parent route automatically.

> 💡 Per-TPL overrides take precedence over category routes.

## 🛒 Manual Trades

Completely independent from auto-routing. Define specific trades for specific traders with full control over every parameter. Manual offers are always injected first, before auto-routing runs.

Each offer supports:
- Rouble price or barter payment (or both combined)
- Stack count (`-1` for unlimited)
- Loyalty level requirement
- Durability for weapons and armor
- Manual children (explicit attachments)
- Auto-resolved required children — for items with required slots (armor plates etc.), the mod automatically injects the correct child items by reading the template from the DB, recursively 😎

---

### Configuration : `manualTradesConfig.json`

> ❔ Offers are grouped by trader ID.

## 📦 Supply

> ⚠️ New, hasn't been thoroughly tested, let me know if something is broken.

Controls trader restock timers and stock amounts per restock cycle.

---

### Configuration : `supplyConfig.json`

- **`EnableRestockTimes`** — Master switch for restock timer overrides.
- **`EnableStockMultipliers`** — Master switch for stock amount multipliers.

---

**`RestockTimes`** — Restock interval in seconds for each trader. If the configured interval is shorter than the trader's current timer, the timer is clamped immediately on server start — no need to wait out the old one.

**`StockMultipliers`** — Multiplies the purchasable quantity (`BuyRestrictionMax`) of trader items per restock cycle. Items with no buy restriction (truly unlimited) are not affected.

- **`EnableByTrader`** — Apply a flat multiplier per trader.
- **`EnableByCategory`** — Apply a multiplier per handbook category. Sub-categories inherit the parent multiplier automatically.

> 💡 Both modes can be active simultaneously — multipliers are combined.

## 🏪 Buyback

Controls what each trader will accept from the player. Each trader can be configured independently — or left untouched if you want to keep vanilla behavior for specific ones.

---

### Configuration : `buybackConfig.json`

Each trader can be set to one of four modes:

- **Default** — leaves the vanilla buy policy untouched
- **Disabled** — trader refuses to buy anything from the player
- **Categories** — trader only accepts items from the specified handbook category IDs
- **AllWithBlacklist** — trader accepts all handbook items except those explicitly blacklisted

## 🏠 Hideout

> ⚠️ New, hasn't been thoroughly tested, let me know if something is broken.

Controls hideout area requirements, construction times, and the bitcoin farm. By default all requirements and construction times are cleared — individual areas can be overridden with custom requirements per level if needed.

---

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

---

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
