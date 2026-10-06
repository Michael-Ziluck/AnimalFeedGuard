# Feeding and pickup audit

Audited 2026-10-05 from the installed Valheim 1.0.16 assembly and asset bundle, the active Gale profile's AnimalFeedGuard DLL/config/log, and installed AutoPicker DLL. No game files, profile DLLs, or saves were modified.

## Evidence

- Client `assembly_valheim.dll` SHA-256: `96CFC004F7F4A6F30D070BEF39EAFD79C466A137121C4665A2F19FB9C15C6127`.
- SoftRef bundle `c4210710`: 150 MonsterAI components; 14 nonempty diets and 48 creature/food pairs. The extracted name data is in `tests/checks/creature-foods.json`.
- Active AnimalFeedGuard 2.0.0 matches the source. Its log reports successful patching, enabled protection, and a 5-metre radius.
- Adult Asksvin's exact food identities: `$item_vineberry`, `$item_fiddleheadfern`, `$item_smokepuff`. The reported Smokepuff case concerns already-loose food beside adult tamed Asksvin, rather than harvesting.
- Moose and summoned moose: `$item_lingonberries`. Bear variants, including summoned Bjorn: `$item_blueberries`. Wild bears remain excluded unless actually tamed; a food list alone does not make a creature eligible.

## Findings and changes

The prior code already used the shared-name comparison from `MonsterAI.CanConsume`, with no Asksvin or biome exclusion. No missing Smokepuff entry was found. Current vanilla feeding lives in MonsterAI; AnimalAI has no consume list. The runtime implementation remains generic and recognizes consume-list changes by other mods.

Distance previously used a snapshot of the character's transform origin. That is a weakness for food near the edge of a large animal. The new code uses `Collider.ClosestPoint`, falling back to the origin when the collider is absent/disabled/inactive. The configured distance remains fully three-dimensional; walls do not affect it.

`Character.IsTamed` caches remote state for a second and stops refreshing it while owned. The new code reads the synchronized ZDO tame flag, using `Character.IsTamed` as the default for missing data. Component discovery is cached per frame; life, tameness, location, and the current consume list are evaluated per item.

The filter still replaces the single `ItemDrop.m_autoPickup` read in `Player.AutoPickup`. Rejection occurs before item ownership requests, movement, or inventory collection. Neither item flags nor saves are changed. Manual item interaction remains outside this filter.

Installed AutoPicker's loop calls `Pickable.Interact` and does not directly call inventory AddItem or loose-item Pickup. Installed `Pickable.Drop` produces loose ItemDrops, which continue through normal automatic pickup. HenEggPickup temporarily changes flags on chicken eggs only; the feed filter remains in place. The installed log has no feed patch-installation failure.

The radius/state changes address identified weaknesses, but static inspection does not establish which caused the reported live failure. Optional diagnostics report the food identity, matching creature, body/origin distances, and alive/tamed flags so the new test can resolve that uncertainty. They do not execute pickup or change network state.

## Verification limits

Automated checks exercise every audited food pair, all cross-diet mismatches, wild/dead/out-of-range/disabled cases, the actual installed pickup IL and transpiler, and framework/game member references. An optional AutoPicker audit reads its installed DLL; it is required neither for building nor running the mod. Physics collider calculations, live ownership transfer, and multiplayer pickup still need in-game testing.
