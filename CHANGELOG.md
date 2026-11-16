# Changelog

## Unreleased

- Monsters now attack the bubble when someone they were fighting runs inside, instead of fleeing. They come up to the edge on their side and strike the wall, or shoot at it from range. Their allies nearby that are already in the fight join in. After 30 seconds a monster loses interest and wanders off, but hitting it from inside starts its time over, so fighting back keeps them at the wall. They go back to the chase if their target comes back out. Monsters that find players already inside still keep away. Flying monsters keep away as before. New `Monsters` settings, synced with the server: `AttackBubble` (on) and `SiegeSeconds` (30).
- Hits on the bubble use up far more bones: one bone per 200 damage, counting the monster's star level and the world's difficulty, so a troll hit costs about 0.35 bones and a two-star troll's about 0.7. Projectiles hitting the bubble use the same rate (vanilla was one bone per 500). Set it with the new synced `Monsters.DamagePerBone` setting.
- Every upgrade on the bubble's stations makes it take 1% less damage from monster hits and projectiles, so a fully upgraded bubble takes about 20% less. Set it with the new synced `Monsters.ResistPerUpgrade` setting (0 turns it off). Hovering over the generator shows the bubble's damage resistance and upgrade count.
- Players in or near a bubble that is being hit get a message with the bones it has left, every few seconds.
- New `startsiege <1-5>` console command (needs `devcommands`) for testing: it spawns a group of monsters from the Mountains (1), Plains, Mistlands, Ashlands or Deep North (5), already hunting you. Use it inside a bubble and they attack the bubble at once.
- The minimap and the large map shade the area each loaded bubble covers in the bubble's tint, with a brighter edge. Overlapping bubbles show as one shape. Turn it off with the new `MapCoverage` setting under `[Look]`.

## 0.6.0

- Monsters no longer wander into a bubble. Before, they only kept out when they had no target, or when their target was inside, so a monster chasing something outside could follow it in. Now a monster that gets inside steps back out: if what it is fighting is still outside, it stops just past the edge and carries on, so monsters can fight right up to the bubble. If its target is inside too, it flees. Tamed creatures and the Dvergr are left alone, as at the trader's camp.
- Raids no longer start on players inside a bubble. Before, a raid could start while you were safe inside, and its enemies gathered around the bubble with no way in. A raid that a bubble held off isn't lost: it starts later, once a player has spent 3 minutes at least 30 metres beyond the edge of every bubble. It starts wherever that player is, even away from the base, as long as the biome and the bosses they have beaten allow it. New `Raids` settings, synced with the server: `BlockRaids` and `DeferRaids` (both on), `DeferMinutes` (3) and `DeferDistance` (30). This also works on dedicated servers, where the bubbles near players aren't loaded.
- The Hammer's highlight no longer lights up the whole bubble bright blue when you hover over the generator; only the generator itself is highlighted, and it no longer flickers. The generator's glow updated a few times a second and wiped the highlight for a frame each time (vanilla generators do this too), and its glow blinked off when the highlight ended.
- A fuelled bubble no longer hums. Vanilla muted the generator's powered hum on a bubble fuelled up from empty but played it at full volume after logging back in, so the same bubble was silent one session and loud the next. The low-fuel hum still plays when bones run low.
- The generator's sounds are quieter. The new `Sound.Volume` setting (default 0.4, where 1 is the vanilla volume) sets how loud they are.
- You get a message when you are in or near a bubble that is low on bones, and again every 5 minutes while you stay and it stays low. The new `Warnings.LowFuelWarning` setting sets how few bones count as low (default 5, 0 turns it off).
- Moved the source files into folders (`Core`, `Bubble`, `Prefabs`, `Patches`), leaving only `Plugin.cs` at the root, with each Harmony patch in its own file under `Patches`. No behaviour change.

## 0.5.0

- The bubble now always uses the most upgraded station of each type in range, like BenchLink picks a cluster's station, and its size follows. This includes the optional Black Forge and Galdr Table. Previously a destroyed station was replaced by whichever unclaimed station was closest, and the bubble never switched to a better one, such as the original station once it was rebuilt.
- The BepInEx log now says when a bubble claims its stations, switches to a different station, and goes down because none is left.
- Switching a bubble on or off with Shift + E now needs access to any ward it stands in, like opening a chest, so other players can't turn off your bubble.
- The switch is now handled by whoever's game is looking after the generator, so two players toggling at once can't overwrite each other.
- `BaseRadius`, `FullRadius`, `MinutesPerBone` and `MaxFuel` are now synced from the server, so every player sees the same bubble size and fuel use.
- The hover hint now shows your own key bindings instead of always saying Shift + E.
- The piece, messages and hover text can now be translated (`Translations/English.json`), and the station names use the game's own translations.
- Added a CI check that the version matches in every file.
- Moved `package.sh` into `.github/scripts` and made it the same for every mod: the name, version and DLL now come from `manifest.json` and the `.csproj`.

## 0.4.0

- Overlapping bubbles now merge into one outer surface, like the vanilla shield generators, instead of showing each other's walls from the inside.

## 0.3.3

- The bubble now also grows as the Black Forge and Galdr Table in range are upgraded. They are still not required to build the bubble or keep it up.

## 0.3.2

- Updated the BepInExPack_Valheim dependency to 5.4.2360.

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
