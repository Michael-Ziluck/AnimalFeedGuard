# Animal Feed Guard

Animal Feed Guard keeps dropped food on the ground when a living, tamed animal that can eat it is nearby. Manual pickup remains available.

## Features

- Configurable protection radius: 0–50 metres; default 5 metres.
- Uses each animal's actual food list, including seeds, barley, and modded feed.
- Protects food even when the animal is already fed.
- Applies only to automatic pickup; manual pickup is unchanged.
- Client-side settings; install it on every player's client who wants protection.
- Compatible with AutoPicker's normal pickup path.

## Configuration

The config file is `BepInEx/config/com.ziluck.valheim.animalfeedguard.cfg`.

`Enabled` controls the feature. `Protection Radius` controls the distance from the dropped item to an eligible tamed animal. A radius of `0` disables protection.

## Recommended optional mods

- [Official BepInEx ConfigurationManager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/) — edit the settings in game instead of opening the config file.
- [AutoPicker](https://thunderstore.io/c/valheim/p/Same/AutoPicker/) — optional compatibility; Animal Feed Guard does not require it.

## Links

- [Source, documentation, and issue tracker](https://github.com/Michael-Ziluck/AnimalFeedGuard)

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/). Ranching and AutoPicker are optional.
