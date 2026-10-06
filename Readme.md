# [Trophy Percent](https://thunderstore.io/c/valheim/p/Combifightet/TrophyPercent/)

**Never again be uncertain when you will get the next trophy.**

> **WARNING:** \
> the "kills" remaining counter for a sspecific enemy is only 100% accurate **after**
> you have killed atleas one of that enemy **during your current game sesion**. \
> _(see [How the "Pity-Timer" works](#how-the-pity-timer-works))_

## Features

- **Live HUD Tracking:** Pin trophie goals to your HUD with checkboxes from the Compendium.
- **Missing Trophies Revealed:** Enemies you have killed, but not yet collected a trophy from will now appear ass greyed out in the Compendium.
- **Global Counter:** Tracks your overall trophie collection progress.

### How the "Pity-Timer" works

Valheim uses a pseudo-random "pity timer" rather than a flat percentage. When
you kill an enemy, the game silently rolls a random number of kills required
before a trophy drop is _100% guaranteed_.

- **Estimates vs. Exact Kills:** Valheim erases the pity timer every time you
  log out. When you first load into a world, the mod estimates your remaining
  kills based on the base drop chance. **Once you kill an enemy**, the game
  rolls a new live timer, and the mod displays the exact kills remaining.
<!-- - **Getting Lucky:** You can absolutely get a trophy _before_ the counter
  reaches zero! If you get lucky, the trophy drops and the game instantly rolls
  a brand new timer. -->

## Installation

**Mod Manager _(Recommended)_:** Click "Install with Mod Manager" using [r2modman] or [Thunderstore Mod Manager].

**Manual:** Extract `TrophyPercent.dll` into your `Valheim\BepInEx\plugins` folder
(requires [BepInEx 5.x](https://github.com/BepInEx/BepInEx) Valheim).

## Compatability

This is a **Client-Side** mod. You can play on vanilla dedicated servers or with
unmodded friends without any issues.
