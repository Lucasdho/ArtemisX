# Run 2A — Dialogue & Storytelling Report

## 1. What Passed / What Failed

All Definition of Done checks and finalized storytelling requirements passed in Play Mode inside `Assets/Scenes/Dev_Dialogue.unity`:

1. **Bauzer Roster, Mood Portraits & Player Name**: Passed. Starting Play Mode showed `Bauzer` with `portrait_bauzer_happy`, `{playerName}` substituted cleanly (`"Commander"`, and verified live updates such as `"Lucas"`), and mood portrait switching (`portrait_bauzer_happy` -> `portrait_bauzer_neutral` -> `portrait_bauzer_worried`). All lines stayed within `<= 110` characters and `<= 2` lines per beat.
2. **Intro + Day 1 Briefing + Day Cap HUD**: Passed. On Play, `intro_day1` (`Seq_Intro_[PLACEHOLDER]`) played first, followed by `briefing_day1` (`Seq_Briefing_Day1_[PLACEHOLDER]`) explaining the 30-day mission cap, while `TestHUD/TopBar/DayCounter` displayed `"Day 1 / 30"` via `MissionDayCapView`.
3. **Typewriter Skip / Advance (Click & Space)**: Passed. First UI tap while typing completed the active line (`6/76` -> `76/76` chars) without advancing; UI clicks and Input System `Space` key presses advanced lines and closed cleanly after the last line.
4. **Next Day Flow & Live Evening Debriefs**: Passed. Clicking `NextDayButton` played `debrief_normal` (`Seq_EveningDebrief_[PLACEHOLDER]`) while on Day 1, called `DayManager.AdvanceDay()` once (`Day 1 / 30` -> `Day 2 / 30`), and selected Day 2's briefing from `okBriefingPool` (`pool_ok_2`). After choosing a storm action (`-10 Power`), the Day 2 -> Day 3 debrief picked `debrief_event_resolved` with live `{power_delta}` (`-10`), `{event_name}` (`dust storm`), and `{outcome_summary}` trade-off sentence. Day 3 -> Day 4 under low Power (`10 <= 25`) selected `lowPowerBriefingPool` (`pool_low_power_2`).
5. **Event Cards (2 Choices, Simulation Events, Max 1/Day) & Input Blocking**: Passed. `NextDayButton` stayed disabled (`interactable = false`) and `NextDayFlow.OnNextDayClicked()` returned early whenever dialogue was open, while `DialogueCanvas/DialogueRoot/InputBlocker` blocked background pointer raycasts. `StormIncoming` played `briefing_first_storm` then `event_storm_placeholder` with 2 choice buttons (`Retract (-10 Power)`, `Ride Out (-10 Parts)`); a second simulation event (`ComponentBroken`) on the same day was rejected (`max 1 per day`). Choosing `Retract (-10 Power)` raised `ChoiceMade` once, logged `[Dialogue] ChoiceMade: event_storm_placeholder -> retract_panels`, reduced `Power` (`60 -> 50`), and closed the box. `ComponentBroken` was verified on Day 5 (`event_broken_placeholder`), clearing `HasBrokenComponent` when patched with Parts (`[Dialogue] ChoiceMade: event_broken_placeholder -> repair_with_parts`).
6. **Auto Supply Ship, Slot Queue Order & Post-Debrief Leaderboard Handoff**: Passed. Day 5 (`day % supplyInterval == 0`, default `supplyInterval = 5`) automatically queued `briefing_supply_day` followed by `milestone_supply_ship`. Enqueuing `MissionEnd` (`slot 50`) before `SupplyArrival` (`slot 30`) while `MorningBriefing` (`slot 10`) was open played them in strict order `milestone_first_building -> milestone_supply_ship -> milestone_end_early_win`. `NicknameFilter` truncated >12-char names and rejected `@`/personal-data inputs. `StoryDirector.LeaderboardHandoffRequested` did **not** fire while `milestone_end_early_win` was open, firing once after it closed with `MissionRunRecord` (`nickname=Lucas, character=astronaut_3, daysTaken=5, stars=3, endReason=EarlyWin`) and logging `[Dialogue] LeaderboardHandoff: nickname=Lucas, character=astronaut_3, days=5, stars=3, reason=EarlyWin`. Day-30 hard-cap guards in `StoryDirector.PlayDebriefThen` and `NextDayFlow` keep `DayCounter` capped at `Day 30 / 30` and queue `DayCap` debrief after the Day 30 evening debrief.
7. **Layout Readability at `1920x1080` and `1680x1050`**: Passed. Re-captured with Bauzer's worried portrait, `"BAUZER"` banner, `"DAY 2 / 30"`, and 2 choice buttons:
   - `Assets/Docs/screenshots/run2a-dialogue-1920x1080.png` (`1920x1080`)
   - `Assets/Docs/screenshots/run2a-dialogue-1680x1050.png` (`1680x1050`)
8. **Console & Environment Notes**: Passed — zero compiler/runtime errors or warnings from `Assets/_Scripts/Story/**` or `Assets/_Scripts/UI/Dialogue/**` (console only showed a pre-existing Unity `SearchInit.IndexationOnStartup` exception and `MCP-FOR-UNITY` port log). On CLI `1.0.0-beta.13`, `unity setup antigravity` exited `2` (`unknown command 'antigravity'`), so installed Unity plugin skills were used.

## 2. Exact Steps to Wire Into `Assets/Scenes/Base.unity`

1. Select `Managers` in `Assets/Scenes/Base.unity` and add from `Assets/_Scripts/Story/`:
   - `DialogueRunner`
   - `StoryDirector`
   - `NextDayFlow`
2. On `DialogueRunner`:
   - Assign `config` -> `Assets/_Config/Story/DialogueConfig.asset`.
3. On `StoryDirector`:
   - Assign `dayManager` -> `Managers` (`DayManager`)
   - Assign `resourceManager` -> `Managers` (`ResourceManager`)
   - Assign `runner` -> `Managers` (`DialogueRunner`)
   - Assign `schedule` -> `Assets/_Config/Story/StorySchedule.asset`
4. On `NextDayFlow`:
   - Assign `dayManager` -> `Managers` (`DayManager`)
   - Assign `storyDirector` -> `Managers` (`StoryDirector`)
   - Assign `dialogueRunner` -> `Managers` (`DialogueRunner`)
   - Assign `nextDayButton` -> `HUD/NextDayButton` (`Button`)
5. Drag `Assets/_Prefabs/UI/DialogueCanvas.prefab` into the root hierarchy of `Assets/Scenes/Base.unity`, then on its `DialogueBoxView` component assign:
   - `runner` -> `Managers` (`DialogueRunner`)
6. Select `HUD/NextDayButton` in `Assets/Scenes/Base.unity`:
   - Replace the existing `Button -> On Click ()` listener targeting `Managers -> DayManager.AdvanceDay` with `Managers -> NextDayFlow.OnNextDayClicked`.
7. Select `HUD/TopBar/DayCounter` (same object holding `DayCounterView`) and add `MissionDayCapView` (`Assets/_Scripts/UI/Dialogue/`):
   - Assign `days` -> `Managers` (`DayManager`)
   - Assign `config` -> `Assets/_Config/Story/DialogueConfig.asset`
   - Assign `label` -> `DayCounter` (`Text`)
   (Result: HUD shows `"Day X / 30"` at all times without modifying `DayCounterView.cs`.)
8. **Hooks for other systems (Start Screen, Simulation, World Input, Leaderboard)**:
   - **Start Screen**: call `dialogueRunner.SetPlayerName(rawInput)` (automatically sanitized by `NicknameFilter` to `<= 12` chars, profanity-filtered, no personal data) and `dialogueRunner.SetPlayerCharacter(characterId)`.
   - **World Input**: guard camera/placement/world clicks on `dialogueRunner.IsOpen`.
   - **Simulation Events & Milestones**: call `storyDirector.TriggerSimulationEvent(SimulationEventType.StormIncoming)` or `ComponentBroken` (capped at 1/day), `storyDirector.TriggerFirstBuilding()`, and at end-of-day resolve `storyDirector.CheckEndOfDayMissionEnd(achievedGoalsCount)` (or `storyDirector.TriggerMissionEnd(MissionEndReason.EarlyWin | DayCap, achievedGoalsCount)`).
   - **Leaderboard Handoff**: subscribe the Leaderboard UI controller to `storyDirector.LeaderboardHandoffRequested` (`Action<MissionRunRecord>`). It fires only **after** Bauzer's final mission-end debrief finishes.

## 3. Leaderboard & Design Handoff Spec + Missing Assets

- **Leaderboard Scope (handled by Leaderboard UI / backend integration outside `Assets/_Scripts/Story/`)**:
  - Receives `MissionRunRecord` (`nickname`, `character`, `daysTaken` 1..30, `stars` 1..3, `endReason` `EarlyWin | DayCap`) from `StoryDirector.LeaderboardHandoffRequested`.
  - Online storage: Supabase REST API via `UnityWebRequest` (WebGL-safe, publishable anon key only, insert + read only; rank by fewest `daysTaken`, then highest `stars`).
  - Bar graph UI: `x` = day the run ended (`1..30`) + separate `"Mission Extended"` bar for `DayCap` runs; `y` = number of runs; player's bar highlighted.
  - Network failure fallback: never block the game or end flow; display the player's own `MissionRunRecord` and `"Leaderboard offline"`.
- **Missing / Placeholder Assets**:
  - Bauzer's final accent color pick (`[Lucas to pick]`) in `Assets/_Config/Story/Speaker_A_[PLACEHOLDER].asset` (currently `#3DB8F5`).
  - Final Bauzer portrait sprites for her 3 moods (`Assets/_UI/Dialogue/portrait_bauzer_neutral.png`, `portrait_bauzer_happy.png`, `portrait_bauzer_worried.png`) and the 4 selectable astronaut portraits (currently Kenney vector emote placeholders).
  - Final copywriting pass to replace `[PLACEHOLDER]` tags across the 22 `DialogueSequence` assets in `Assets/_Config/Story/`, plus Supabase URL / publishable anon key for the online leaderboard client.
