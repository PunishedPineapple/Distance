# Distance

## Purpose
**WARNING: BETA PLUGIN**

This is a plugin for [XIVLauncher/Dalamud](https://github.com/goatcorp/FFXIVQuickLauncher) that shows basic distance information in a movable game UI element.

## Usage
This plugin works automatically, showing the distance to the target (or the target's hitring).  It can also show distance until aggroing the boss (for supported bosses).

![Screenshot](Images/image1.png)

## Contributing
If you would like to contribute boss aggro range data, please create an issue with the instance/zone name (remember to note normal, savage, extreme, etc.!), boss name, and the distance (preferably to two decimal points).  Try to measure aggro distances from both the center of the boss *and* the hitring; I'm now reasonably sure that the game uses distance from the hitring from the numbers I've gathered so far, but I'm trying to keep a database of both for the time being.  Data will *probably not* be added for cases like Bozjan Southern Front and Zadnor where enemies can have the same name and BattleNpc data across different skirmishes, CEs, and duels.

## License
Code and executable are covered under the [MIT License](../LICENSE).  Final Fantasy XIV (and any associated data used by this plugin) is owned by and copyright Square Enix.

Icon modified from https://thenounproject.com/icon/measure-662431/ by Creaticca.

Text node creation code stolen from [SimpleTweaks](https://github.com/Caraxi/SimpleTweaksPlugin).
