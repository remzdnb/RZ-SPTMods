[![GameIcons](https://i.imgur.com/0rtUrnW.png)](https://i.imgur.com/0rtUrnW.png)

# RZCustomTrader {.tabset}

## 🔎 Overview

> Out of the box, this mod does one thing and one thing only : replace all vanilla trader portraits, names and descriptions with custom ones. Trades, quests and behavior are completely untouched. It's fully non-destructive, the mod can be removed or updated at any point.
> This was intentional : the cosmetic overhaul is the most immediately fun part, and it should be accessible to everyone without having to touch config files.
That said, the mod also exposes a full set of configuration options that let you modify trader parameters across the board for any trader, vanilla or custom.

> ❗ **IMPORTANT** ❗ After installing the mod, or making any change to trader portraits, press **Clean Temp Files** in the SPT launcher settings before restarting the game. Without this step, portraits won't be updated.
[![CleanTempFiles](https://i.imgur.com/BvkM0VB.png)](https://i.imgur.com/BvkM0VB.png)

> 💡 Sable, a custom trader, is also included but disabled by default. She ships with no trades out of the box, see the Custom Trader section for setup.

> 💡 This mod contains no quests.

---

---

---

Even if you're not comfortable with config files, there are a few minimal changes you can make. All of the mod's config is grouped in a single file : `masterConfig.json`.

---

### 🌐 Locale Overrides

Trader names and descriptions are overridden across all languages : there's no per-language support for now, I'll look into shipping default translations at some point, but that's for later.

If you want to disable renaming entirely, find this line in `masterConfig.json` : *"EnableLocaleOverrides": true* and set it to *false*.

---

### 🖼️ Avatar Overrides

Trader portraits are replaced for all traders out of the box. If you want to keep a specific vanilla portrait, or swap in your own image, find this line in *masterConfig.json* : *"EnableAvatarOverrides": true*. Set it to *false* to disable all portrait replacements at once, or edit the *AvatarOverrides* entries individually to point to your own images.

> 💡 All images are located in the `db/` folder. Each image has a variant suffix (`_01`, `_02`...) : multiple variants are planned per trader so you can pick your favorite.

> 💡 Fields left empty or omitted are ignored, only what you explicitly set will be overridden.

---

> **Fence**

[![Fence](https://i.imgur.com/siQN3sL.jpeg)](https://i.imgur.com/siQN3sL.jpeg)

---

> **Jaeger**

[![Jaeger](https://i.imgur.com/tqLs4sQ.jpeg)](https://i.imgur.com/tqLs4sQ.jpeg)

---

> **Sable**

[![Sable](https://i.imgur.com/rD7hSeA.jpeg)](https://i.imgur.com/rD7hSeA.jpeg)

---

> **Ref ➡️ Fixer**

[![Ref](https://i.imgur.com/XhRuoO6.jpeg)](https://i.imgur.com/XhRuoO6.jpeg)

---

> **Peacekeeper**

[![Peacekeeper](https://i.imgur.com/gcwSBzL.jpeg)](https://i.imgur.com/gcwSBzL.jpeg)

---

> **Ragman ➡️ Goldman**

[![Ragman](https://i.imgur.com/6oRTCx2.jpeg)](https://i.imgur.com/6oRTCx2.jpeg)

---

> **Mechanic**

[![Mechanic](https://i.imgur.com/WRyEHxw.jpeg)](https://i.imgur.com/WRyEHxw.jpeg)

---

> **Prapor ➡️ Sidorovich**

[![Prapor](https://i.imgur.com/SgfYBwR.jpeg)](https://i.imgur.com/SgfYBwR.jpeg)

---

> **Therapist ➡️ Mira**

[![Therapist](https://i.imgur.com/BsgT2V9.jpeg)](https://i.imgur.com/BsgT2V9.jpeg)

---

> **Skier ➡️ Skif**

[![Skier](https://i.imgur.com/Sx1Riy0.jpeg)](https://i.imgur.com/Sx1Riy0.jpeg)

## 🧑‍🤝‍🧑 Vanilla Traders

> Beyond the cosmetic overhaul, the mod also lets you override core trader parameters for any vanilla trader.

### ⚙️ Configuration : `masterConfig.json`

**`EnableLoyaltyLevels`** — Lets you redefine the requirements and coefficients for each level : player level, standing, sales sum, buy/repair/insurance price coefficients.

**`EnableRepairOverrides`** — Only fields you explicitly set are applied : availability, quality, currency, price rate, excluded items and categories.

> ⚠️ This haven't been extensively tested yet; I put this together fairly quickly. I'll look into it more carefully in a future update, but use it at your own risk for now. That said, the default values shipped with the mod are automatically dumped from the game, so enabling these features without changing anything shouldn't affect your game at all.

## 🔧 Custom Trader

**Sable** is disabled by default. She ships with no trades out of the box, there's no point having her show up in the menu if she has nothing to sell.

She has no default trades because my CustomEconomy mod already handles all the logic needed to inject custom trades into any trader, so there's no point duplicating that logic across mods. Separation of concerns. It's a big mod with a lot of features, but everything is disabled by default and it won't touch your game unless you explicitly ask it to. Its mod description covers everything, but I'll also walk through a practical setup here, it's a good excuse to show some of its features in action.

Setting her up takes about five minutes. Here's how.

---

### 1. Enable the trader

Go to **CustomTraders** mod folder / config / `masterConfig.json`, find :
```json
"EnableCustomTrader": false
```
and set it to `true`. Sable should now show up in your game.

---

### 2. Install CustomEconomy

[CustomEconomy](https://forge.sp-tarkov.com/mod/2604/rzcustomeconomy) (v1.2.0 and onwards). Download and install it, then open its `config/` folder.

---

### 3. Enable Routed Trades

Go to **CustomEconomy** mod folder / config / `masterConfig.json` and set :
```json
"EnableRoutedTrades": true
```

*Routed trades automatically assign items to traders based on their category. Every item in a category you assign to a trader will show up in their stock, priced at handbook value. That's all there is to it.*

---

### 4. Assign categories to Sable

Open `routedTradesConfig.json` and navigate to `CategoryRoutes` / `Sable`. Every in-game category is already listed, just toggle the ones you want her to sell to `true`. By default she only sells Keys.

---

### 5. Set her currency *(optional)*

At the top of `routedTradesConfig.json`, find `TraderCurrencies` and replace Sable's entry with `"eur"` or `"usd"`.

---

### 6. Blacklist specific items *(optional)*

If a category you enabled contains items you don't want Sable to sell, you can exclude them individually. Scroll to the bottom of `routedTradesConfig.json` and add their TPLs to Sable's blacklist.

---

### 7. Manual trades *(optional)*

For more specific offers (custom prices, barters, durability, attachments, stock size...), CustomEconomy also supports manual trades. The full documentation is in the mod description. If anything isn't clear, feel free to ask.

> ⚠️ If you're reading this, you're an early tester. Some things might not work as expected. Don't hesitate to report bugs or ask questions directly. That said, CustomEconomy is safe to use, and if anything breaks, simply removing or reinstalling it will restore everything back to defaults.

## 🖼️ About the Pictures

These images were generated locally, using a ComfyUI + SDXL workflow. Every image was upscaled and went through multiple refinement passes (face/eyes), then through Photoshop before being integrated (artifact cleanup, cropping, filters...).

*What that does not mean:*
- *that I'm an artist (matter of fact I'm everything but an artist)*
- *that I'm a better individual than people using ChatGPT or any other web service*

*What it does mean:*
- *that I spent a fuckton of time getting to this result*
- *that I enjoyed the process*
- *that getting consistent yet varied results out of AI is harder than it looks*

---

In the `extras` folder of the mod you'll find all the uncropped images, right before the Photoshop pass. Public domain.

And by the way, you can zoom in as much as you want on these pictures, it holds up. Completely useless since the in-game portraits are 128x128 at best lol, but if I was going to do it I wanted them in HD anyway.

{.endtabset}
