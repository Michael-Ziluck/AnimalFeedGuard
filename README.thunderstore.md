# Animal Feed Guard

**2.0.3 targets Valheim 1.0**, built and checked against 1.0.16. Use the 1.x releases for Ashlands.

Animal Feed Guard keeps dropped food on the ground when a living, tamed animal that can eat it is nearby. Manual pickup remains available.

## Features

- Configurable protection radius: 0-50 metres; default 25 metres.
- Uses each animal's live food list, including Asksvin Smokepuffs, Deep North feeds, seeds, barley, and modded feed.
- Measures distance to the animal's body, so a large animal's origin does not leave nearby feed unprotected.
- Reads synchronized tameness through ownership changes.
- Protects food even when the animal is already fed.
- Applies only to automatic pickup; manual pickup is unchanged.
- Client-side settings; install it on every player's client who wants protection.
- Compatible with AutoPicker's normal pickup path.

## Creature and food coverage

The audit read all 150 MonsterAI components in the installed 1.0.16 asset bundle. Fourteen have nonempty diets, totaling 48 creature/food combinations (including juvenile and summoned variants). Protection uses live consume lists, with no species or biome whitelist. See [the audit](https://github.com/Michael-Ziluck/AnimalFeedGuard/blob/75c0c1573911dfd6652903e98d6ea0886fdc1241/docs/feeding-audit.md).

| Creature | Feed found in installed assets |
| --- | --- |
| Asksvin | Smokepuffs, vineberries, fiddleheads |
| Asksvin hatchling | Cloudberries, barley, flax; only qualifies if tamed |
| Hen | Dandelions, barley, beech/birch/carrot/onion/turnip seeds |
| Boar and summoned boar | Carrots, turnips, onions, mushrooms, raspberries, blueberries |
| Wolf and summoned wolf | Neck tails, boar/lox/deer/chicken meat, sausages, raw fish |
| Lox | Cloudberries, barley, flax |
| Deep North moose and summoned moose | Lingonberries |
| Deep North bear variants | Blueberries; only qualifies if tamed (including the summoned bear) |

These are audited game defaults, not hardcoded lists. Modded additions to an animal's consume list are also recognized. Creatures without any feeding behavior do not protect arbitrary items.

## Testing and diagnostics

For regression tests, import the ZIP into a separate Gale profile, replacing the old AnimalFeedGuard DLL. Confirm an empty inventory slot is available when testing; otherwise failure to collect does not prove protection. Compare matching and nonmatching food inside/outside the radius, then repeat with the mod disabled and with AutoPicker enabled.

For the Asksvin test, drop Smokepuffs beside adult tamed Asksvin and run over them. Enable **Diagnostics → Log Pickup Decisions** in ConfigurationManager (or the config file) to log the protection decision, body/origin distances, and synchronized tame state in `BepInEx/LogOutput.log`. Turn logging off after the test. Logging is rate-limited per food and outcome and does not write to saves.

Automated coverage passed. The reported Smokepuff was 7.67 metres from the Asksvin's body, outside the former 5-metre radius. The maintainer has tested the current build and confirmed it works as intended. Manual pickup is deliberately allowed. Another player's unmodified client, or a mod that directly inserts items into an inventory, can still collect the feed.

## Configuration

The config file is `BepInEx/config/com.ziluck.valheim.animalfeedguard.cfg`.

`Enabled` controls the feature. `Protection Radius` controls the distance from the dropped item to the nearest point on an eligible tamed animal's body collider. This handles large animals such as Asksvin, lox, and moose. A radius of `0` disables protection.

The default is 25 metres. Existing configs retain their saved radius when upgrading; set `Protection Radius = 25` to adopt the new default.

## Recommended optional mods

- [ConfigurationManager](https://thunderstore.io/c/valheim/p/shudnal/ConfigurationManager/) - edit the settings in game instead of opening the config file. It is optional.
- [AutoPicker](https://thunderstore.io/c/valheim/p/Same/AutoPicker/) - optional compatibility; Animal Feed Guard does not require it.

## Links

- [Source, documentation, and issue tracker](https://github.com/Michael-Ziluck/AnimalFeedGuard)

## Check out my other mods

- [HenEggPickup](https://thunderstore.io/c/valheim/p/DocZee/HenEggPickup/) - automatically collects chicken eggs once enough adult hens are nearby.
- [RanchingAddon](https://github.com/Michael-Ziluck/RanchingAddon) - adds skill-scaled chick and Asksvin hatchling growth, plus growth and egg incubation information to Ranching.

If you'd like to support ongoing modding work, [Ko-fi](https://ko-fi.com/doczee) is available.

Requires [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/). Ranching and AutoPicker are optional.
