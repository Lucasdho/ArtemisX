# Run 3A: simulation and NASA DONKI data (report)

Branch `feat/simulation`, created from `develop` in the main checkout (no worktree; the Editor was driven over MCP).
Nothing merged. Scenes, prefabs, `_Scripts/UI/**` and `_Scripts/Visuals/**` were not touched.

## What was done

All rules from `prompts/03a-simulation-prompt.md` (version of 15/11 ~13:38) and `GDD.md` §4–§11 are implemented as
plain C# managers in `Assets/_Scripts/Simulation/` (namespace `ArtemisX.Simulation`). Every number lives in a
ScriptableObject under `Assets/_Config/`. None of the new components are in a scene yet: Run 3B adds and wires them.

### Components (one MonoBehaviour per file, put them all on `Managers`)

| Component | Serialized refs | Main API |
| --- | --- | --- |
| `DayManager` (unchanged) | GameConfig | `CurrentDay`, `AdvanceDay()` |
| `ResourceManager` (extended, same class/API) | GameConfig | `Get/GetMax/Add/Has/TrySpend`, `GetNetRate/SetNetRate`, `IsCrewRestricted` |
| `SpaceWeatherManager` | SpaceWeatherConfig, DayManager, ResourceManager, SolarFieldManager | `TodayWeather`, `TomorrowWeather`, `TodayEntry` (DONKI metadata), `IsEventToday`, `NeedsChoiceToday`, `IsRoverLockedToday`, `CanApplyWeatherChoice(c)`, `ApplyWeatherChoice(c)` |
| `UnexpectedDemandManager` | UnexpectedDemandConfig, ResourceManager, DayManager, SpaceWeatherManager | `TryRollMorningDemand()`, `HasActiveDemand`, `ActiveDemand`, `CanChoose(i)`, `ResolveDemandChoice(i)` |
| `RoverExpeditionManager` | ExpeditionSiteConfig, GameConfig, ResourceManager, SpaceWeatherManager | `Status`, `RoverName`, `CanDispatch(siteId)`, `TryDispatchRover(siteId)`, `CanRepair()`, `TryRepairRover()`, `Config.sites` |
| `SolarFieldManager` | GameConfig, ResourceManager | `State`, `ToggleRetract()`, `CanRepair()`, `TryRepairSolar()`, `PowerPerDay` |
| `BuildingManager` | BuildingConfig, ResourceManager | `BuiltCount`, `Stars`, `NextBuilding`, `IsBuilt(i)`, `CanBuildNext()`, `TryBuildNext()` |
| `SupplyManager` | GameConfig, ResourceManager, DayManager | `HasPendingSupply`, `PendingFood/PendingParts`, `CollectSupply()` |
| `DayResolutionController` | GameConfig, DayManager, ResourceManager, SpaceWeatherManager, RoverExpeditionManager, SolarFieldManager, BuildingManager, SupplyManager, UnexpectedDemandManager, StoryDirector (optional) | `EndDay()`, `CompleteNightResolution()`, `GetPassiveDelta(type)`, `IsResolving`, `IsMissionOver`, `LastReport` |

Every `Can...()` returns an `ActionBlockReason` (`None`, `InputLocked`, `CrewRestricted`, `NotEnoughPower`,
`NotEnoughParts`, `RoverOnMission`, `RoverBroken`, `RoverLockedByRadiation`, `UnknownSite`, `AllBuilt`,
`NothingToRepair`, `NothingToCollect`, `WrongWeather`, `AlreadyChosen`, `NoActiveDemand`) so the UI can grey out a
button and pick the right Bauzer tip ("clicking a locked Rover or Ghost Slot explains why").

### Public events

| Source | Event | Notes |
| --- | --- | --- |
| ResourceManager | `ResourceChanged(ResourceType, value, max)` | also once per resource on Start |
| ResourceManager | `NetRateChanged(ResourceType, perDay)` | passive delta (base + solar + buildings), no supply/rover hauls |
| ResourceManager | `CrewRestrictionChanged(bool, ResourceType)` | type = resource at 0 (Oxygen or Food) |
| DayManager | `DayAdvanced(int)` | unchanged |
| SpaceWeatherManager | `WeatherUpdated(today, tomorrow)` | `WeatherEventType`: Calm / SolarStorm / SolarRadiation |
| SpaceWeatherManager | `StormIncoming(day, WeatherEventType)` | morning of every DONKI event day |
| SpaceWeatherManager | `RoverLockChanged(bool)`, `WeatherChoiceApplied(WeatherChoice)` | |
| UnexpectedDemandManager | `DemandRolled(Demand)`, `DemandResolved(demandId, choiceIndex)` | |
| RoverExpeditionManager | `RoverDispatched(siteId)`, `RoverReturned(siteId, water, parts, isBroken)`, `RoverStatusChanged(RoverStatus)` | `RoverStatus`: Ready / OnMission / Broken / LockedByRadiation |
| RoverExpeditionManager, SolarFieldManager | `ComponentBroken(BaseComponent)`, `ComponentRepaired(BaseComponent)` | `BaseComponent`: Rover / SolarField |
| SolarFieldManager | `SolarStateChanged(SolarFieldState)` | Active / Retracted / Broken |
| BuildingManager | `BuildingCompleted(index)`, `StarsChanged(stars)` | index 0 = Water & O2 Plant |
| SupplyManager | `SupplyDropped(day, food, parts)`, `SupplyCollected(food, parts)` | |
| DayResolutionController | `DayResolvingStarted(WeatherEventType)` | only the visual day-to-night player may subscribe (see below) |
| DayResolutionController | `NightResolved(DailyReportData)` | also on the final night |
| DayResolutionController | `MorningReady(DailyReportData)` | new day started, input unlocked |
| DayResolutionController | `MissionFinished(MissionEndReason, stars)` | `StoryDirector.TriggerMissionEnd` is also called |

### ScriptableObjects

- `Assets/_Config/GameConfig.asset` (updated): start/max/daily base change per resource (Power +5 dome, Water/Oxygen/Food -8,
  Parts 0), `missionLength` 30, `solarFieldPowerPerDay` 20, `solarStormBreakChance` 0.75, `repairPartsCost` 5,
  `supplyInterval` 5, `supplyFood` 30, `supplyParts` 15. The old placeholder `netPerDay` field is gone (rates are computed now).
- `Assets/_Config/Simulation/SpaceWeatherConfig.asset`: 30 days, `overclockShieldsPowerCost` 15, DONKI source name/URL.
- `Assets/_Config/Simulation/ExpeditionSiteConfig.asset`: rover name `Luna`, storm breakdown 100%, the 5 sites (ids match the scene hotspots).
- `Assets/_Config/Simulation/BuildingConfig.asset`: Water & O2 Plant (15 Parts + 20 Power, +10 Water +12 Oxygen), Greenhouse (25 + 25, +12 Food), Fuel Reserve (35 + 30).
- `Assets/_Config/Simulation/UnexpectedDemandConfig.asset`: `dailyChance` 0.27, `firstDay` 2, 4 demands (see "unsure").

### NASA DONKI schedule (please double-check dates at https://kauai.ccmc.gsfc.nasa.gov/DONKI/)

Written from memory, no network in this run.

| Game day | Type | DONKI date | DONKI type | Event |
| --- | --- | --- | --- | --- |
| 4 | Solar Storm | 2015-03-17 | GST | St. Patrick's Day storm, G4 (Kp 8) |
| 9 | Solar Radiation | 2017-09-10 | SEP | X8.2 flare, ground level event GLE 72 |
| 15 | Solar Storm | 2024-05-10 | GST | Gannon storm, G5 (Kp 9) |
| 21 | Solar Radiation | 2021-10-28 | SEP | X1.0 flare, ground level event GLE 73 |
| 27 | Solar Storm | 2023-04-23 | GST | G4 (Kp 8) |

All other days are Calm. These should go into `references.md` (outside this run's ownership).

## Turn sequence as implemented

1. `EndDay()` locks input (rover, solar, buildings, supply) and raises `DayResolvingStarted(todayWeather)`.
   With no subscriber it resolves at once; otherwise the subscriber must call `CompleteNightResolution()` when the
   Daylight -> Storm/Radiation -> Night visuals end. Do not subscribe anything else to that event.
2. Night: Solar Storm -> Active solar field rolls 75% to break, a rover that is out always comes back Broken.
   Solar Radiation -> no damage (the rover can only be out if shields were overclocked). Then passive deltas
   (base + solar + buildings) are applied, clamped to [0, max].
3. End check: all 3 built -> EarlyWin (3 stars); else Day 30 -> DayCap (stars = buildings built). `MissionFinished`
   fires and `StoryDirector.TriggerMissionEnd(reason, stars)` is called. The day does not advance.
4. Otherwise: the rover returns (haul or Broken), `DayManager.AdvanceDay()` (weather, supply and StoryDirector react),
   an Unexpected Demand is rolled (not on Day 1, not on DONKI days), and `MorningReady(DailyReportData)` fires.

`DailyReportData`: `day`, `newDay`, `nightWeather`, `stormHit`, `solarBroken`, `crewRestricted`, `missionEnded`,
`resourceDeltas[]` (after - before, rover haul included), `passiveDeltas[]`, `rover` (returned, siteId, isBroken,
stormHit, water, parts), `supplyArrived/supplyFood/supplyParts`, `todayWeather`, `demandRolled`, and
`bauzerSummaryKey` (`mission_end`, `crew_restricted`, `solar_broken`, `rover_broken`, `supply_arrived`,
`rover_haul`, `storm_safe`, `calm`, in that priority).

## Story hooks (minimal)

- `NextDayFlow`: new optional `dayResolution` field (auto-found in the scene). When present, Next Day calls
  `DayResolutionController.EndDay()` and re-enables on `MorningReady` / final `NightResolved`; the Run 2A evening
  debrief is skipped in that path, as the GDD now reports the night in the morning. Without it, the old path runs.
- `StoryDirector.SetOvernightBaseline(int[])`: the controller passes pre-night resources, so `{power_delta}` etc. in
  the morning briefing show the overnight change.
- `DayResolutionController` calls `StoryDirector.TriggerFirstBuilding()` when building 0 completes and
  `TriggerMissionEnd(reason, stars)` at the end. It no longer calls `TriggerSimulationEvent` (the old placeholder
  storm/broken event cards would clash with the new Weather Choice card).

## Tests (EditMode, `Assets/_Scripts/Simulation/Editor/SimulationTests.cs`)

24/24 passed (Test Runner, EditMode, group `ArtemisX.Simulation.Tests`). They load the real `_Config` assets.
Covered: config numbers, DONKI schedule shape, linear buildings with immediate Parts + Power cost, net rates,
rover immediate Power cost / next-morning return / 100% storm break / calm risk / repair 5 Parts, solar breaks only
when Active in a storm / never Retracted / repair 5 Parts, storm and radiation choices (retract, keep power, lock,
overclock -15 Power), crew restriction on Oxygen or Food 0 blocking build and rover until restored, supply every 5 days,
demands (roll rules, costs, losses, skip), visual-listener handshake, EarlyWin at End Day, DayCap on Day 30, and full
scripted runs over 25 seeds for three player styles.

### Balance finding (DoD item not met)

With the GDD numbers every scripted run wins, but far earlier than the 12–18 target:

| Player style | EarlyWin day (min / median / max over 25 seeds) |
| --- | --- |
| Greedy (far mineral runs, overclocks) | 5 / 6 / 10 |
| Cautious (near sites, shelters) | 6 / 7 / 10 |
| Careless (balanced site, keeps full power in storms) | 7 / 10 / 15 |

The full-run test therefore asserts EarlyWin within 30 days and logs the days (`[Sim balance]` in the console) instead
of asserting 12–18. Main reason: Parts come in fast (start 20, +16 per far run, +15 per supply) against 75 Parts for all
three buildings, and Power refills at +25/day. Possible knobs for Lucas (all in the SOs, none changed): higher building
Parts costs, lower mineral hauls, lower starting Parts, or a lower Parts max.

## Not done

- Nothing is placed or wired in a scene (Run 3B).
- `references.md` not updated with the DONKI events (outside ownership).
- No Play Mode check of the new flow, since no scene has the new components yet. The existing scene still runs the
  Run 2A path (NextDayFlow finds no controller).

## Unsure (my picks, please confirm)

1. Radiation day before the choice is made: the rover counts as locked (safe default) until "Overclock Shields" is picked.
2. "Patch Later (-6 Water or -6 Oxygen)" and "Run Science (+15 Food or +10 Parts)" are split into 4 equally weighted
   demands: Water Leak (-6 Water), Clogged Air Filter (-6 Oxygen), Plant Study (+15 Food), Moon Rock Study (+10 Parts).
   Labels are placeholders in the SO.
3. A demand left unanswered at End Day is dropped with no effect.
4. Crew restriction is live: it turns on whenever Oxygen or Food reaches 0 and off as soon as both are above 0
   (e.g. after collecting supplies), not for a fixed whole turn.
5. A broken rover returns with no haul.
6. "Keep Full Power" also un-retracts a field the player had retracted earlier that day.
7. The solar field stays Retracted after a storm until the player toggles it back.
8. Rover dispatched on a Solar Storm day is allowed (it will surely break); only radiation locks it.
9. DONKI dates come from memory; calm days carry no DONKI date.
10. `StoryDirector.TriggerMissionEnd` clamps DayCap stars to at least 1 (Run 2A); the GDD says 0–2. Left as is.
11. `supplyInterval` / `missionLength` now exist in both `GameConfig` (simulation) and `DialogueConfig` (story/HUD); both are 5 / 30.

## Integration notes for Run 3B

1. Add to `Managers`: `SpaceWeatherManager`, `UnexpectedDemandManager`, `RoverExpeditionManager`, `SolarFieldManager`,
   `BuildingManager`, `SupplyManager`, `DayResolutionController`, and assign the refs in the table above
   (configs from `Assets/_Config/` and `Assets/_Config/Simulation/`). Set `NextDayFlow.dayResolution` (or let it auto-find).
2. Visual day-to-night: subscribe one component to `DayResolutionController.DayResolvingStarted(WeatherEventType)`,
   play `MoodLighting`/`AmbientMood` Storm (or a radiation look) then Night, then call `CompleteNightResolution()`.
   On `MorningReady` switch back to Normal.
3. Morning cards, in order on `MorningReady(report)`: Daily Report (uses `report`), then Unexpected Demand if
   `report.demandRolled` (`demands.ActiveDemand`, buttons -> `ResolveDemandChoice(0/1)`, grey out with `CanChoose`),
   then the Space Weather Choice if `weatherManager.NeedsChoiceToday` (`TodayEntry` gives DONKI date/type/note;
   buttons -> `ApplyWeatherChoice(...)`). Day 1 has no demand; the Day 1 schedule is Calm.
   StoryDirector still enqueues its own morning briefing on `DayAdvanced`; decide whether to keep it or use the
   Daily Report card's Bauzer line (`bauzerSummaryKey`) instead.
4. Rover map pins: use `Config.sites` (`siteId` = `ResourceHotspot.hotspotId`, `powerCost`, `waterReward`, `partsReward`,
   `calmBreakdownChance`), click -> `TryDispatchRover(siteId)`; drive `RoverDriver` from `RoverDispatched` / `RoverReturned`.
5. Solar field card: `ToggleRetract()` / `TryRepairSolar()`; rocks on panels on `ComponentBroken(SolarField)`, clear on `ComponentRepaired`.
6. Ghost slots: `BuildingManager.NextIndex`, `CanBuildNext()`, `TryBuildNext()`, spawn the building on `BuildingCompleted(index)`.
7. Supply pile: show on `SupplyDropped`, click -> `CollectSupply()`.
8. Pills: existing `ResourcePillView` already listens to `ResourceChanged` / `NetRateChanged`; add the red badge on `CrewRestrictionChanged`.
9. Pending-cost hints for End Day: `DayResolutionController.GetPassiveDelta(type)`.
