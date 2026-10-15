# CLLC Extended

An add-on for [Creature Level & Loot Control](https://valheim.hexium.gg/mods/Smoothbrain/CreatureLevelAndLootControl)
(CLLC). CLLC keeps handling levels, stars and loot; CLLC Extended adds what it does not show and
new ways for creatures to fight.

> **Young project, use at your own risk.** This mod was written with an AI coding assistant
> (Claude), directed and play-tested by its author on a private server. It has not been battle-tested
> on many servers yet: expect bugs, back up your world, and report issues with your
> `BepInEx/LogOutput.log` on [GitHub](https://github.com/Elliog/CLLC-Extended/issues).

**Requires Creature Level & Loot Control 5.0.7** (installed automatically by mod managers).

## Features

### Creature inspection
- **Crouch** while aiming at a creature (up to 50 m) to see, under its nameplate: damage taken,
  immunities, resistances, weaknesses and modifiers. CLLC's infusions and effects are included
  (e.g. a Fire-Infused creature resists fire and is weak to frost).
- Press **`I`** for the full explanation of every modifier in your own chat.

### Stars
- **Star colours**: from 3 stars, each star count gives the creature its own body colour (the game
  only has 1 and 2-star variants).
- **Star counter**: above 5 stars, the nameplate shows one star and the number instead of rows.

### 24 combat modifiers
A creature gets at most one modifier per group (Offense, Defense, Affliction, Special). They show
as icons after the stars and as words in its name ("Vampiric Curious Fire-Infused Troll").

| Group | Modifiers |
|---|---|
| Offense | Furious, Armor Piercing, Staggering, Undodgeable |
| Defense | Deathward, Reflection, Vortex, Adaptive, Unflinching, Chameleon |
| Affliction | Exposing, Weakening, Withering, Crippling, Disruptive, Adrenaline Drain, Corrosive, Toxic Death |
| Special | Vampiric, Reaping, Blink, Omen, Juggernaut, Blamer |

Afflictions put a 5 s status effect on the player hit (with its icon in your HUD). Omen and Blamer
are adapted from CreatureManager without its Karma system: Omen spawns a stronger avenger when the
creature is killed, Blamer flees at low health and alerts creatures around it.

## Installation

- Install on the **server and every player**, with CLLC. Modifiers are rolled and applied by
  whichever game owns a creature, so every player needs the mod.
- **Versions are checked like CLLC does** (ServerSync): the server refuses a player who does not
  have the mod or has another version, and says why.
- **Settings come from the server**: while "Lock Configuration" is on, gameplay settings (sections
  4 to 6) use the server's values and only admins change them. Display settings, language and keys
  stay personal.

## Compatibility

| Mod | Status |
|---|---|
| Creature Level & Loot Control 5.0.7 | Required. Its effects, infusions and boss affixes are shown in the inspection; its star rows are kept up to the configured count. |
| WackyEpicMMOSystem | Compatible. Its creature level label overlaps the stars: hidden by default ("Hide EpicMMO creature level"). Levels and XP are unchanged. |
| BetrValheim | Compatible. Turn off its own star counter ("9 - HUD (local)" > "Enemy stars"), or both show. |
| Ultimate Server Configuration Manager | Recommended: admins edit every setting in game. |
| CreatureManager, StarLevelSystem | Not usable together: both declare themselves incompatible with CLLC. CLLC Extended brings their modifiers and star colours on top of CLLC instead. |

## Configuration

`BepInEx/config/Elliog.CLLC_Extended.cfg`:

| Section | Settings |
|---|---|
| 1 - General | Lock Configuration (server settings apply to everyone), language (Auto, English, French) |
| 2 - Inspection | Crouch inspection, inspect key |
| 3 - Display | Star colours, stars shown before the counter, modifier names in the creature name, icon size, background shape and colour, hide EpicMMO level |
| 4 - Modifiers | Enabled, max modifiers per creature, minimum stars |
| 5 - Modifier chances | Spawn chance of each modifier within its group (0 disables it) |
| 6 - Modifier values | Strength of every modifier: damage bonuses, durations, cooldowns, radii, caps |

## Commands (with `devcommands`)

- `cllcx_modifier <Name> [Name...]` sets the modifiers of the creature you aim at (`None` clears them).
- `cllcx_hud` writes the aimed creature's nameplate structure to the log, to debug display conflicts.

## Credits and licence

- [CreatureManager](https://github.com/sighsorry1029/CreatureManager) by sighsorry (GPL-3.0): the
  modifiers, their rules, default values and icons.
- [StarLevelSystem](https://github.com/MidnightsFX/Valheim_Star_Levels_Expanded) by MidnightsFX
  (GPL-3.0): the per-star colours.
- Creature Level & Loot Control by Smoothbrain: the base this add-on extends. CLLC Extended only
  uses CLLC's public API and is not affiliated with Smoothbrain.

- [ServerSync](https://github.com/blaxxun-boop/ServerSync) by blaxxun (MIT-0): version check and
  server-enforced settings, as in CLLC.

CLLC Extended is licensed under the GPL-3.0. Source code: https://github.com/Elliog/CLLC-Extended
