# Run 5: final review (Reviewer)

Checked on `develop` @ `8a65373` in Lucas's open Editor (Unity 6000.3.6f1), scene `Assets/Scenes/HandMadeBase.unity`, Game view 2214x1024.
Judged against `GDD.md`. Clicks were simulated through the same code paths the UI uses (`Button.onClick`, `WorldClickRouter.Click(screenPoint)`,
`DialogueRunner.AdvanceOrSkip`), not real mouse input. The Editor was unfocused, so Play Mode was pumped from an editor-update hook: real-time
lengths (10 s transition, intro orbit) could not be timed. To reach the win quickly, +30 Parts were added at runtime on Day 6 and one storm
break was forced through `SolarFieldManager.ResolveStorm()` with a fixed roll. Scene not saved, Play Mode stopped.

## 1. Passes

- Start screen: 4 astronauts (NOVA, LEO, MAYA, KAI), callsign capped at 12, filter rejects profanity, emails and digit-only names (falls back to "Commander"). LAUNCH MISSION works; the chosen portrait and name show in dialogue.
- Intro: `Seq_Intro` plays (5 Bauzer beats + 1 player line), HUD hidden and world input blocked until it ends. No `[PLACEHOLDER]` text seen anywhere.
- Day 1 onboarding: step 1 (weather widget, then solar click), step 2 (rover dispatch), step 3 (Next Day), then "Training done" on Day 2. Next Day stays locked until step 3.
- HUD: pills start at Power 60 (+25), Water 40 (-8), O2 70 (-8), Food 50 (-8), Parts 20 (0); Today/Tomorrow weather widget; Day X; supply countdown; bottom-right Next Day, Rover status, `?`. Bottom buttons hide while Bauzer talks.
- Solar field: Retract changes the Power rate +25 -> +5. A retracted field redeploys at dawn ("Solar panels deployed again"). Storm break: 4 rocks on the panels, Power rate +5; Repair (5 Parts) clears them and sets the field back to Active.
- Rover map: top-down view, 5 pins with the right ids and GDD numbers (-10/+18, -18/+32, -10/+8, -18/+16, -14/+16/+8), risk % (shows 30% on storm days). A pin click dispatches at once (60 -> 50 Power), the rover returns next morning with its haul. Out during the Day 4 storm, it came back intact (30% rule; the 100% rule is gone).
- Morning order: Daily Report (icons, deltas, Bauzer line, Start Day) -> Unexpected Demand -> DONKI choice. The Day 4 card shows the real St. Patrick's Day 2015 storm with Shield & Retract / Keep Full Power.
- Unexpected Demand blocks Next Day: button not interactable, `CanEndDay() = DemandPending`, a forced `OnNextDayClicked()` doesn't advance the day. Answering unlocks it.
- Buildings: built strictly in order 1 -> 2 -> 3; out-of-order click gives the Bauzer "build in order" tip. Parts + Power are taken at once (28/55 -> 13/35), the building appears, the star lights, and the net rates update (Water +2, O2 +4, Food +4).
- Supply on Day 5: shown in the Daily Report; the pile click gives +30 Food and +15 Parts.
- Zero O2: pulsing red `!` badge on the O2 pill, rover button shows LOCKED, ghost slot click gives a Bauzer tip, no game over. Restored as soon as O2 > 0.
- Storm transition visuals (particles, tint, lamps, Skip button, "Tap anywhere to skip") look strong on screen. `transitionSeconds = 10`.
- EarlyWin: Next Day after the 3rd building -> `Seq_MissionEnd_EarlyWin` -> MISSION COMPLETE, 3 stars, Bauzer grade line, histogram with the player's bar, "Leaderboard offline. Showing your run only." PLAY AGAIN reloads (Day 1, 0 stars, Power 60, start screen prefilled, "welcome back" line).
- Console during the whole playthrough: 0 errors and 0 warnings from game scripts (only MCP-FOR-UNITY port notices and URP "memoryless" info lines).
- `references.md` lists all packs, fonts, sounds, DOTween, AI assets, NASA DONKI/CCMC with the 5 dates, Supabase, and Dr. Claudia Maria Bauzer Medeiros. The 5 DONKI events match well-known real events (2015-03-17 G4, 2017-09-10 X8.2/GLE 72, 2024-05-10 G5, 2021-10-28 X1.0/GLE 73, 2023-04-23 G4).
- EditMode tests: 30/31 pass (all 27 simulation tests green).

## 2. Fails (ordered by harm to the demo video / judging)

1. **Clicking the rover mostly opens the wrong thing.** At the default diorama camera, 105 of 125 sample rays aimed at the rover hit the invisible Greenhouse ghost-slot collider first, so a click on the rover usually shows Bauzer's "Build in order..." tip instead of the rover map. Onboarding step 2 literally says "Tap the rover". Once the Greenhouse is built, its model also sits on top of the rover visually.
   Repro: Play -> launch -> skip/advance intro -> step 2 -> click the middle of the rover (`ExplorationHover`). Cause: `foodFarm` box collider (center ~(6.9, 1.4, -4.5), size ~5.1 x 2.3 x 5.1) sits between `CM_BaseCamera` and `ExplorationHover` (8.65, 0.28, -1.88). The HUD Rover button works (that's what this review used).
   Fix options: shrink/disable unbuilt slot colliders (`GhostSlotInteractable`), or move the rover's parking spot away from the Greenhouse slot.
2. **Missions checklist (left side) has unreadable text.** `MissionCard` Image is white (1,1,1) and its 3 labels use `UIStyle.textColor` (near-white 0.97): "Star 1: Water & O2 Plant" etc. are invisible in every shot. `MissionCardView` sets label/track colors from `UIStyle` but never the card background.
   Fix: set the `MissionCard` Image color to `UIStyle.panelColor` (0.118, 0.161, 0.231, 0.95) and the `Title` text to `textColor`.
3. **Run 3D audio isn't in the game.** `Assets/_Prefabs/Audio/AudioManager.prefab` isn't in `HandMadeBase.unity` (0 references), so the 16 cues (build, star, rover dispatch/return, storm alert, broken/repaired, day advance, mission win) never play. Only `GameplayHud`'s own clips play. CLAUDE.md says to drop the prefab in. Repro: search the scene hierarchy for `AudioManager` -> none.
4. **Leaderboard is offline.** `_Config/Leaderboard/LeaderboardConfig.asset` has empty `projectUrl` and `publishableKey`, so every end screen says "Leaderboard offline". GDD §2/§9 promise an online histogram. The SQL is in `run4-screens.md`.
5. **Balance is far below the GDD target (12–18 days).** This run ended on Day 6 (with a +30 Parts shortcut; without it, about Day 8–9). The 3A/3B sims give a median win on Day 6–9, and Bauzer says "self-sufficient in 6 days" / "record time". Knobs (SOs only): `BuildingConfig` Parts costs, mineral hauls in `ExpeditionSiteConfig`, start Parts in `GameConfig`.
6. **Next Day button is just a play icon.** It has no "Next Day" label and no pending-impact mini-summary (GDD §3). Kids and judges may not read it as "end the day". `NextDayFlow.nextDayButton` (bottom-right).
7. **EditMode test fails:** `AudioTests.SimulationAudioBridge_SubscribesAndUnwiresCleanly` (`Assets/_Scripts/Audio/Editor/AudioTests.cs:202`). The test still assumes the old 100% storm break; with the 30% rule and `RandomValue = 0.99` the rover returns intact (`RoverReturn`, not `ComponentBroken`). This is a stale test, not a game bug: set `rover.RandomValue = () => 0f` before `ResolveReturn()`.
8. **Copy mismatch on the demand card.** Title "Water Leak", but the button says "Fix Filter (-5 Parts)" (`UnexpectedDemandConfig.asset`).

Unsure picks from the run reports that contradict GDD.md:
- 3A #7 (field stays Retracted after a storm) is superseded and now correct (redeploys at dawn).
- 3A #3 (unanswered demand dropped) is superseded and now correct (blocks End Day).
- Run 4 #1: Play Again skips the onboarding. The GDD calls Day 1 onboarding mandatory, but only once per session; acceptable.
- 3B #8 (rover can be dispatched on a storm day) and 3B #5 (morning cards wait for dawn) don't contradict the GDD.
- Run 4: the chosen astronaut doesn't change the 3D walker. GDD §3 says "the chosen astronaut character walks around". Minor.

Needs a manual check (not provable over MCP):
- Real mouse/touch: drag-pan vs click, Skip by tap, input field typing, button hover.
- Intro camera actually frames the lander (`lander_A`). The shots taken mid-intro showed the base edge, not clearly the lander.
- Real-time length of the day -> night transition (~10 s) and of the intro orbit.
- Radiation day (Day 9) choice card and the Intro SKIP button were not re-run here; Runs 3B and 4 covered them.

## 3. Console / Asset / WebGL Risks

- **WebGL build not run** (per instructions). The active build target is **StandaloneOSX**. Switching to WebGL will reimport the project, which takes a while; start the switch well before the upload.
- `PlayerSettings.WebGL.compressionFormat = Brotli` with `decompressionFallback = false`. On hosts that don't send `Content-Encoding: br` (GitHub Pages, some itch.io setups), the build won't load. Turn on Decompression Fallback, or use Gzip/Disabled for the submission host.
- Runtime script scan: no threads, `System.IO`, sockets, sync web calls, `Task.Wait` or async/await. The only `UnityEditor` call (`GameFlowController.PlayAgain`) is inside `#if UNITY_EDITOR`. Networking uses `UnityWebRequest` coroutines. No blockers found.
- Build Settings: `HandMadeBase.unity` index 0 enabled; `Base.unity` disabled. Dev scenes are not in the list.
- `Assets/GameAssets/Free Icon Pack v3.1 (Basic)/` is **untracked**: it's listed in `references.md`, but a clone or CI build won't have it. It also contains a `Claude outputs` folder. Commit it (from the Mac with git-lfs) or remove it if unused.
- Many binaries (`.png`, `.fbx`, `.blend`, `.dll`...) show as modified, and run 3B/4 screenshot `.meta` files are untracked. That's the missing git-lfs in the VM. Commit the screenshots from the Mac; delete the stale ones run 4 flagged (`run4-intro-a.png`, `run4-intro-cutscene.png`, `run4-onboarding-step1-weather.png`).
- Each resource pill has two `AlertBadge` children (one unused). Harmless.
- Quaternius licence is still marked "to be verified" in `references.md`. The DONKI dates still say "to be verified"; they look right.
- Every Play session rewrites `PlayerRunProfile.asset` / `Speaker_Player.asset` at runtime. Don't commit those changes.

## 4. Submission Readiness (Before 23:59)

Playable start-to-finish with no console errors; core loop, morning sequence, DONKI story and end screen all work. Not ready to film as-is: fix 1–2 first.
Do in this order:
1. Fix the rover click (Fail 1): disable or shrink the unbuilt `foodFarm` slot collider, or move the rover. Re-check by clicking the rover in the Game view.
2. Fix the Missions card colors (Fail 2). It's a 2-value change.
3. Drop `AudioManager.prefab` into `HandMadeBase.unity` and save (Fail 3). Listen for double sounds alongside `GameplayHud` clips.
4. Switch to WebGL, enable Decompression Fallback (or Gzip), build, and test in a browser on the real host. Budget time for the reimport.
5. Optional: Supabase project + paste URL/key (Fail 4); otherwise say "offline histogram" in the video.
6. Optional: raise building Parts costs for a ~12-day run (Fail 5); fix the stale audio test (Fail 7); label the Next Day button (Fail 6).
7. Commit `Free Icon Pack` + screenshots from the Mac with git-lfs; record the video on a real mouse run.

## 5. Fixes after review

Branch `fix/rover-button-audio`. Checked in Play Mode in Lucas's Editor; clicks simulated through the UI's own code paths
(`Button.onClick`, `WorldClickRouter.Click(screenPoint)`, `DialogueRunner.AdvanceOrSkip`, popup confirm button).

- **Fail 1 (rover click), Lucas's decision: the rover is operated only from the HUD Rover button (GDD §6).**
  `RoverInteractable.OnWorldClicked` is now a no-op, so clicking the 3D rover does nothing (model, collider, `RoverMissionDriver`
  and routes untouched). The HUD button still calls `RoverInteractable.Activate()`: Ready -> rover map, Broken -> Repair (5 Parts)
  card (kept anchored over the rover, readable next to the button), On Mission / Radiation / crew restricted -> Bauzer tip.
  Day 1 step 2 now frames the HUD Rover button (`Day1Onboarding.roverButton`, replaces `roverTarget`) after Bauzer's line,
  because the bottom buttons are hidden while he talks. Copy: step 2 line "Tap the Rover button, then pick a site...",
  hint "STEP 2/3  Tap the Rover button", `?` help "Use the Rover button for Water and Parts".
  Verified: rover click -> nothing opens; step 2 frame sits on the button (alpha 1, interactable); button -> map -> pin dispatch;
  On Mission tip; Broken card -> repair; crew restricted -> "Locked" + tip.
- **Fail 3 (audio):** `AudioManager.prefab` added to `HandMadeBase.unity` (root), `SimulationAudioBridge` scene references
  assigned explicitly. Duplicates removed: `GameplayHud.PlaySfx` and the `DialogueBoxView` advance click now skip when an
  `AudioManager` is present (fallback kept for scenes without it); blocked actions play `ActionBlocked`.
  Heard in Play Mode: UiOpen/UiClick/UiClose, ToggleSwitch, RoverDispatch, ComponentBroken, DayAdvance, ComponentRepaired,
  ActionBlocked, BuildSuccess, StarEarned, MissionWin; StormAlert and RoverReturn checked by raising the scene managers' events.
- **Fail 7 (stale test):** `AudioTests` forces `rover.RandomValue = () => 0f` before the storm return. EditMode 31/31 pass.
- Console after the run: 0 errors / 0 warnings from game scripts.
