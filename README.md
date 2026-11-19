[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-homebubble/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-homebubble/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-homebubble/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-homebubble/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# HomeBubble

Keep enemies out of your base with a protective bubble.

The bubble even protects against projectiles and rain, and overlapping bubbles merge into one.
Keep the bubble generator fuelled with bones, and toggle it on and off with `Shift + E`.
You get a message when you are near a bubble that is running low on bones.

![Small Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/images/small_bubble.png)

![Generator Hover](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/images/generator_hover.png)

## Building It

The bubble requires a **Workbench, Forge, Stonecutter and Artisan Table** all within build range, including range extended by [BenchLink](https://thunderstore.io/c/valheim/p/Aestelwen/BenchLink/).
Each bubble claims one set of these stations, so two bubbles cannot be built on the same set.

Find it in the Hammer's **Defence** tab.

| Material    | Amount |
| ----------- | ------ |
| Iron        | 5      |
| Copper      | 5      |
| Dragon Tear | 2      |

## Bubble Size

The bubble grows as you build upgrades on the Workbench, Forge, Stonecutter and Artisan Table.
A Black Forge and Galdr Table within range, including range extended by BenchLink, make it grow too, but they are never required: without them the bubble simply ignores them.
The bubble always uses the most upgraded station of each type in range, so upgrading a different bench of the same type, or building a better one, makes the bubble grow.
If one of its stations is destroyed, the bubble switches to the next best one in range; if none is left, the bubble goes down until one is built.

## Monsters

Monsters keep away from a bubble, but one that was fighting you when you ran inside attacks the bubble, and its allies nearby join in.
Their hits use up bones, and the harder they hit, the more it takes, so retreat to restock and heal, then go back out and deal with them.
They give up after a while if you leave them alone, but fighting back from inside keeps them at the wall.
Every upgrade on the bubble's stations makes it take 1% less damage, so a fully upgraded bubble holds out about 20% longer.

## Getting Ready

Your food and the Rested effect don't wear off while you are inside a bubble, so you can prepare for an adventure and leave in your own time.
Turn this off with the `PauseTimers` setting.

## On the Map

The minimap and the large map show the area each bubble covers.
Only bubbles loaded around you are shown.

![Coverage Map](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/images/coverage_map.png)

## Raids

Raids don't start on players inside a bubble.
A raid that a bubble holds off happens later instead, once you have spent a few minutes away from every bubble.

## Configuration

Settings are in the BepInEx config file `javidahmed64592.homebubble.cfg`.
Settings marked "(Synced with server)" use the server's values when the server has the mod installed.

## Compatibility

- The bubble is built from the vanilla Shield Generator, so mods that change the Shield Generator may change the bubble too.
- Mods that change how monsters avoid safe zones, such as the trader's camp, may change how the bubble keeps them out.
- This mod has not been tested with a gamepad, only with keyboard and mouse.

## Uninstalling

Home Bubbles disappear from the world when the mod is removed, along with any bones in them.
Deconstruct them first to get their materials back.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-HomeBubble/blob/main/LICENSE) file for details.
