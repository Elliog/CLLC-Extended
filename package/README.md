# CLLC Extended

An add-on for Creature Level & Loot Control: creatures get combat modifiers, stars get colours and a
counter, and you can inspect any creature's resistances and weaknesses. Install it with CLLC on the
server and on every player.

> Young project written with AI: use at your own risk, back up your world, and report bugs in the
> Issues tab of this page.

## Features

- **Creature inspection**: crouch while aiming at a creature to see its resistances, weaknesses,
  immunities and modifiers (CLLC's effects and infusions included). Press `I` for the full
  explanation in your chat.
- **24 combat modifiers** in four groups, at most one per group, shown as icons after the stars and
  as words in the creature name ("Vampiric Curious Fire-Infused Troll"):
  - Offense: Furious, Armor Piercing, Staggering, Undodgeable
  - Defense: Deathward, Reflection, Vortex, Adaptive, Unflinching, Chameleon
  - Affliction: Exposing, Weakening, Withering, Crippling, Disruptive, Adrenaline Drain, Corrosive, Toxic Death
  - Special: Vampiric, Reaping, Blink, Omen, Juggernaut, Blamer
- **Star colours**: from 3 stars, each star count gives the creature its own colour.
- **Star counter**: above 5 stars, one star and the number instead of rows of stars.
- **English and French**, following the game language.
- **Server-enforced**: players need the same version as the server, and gameplay settings come
  from the server.

## Configuration

`BepInEx/config/Elliog.CLLC_Extended.cfg`, editable in game with Ultimate Server Configuration Manager.

- **1 - General**: lock the server's settings, language.
- **2 - Inspection**: crouch inspection, inspect key.
- **3 - Display**: star colours, stars shown before the counter, icon size and background, hide
  WackyEpicMMOSystem's creature level label.
- **4 - Modifiers**: on/off, max modifiers per creature, minimum stars.
- **5 - Modifier chances**: chance of each modifier (0 disables it).
- **6 - Modifier values**: strength of each modifier (bonuses, durations, cooldowns, radii).

With BetrValheim, turn off its own star counter ("9 - HUD (local)" > "Enemy stars").

## Credits

- Smoothbrain for Creature Level & Loot Control, which this add-on builds on.
- sighsorry for [CreatureManager](https://github.com/sighsorry1029/CreatureManager): the modifiers and their icons.
- MidnightsFX for [StarLevelSystem](https://github.com/MidnightsFX/Valheim_Star_Levels_Expanded): the star colours.
- blaxxun for [ServerSync](https://github.com/blaxxun-boop/ServerSync).

GPL-3.0, source on [GitHub](https://github.com/Elliog/CLLC-Extended).
