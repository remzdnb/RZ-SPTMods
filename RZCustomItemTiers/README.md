> A lightweight inventory color system focused on performance over rarity. Six tiers, sensible defaults, fully configurable, so you always know at a glance what's worth using.

> [![image](https://i.imgur.com/LZ5B2hp.png)](https://i.imgur.com/LZ5B2hp.png)

# RZCustomItemTiers {.tabset}

Color-coded item tiers for your SPT inventory.

## 🔎 Overview

[ODT-ItemInfo](https://forge.sp-tarkov.com/mod/2430/odts-item-info-spt-40) is a great mod and its item information display is very useful. That said, I was never fully satisfied with its color coding implementation, and this mod is my own attempt at doing it better.

In ItemInfo's approach, items are colored by **rarity**, essentially, how hard they are to obtain. The problem is that acquisition difficulty and actual usefulness are two different things. For new players especially, this creates a confusing visual language where high-value colors don't reliably mean "this is what you want to use".

I also realized that most immediately recognizable items (weapons, medical supplies, gear...) don't really benefit from color coding at all. When you already know what everything is at a glance, adding colors to those items creates visual noise more than anything else.

So the approach ended up being fairly simple :

- **Most item categories get no color** : weapons, armor, meds, and other instantly readable items are left as default. Less clutter.
- **The tier system is small and intentional** : instead of 7+ color grades, there are only 6 tiers, focused almost exclusively on ammo and a handful of important categories like armor plates and keys.

The goal is a color system you can actually trust : if something is glowing red, it means it's genuinely the best option in its class.

---

🖤 **Default** — Unassigned or not worth highlighting.

💙 **Notable** — Decent option.

💜 **Superior** — Good, reliable choice.

💛 **Excellent** — High-end, strong performer.

❤️ **Elite** — Best-in-class. Hard capped at 1–3 per category.

🤎 **Situational** — Ammo only : useful in specific contexts (leg meta, etc...)

---

> 💡 This is a first iteration, and I'm far from a Tarkov meta expert : the default configuration almost certainly contains mistakes, wrong tier assignments, and approximations. I'll be improving it regularly.

> 💡 Unlike my other mods, this one works out of the box, no config required to get started. And since it only touches inventory background colors, it's completely safe to update or remove at any time without affecting your save.

> 💡 This mod does not require ColorConverterAPI, the native Tarkov background colors cover everything needed out of the box.

*Ammo stats sourced from [eft-ammo.com](https://www.eft-ammo.com/)*.

## 📸 Screenshots

> [![1](https://i.imgur.com/LzYeLW2.jpeg)](https://i.imgur.com/LzYeLW2.jpeg)

---

> [![2](https://i.imgur.com/2c9nQGC.jpeg)](https://i.imgur.com/2c9nQGC.jpeg)

## ⚙️ Configuration

All config files live in `config/`. The system runs 3 passes on every item at server startup, in order of increasing priority.

---

### `masterConfig.json`

Defines the color for each tier name and the global price thresholds used for automatic tiering.

```jsonc
{
  "Tiers": {
    "Default":     "default",
    "Situational": "orange",
    "Notable":     "blue",
    "Superior":    "violet",
    "Excellent":   "tracerYellow",
    "Elite":       "tracerRed"
  },
  "PriceThresholds": [
    { "MinPrice": 200000, "Tier": "Elite"     },
    { "MinPrice": 90000,  "Tier": "Excellent" },
    { "MinPrice": 60000,  "Tier": "Superior"  },
    { "MinPrice": 30000,  "Tier": "Notable"   }
  ]
}
```

---

### `categoryRules.json` — Pass 1

Maps item category IDs to tier names. Walks the DB parent chain, so assigning a category also covers all its sub-categories automatically. The full vanilla category hierarchy is included as comments for reference.

---

### `priceRules.json` — Pass 2

Lists category IDs whose tier should be assigned automatically based on handbook price, using the thresholds defined in `masterConfig.json`. Items below the lowest threshold fall back to `Default`. Uses the same DB category IDs as `categoryRules.json`.

```jsonc
{
  "Enabled": true,
  "Categories": [
    "5448eb774bdc2d0a728b4567",  // BarterItem
    "5c164d2286f774194c5e69fa"   // Keycard
  ]
}
```

---

### Override files - Pass 3

`ammo.json`, `armorplates.json`, and any other `*.json` in `config/` map individual item TPLs to tier names with absolute priority. Set `"Enabled": false` to disable a file without deleting it.

The per-TPL overrides are intentionally split across multiple files rather than lumped into one. This makes updates easier to manage : if you've built your own custom config, you can replace or ignore individual files without touching everything else. It also makes it easier to contribute to specific categories. If you know the ammo meta better than I do and want to submit a corrected `ammo.json`, you're welcome to contribute.

```jsonc
{
  "Enabled": true,
  "Overrides": {
    "5efb0da7a29a85116f6ea05f": "Elite",      // 9x19mm PBP gzh   pen:39
    "5c925fa22e221601da359b7b": "Elite",      // 9x19mm AP 6.3    pen:30
    "56d59d3ad2720bdb418b4577": "Superior"    // 9x19mm PST gzh   pen:20
  }
}
```

### Ammo boxes - Pass 4

Automatically inherits the color of the corresponding loose round. No configuration required.

## 🔌 Compatibility

### ODT-ItemInfo

Runs after ItemInfo, so both mods are fully compatible, we will simply overwrite whatever colors it assigned. That said, if you're using ItemInfo alongside this mod, it's cleaner to disable its color feature in its config to avoid doing the work twice.

---

### WTT - Content Backport

Items added by [WTT - Content Backport](https://forge.sp-tarkov.com/mod/2512/wtt-content-backport) are covered in the default config out of the box.

---

### Other mods

Any mod that adds new items is supported, category-based and price-based tiering will apply automatically as long as the items follow the vanilla parent chain. Items that require manual tier assignment will need to be added to the appropriate override file.

## 🐱

![description](https://static.boredpanda.com/blog/wp-content/uploads/2025/10/funny-cat-memes-go-hard-cover_675.jpg)
