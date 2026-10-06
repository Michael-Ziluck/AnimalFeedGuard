# Changelog

## 2.0.3 - 2026-10-06

- Share food matching between protection and pickup diagnostics.
- Add simulated filter checks and tests for diet enumeration, branch labels, and exception boundaries.
- Include every check project in dependency updates.
- Maintainer confirmed the current build works as intended in game.

## 2.0.2

- Increase the default feed protection radius from 5 to 25 metres.
- Preserve existing configured radii; set Protection Radius to 25 to use the new default with an existing config.

## 2.0.1

- Measure feed protection from the animal's body collider instead of its origin, with an origin fallback.
- Read synchronized tame state through ownership changes and recheck live creature state and diet for each item.
- Add optional, rate-limited pickup diagnostics.
- Audit all installed 1.0.16 feeding prefabs, including Asksvin Smokepuffs and Deep North creatures.
- Add regression coverage for all 48 creature/food pairs and actual pickup-transpiler IL.
- Prepare a test ZIP; live regression testing remains pending.

## 2.0.0

- Formally target Valheim 1.0; compiled and checked against 1.0.16.
- Require BepInExPack Valheim 5.4.2350.
- Add GitHub Actions builds and automatic publication of new versions from main.
- Add Hexium publishing scaffolding, disabled until DocZee is approved.
- Keep plugin IDs and configuration files stable for existing installations.

## 1.0.0

- Added configurable automatic pickup protection for food near living tamed animals.
- Match each animal's actual food list, including seeds and modded feed.
- Preserve manual pickup and normal pickup outside the protection radius.
- Inspect compatibility with AutoPicker 1.1.3's ground pickup path.
- Client-side settings; no dedicated-server installation required.

Build and automated code checks passed. Live gameplay testing is still pending.
