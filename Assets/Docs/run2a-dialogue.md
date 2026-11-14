# Run 2A — Dialogue & Storytelling Report

## 1. What Passed / What Failed

All Definition of Done checks passed in Play Mode inside `Assets/Scenes/Dev_Dialogue.unity`:

1. **Intro milestone + Day 1 morning briefing on Play**: Passed. Starting Play Mode triggered `DayManager` Day 1, playing `Seq_Intro_[PLACEHOLDER]` (`intro_day1`) followed immediately by `Seq_MorningBriefing_[PLACEHOLDER]` (`briefing_default`).
2. **Typewriter skip / advance (Space & click)**: Passed. First tap while typing completed the active line (`6/72` -> `72/72` chars) without advancing; `Space` and clicks advanced lines (`Speaker B` -> `Speaker A`) and closed cleanly after the final line.
3. **Next Day flow (debrief -> advance once -> briefing)**: Passed. Clicking `NextDayButton` played `Seq_EveningDebrief_[PLACEHOLDER]` (`debrief_default`) while still on Day 1, called `DayManager.AdvanceDay()` once on completion (`Day 1` -> `Day 2`, `DayCounter` updated to `Day 2`), and immediately played Day 2's morning briefing (`briefing_default`).
4. **Event card & choice event**: Passed. Clicking `TriggerEventButton` opened `Seq_EventCard_Storm_[PLACEHOLDER]` (`event_storm_placeholder`) with 2 choice buttons (`Retract Panels`, `Keep Deployed`). Picking `Retract Panels` raised `DialogueRunner.ChoiceMade("event_storm_placeholder", "retract_panels")` once, logged `[Dialogue] ChoiceMade: event_storm_placeholder -> retract_panels`, and closed the dialogue box (`IsOpen = False`).
5. **Input blocking while dialogue is open**: Passed. `NextDayFlow` set `NextDayButton.interactable = false` whenever `DialogueRunner.IsOpen` was true, and `DialogueCanvas`'s full-screen `InputBlocker` absorbed background clicks before they could reach underlying UI.
6. **Layout readability at `1920x1080` and `1680x1050`**: Passed. Verified and captured:
   - `Assets/Docs/screenshots/run2a-dialogue-1920x1080.png` (`1920x1080`)
   - `Assets/Docs/screenshots/run2a-dialogue-1680x1050.png` (`1680x1050`)
   Both render crisp portraits on the left, speaker name banner, body text, and 2 choice buttons with zero clipping and clear clearance left of the bottom-right `NextDayButton`.
7. **Console cleanliness**: Passed. Zero compiler errors, zero runtime errors, and zero warnings from `Assets/_Scripts/Story/**` or `Assets/_Scripts/UI/Dialogue/**` in Unity console checks (`console get --level warn` showed only a pre-existing Unity QuickSearch `SearchInit.IndexationOnStartup` exception and pre-existing Quaternius `.blend` / `MCP-FOR-UNITY` port messages outside our code).
8. **Environment / CLI setup note**: Running `unity setup antigravity` on Unity CLI `1.0.0-beta.13` exited with code 2 (`unknown command 'antigravity'`), so Antigravity's installed Unity plugin skills (`unity-cli`, `ui-ugui`) were used instead. All edits remained strictly inside the Run 2A file-ownership boundaries on `feat/dialogue`.

## 2. Exact Steps to Wire Into `Assets/Scenes/Base.unity`

1. Select `Managers` in `Assets/Scenes/Base.unity` and add these three components from `Assets/_Scripts/Story/`:
   - `DialogueRunner`
   - `StoryDirector`
   - `NextDayFlow`
2. On `DialogueRunner`:
   - Assign `config` -> `Assets/_Config/Story/DialogueConfig.asset`.
3. On `StoryDirector`:
   - Assign `dayManager` -> `Managers` (`DayManager`)
   - Assign `runner` -> `Managers` (`DialogueRunner`)
   - Assign `schedule` -> `Assets/_Config/Story/StorySchedule.asset`
4. On `NextDayFlow`:
   - Assign `dayManager` -> `Managers` (`DayManager`)
   - Assign `storyDirector` -> `Managers` (`StoryDirector`)
   - Assign `dialogueRunner` -> `Managers` (`DialogueRunner`)
   - Assign `nextDayButton` -> `HUD/NextDayButton` (`Button`)
5. Drag `Assets/_Prefabs/UI/DialogueCanvas.prefab` into the root hierarchy of `Assets/Scenes/Base.unity`, then on its `DialogueBoxView` component assign:
   - `runner` -> `Managers` (`DialogueRunner`)
6. Select `HUD/NextDayButton` in `Assets/Scenes/Base.unity` and update `Button -> On Click ()`:
   - Remove the existing persistent listener targeting `Managers -> DayManager.AdvanceDay`.
   - Add a persistent listener targeting `Managers -> NextDayFlow.OnNextDayClicked`.
7. **Milestone / Event / World-input hooks for other runs**:
   - World-click, building-placement, and camera controllers should check `dialogueRunner.IsOpen` (and/or `EventSystem.current.IsPointerOverGameObject()`) to stay paused while dialogue is open.
   - Hook `storyDirector.TriggerFirstBuilding()`, `storyDirector.TriggerSupplyShipArrived()`, `storyDirector.TriggerMissionEnd()`, and `storyDirector.TriggerEventCard(sequence)` at the corresponding simulation events.
   - Subscribe to `dialogueRunner.ChoiceMade` (`Action<string, string>` receiving `sequenceId, choiceId`) to apply gameplay consequences when event-card mechanics are implemented.

## 3. What We Are Unsure About & Missing Assets

- **Missing character portrait art**: Using placeholder Kenney emote sprites copied into `Assets/_UI/Dialogue/portrait_speaker_a.png` and `Assets/_UI/Dialogue/portrait_speaker_b.png`; real character portraits (and any alternate expression variants) are still needed.
- **Unassigned milestone sequences**: `StorySchedule.asset` currently leaves `firstBuildingMilestone`, `supplyShipArrivedMilestone`, and `missionEndMilestone` null until copy and speakers are decided (the `Trigger*` methods on `StoryDirector` are already wired and safely no-op when unassigned).
- **World-space input pause**: `DialogueCanvas` blocks all UI pointer raycasts via `InputBlocker`, and `NextDayFlow` disables `HUD/NextDayButton` while open, but any future scripts reading keyboard/mouse actions directly from the Input System must check `DialogueRunner.IsOpen`.
- **Event card gameplay effects**: Per spec, choice selection fires `DialogueRunner.ChoiceMade(sequenceId, choiceId)` with zero simulation side effects; wiring consequences into `ResourceManager` or `DayManager` remains for a follow-up run.

## 4. Storytelling Design Questions for Lucas

1. **Speaker Roster & Identities**: Who are the recurring crew/AI characters (names, roles, callsigns, accent colors), and does each speaker need multiple mood/expression portraits?
2. **Tone & Reading Level**: For the 9-99 target audience, should dialogue lean playful arcade-sci-fi or grounded NASA-mission-control, and what max line length / reading cadence feels right?
3. **Morning Briefing Structure**: Should Days 1-20 each have an authored entry in `StorySchedule.dayBriefings`, or should only key plot days be authored while regular days pull from a non-repeating pool?
4. **Evening Debrief Content**: Should evening debriefs summarize the day's actual resource deltas / events before `AdvanceDay()`, or serve as short reflective end-of-day sign-offs?
5. **Event Card Trigger Rules & Consequences**: How often should event cards trigger (e.g., chance per morning, mid-day popup, or scripted days), should cards offer 2 or 3 choices, and can choices modify resources, unlock buildings, or queue follow-up lines?
6. **Milestone Definitions**: What exact thresholds trigger `TriggerSupplyShipArrived()` (e.g., fixed day interval vs. beacon built) and `TriggerMissionEnd()` (survival day target vs. victory objective), and should milestones wait in queue after active briefings or interrupt immediately?
