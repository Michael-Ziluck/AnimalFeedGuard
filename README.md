# Animal Feed Guard

**2.0.2 targets Valheim 1.0**, built and checked against 1.0.16. Use the 1.x releases for Ashlands.

A standalone BepInEx mod for Valheim. Automatic pickup leaves dropped food alone when a living, tamed animal that can eat it is nearby. Manual pickup still works.

**Client-side only:** install on each player's client. Installing only on a dedicated server does not protect players' automatic pickup. BepInEx is required; Ranching and AutoPicker are not required.

**Release status:** Prepared for testing. Pickup diagnostics identified the reported Asksvin Smokepuff case as outside the former 5-metre radius; the default is now 25 metres. Automated checks cover all 48 food combinations extracted from installed 1.0.16 assets, actual pickup IL, and assembly references. Full live regression testing remains pending.

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

Import the test ZIP into a separate Gale profile, replacing the old AnimalFeedGuard DLL. Confirm an empty inventory slot is available when testing; otherwise failure to collect does not prove protection. Compare matching and nonmatching food inside/outside the radius, then repeat with the mod disabled and with AutoPicker enabled.

For the Asksvin test, drop Smokepuffs beside adult tamed Asksvin and run over them. Enable **Diagnostics → Log Pickup Decisions** in ConfigurationManager (or the config file) to log the protection decision, body/origin distances, and synchronized tame state in `BepInEx/LogOutput.log`. Turn logging off after the test. Logging is rate-limited per food and outcome and does not write to saves.

Automated coverage passed. The reported Smokepuff was 7.67 metres from the Asksvin's body, outside the former 5-metre radius. Full live regression testing remains pending. Manual pickup is deliberately allowed. Another player's unmodified client, or a mod that directly inserts items into an inventory, can still collect the feed.

## Settings

Generated file: `BepInEx/config/com.ziluck.valheim.animalfeedguard.cfg`.

| Setting | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Enable protection during automatic pickup. |
| Protection Radius | 25 | Metres from the dropped item to the animal's body, configurable from 0 to 50. Zero disables protection. |
| Log Pickup Decisions | false | Optional, rate-limited diagnostic log of protection decisions. |

Existing configs retain their saved radius when upgrading. Set `Protection Radius = 25` to adopt the new default.

Animals need not be hungry. Wild animals and animals still being tamed do not qualify. Food means anything in the animal's actual consume list, not just food players can eat: seeds, barley and modded feeds qualify when listed by the animal. Distance is measured in 3D from the item to the nearest point on the animal's body collider (falling back to its origin when no active collider is available), without a wall or line-of-sight check. One eligible animal is enough to protect the stack.

Only locally loaded animals can be detected. Settings are local to each player. Every player who should avoid automatically collecting feed needs the mod installed; a host installation does not protect against another player's unmodified client.

## AutoPicker compatibility

Inspected against Same-AutoPicker 1.1.3. Its radius adjustment continues to work. It harvests `Pickable` plants through `Interact`, then loose drops use Valheim's automatic pickup path. This mod filters that path before items move toward the player, without changing item flags or inventory behavior. AutoPicker may still harvest nearby plants; the resulting edible drops remain protected if they land inside the configured radius.

No AutoPicker dependency or copied AutoPicker code is bundled. Mods that directly insert items into inventories or replace the normal automatic pickup method may bypass this filter. An incompatible method layout generates an explicit error in the BepInEx log rather than silently claiming protection.

## Build and installation

```powershell
dotnet build -c Release -p:GamePath="E:\Games\SteamLibrary\steamapps\common\Valheim"
```

Output: `bin/Release/net48/AnimalFeedGuard.dll`. No installation occurs during build. Requires a .NET SDK, local Valheim and BepInEx 5 assemblies; .NET Framework 4.8 reference assemblies restore from NuGet. No publicized game assemblies are required.

When ready to test, exit Valheim and copy the DLL into your active mod profile's `BepInEx/plugins/AnimalFeedGuard` directory. In Gale, use the active profile directory rather than the Steam installation. It has no dependency on Ranching. Remove the DLL to uninstall; no save migration is needed because it writes no world or item data.

Run `./ci/Build.ps1` to compile, run checks, and produce a Thunderstore-ready ZIP. `./ci/Package.ps1` can also package an existing Release build. The package includes the required manifest, PNG icon and UTF-8 README at the ZIP root. The manifest links to the public GitHub repository. Builds do not install the mod.

## Verification

```powershell
# Normal verification; AutoPicker is not required or loaded.
dotnet run --project tests/checks -c Release -- "E:\Games\SteamLibrary\steamapps\common\Valheim"

# Optional extra compatibility check when AutoPicker is installed:
dotnet run --project tests/checks -c Release -- "E:\Games\SteamLibrary\steamapps\common\Valheim" "PATH_TO_AUTOPICKER_DLL"
```

The normal command checks radius boundaries, diet identity, and the installed game's pickup IL. When a second path is supplied, it additionally checks AutoPicker's pickup integration. Live gameplay still needs testing: drop matching and nonmatching feed inside and outside the radius, confirm manual pickup, repeat with AutoPicker enabled and a second client, and check fed versus hungry animals. Use a disposable test world before relying on protection in a shared world.

`./ci/Build.ps1` also runs the production filter and Harmony patch against simulated game objects. These checks cover live-state changes within a cached frame, synchronized tameness, collider selection, configuration changes, and diagnostic rate limiting. They do not run Unity physics or multiplayer.

## Credits

Code is MIT licensed. The icon depicts Valheim's carrot item; see `ATTRIBUTION.md` for the image source and separate artwork attribution.

The plugin ID is now com.ziluck.valheim.animalfeedguard. If you tried the earlier build, remove that old DLL before installing this one. To preserve settings, rename the old com.michaelziluck.valheim.animalfeedguard.cfg to the new filename while the game is closed.

If you'd like to support ongoing modding work, [Ko-fi](https://ko-fi.com/doczee) is available.

## Check out my other mods

- [HenEggPickup](https://thunderstore.io/c/valheim/p/DocZee/HenEggPickup/) - automatically collects chicken eggs once enough adult hens are nearby.
- [RanchingAddon](https://github.com/Michael-Ziluck/RanchingAddon) - adds skill-scaled chick and Asksvin hatchling growth, plus growth and egg incubation information to Ranching.

## Automated builds and releases

See [ci/README.md](ci/README.md) for GitHub Actions builds, versioned releases, and automatic publishing to Thunderstore and Hexium. Pull requests run build checks. Publishing jobs run from `main` when the corresponding repository variable is enabled.
