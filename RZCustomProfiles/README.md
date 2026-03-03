Profile template manager that lets you define custom profile editions that appear in the SPT launcher at character creation, each with their own starting items, hideout levels, trader standings, skills, and more, all driven by simple config files.

This is part of my personal suite of SPT mods that I use to run my own server.

All in all a pretty boring mod, but the basics deserve a clean foundation, so here it is.

> **Note:** As of this update, all my mods share a common library called **RZShared**, bundled directly in each mod's zip. It's still too small to justify its own page on the forge, but I'm too obsessive about code hygiene to keep copy-pasting utilities across five projects.

---

## ⚙️ How it works

The mod runs at server startup and registers your custom profiles into SPT's profile template system. When a player creates a new character in the launcher, your editions appear alongside (or instead of) the vanilla SPT ones.

Each profile is defined by a separate `.json` file dropped in the `profiles/` folder. All files are loaded automatically — no registration needed, just drop and go.

Vanilla SPT profiles (Standard, Edge of Darkness, etc.) can be individually re-enabled via `masterConfig.json`. All are hidden by default.

---

## 🔧 Configuration

### masterConfig.json

Controls global settings and which vanilla SPT profiles remain visible in the launcher.
```jsonc
{
  // 0 = Standard        1 = Left Behind       2 = Prepare To Escape   3 = Edge Of Darkness
  // 4 = Unheard         5 = Tournament        6 = SPT Developer       7 = SPT Easy start
  // 8 = SPT Zero to hero
  "EnabledBaseProfiles": [
    6,
    //7,
    //8,
  ],

  // Unlocks all Ragman outfits for every profile on server start.
  // Don't quote me on this, but SVM doesn't seem to expose an option to have outfits UNLOCKED by default.
  // Applies globally regardless of which profile is used.
  "UnlockAllOutfits": false,
}
```

---

### Profile files

Each `.json` file in the `profiles/` folder defines one profile edition. Files are loaded in alphabetical order.
```jsonc
{
  // ─────────────────────────────────────────────────────────────────────
  // Base profile to clone
  //
  // 0 = Standard        1 = Left Behind       2 = Prepare To Escape   3 = Edge Of Darkness
  // 4 = Unheard         5 = Tournament        6 = SPT Developer       7 = SPT Easy start
  // 8 = SPT Zero to hero
  // ─────────────────────────────────────────────────────────────────────
  "BaseProfile": 0,

  // ─────────────────────────────────────────────────────────────────────
  // Profile identity
  // ─────────────────────────────────────────────────────────────────────
  "Name": "MyCustomProfile",
  "Description": "MyCustomDescription",

  // ─────────────────────────────────────────────────────────────────────
  // Progression
  // ─────────────────────────────────────────────────────────────────────

  // Start at max level (level 79)
  "MaxLevel": false,

  // Max out all skills (5100 progress each)
  "MaxSkills": false,

  // ─────────────────────────────────────────────────────────────────────
  // Inventory
  // ─────────────────────────────────────────────────────────────────────

  // Clear stash items from the base profile before injecting ours
  "ClearStash": false,

  // Clear equipped items from the base profile (pouch, pockets and dogtag are always kept)
  "ClearEquipment": false,

  // -1 = Remove, 0 = Default, 1 = Waist Pouch, 2 = Alpha, 3 = Beta,
  //  4 = Epsilon, 5 = Gamma, 6 = Theta, 7 = Kappa, 8 = Kappa Desecrated
  "SecureContainer": 0,

  // 0 = Default, 1 = 1x3, 2 = 1x4, 3 = 1x4 Special, 4 = 1x4 TUE, 5 = 2x3, 6 = Large
  "Pockets": 0,

  // Starting items injected into the stash
  "StartingItems": {
    "Enabled": true,
    "Items": [
      { "Tpl": "5449016a4bdc2d6f028b456f", "Count": 10000000 } // Roubles
    ]
  },

  // ─────────────────────────────────────────────────────────────────────
  // Traders
  // ─────────────────────────────────────────────────────────────────────

  "TradersLoyalty": {
    "54cb50c76803fa8b248b4571": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Prapor
    "54cb57776803fa99248b456e": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Therapist
    "579dc571d53a0658a154fbec": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Fence
    "58330581ace78e27b8b10cee": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Skier
    "5935c25fb3acc3127c3d8cd9": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Peacekeeper
    "5a7c2eca46aef81a7ca2145d": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Mechanic
    "5ac3b934156ae10c4430e83c": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Ragman
    "5c0647fdd443bc2504c2d371": { "Standing": 0.0, "SalesSum": 0, "Unlocked": false }, // Jaeger
    "638f541a29ffd1183d187f57": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // Lighthousekeeper
    "656f0f98d80a697f855d34b1": { "Standing": 0.0, "SalesSum": 0, "Unlocked": true }, // BTR
    "6617beeaa9cfa777ca915b7c": { "Standing": 0.0, "SalesSum": 0, "Unlocked": false }  // Ref
  },

  // ─────────────────────────────────────────────────────────────────────
  // Hideout
  // ─────────────────────────────────────────────────────────────────────

  "HideoutStartingLevels": {
    "Generator": 0,
    "Vents": 0,
    "Security": 0,
    "Lavatory": 0,
    "Heating": 0,
    "WaterCollector": 0,
    "MedStation": 0,
    "Kitchen": 0,
    "RestSpace": 0,
    "Workbench": 0,
    "IntelligenceCenter": 0,
    "ShootingRange": 0,
    "Illumination": 0,
    "PlaceOfFame": 0,
    "BitcoinFarm": 0,
    "EmergencyWall": 0,
    "WeaponStand": 0,
    "WeaponStandSecondary": 0,
    "EquipmentPresetsStand": 0,
    "CircleOfCultists": 0
  }
}
```

> 💡 `HideoutStartingLevels` entries are optional — omit any area you don't want to override.

> 💡 `TradersLoyalty` supports modded traders — just use their MongoDB ID as the key.

---

## 🔌 Compatibility

- Safe to use alongside other mods since it only runs at server startup during profile template registration.
- **Fika** — Should work if installed server side, but this hasn't been tested yet. Please let me know.
