A BepInEx mod for MageQuit that tracks and displays in-game player statistics.

Thank you to the developers for letting people mod this game, check out their discord if you have a chance https://discord.gg/jS5Rsvtp

DO NOT USE THESE MODS FOR ONLINE PLAY WITH RANDOMS. IT CAN CAUSE SOME MAJOR ISSUES.


## How to Install

1. Install [BepinEx 5](https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.4) into your MageQuit directory.
2. Run and then open MageQuit to generate BepInEx files.
3. Download the latest release here.
4. Extract the contents of the downloaded zip into your MageQuit directory. (e.g. `MageQuit/BepInEx/plugins/PlayerStats.dll`)
5. Download [MageQuitModFramework](https://github.com/OneAutumnMango/MageQuitModFramework) and place it next to the dll.
6. Launch the game.
7. Press F5 to open the mod menu and configure PlayerStats.

BepInEx mirror: https://www.nexusmods.com/magequit/mods/1 <br>
MageQuitModFramework mirror: https://www.nexusmods.com/magequit/mods/3 <br>
PlayerStats mirror: https://www.nexusmods.com/magequit/mods/6


## Features

### Stats Overlay

An in-game overlay showing live statistics for every player in the session.

| Column  | Description                                  |
| ------- | -------------------------------------------- |
| Player  | Player name                                  |
| Kills   | Total kills across all rounds                |
| Deaths  | Total deaths across all rounds               |
| Damage  | Total damage dealt across all rounds         |
| Healing | Total healing applied across all rounds      |

Stats accumulate across rounds and reset at the start of a new game session (round 1).

#### Spell Breakdown

Each player entry can be expanded with a per-spell breakdown:

| Column   | Description                                                  |
| -------- | ------------------------------------------------------------ |
| Spell    | Spell name                                                   |
| Dmg      | Total damage dealt with this spell                           |
| %        | Percentage of the player's total damage from this spell      |
| Hit/Cast | Hits landed out of total casts (e.g. `5/8`)                  |
| Rate     | Hit rate percentage (e.g. `62%`)                             |

Spells that deal damage over time (DoTs) or have no meaningful aim component show `--` for hit tracking columns.

#### Tab Mode

When **Use Tab to Show** is enabled in the mod menu, the overlay is hidden by default and only appears while **Tab is held**. Useful if you only want to check stats occasionally without the overlay cluttering the screen.


## Mod Menu Options

Open the mod menu with **F5**.

| Option             | Description                                      |
| ------------------ | ------------------------------------------------ |
| Stats Overlay: ON/OFF   | Toggle the stats overlay on or off         |
| Use Tab to Show: ON/OFF | When ON, overlay only shows while Tab held |


## Notes

- Spells like SevenTears and PetRock that can hit the same target multiple times from one cast count as a single hit per target per cast.
- Additional/recast activations (e.g. StealTrap pull, NorthPull second cast) do not count as extra spell casts.
- Some spells (DoTs and a few others) are excluded from hit tracking and show `--` due to difficulties in reliably detecting individual hits.
- Spell stats reset at round 1 of each new game session.
