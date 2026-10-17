# Changelog

## 0.1.0

- Added the **Home Bubble**: a buildable piece (5 Iron, 5 Copper, 2 Dragon Tears, Hammer, Misc tab) that surrounds your base with a protective bubble. Monsters keep away from it just as they do from a trader's camp, but you can still build inside it.
- Looks like the force field around Haldor's camp, with a configurable colour, solidity and distortion.
- Needs a Workbench, Forge, Stonecutter and Artisan Table all within build range to place. Each bubble claims the closest unclaimed set of the four, so two bubbles cannot share crafting stations.
- The bubble grows with upgrades: it starts at 24m radius and reaches 48m radius once every upgrade is built on its four stations. Destroying upgrades shrinks it again.
- Fuelled with bones like the shield generator. Bones also burn down over time, and projectile hits drain it further.
- If one of its crafting stations is destroyed, the bubble drops and explains why when you try to add bones. Rebuilding the station in range brings it back.
- Configurable: `BaseRadius`, `FullRadius`, `MinutesPerBone`, and the bubble's `Tint`, `TintOpacity` and `Distortion`.
