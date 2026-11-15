# Run 3B: gameplay scene, world pop-ups, rover map and HUD (report)

Branch `feat/gameplay-ui` (from `develop`, main checkout, Editor driven over MCP). Nothing merged.
Scene: `Assets/Scenes/HandMadeBase.unity`. Lucas's terrain and buildings were kept; nothing was moved.

## What was done

### Wiring (re-runnable)
- `Artemis X > Run 3B > Build Gameplay HUD and Wiring` (`_Scripts/UI/Editor/GameplayHudBuilder.cs`) adds and wires everything below.
  It is safe to run again: it rebuilds only its own objects (`WeatherWidget`, `RoverButton`, `HelpButton`, `MorningCard`,
  `RoverMap`, `FlyLayer`, `NightSkip`, `GhostRing`, `StormAlertLamp`, popup `CostRow`, pill `AlertBadge`).
- `---------MANAGERS-------------` now also has `SpaceWeatherManager`, `UnexpectedDemandManager`, `RoverExpeditionManager`,
  `SolarFieldManager`, `BuildingManager`, `SupplyManager`, `DayResolutionController`, `DayNightTransitionPlayer`, `WorldClickRouter`.
  `NextDayFlow.dayResolution` is set. The old `BuildingClickInteractor` and all static `ClickableBuilding` components were removed
  from the scene (scripts kept in `Core/`).
- New configs: `_Config/Visuals/GameplayUIConfig.asset` (texts, Bauzer lines, tips, map camera, sounds) and
  `_Config/Visuals/GameplayVisualsConfig.asset` (transition length/split, rover speed, capsule drop, rocks, ghost ring).

### World clicks and pop-up cards
- `WorldInteractable` base + `WorldClickRouter` (click without drag, ignored over UI and while blocked: dialogue open,
  morning cards, rover map, Next Day transition, night resolving, mission over).
- `ContextConfirmPopupView` now opens next to any world anchor with title, text, resource cost chips and Confirm/Cancel
  (the old `Open(ClickableBuilding, Camera)` still works). Clicking elsewhere closes it.
- Solar field (`solarFarm`): Retract / Deploy, or Repair (5 Parts) when broken. Retract folds the panels (scale tween).
  Storm damage drops 4 rock prefabs (`Rock_Chunk_Small`, `Rock_Quat_1`) on top of the panels + a red `BlinkingLight`
  lamp + alert emote; repair removes them (`SolarDamageView`).
- Ghost slots 1 -> 2 -> 3: `ProcessingPlant` = Water & O2 Plant, `foodFarm` = Greenhouse, `Electrolyser_H2Generator` = Fuel Reserve.
  The buildings are hidden until built; only the next slot shows a pulsing `GhostRingPulse` ring. Build card shows
  Parts + Power cost and output; confirm builds at once with a scale pop + happy emote + sound, and `MissionCardView` lights the star.
  Clicking a later slot -> Bauzer "build in order" tip; crew restricted -> Bauzer tip.
- Rover (`ExplorationHover`): Ready -> top-down map; Broken -> Repair (5 Parts) card; On Mission / Radiation locked / crew
  restricted -> Bauzer tip. `RoverDriver` (decorative loop) is disabled; `RoverMissionDriver` drives out along
  `RoverRoutes/Route_<siteId>` on dispatch and back home on return (happy emote with cargo, repeating alert emote when broken).
- Supply (`suplyLandingPad`): on supply days the `dropship` falls from the sky onto the pad after the morning cards, then the
  `cargo_A_stacked` pile pops in. Clicking collects (`CollectSupply`) and flies Food and Parts icons to their pills.

### Top-down rover map
- `RoverMapController` flies `CM_BaseCamera` (pan controller paused) to a framed top-down pose over the 5 hot spots, shows a
  pin per site (name, -Power, +Water / +Parts, risk %; storm days show the storm risk) and a Back button. One click on a pin
  dispatches at once (`TryDispatchRover`) and flies back. Blocked pins stay clickable and Bauzer explains why.

### HUD
- Top bar unchanged (5 pills + supply tag + Day pill). Pills got a pulsing red `!` badge while Oxygen or Food is 0.
- `SpaceWeatherWidgetView` under the Day pill: Today / Tomorrow icon + `Calm` / `Solar Storm` / `Radiation` / `Storm Warning`.
- Bottom-right: Next Day, Rover status button (`Ready` / `On Mission` / `Broken` / `Locked`, acts like clicking the rover),
  `?` help (Bauzer tip). They fade out while Bauzer talks or the map is open.
- Bauzer tips use a runtime `DialogueSequence` through `DialogueRunner` (bottom-center box).

### Morning sequence and Next Day
- Next Day (Lucas's request, overrides the 1.5-2 s): Daylight -> Storm/Radiation (event days, storm particles from
  `MoodLighting`) -> Night -> Dawn, about **10 s** total, tunable (`transitionSeconds`, `eventDaySplit`, `calmDaySplit`).
  `CompleteNightResolution()` is called at nightfall, exactly once. Tap anywhere or the `Skip >>` button jumps to the end
  (and resolves the night if it hadn't yet). A full-screen invisible overlay keeps clicks off the base during the transition.
- Dawn: `ModalCardView` cards in strict order, after the dawn phase: Daily Report (deltas with icons, rover / storm / supply
  lines, Bauzer comment from `bauzerSummaryKey`, `Start Day`) -> Unexpected Demand (2 choices, can't be dismissed, Next Day
  stays locked until answered) -> NASA DONKI Alert + 2 choices (`ApplyWeatherChoice`).

### Simulation changes (approved by Lucas, committed as fix/test(sim))
1. A pending Unexpected Demand blocks End Day: new `DayResolutionController.CanEndDay()` (returns `DemandPending`), event
   `EndDayAvailabilityChanged`; the demand is no longer discarded at End Day. `NextDayFlow` keeps the button locked.
2. A Retracted solar field redeploys to Active at dawn (`SolarFieldManager.AutoRedeploy()`, `DailyReportData.solarRedeployed`); Broken stays broken.
3. Rover out in a solar storm: 30% break chance (`ExpeditionSiteConfig.stormBreakdownChance` = 0.3). Radiation never breaks it (unchanged).
- Tests: 27/27 EditMode green (new: storm chance survive/break, broken field never auto-redeploys, demand blocks End Day).
  Balance logs: Greedy 5/6/10, Cautious 6/7/10, Careless 6/9/15 (min/median/max win day) - still early, not retuned.

### Small edits outside UI/Visuals
- `StoryDirector.queueDailyBriefings` (off in this scene): after Day 1 the daily briefing and supply milestone are not queued,
  because the Daily Report card carries Bauzer's line (no double Bauzer at dawn). Day 1 intro + briefing still play.

## Definition of Done (checked in Play Mode via MCP, 1920x1080 and 1680x1050 Game view)
- [x] Diorama camera frames the outpost at both resolutions (pan/zoom code unchanged from Run 1; checked by screenshot only).
- [x] Rover click -> top-down view with 5 pins (-Power, +Water/+Parts, risk); pin click dispatched at once (Power 40 -> 26).
- [x] Solar field: retract/deploy card; storm with Keep Full Power broke it -> 4 rocks + blinking lamp; Repair (5 Parts) removed them.
- [x] Ghost slots 1 -> 2 -> 3; building 1 built at once (Parts 20 -> 5, Power 60 -> 40), Star 1 lit.
- [x] Day 5 supply capsule dropped; pile click: Food 18 -> 48, Parts 8 -> 23, 10 icons flew to the pills.
- [x] Next Day: Daylight -> Storm -> Night -> Dawn (9.7 s measured), then Daily Report -> Demand -> Space Weather Choice.
- [x] Skip resolves the night once (second Skip is a no-op), day advanced once.
- [x] Zero errors/warnings from our scripts (only MCP-FOR-UNITY port messages and URP "depth surface memoryless" info lines).
- [x] Scene saved, Play Mode stopped. Commits on `feat/gameplay-ui`, no AI attribution, not merged.
- [x] Screenshots taken (see below; not committed, git-lfs is missing in the VM).

Note: clicks were simulated by calling the same code paths (`WorldClickRouter.Click(screenPoint)`, `Button.onClick`), not by real mouse input.

## Screenshots (uncommitted, commit from the Mac with git-lfs)
`Assets/Docs/screenshots/run3b-hud-diorama-1920x1080.png`, `run3b-hud-diorama-1680x1050.png`, `run3b-rover-map.png`,
`run3b-daily-report.png`, `run3b-unexpected-demand.png`, `run3b-space-weather-choice.png`, `run3b-space-weather-radiation.png`,
`run3b-solar-rocks-damage.png`, `run3b-supply-collect.png`, `run3b-zero-oxygen-alert.png`, `run3b-night-transition.png` (+ their `.meta`).

## Not done
- Day 1 mandatory tutorial (GDD §2) - not in this prompt.
- Next Day "pending turn summary" text on the button: skipped, the pills already show the per-day net change.
- No separate radiation look: radiation days use the Storm mood (particles + tint).
- Real mouse/touch input was not exercised (no input injection over MCP).
- `references.md` / `asset-inventory.md` not updated (outside ownership; the DONKI events from Run 3A still need adding).

## Unsure (my picks)
1. Slot mapping: Fuel Reserve = `Electrolyser_H2Generator` (H2 tanks), Water & O2 Plant = `ProcessingPlant`, Greenhouse = `foodFarm`.
   The built buildings are hidden at start, so the base looks emptier on Day 1.
2. Double Bauzer at dawn: Daily Report card carries the line; StoryDirector daily briefings off after Day 1 (supply milestone too).
3. Retracted look = panels squashed to 35% height (no retracted mesh).
4. Supply capsule = the existing `dropship` falling onto the pad; pile = `cargo_A_stacked`.
5. Morning cards wait for the end of dawn; HUD numbers already update at nightfall during the transition.
6. Weather icons are stand-ins: yellow circle (calm), Kenney `emote_swirl` (storm), `emote_alert` (radiation).
7. Rover map pins are screen-space UI pinned over the hot spots, not 3D meshes.
8. Demand card body text "Pick one now. Next Day stays locked until you answer."

## Missing assets
- In-house Sun / Storm / Radiation weather icons.
- A ghost/hologram material for unbuilt slots (only the ring shows now).
- Retracted and broken solar panel meshes (rocks + squash are stand-ins).
- Supply capsule model (dropship stands in).

## Integration notes for Run 4
- Start / end screens: `DayResolutionController.MissionFinished(reason, stars)` and `StoryDirector.MissionEnded` /
  `LeaderboardHandoffRequested` are untouched; the transition skips its dawn phase when the mission is over.
- `GameplayHud.IsWorldInputBlocked` is the single place to add more blockers (e.g. a pause menu).
- `GameplayHud.ShowTip(text, mood)` shows a one-line Bauzer tip from anywhere.
- `DayNightTransitionPlayer.TransitionStarted / TransitionFinished / Skip()` for audio or cutscene hooks.
- Re-run the builder after renaming HUD objects; it finds buildings by name (`solarFarm`, `ProcessingPlant`, `foodFarm`,
  `Electrolyser_H2Generator`, `suplyLandingPad`, `dropship`, `RoverRoutes`).

## Heads-up: someone else is editing this checkout
While this run worked, uncommitted changes appeared in the same working tree that are not from Run 3B:
`_Scripts/Visuals/MoodLighting.cs`, `AmbientMood.cs`, `NightLightPool.cs`, `VisualsConfig.cs`, `_Config/Visuals/VisualsConfig.asset`,
`_Materials/M_Dust.mat`, new `_Materials/M_StormSpark.mat`, `_Prefabs/Base/LampPost.prefab` (storm lighting, rim light, fog,
post-processing, storm particles). Play Mode was also entered by someone else once. I did not commit those files. Because the
open scene had their storm-particle / rim-light edits, the scene commits (`4b33765`, `83db40c`) include those scene changes,
and they reference `M_StormSpark.mat` and a `MoodLighting.rimLight` field that are only in the uncommitted files.
The 10 s transition uses whatever ramp `MoodLighting` does (`VisualsConfig.moodTransitionSeconds` / `stormTransitionSeconds`);
longer ramps there make the phases smoother.
