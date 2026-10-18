# Home Bubble

Protect your base with a big bubble that lets you build inside it.

![Small Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/game_screenshot_1.png)
![Big Bubble](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/game_screenshot_2.png)

The Home Bubble works like the protective bubble around a trader's camp: monsters will not come in, but you can build freely inside it.
The bubble requires a **Workbench, Forge, Stonecutter and Artisan Table** all within build range.
Each bubble claims one set of these stations, so two bubbles cannot be built on the same set.

The bubble grows as you build upgrades on the Workbench and Forge.
If one of the bubble's four stations is destroyed, the bubble goes down.
Rebuild the station within range and it comes back.

| Material | Amount |
|----------|--------|
| Iron | 5 |
| Copper | 5 |
| Dragon Tear | 2 |

## Configuration

Settings are in the BepInEx config file for the mod:

| Setting | Description |
|---------|-------------|
| BaseRadius | The bubble's size without any upgrades. |
| FullRadius | The bubble's size with all upgrades. |
| MinutesPerBone | How long one bone lasts. 0 turns the drain off. |
| Tint | The bubble's colour as a hex code, e.g. #4DB8FF. |
| TintOpacity | How solid the bubble's coloured surface is. 0 turns it off. |
| Distortion | How strongly the bubble warps what you see through it. |
