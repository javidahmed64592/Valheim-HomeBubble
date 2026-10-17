# Home Bubble

Protect your base with a big bubble that lets you build inside it.

![Home Bubble Example](https://github.com/javidahmed64592/Valheim-HomeBubble/raw/main/game_screenshot.png)

The Home Bubble works like the protective bubble around a trader's camp: monsters will not come in, but you can build freely inside it.
The bubble requires a **Workbench, Forge, Stonecutter and Artisan Table** all within build range.
Each bubble claims one set of these stations, so two bubbles cannot be built on the same set.

The bubble starts at a radius of 16m and grows to 32m as you build every upgrade on the four stations.
Lose an upgrade and it shrinks again.

Add bones to the bubble as you would a shield generator.
They burn down slowly over time (30 minutes per bone by default) and projectile hits drain it faster.

If one of the bubble's four stations is destroyed, the bubble goes down. Rebuild the station within range and it comes back.

## Configuration

Settings are in the BepInEx config file for the mod:

- `BaseRadius` and `FullRadius`: the bubble's size without and with all upgrades.
- `MinutesPerBone`: how long one bone lasts. 0 turns the drain off.
- `Tint`, `TintOpacity` and `Distortion`: how the bubble looks.
