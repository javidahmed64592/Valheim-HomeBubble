# Changelog

## 0.3.1

- A bubble can now use a station that is out of its own build range but still lets you build there, such as one linked by BenchLink.

## 0.3.0

- Added an `.editorconfig`, `Directory.Build.props` and a CI workflow that runs `dotnet format`, a Release build and a NuGet vulnerability check.
- Reformatted the code to match the new code style rules (no behaviour change).
- Added the `UnityEngine.AudioModule` and `UnityEngine.ParticleSystemModule` references so local builds compile.
- Tidied up packaging scripts and workflow files.
- Added a `.gitattributes` to keep line endings consistent.
- Moved the local Valheim install paths into `Directory.Build.props`; set `VALHEIM_DIR` and `BEPINEX_PROFILE_DIR` once to build every mod without editing the `.csproj`.
- Updated README and icon.

## 0.2.1

- Updated README to include the new `MaxFuel` configuration option.

## 0.2.0

- The bubble now becomes more red and fades as it loses fuel.
- The bubble can now be toggled on and off by pressing `Shift + E` on the generator.
- The maximum amount of fuel the generator can hold is now configurable.

## 0.1.0

- Added the **Home Bubble**: a buildable piece (5 Iron, 5 Copper, 2 Dragon Tears, Hammer, Defence tab) that surrounds your base with a protective bubble. Monsters keep away from it just as they do from a trader's camp, but you can still build inside it.
- Looks like the force field around Haldor's camp, with a configurable colour, solidity and distortion.
- Needs a Workbench, Forge, Stonecutter and Artisan Table all within build range to place. Each bubble claims the closest unclaimed set of the four, so two bubbles cannot share crafting stations.
- The bubble grows with upgrades: it starts at 24m radius and reaches 48m radius once every upgrade is built on its four stations. Destroying upgrades shrinks it again.
- Fuelled with bones like the shield generator. Bones also burn down over time, and projectile hits drain it further.
- If one of its crafting stations is destroyed, the bubble drops and explains why when you try to add bones. Rebuilding the station in range brings it back.
- Configurable: `BaseRadius`, `FullRadius`, `MinutesPerBone`, and the bubble's `Tint`, `TintOpacity` and `Distortion`.
