# Run 2A — Dialogue & Storytelling Report and Remaining Requirements

## 1. What Passed / What Failed

All Definition of Done checks and finalized storytelling requirements passed in Play Mode inside `Assets/Scenes/Dev_Dialogue.unity`, and the wired scene flow passed in Play Mode inside `Assets/Scenes/Base.unity`:

1. **Base Scene Play Mode Verification (`Assets/Scenes/Base.unity`)**: Passed. On Play, `Managers` (`DialogueRunner`, `StoryDirector`, `NextDayFlow`) automatically played `intro_day1` then `briefing_day1`, `HUD/TopBar/DayCounter` (`MissionDayCapView`) displayed `"Day 1 / 30"`, `HUD/NextDayButton` stayed disabled (`interactable = false`) while open, and clicking `HUD/NextDayButton` (`NextDayFlow.OnNextDayClicked`) played `debrief_normal` before advancing once to `"Day 2 / 30"` and queuing `pool_ok_2`. Zero compiler or runtime errors/warnings from `Assets/_Scripts/Story/**` or `Assets/_Scripts/UI/Dialogue/**`.
2. **Bauzer Roster, Mood Portraits & Player Name (`Dev_Dialogue.unity`)**: Passed. Starting Play Mode showed `Bauzer` with `portrait_bauzer_happy`, `{playerName}` substituted cleanly (`"Commander"`, and verified live updates such as `"Lucas"`), and mood portrait switching (`portrait_bauzer_happy` -> `portrait_bauzer_neutral` -> `portrait_bauzer_worried`). All lines stayed within `<= 110` characters and `<= 2` lines per beat.
3. **Intro + Day 1 Briefing + Day Cap HUD (`Dev_Dialogue.unity`)**: Passed. `intro_day1` (`Seq_Intro_[PLACEHOLDER]`) played first, followed by `briefing_day1` (`Seq_Briefing_Day1_[PLACEHOLDER]`) explaining the 30-day mission cap, while `DayCounter` displayed `"Day 1 / 30"`.
4. **Typewriter Skip / Advance (Click & Space)**: Passed. First UI tap while typing completed the active line without advancing; UI clicks and Input System `Space` key presses advanced lines and closed cleanly after the last line.
5. **Next Day Flow & Live Evening Debriefs**: Passed. Clicking `NextDayButton` played `debrief_normal` while on Day 1, called `DayManager.AdvanceDay()` once (`Day 1 / 30` -> `Day 2 / 30`), and selected Day 2's briefing from `okBriefingPool` (`pool_ok_2`). After choosing a storm action (`-10 Power`), Day 2 -> Day 3 picked `debrief_event_resolved` with live `{power_delta}` (`-10`), `{event_name}` (`dust storm`), and `{outcome_summary}` trade-off sentence. Day 3 -> Day 4 under low Power (`10 <= 25`) selected `lowPowerBriefingPool` (`pool_low_power_2`).
6. **Event Cards (2 Choices, Simulation Events, Max 1/Day) & Input Blocking**: Passed. `NextDayButton` stayed disabled (`interactable = false`) and `NextDayFlow.OnNextDayClicked()` returned early whenever dialogue was open, while `DialogueCanvas/DialogueRoot/InputBlocker` blocked background pointer raycasts. `StormIncoming` played `briefing_first_storm` then `event_storm_placeholder` with 2 choice buttons (`Retract (-10 Power)`, `Ride Out (-10 Parts)`); a second simulation event (`ComponentBroken`) on the same day was rejected (`max 1 per day`). Choosing `Retract (-10 Power)` raised `ChoiceMade` once, logged `[Dialogue] ChoiceMade: event_storm_placeholder -> retract_panels`, reduced `Power` (`60 -> 50`), and closed the box. `ComponentBroken` cleared `HasBrokenComponent` when patched with Parts (`[Dialogue] ChoiceMade: event_broken_placeholder -> repair_with_parts`).
7. **Auto Supply Ship, Slot Queue Order & Post-Debrief Leaderboard Handoff**: Passed. Day 5 (`day % supplyInterval == 0`, default `supplyInterval = 5`) automatically queued `briefing_supply_day` followed by `milestone_supply_ship`. Enqueuing `MissionEnd` (`slot 50`) before `SupplyArrival` (`slot 30`) while `MorningBriefing` (`slot 10`) was open played them in strict order `milestone_first_building -> milestone_supply_ship -> milestone_end_early_win`. `NicknameFilter` truncated >12-char names and rejected `@`/personal-data inputs. `StoryDirector.LeaderboardHandoffRequested` fired once after `milestone_end_early_win` closed with `MissionRunRecord` (`nickname=Lucas, character=astronaut_3, daysTaken=5, stars=3, endReason=EarlyWin`). Day-30 hard-cap guards in `StoryDirector.PlayDebriefThen` and `NextDayFlow` keep `DayCounter` capped at `Day 30 / 30` and queue `DayCap` debrief after the Day 30 evening debrief.
8. **Layout Readability at `1920x1080` and `1680x1050`**: Passed. Screenshots captured in `Dev_Dialogue.unity`:
   - `Assets/Docs/screenshots/run2a-dialogue-1920x1080.png` (`1920x1080`)
   - `Assets/Docs/screenshots/run2a-dialogue-1680x1050.png` (`1680x1050`)
9. **Console & Environment Notes**: Passed — zero compiler/runtime errors or warnings from `Assets/_Scripts/Story/**` or `Assets/_Scripts/UI/Dialogue/**` (Editor console only showed pre-existing Unity `SearchInit.IndexationOnStartup` and `MCP-FOR-UNITY` startup logs). On CLI `1.0.0-beta.13`, `unity setup antigravity` exited `2` (`unknown command 'antigravity'`), so installed Unity plugin skills were used.

## 2. `Assets/Scenes/Base.unity` Wiring & Branch Merge Note

`Assets/Scenes/Base.unity` is **now wired on `feat/dialogue`** as follows:
1. `Managers` has `DialogueRunner` (`config` -> `Assets/_Config/Story/DialogueConfig.asset`), `StoryDirector` (`dayManager`, `resourceManager`, `runner` -> `Managers`, `schedule` -> `Assets/_Config/Story/StorySchedule.asset`), and `NextDayFlow` (`dayManager`, `storyDirector`, `dialogueRunner` -> `Managers`, `nextDayButton` -> `HUD/NextDayButton`).
2. `Assets/_Prefabs/UI/DialogueCanvas.prefab` is instantiated at root with `DialogueBoxView.runner` -> `Managers` (`DialogueRunner`).
3. `HUD/NextDayButton` persistent `Button.onClick` listener replaces `DayManager.AdvanceDay` with `NextDayFlow.OnNextDayClicked`.
4. `HUD/TopBar/DayCounter` has `MissionDayCapView` (`days` -> `Managers`, `config` -> `DialogueConfig.asset`, `label` -> `DayCounter` `Text`), `sizeDelta.x = 360`, initial text `"Day 1 / 30"` (without modifying `DayCounterView.cs`).

> **Important Merge Note (`feat/dialogue` + `feat/living-base`)**: `feat/living-base` heavily rebuilt `Assets/Scenes/Base.unity` (leaving `Managers`, `HUD`, `HUD/NextDayButton`, and `HUD/TopBar/DayCounter` intact). When merging the two branches, **take `feat/living-base`'s `Base.unity` and re-apply the 4 steps above** instead of YAML-merging `Base.unity`.

## 3. Remaining Requirements for Next Agent

Everything below lives outside Run 2A's already-complete dialogue core (`Assets/_Scripts/Story/**`, `Assets/_Scripts/UI/Dialogue/**`) and still needs implementation/replacement in subsequent runs:

### A. Locked Narrative Rules (Preserve When Editing Content or Screens)
- **Roster**: `Bauzer` (named after Claudia Maria Bauzer Medeiros) is the **only** guide (tutorials, briefings, debriefs, event cards, and supply ship arrival announcements). No separate Mission Control character, no supply ship pilot, and rovers keep their names and never speak.
- **Player Speaker**: Chosen from 4 astronauts on the start screen (single portrait per astronaut, no mood variants; lines rare and mostly from event card choices). Name typed on the start screen is substituted via `{playerName}` in key beats (Day 1, supply day, mission end).
- **Copywriting Constraints (`DialogueConfig`)**: Playful, clear, kids' mobile-game style; max `~110` characters per line (`maxCharactersPerLine = 110`), max `2` lines per beat (`maxLinesPerBeat = 2`). Bauzer is friendly and a little nerdy, never scary.
- **Queue & Day Cadence**:
  - `SupplyInterval`: every `5` days (`day % supplyInterval == 0`, configurable in `DialogueConfig.asset`).
  - `MissionLength`: `30` days (`DialogueConfig.asset`, hard cap, no extensions).
  - Never interrupt active dialogue; within a day, slots strictly order `Intro -> MorningBriefing -> EventCard -> SupplyArrival -> EveningDebrief -> MissionEnd`.
  - Inform player of the 30-day cap in Day 1 briefing, permanently on the HUD (`"Day X / 30"`), and via Bauzer's Day 25 warning.

### B. Pending Art & Copy Assets
1. **Bauzer Accent Color (`[Lucas to pick]`)**: Update `accentColor` on `Assets/_Config/Story/Speaker_A_[PLACEHOLDER].asset` (currently placeholder cyan `#3DB8F5`).
2. **Final Portrait Sprites**:
   - Replace the 3 placeholder Bauzer mood sprites (`Neutral`, `Happy`, `Worried`):
     - `Assets/_UI/Dialogue/portrait_bauzer_neutral.png`
     - `Assets/_UI/Dialogue/portrait_bauzer_happy.png`
     - `Assets/_UI/Dialogue/portrait_bauzer_worried.png`
   - Provide final sprites for the **4 selectable astronauts** and connect the chosen character portrait onto `Assets/_Config/Story/Speaker_Player_[PLACEHOLDER].asset` (or map `characterId -> Sprite`).
3. **Final Dialogue Copy**: Replace `[PLACEHOLDER]` text across all 22 `DialogueSequence` assets in `Assets/_Config/Story/` while keeping existing tokens intact (`{playerName}`, `{day}`, `{mission_length}`, `{supply_interval}`, `{event_name}`, `{outcome_summary}`, `{power_delta}`, `{water_delta}`, `{oxygen_delta}`, `{food_delta}`, `{parts_delta}`, `{choice_label}`).
4. **Authored Debrief & Event Rules**: Keep one authored debrief template per outcome (`Normal`, `EventResolved`, `ResourceLow`, `ComponentBroken`) displaying live numbers and stating the trade-off in one sentence; keep event cards at 2 choices using only the 5 existing `ResourceType` values (`Power`, `Water`, `Oxygen`, `Food`, `Parts`) and the broken-component mechanic (`fixesBrokenComponent`).

### C. Pending Gameplay & Start-Screen Hooks
- **Start Screen**:
  - Let the player pick one of the 4 astronauts (`astronaut_1..4`) and type a nickname (`max 12 chars`).
  - Call `dialogueRunner.SetPlayerName(rawInput)` (internally sanitized by `NicknameFilter.Sanitize`: strips control chars, rejects `@` and >=7 digits/personal data and profanity, trims/truncates to `<= 12` chars, defaults to `"Commander"`) and `dialogueRunner.SetPlayerCharacter(characterId)`.
- **World Input Guard**: Skip camera drag/zoom and building-slot clicks whenever `dialogueRunner != null && dialogueRunner.IsOpen`.
- **Simulation Hooks (to `StoryDirector`)**:
  - Call `storyDirector.TriggerSimulationEvent(SimulationEventType.StormIncoming)` or `SimulationEventType.ComponentBroken` when the simulation rolls a storm or breaks a component (automatically enforces max 1 event card/day and triggers first-time briefing intro lines).
  - Call `storyDirector.TriggerFirstBuilding()` when the first building is constructed.
  - At the end of each day (after resource/goal resolution), call `storyDirector.CheckEndOfDayMissionEnd(achievedGoalsCount)` (fires `MissionEndReason.EarlyWin` when `achievedGoalsCount >= 3`, or `MissionEndReason.DayCap` at Day 30). Note: the `DayCap` screen shows `1..3` stars and Bauzer's `"mission extended"` debrief when all 3 self-sufficiency goals were not met.

### D. Pending Online Leaderboard Screen & Supabase Client
- **Handoff Subscription**: Subscribe the Leaderboard UI controller to `StoryDirector.LeaderboardHandoffRequested` (`event Action<MissionRunRecord>`). The dialogue system fires this **only after** Bauzer's final mission-end debrief finishes closing.
- **Run Record Payload (`ArtemisX.Story.MissionRunRecord`)**:
  - `nickname` (`string`, `<= 12` chars, profanity-filtered, no personal data)
  - `character` (`string`, chosen astronaut ID)
  - `daysTaken` (`int`, `1..30`)
  - `stars` (`int`, `1..3`)
  - `endReason` (`MissionEndReason`: `EarlyWin` | `DayCap`)
- **Backend & Networking**:
  - **Supabase REST API** via `UnityWebRequest` (WebGL-safe; no blocking sockets or worker threads).
  - Use a **publishable anon key only** (`apikey` + `Authorization: Bearer <publishable_key>` headers, configured via an `Assets/_Config` ScriptableObject alongside the Supabase project URL and table name).
  - Operations: **insert + read only** (`POST` current `MissionRunRecord`, `GET` existing records). **No fake/mock seed data** in storage or UI.
  - Ranking rule: sort by fewest `daysTaken` first, then highest `stars`.
- **Bar Graph Visualization**:
  - `x`-axis: day the run ended (`1..30`) for `EarlyWin` runs, plus a separate `"Mission Extended"` bar for `DayCap` runs.
  - `y`-axis: number of runs per bucket.
  - Visually highlight the current player's bar (`daysTaken` bar for `EarlyWin`, or the `"Mission Extended"` bar for `DayCap`).
- **Offline / Error Handling**:
  - Never block gameplay or end-of-mission UI waiting on the network.
  - On timeout, offline state, or HTTP error, still display the player's own `MissionRunRecord` result (stars, days/extended status) alongside `"Leaderboard offline"`.
