# Run 2B: living lunar base (report)

Branch `feat/living-base`, worktree `ArtemisX-livingbase`. Nothing pushed or merged; `main` untouched.

Two instructions arrived from Lucas mid-run and override the prompt: **build the ground from the pack's terrain pieces**, and **no shadows at all**.

## What passed (checked in Play mode in `Base.unity`)

- **Connected grey lunar base in one frame.** The ground is an island of KayKit `terrain_*` blocks (9 x 7 tiles) with a raised plateau, a ramp, two `terrain_mining` craters and rounded corners, on the grey `M_LunarTerrain` material. Fits with the HUD at 1920x1080 and 1680x1050 (both captured and looked at).
- **Four named modules and five ghosts.** `Base/Habitat` (two storeys, roof, radar), `Base/SolarArray` (on the plateau), `Base/ProcessingPlant` (module + roof cargo + tanks), `Base/LandingPad`, plus `Tunnel_East` / `Tunnel_West`. All five `Slots/Slot_*` keep their ghost model and ring; the rings now pulse.
- **Rover loop.** `Base/Rover_Luna` drives plant -> ice field -> back on a smoothed 8-node loop (24 units), stops at both ends, wobbles and trails dust. A bounds check of the path against every prop and building came back clear.
- **Astronauts.** Four walkers at one third of the old size (model scale ~0.083, about 0.25 units tall) hop-walk on three paths and sometimes stop to wave, nod or do two big floaty jumps.
- **Emotes and mood.** `EmoteBubble.Show(target, kind, seconds)` works. `AmbientMood` Night gives zzz bubbles and slower walking; Storm gives alert/scared bubbles and all four walk to the habitat door; Normal gives random hearts, stars and ideas.
- **HUD.** Bars show; clicking Next Day went Day 1 -> 2 -> 3. `HUD`, `Managers` and `EventSystem` were not touched.
- **Console.** Zero errors and zero warnings from our scripts in the final Play session.
- **Render.** MSAA 4x on in `PC_RPAsset`.
- **Screenshots** in `Assets/Docs/screenshots/`: `run2b-overview.png`, `run2b-walkers.png`, `run2b-night-emotes.png`, `run2b-storm-emotes.png`.

## What differs from the prompt, or was not done

- **Shadows are off** (Lucas's instruction), not tuned to 30-40 as section 4.6 said. Off in `PC_RPAsset` and on the `Sun` light, and every new renderer has shadow casting off. The scene reads flatter without them.
- **The flat `LunarGround` plane is deactivated**, not deleted. The island sits on the black sky like the pack's reference dioramas.
- **No rover train** (`mobile_base_*`): optional, and there was no room in the frame.
- **Not committed on purpose:** `ProjectSettings/QualitySettings.asset` (Unity rewrote it when MSAA changed: `antiAliasing: 4` for the PC level plus a format bump), `Assets/Settings/Mobile_RPAsset.asset` (Unity auto-upgraded it on open) and `Assets/TutorialInfo/Icons/URP.png` (git LFS noise). All three are outside my file ownership. Unity will produce the same QualitySettings change on `main` after the merge; commit it there.
- **Mobile quality level:** `Mobile_RPAsset` still has shadows enabled. The `Sun` light has shadows off, so nothing casts either way, but if the WebGL build uses the Mobile level, switch its shadows and MSAA to match.

## Stand-ins

- **Radar dish** (`_Prefabs/Base/RadarDish.prefab`): built from Unity primitives, since the pack has no part that can spin on its own.
- **Lamp glow**: a small unlit sphere on the pack's `lights` model, blinking by colour. No real lights.
- **Ice**: Resource Bits `Stone_Chunks_*` with a flat pale-blue material (`M_Ice`).
- **Ice drill**: `drill_structure` on one mining block.
- **Processing plant house**: `basemodule_C` beside the `containers_C` tanks, so the tunnel has something to connect to.
- **Jump**: the pack's `Jump_Idle` pose looped, with the height done in code.

## What I am unsure about

- **Astronaut size.** At one third they are about 12 px tall in the gameplay view and hard to spot without a bubble over them. It is one number per model (the model child's scale under each `Walker_*`).
- **Grey texture.** I followed the decision and generated a desaturated copy (`_Materials/T_SpaceBitsLunar.png`). The pack already ships a grey alternative, `Assets/textures/spacebits_texture_alt.png`, with warm grey terrain and blue module trim. Swapping the texture on `M_LunarTerrain` is a one-field change.
- **Emote sharpness.** The Kenney vector emotes are only 32 px, so bubbles are soft up close. Fine at gameplay zoom. I did not change their import settings (read-only pack).
- **Flatness without shadows.** Cheap blob shadows under the rover and astronauts would bring back some grounding.
- **Rover in a storm** keeps driving. Losing a rover is a simulation rule, so I left it alone.
- **Walk_Pad walker** can take about 20 s to reach the habitat in a storm from the far end of its path.

## Missing assets for Lucas to source

- Helmeted human astronauts, if the animal characters go. Any rigged model with an Animator works (see below).
- Higher-resolution emote icons (128 px or SVG), or in-house ones in Figma like the resource icons.
- A real radar/antenna model, an electrolyser, a processing plant and a hydrogen engine (all still stand-ins).

## Design questions

1. Keep the diorama island on a black sky, or should the ground fill the screen?
2. Are the astronauts too small at one third?
3. Keep the two-storey habitat and the extra plant module, or should only the four gameplay buildings be solid?
4. Should Night and Storm also change the lighting? I did not touch the light mood.

## How to use it

- **Swap the astronaut model:** drop any rigged model under a `Walker_*` root (prefab `_Prefabs/Characters/MoonWalker.prefab`), assign it as `Visual`, and give its Animator a controller with states named `Idle`, `Walk`, `Wave`, `Yes`, `Jump` (an Animator Override Controller on `_Animations/AstronautWalker.controller` is enough). Missing states are skipped. No code changes.
- **Moods:** `LivingBase/AmbientMood` component menu -> "Mood: Normal / Night / Storm", or `SetMood(MoodState)` from code. It has a `MoodChanged` event.
- **Tunables:** everything is in `Assets/_Config/Visuals/VisualsConfig.asset`.
- **Paths:** `LivingBase/Paths/*` are plain child transforms; move the points in the Scene view.

## Merge notes for Run 2A

- `Base.unity` was rebuilt heavily. If 2A only adds a dialogue prefab, the easiest merge is to take this branch's `Base.unity` and re-add the dialogue prefab instance on top, rather than merging the YAML.
- New root object `LivingBase`; `Base`, `Slots`, `Astronauts`, `Environment` keep their names. `HUD`, `Managers`, `EventSystem`, `CM_BaseCamera` and `Main Camera` are unchanged.
- New code is only in `Assets/_Scripts/Visuals/` (namespace `ArtemisX.Visuals`). No changes to Core, Simulation, UI, Story, packages or GameConfig.
- Shared docs to update on `main`: add the new stand-ins to `asset-inventory.md`. No new third-party assets, so `references.md` needs nothing.
- The Unity Pipeline package logged two of its own errors once ("Failed to handle /api/commands request") when a CLI call hit a domain reload. Not from game code.
