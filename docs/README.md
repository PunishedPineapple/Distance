# Distance

## Purpose

This is a plugin for [XIVLauncher/Dalamud](https://github.com/goatcorp/FFXIVQuickLauncher) that shows basic distance information in a movable game UI element.

## Usage
This plugin comes with the following defaults:
- A distance widget on the target bar, showing the distance to the target's hitring.
- A widget to show distance until aggroing the boss (for supported bosses).
- Distances on player and combatant nameplates.

You can add other widgets, change settings for distances shown, nameplates, etc. in the plugin settings window (`/pdistance config`).

## Contributing
If you would like to contribute boss aggro range data, please create an issue with the following information:
- Instance/zone name (remember to note normal, savage, extreme, etc.!)
- Boss name
- BNpc ID (can be found in the `/pdistance debug` window)
- TerritoryType (also in debug), and the distance.

Be sure to measure the distance to the hitring, not the center, and measure to at least one decimal point.  Ensure that you do not have any distance offset in the plugin settings.

Data will *probably not* be added for cases like Bozjan Southern Front and Zadnor where enemies can have the same name and BattleNpc data across different skirmishes, CEs, and duels.

## License
Code and executable are covered under the [MIT License](../LICENSE).  Final Fantasy XIV (and any associated data used by this plugin) is owned by and copyright Square Enix.

Icon modified from https://thenounproject.com/icon/measure-662431/ by Creaticca.

Text node creation code stolen from [SimpleTweaks](https://github.com/Caraxi/SimpleTweaksPlugin).
