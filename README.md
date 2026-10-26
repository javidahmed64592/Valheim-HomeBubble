[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-homebubble/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-homebubble/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-homebubble/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-homebubble/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# HomeBubble

Keep enemies out of your base with a protective bubble.
The bubble even protects against projectiles and rain.
Keep the bubble generator fuelled with bones, and toggle it on and off with `Shift + E`.

![Small Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/images/game_screenshot_1.png)
![Big Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/images/game_screenshot_2.png)

## Building It

The bubble requires a **Workbench, Forge, Stonecutter and Artisan Table** all within build range, including range extended by [BenchLink](https://github.com/javidahmed64592/Valheim-BenchLink).
Each bubble claims one set of these stations, so two bubbles cannot be built on the same set.

Find it in the Hammer's **Defence** tab.

| Material    | Amount |
| ----------- | ------ |
| Iron        | 5      |
| Copper      | 5      |
| Dragon Tear | 2      |

## Bubble Size

The bubble grows as you build upgrades on the Workbench and Forge.
If one of the bubble's four stations is destroyed, the bubble goes down.
Rebuild the station within range and it comes back.

## Configuration

Settings are in the BepInEx config file `javidahmed64592.homebubble.cfg`.
You can change the bubble's size with and without upgrades, how long a bone lasts, how much fuel the generator holds, and the bubble's colour, opacity and distortion.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-HomeBubble/blob/main/LICENSE) file for details.
