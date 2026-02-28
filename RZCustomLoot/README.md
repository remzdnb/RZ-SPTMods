# RZCustomLoot — Forced Pocket Loot

Deterministic, guaranteed pocket loot for any bot type.

---

## 🎲 Why not just edit the loot pool?

SPT's native loot system assigns weights to items in a pool and rolls randomly when generating a bot. The higher the weight, the more likely an item is to appear, but it's never guaranteed.

This mod sidesteps that entirely by injecting items directly into bot pockets after generation, so you get exactly what you configured every time.

---

## ⚙️ How it works

The mod runs after the bot is fully generated and injects items in two passes:

1. **Free slots first** — empty pocket slots are filled first.
2. **Overwrite SPT items** — if all slots are full, SPT-generated items are replaced.

This guarantees your configured items always end up in the pockets, regardless of what SPT put there.

> ⚠️ **First iteration** : currently covers pocket injection only. Backpack and vest support may come later, but it's not a priority right now. Current features are already enough for my needs.

> ⚠️ Only **1x1 items** are supported. There are no safeguards but anything larger will most likely cause issues.

> ⚠️ Item stacking is not handled yet. Each entry injects a single item, so this is really designed for unique high-value drops like bitcoins or lega medals, not stackable quantities.

---

## 🔧 Configuration

### Bot categories

- `Bosses` — All bosses, global config or per-boss
- `Followers` — All followers
- `Pmc` — USEC and BEAR PMCs
- `Scav` — Regular scavs

### Roles

Bot roles are fully configurable directly in the config file via `BossRoles` and `FollowerRoles`. Any role listed in `BossRoles` gets boss loot, any role in `FollowerRoles` gets follower loot. The Goon followers (Big Pipe, Birdeye) are in `BossRoles` by default.

### Boss config

Bosses support two modes controlled by `UseGlobalConfig`:

- **`true`** — one config applies to all bosses
- **`false`** — each boss has its own pocket loot list via `PerBoss`

If `UseGlobalConfig` is false and a boss has no `PerBoss` entry, it falls back to `Global` automatically.

### Per-item config
```jsonc
{ "Tpl": "59faff1d86f7746c51718c9c", "Chance": 100 } // Bitcoin
```

- `Chance` — 0 to 100. 100 = guaranteed, 0 = never drops.

Items are processed **in order, top to bottom**. A failed chance roll does **not** consume a slot — the next item in the list gets a chance to fill it. Put your highest priority items first.

### Overwrite blacklist

Items protected from being overwritten when the mod needs to make room. Two ways to blacklist:

- **`Tpls`** — Exact TPL match.
- **`Categories`** — Any item whose parent chain contains the category ID is protected.

---

## 🔀 How the sequential logic works

Say you have 4 pocket slots and configure 3 items, with SPT already filling 3 of them:
```jsonc
"Pockets": [
  { "Tpl": "Bitcoin",    "Chance": 100 },
  { "Tpl": "GP Coin",    "Chance": 50  },
  { "Tpl": "Lega Medal", "Chance": 30  }
]
```

1. **Bitcoin** — rolls 100%, passes. One free slot available → injected. ✓
2. **GP Coin** — rolls 50%, fails. Slot is **not consumed** → next item gets a shot.
3. **Lega Medal** — rolls 30%, passes. No free slots left → overwrites an SPT item. ✓

A failed roll never wastes a slot. Only a successful roll claims one.

---

## 📋 Example config

```jsonc
{
  "Enabled": true,

  "BossRoles": [ "bossbully", "bosskilla", "followerbigpipe" ],
  "FollowerRoles": [ "followerbully", "followergluharscout" ],

  "Pmc": {
    "Enabled": true,
    "Pockets": [
      { "Tpl": "59faff1d86f7746c51718c9c", "Chance": 100 } // Bitcoin
    ]
  },

  "Bosses": {
    "Enabled": true,
    "UseGlobalConfig": true,
    "Global": {
      "Pockets": [
        { "Tpl": "59faff1d86f7746c51718c9c", "Chance": 100 }, // Bitcoin
        { "Tpl": "6656560053eaaa7a23349c86", "Chance": 50  }  // Lega Medal
      ]
    }
  },

  "Followers": {
    "Enabled": true,
    "Pockets": [
      { "Tpl": "59faff1d86f7746c51718c9c", "Chance": 50 } // Bitcoin
    ]
  },

  "Scav": {
    "Enabled": false,
    "Pockets": []
  },

  "OverwriteBlacklist": {
    "Tpls": [ "5c0e874186f7745dc7616606" ], // LEDX
    "Categories": [ "543be5e94bdc2df1348b4568" ] // Keys
  }
}
```

---

## 🔌 Compatibility

- Safe to use alongside other loot mod runs after all loot generation is complete.
- **Fika** — Should work since bots are generated server-side, but this hasn't been tested yet

---

> Final note : This was genuinely painful to figure out, and I came close to scrapping it more than once. It seems to work, but please don't hate me if something breaks.
