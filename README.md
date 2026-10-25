[![CI](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-homebubble/ci.yml?branch=main&style=flat-square&label=CI&logo=github)](https://github.com/javidahmed64592/valheim-homebubble/actions/workflows/ci.yml)
[![Package](https://img.shields.io/github/actions/workflow/status/javidahmed64592/valheim-homebubble/package.yml?branch=main&style=flat-square&label=Package&logo=github)](https://github.com/javidahmed64592/valheim-homebubble/actions/workflows/package.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

# Home Bubble

Keep enemies out of your base with a protective bubble.
The bubble even protects against projectiles and rain.
Keep the bubble generator fuelled with bones, and toggle it on and off with `Shift + E`.

![Small Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/game_screenshot_1.png)
![Big Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/game_screenshot_2.png)

## Building It

The bubble requires a **Workbench, Forge, Stonecutter and Artisan Table** all within build range.
Each bubble claims one set of these stations, so two bubbles cannot be built on the same set.

Find it in the Hammer's **Defence** tab.

| Material | Amount |
|----------|--------|
| Iron | 5 |
| Copper | 5 |
| Dragon Tear | 2 |

## Bubble Size

The bubble grows as you build upgrades on the Workbench and Forge.
If one of the bubble's four stations is destroyed, the bubble goes down.
Rebuild the station within range and it comes back.

## Configuration

Settings are in the BepInEx config file for the mod:

| Setting | Description |
|---------|-------------|
| BaseRadius | The bubble's size without any upgrades. |
| FullRadius | The bubble's size with all upgrades. |
| MinutesPerBone | How long one bone lasts. 0 turns the drain off. |
| MaxFuel | The maximum amount of fuel the generator can hold. |
| Tint | The bubble's colour as a hex code, e.g. #4DB8FF. |
| TintOpacity | How solid the bubble's coloured surface is. 0 turns it off. |
| Distortion | How strongly the bubble warps what you see through it. |

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/javidahmed64592/Valheim-HomeBubble/blob/main/LICENSE) file for details.
