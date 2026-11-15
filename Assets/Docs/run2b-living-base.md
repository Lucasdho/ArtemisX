# Run 2B: living lunar base (report)

Branch `feat/living-base`, worktree `ArtemisX-livingbase`. Nothing pushed or merged; `main` untouched.

This report covers the first pass plus the feedback round Lucas gave afterwards.

## What passed (checked in Play mode in `Base.unity`)

- **Connected lunar base, ground fills the screen.** The base sits on KayKit `terrain_*` blocks with a raised ridge, a ramp and two `terrain_mining` craters. Large scaled `terrain_low` blocks, extra ridge blocks and scattered rocks carry the ground past every screen edge. Terrain and rocks use the pack's own grey atlas (`spacebits_texture_alt.png`) through `M_LunarTerrain`. Checked at 1920x1080 and 1680x1050.
- **Four named modules and five ghosts.** `Base/Habitat` (two storeys, roof, radar), `Base/SolarArray`, `Base/ProcessingPlant`, `Base/LandingPad`, `Tunnel_East` / `Tunnel_West`, and all five `Slots/Slot_*` with pulsing rings.
- **Small soft shadows.** Main-light shadows are back on in `PC_RPAsset` (soft, one cascade, 45 units), with the sun raised to 58 degrees so shadows stay short.
- **Rover.** `Base/Rover_Luna` loops plant -> ice field -> plant. In a storm it finishes the loop to the nearest node, drives to `Slot_RoverGarage` and parks there; when the storm ends it rejoins the loop.
- **Kit models.** The radar is now `Roof_Radar`, the processing plant house is `Building_L`, the electrolyser ghost is `House_Long` and the hydrogen engine ghost is `House_Cylinder`, all from the Quaternius Ultimate Space Kit on the existing `M_QuaterniusAtlas` material.
- **Astronauts.** One of each kind (Finn, Barbara, Fernando, Rae), now about 0.44 units tall (was 0.25 in the first pass, 0.75 in Run 1). They hop-walk, pause, wave, nod and jump.
- **Bubbles.** Small and sharp: 256 px textures built from the Kenney pack's vector SVG, stored in `Assets/_Materials/Emotes/`. They only appear when two astronauts meet (both stop, turn and wave), at night (zzz), in a storm (alert / scared), and on good fortune (`AmbientMood.Celebrate()`, also in the component menu). The random happy bubbles are gone.
- **Night indoors.** At night the astronauts walk to the habitat door and go inside (they are hidden), and a zzz bubble shows over the habitat. They come back out when the mood changes.
- **Night and storm.** `MoodLighting` tweens the sun and ambient light per mood and plays particles: slow stardust at night, fast streaks with a light flicker in a storm. Normal uses whatever the scene starts with.
- **HUD.** Bars show; Next Day went Day 1 -> 2 -> 3. `HUD`, `Managers`, `EventSystem` untouched.
- **Console.** Zero errors and zero warnings from our scripts.
- **Screenshots** in `Assets/Docs/screenshots/`: `run2b-overview.png`, `run2b-walkers.png` (a greeting), `run2b-night-emotes.png`, `run2b-storm-emotes.png`.

## Not done, or worth knowing

- **Hamster Island look.** I had no screenshot of it, so the shadows are my reading of "small shadows": short, soft, 60% strength. Send a reference and I will match it.
- **Light changed.** To get short shadows the sun went from 22 to 58 degrees, and its intensity from 2.2 to 1.15 with a brighter ambient so the ground did not blow out. This replaces the "harsh low sun" mood from Run 1.
- **New asset pack is committed on this branch.** `Assets/GameAssets/Ultimate_Space_Kit_QuaterniusCharacters` was replaced in the worktree by the full `Ultimate_Space_Kit_Quaternius` (Characters, Environment, Items, Vehicles). The scene now depends on it, so the folder swap is committed here; astronaut GUIDs are unchanged. `CLAUDE.md` and the shared docs still name the old folder.
- **Model choices are mine.** The kit has no literal electrolyser, plant or engine, so I picked by shape: a pipe elbow for the plant, a capsule tank for the electrolyser, a sphere tank on legs for hydrogen. The two art styles (KayKit and Quaternius) now sit side by side.
- **Stand-ins still in place:** ice (tinted stone chunks), ice drill, lamp glow spheres, the blue tank cluster beside the plant.
- **Slow walk home.** The astronaut on the long east path can take over 30 seconds to reach the door at night.
- **No rover train** (`mobile_base_*`).
- **Not committed on purpose:** `ProjectSettings/QualitySettings.asset` (Unity rewrote it when MSAA changed), `Assets/Settings/Mobile_RPAsset.asset` (auto-upgraded on open), `Assets/TutorialInfo/Icons/URP.png` (LFS noise). Outside my file ownership.
- **Mobile quality level** was not touched; match its shadow and MSAA settings if WebGL uses it.

## What I am unsure about

- **Storm colour.** The warm tint on the taupe ground reads a little like Mars. `stormSunColor` and `stormAmbient` in the config control it.
- **Terrain colour.** The pack's grey atlas is a warm taupe, not a neutral grey.
- **Astronaut size.** "A bit bigger" became 0.44 units; it is one scale value per model.
- **Rover parking** drives into the middle of the ghost garage, since the garage is not built yet.

## Design questions

1. Are the kit models I picked the right ones, or do you want others (`GeodesicDome`, `Base_Large`, `SolarPanel_*`, `Rover_*` are also there)?
2. Should the rover and solar panels also switch to the Quaternius kit, so the base is one style?

## How to use it

- **Swap the astronaut model:** drop any rigged model under a `Walker_*` root (prefab `_Prefabs/Characters/MoonWalker.prefab`), assign it as `Visual`, and give its Animator a controller with states `Idle`, `Walk`, `Wave`, `Yes`, `Jump`. Missing states are skipped. No code changes.
- **Moods:** `LivingBase/AmbientMood` component menu -> "Mood: Normal / Night / Storm" and "Good fortune", or `SetMood(MoodState)` / `Celebrate()` from code. `MoodChanged` is a plain C# event.
- **Bubbles from other code:** `EmoteBubble.Show(target, EmoteKind, seconds)`. Kinds: Sleep, Scared, Happy, Alert, Idea, Greet.
- **Tunables:** `Assets/_Config/Visuals/VisualsConfig.asset`.
- **Paths:** `LivingBase/Paths/*` are plain child transforms.

## Merge notes for Run 2A

- `Base.unity` was rebuilt heavily. Take this branch's scene and re-add the dialogue prefab instance on top, rather than merging the YAML.
- New root object `LivingBase`; `Base`, `Slots`, `Astronauts`, `Environment` keep their names. `HUD`, `Managers`, `EventSystem`, `CM_BaseCamera` and `Main Camera` are unchanged.
- New code is only in `Assets/_Scripts/Visuals/` (namespace `ArtemisX.Visuals`).
- Shared docs to update on `main`: `asset-inventory.md` (stand-ins, the emote SVG use, and the new Quaternius kit if it is kept) and `references.md` (the kit's full name).
- The Unity Pipeline package logged two of its own errors once when a CLI call hit a domain reload. Not from game code.
