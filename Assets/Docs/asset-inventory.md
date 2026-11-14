# Artemis X: asset inventory

Everything below is already imported under `Assets/GameAssets/` (read-only). Model paths use the `fbx(unity)` folders.

## Key answers

**Base buildings (existing modules)**

| Role | Model (KayKit Space Base Bits) | Notes |
|---|---|---|
| Habitat | `basemodule_A` | Octagonal crew module with door and orange trim. `basemodule_B`/`C`/`D` are variants. |
| Solar array | `solarpanel` | Single tilted panel on a frame. Group 2–3 for an "array". `roofmodule_solarpanels` is a smaller rooftop version. |
| Processing plant (ice → water) | `containers_C` | Cluster of blue tanks; reads as "water". Stand-in: there is no dedicated processing-plant model. Alternatives: `water_storage` (red cylinder), `cargodepot_B`. |
| Landing pad | `landingpad_large` | Octagonal pad with orange edge. `landingpad_small` also exists; `lander_A`/`lander_B`/`dropship` can sit on it for supply arrivals. |

**Ghost-slot buildings**

| Slot | Model | Stand-in? |
|---|---|---|
| Greenhouse | `eco_module` | Good fit: glass cylinder with plants inside. `space_farm_large` / `space_farm_small` are open-air farm beds. |
| Electrolyser | `containers_A` | Stand-in (green tank cluster, reads as "oxygen tanks"). No electrolyser model. |
| Extra solar array | `solarpanel` | Same model as the existing array. |
| Rover garage | `basemodule_garage` | Good fit: module with a garage door. |
| Hydrogen engine | `lander_A` | Stand-in (small rocket lander). No engine/fuel-plant model. `dropship` or `structure_tall` (launch tower) are other options. |

**Rover:** yes. `spacetruck` (small 4-wheel pickup) is the rover. `spacetruck_large` (van) and `spacetruck_trailer` exist too, and `mobile_base_frame` (6-wheel chassis) + `mobile_base_command` could be combined into a bigger rover later.

**Astronaut:** the KayKit `Mannequin_Medium` (with the Rig_Medium animation sets) is the placeholder. There is also the Quaternius *Ultimate Space Kit* with four astronaut characters (`Astronaut_FinnTheFrog`, etc.), which may fit a kids' game better. Design call for Lucas.

**Resource icons:** there are **no proper resource icons** in the Kenney UI packs. Stand-ins from Kenney Emotes (`PNG/Vector/Style 1`):

| Resource | Sprite | Fit |
|---|---|---|
| Power | `emote_idea` (light bulb) | OK |
| Water | `emote_drop` (blue drop) | Good |
| Oxygen | `emote_circle` (blue ring, reads as "O") | Weak |
| Food | `emote_heart` | Weak |
| Parts | `emote_star` | Weak (no gear/cog icon) |

Missing: a lightning bolt, O2 bubble, food/leaf and gear icon set. See "Missing assets" in the run report.

## KayKit Space Base Bits 1.0 (SOURCE)

`KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/`, single shared texture `spacebits_texture.png`.

- **Base modules**: `basemodule_A`–`E`, `basemodule_garage`. Habitat, lab, garage; `basemodule_E` has a brown dome roof.
- **Roof add-ons**: `roofmodule_base`, `roofmodule_cargo_A/B/C`, `roofmodule_solarpanels`. Upgrade visuals for existing modules.
- **Power**: `solarpanel`, `windturbine_low`, `windturbine_tall` (wind is wrong for the Moon; skip).
- **Food**: `eco_module` (greenhouse), `space_farm_large`, `space_farm_large_sprinkler`, `space_farm_small`.
- **Storage / processing**: `containers_A` (green), `containers_B` (yellow), `containers_C` (blue), `containers_D` (red), `water_storage`, `cargodepot_A/B/C`.
- **Cargo / supply pile**: `cargo_A`, `cargo_B` (+ `_packed`, `_stacked`). Good for the Earth supply pile.
- **Vehicles**: `spacetruck`, `spacetruck_large`, `spacetruck_trailer`, `mobile_base_frame`, `mobile_base_carriage`, `mobile_base_cargo`, `mobile_base_command`.
- **Landing / rockets**: `landingpad_large`, `landingpad_small`, `lander_A`, `lander_B`, `lander_base`, `dropship`, `dropship_packed`.
- **Structures**: `structure_low`, `structure_tall`, `drill_structure` (ice drill candidate), `dome` (glass dome), `lights` (lamp post).
- **Tunnels**: `tunnel_straight_A/B`, `tunnel_diagonal_long_A/B`, `tunnel_diagonal_short_A/B`. Corridors between modules.
- **Terrain / rocks**: `terrain_*` blocks and `rock_A/B`, `rocks_A/B`. Red-brown (Mars-like); would need a grey material swap for the Moon.
- `SOURCE/` holds a `.blend` file (needs Blender; not used).

## KayKit Resource Bits 1.0 (FREE)

`KayKit_ResourceBits_1.0_FREE/Assets/fbx(unity)/`, texture `resource_bits_texture.png`.

- **Parts**: `Parts_Cog`, `Parts_Pile_Small/Medium/Large`. Parts resource, supply pile, repairs.
- **Fuel**: `Fuel_A/B/C_Barrel(s)`, `_Jerrycan`, `_Dirty`. Hydrogen/fuel reserve visuals.
- **Metals**: Copper/Iron/Silver/Gold `_Bar(s)`, `_Bars_Stack_*`, `_Nugget(s)`. Could be "ice"/ore pickups with a tint.
- **Stone**: `Stone_Chunks_Small/Large`, `Stone_Brick(s)`. **Best stand-in for ice chunks** with a pale-blue material.
- **Other**: `Pallet_Wood(_Covered_A/B)` (supply pile base), `Textiles_*`, `Wood_*` (not thematic).

## KayKit Character Animations 1.1

- `Mannequin Character/characters/Mannequin_Medium.fbx` (+ `_Large`). Placeholder astronaut.
- `Animations/fbx/Rig_Medium/`: `General`, `MovementBasic`, `MovementAdvanced`, `Tools`, `Simulation`, `Special`, `CombatMelee`, `CombatRanged`. Idle/walk/interact clips for cutscenes.

## Quaternius Ultimate Space Kit (characters)

Not listed in CLAUDE.md, but present in `Ultimate_Space_Kit_QuaterniusCharacters/FBX/`.

- `Astronaut_BarbaraTheBee`, `Astronaut_FernandoTheFlamingo`, `Astronaut_FinnTheFrog`, `Astronaut_RaeTheRedPanda`. Cartoon astronauts.
- `Mech_*` (4), `Enemy_*` (4). Probably out of scope.
- `Blends/` has 12 `.blend` files that log "Blender could not be found" on import; the FBX versions are fine.

## Kenney UI Pack 2.0

`kenney_ui-pack/PNG/<Color>/Default` (Blue, Green, Grey, Red, Yellow; `Double` = 2x resolution).

- **Buttons**: `button_round_*`, `button_square_*`, `button_rectangle_*` in flat/gloss/gradient/border/depth/line styles. Next Day button, card buttons.
- **Arrows**: `arrow_basic_*`, `arrow_decorative_*` (n/e/s/w).
- **Icons**: `icon_checkmark`, `icon_cross`, `icon_circle`, `icon_square` (+ outline). `Extra/`: `icon_play_*`, `icon_repeat_*`, `icon_arrow_up/down_*`. `icon_play_light` = Next Day.
- **Stars**: `star`, `star_outline`, `star_outline_depth`. The 3-star mission result.
- **Sliders / checks**: `slide_*`, `check_*`.
- **Font**: `Kenney Future`, `Kenney Future Narrow`.
- **Sounds**: `click-a/b`, `switch-a/b`, `tap-a/b`.

## Kenney UI Pack: Sci-fi (Space Expansion) 2.0

`kenney_ui-pack-space-expansion/PNG/<Color>/Default`.

- **Fill bars**: `bar_round_large`, `bar_round_small`, `bar_square_*`, gloss variants, plus `_l/_m/_r` 9-slice pieces. Resource bar fills.
- **Bar backgrounds** (`Extra/`): `bar_shadow_round_large`, `bar_shadow_square_*` (+ outline). Resource bar tracks.
- **Panels** (`Extra/`): `panel_glass*`, `panel_rectangle*`, `panel_square*`. Top bar, debrief cards.
- **Header buttons**: `button_square_header_*` (blade/notch/large/small). Event cards.
- **Cursors / crosshairs**: `cursor_a–h`, `crosshair_*`.
- Same fonts as the base UI pack.

## Kenney Emotes

`kenney_emotes-pack/PNG/Vector/Style 1–8` (plus Pixel styles and spritesheets).

- 30 emotes per style: `emote_alert`, `emote_exclamation(s)`, `emote_question`, `emote_drop(s)`, `emote_idea`, `emote_heart`, `emote_star(s)`, `emote_cloud`, `emote_sleep(s)`, `emote_cross`, `emote_faceHappy/Sad/Angry`, etc.
- Use: map pings over objects (alert on a broken panel, drop over a returning rover, idea over an affordable ghost) and the resource icon stand-ins above.

## Kenney Interface Sounds 1.0

`kenney_interface-sounds/Audio/*.ogg`, families (count): `click` (5), `select` (8), `confirmation` (4), `error` (8), `switch` (7), `toggle` (4), `tick` (3), `drop` (4), `glass` (6), `glitch` (4), `maximize` (9), `minimize` (9), `open` (4), `close` (4), `back` (4), `scroll` (5), `scratch` (5), `question` (4), `pluck` (2), `bong` (1).

- Use: button clicks, confirmation on build, error on unaffordable, `glitch` for storm alerts.

## Other third-party code

- **DOTween** (Demigiant), `Assets/Plugins/Demigiant/`. Tweening (resources flying to bars, pulses).
