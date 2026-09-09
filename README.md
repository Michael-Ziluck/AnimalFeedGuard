# Animal Feed Guard

A standalone BepInEx mod for Valheim. Automatic pickup leaves dropped food alone when a living, tamed animal that can eat it is nearby. Manual pickup still works.

**Client-side only:** install on each player's client. Installing only on a dedicated server does not protect players' automatic pickup. BepInEx is required; Ranching and AutoPicker are not required.

**Release status:** compiled successfully and passed 20 automated feed-rule and installed-code compatibility checks. Live gameplay testing is still pending.

## Settings

Generated file: `BepInEx/config/com.ziluck.valheim.animalfeedguard.cfg`.

| Setting | Default | Meaning |
| --- | --- | --- |
| Enabled | true | Enable protection during automatic pickup. |
| Protection Radius | 5 | Metres from the dropped item to the animal, configurable from 0 to 50. Zero disables protection. |

Animals need not be hungry. Wild animals and animals still being tamed do not qualify. Food means anything in the animal's actual consume list, not just food players can eat: seeds, barley and modded feeds qualify when listed by the animal. Distance is measured in 3D, including height, without a wall or line-of-sight check. One eligible animal is enough to protect the stack.

Only locally loaded animals can be detected. Settings are local to each player. Every player who should avoid automatically collecting feed needs the mod installed; a host installation does not protect against another player's unmodified client.

## AutoPicker compatibility

Inspected against Same-AutoPicker 1.1.3. Its radius adjustment continues to work. It harvests `Pickable` plants through `Interact`, then loose drops use Valheim's automatic pickup path. This mod filters that path before items move toward the player, without changing item flags or inventory behavior. AutoPicker may still harvest nearby plants; the resulting edible drops remain protected if they land inside the configured radius.

No AutoPicker dependency or copied AutoPicker code is bundled. Mods that directly insert items into inventories or replace the normal automatic pickup method may bypass this filter. An incompatible method layout generates an explicit error in the BepInEx log rather than silently claiming protection.

## Build and installation

```powershell
dotnet build -c Release -p:GamePath="E:\Games\SteamLibrary\steamapps\common\Valheim"
```

Output: `bin/Release/net48/AnimalFeedGuard.dll`. No installation occurs during build. Requires a .NET SDK, local Valheim and BepInEx 5 assemblies; .NET Framework 4.8 reference assemblies restore from NuGet. No publicized game assemblies are required.

When ready to test, exit Valheim and copy the DLL into your active mod profile's `BepInEx/plugins/AnimalFeedGuard` directory. In r2modman use the profile directory rather than the Steam installation. It has no dependency on Ranching. Remove the DLL to uninstall; no save migration is needed because it writes no world or item data.

Run `./Build.ps1` to compile, run checks, and produce a Thunderstore-ready ZIP. `./Package.ps1` can also package an existing Release build. The package includes the required manifest, PNG icon and UTF-8 README at the ZIP root. The manifest links to the public GitHub repository. Builds do not install the mod.

## Verification

```powershell
# Normal verification; AutoPicker is not required or loaded.
dotnet run --project tests/Checks -c Release -- "E:\Games\SteamLibrary\steamapps\common\Valheim"

# Optional extra compatibility check when AutoPicker is installed:
dotnet run --project tests/Checks -c Release -- "E:\Games\SteamLibrary\steamapps\common\Valheim" "PATH_TO_AUTOPICKER_DLL"
```

The normal command checks radius boundaries, diet identity, and the installed game's pickup IL. When a second path is supplied, it additionally checks AutoPicker's pickup integration. Live gameplay still needs testing: drop matching and nonmatching feed inside and outside the radius, confirm manual pickup, repeat with AutoPicker enabled and a second client, and check fed versus hungry animals. Use a disposable test world before relying on protection in a shared world.

## Credits

Code is MIT licensed. The icon depicts Valheim's carrot item; see `ATTRIBUTION.md` for the image source and separate artwork attribution.

The plugin ID is now com.ziluck.valheim.animalfeedguard. If you tried the earlier build, remove that old DLL before installing this one. To preserve settings, rename the old com.michaelziluck.valheim.animalfeedguard.cfg to the new filename while the game is closed.

## Check out my other mods

- [Ranching - Chick Addon](https://thunderstore.io/c/valheim/p/DocZee/Ranching_Chick_Addon/) — adds configurable chick growth and growth percentage hover text to Ranching.


