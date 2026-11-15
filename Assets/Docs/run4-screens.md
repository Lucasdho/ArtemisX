# Run 4: start screen, intro cutscene, Day 1 onboarding, end screen and leaderboard (report)

Branch `feat/screens-polish` (from `develop`, main checkout, Editor driven over MCP). Not merged.
Scene: `Assets/Scenes/HandMadeBase.unity`.

## What was done

### Flow (re-runnable wiring)
- `Artemis X > Run 4 > Build Screens and Wiring` (`_Scripts/UI/Editor/Run4ScreensBuilder.cs`) rebuilds only its own objects:
  `StartScreenCanvas` (the Run 3C prefab, `StartScreenView.dialogueRunner` wired), `Run4_TutorialCanvas` (highlight frame,
  hint banner, intro Skip button), `Run4_EndScreenCanvas` (`EndScreenView`), `Run4_Flow` (`GameFlowController`,
  `Day1Onboarding`, `LeaderboardClient`). It also sets `StoryDirector.playDay1IntroOnStart = false`, adds CanvasGroups to the
  top HUD (`GameplayHud.topBar`), puts the 3C weather icons into the 3B weather widget and makes `HandMadeBase` build scene 0.
- Start screen (3C prefab, reused): pick NOVA/LEO/MAYA/KAI + 12-char filtered callsign -> LAUNCH MISSION. The HUD is hidden and
  world clicks are blocked (`GameplayHud.Cinematic`) until the intro ends.
- Intro cutscene: the camera rig (`CM_BaseCamera`, pan paused) orbits the lander (`lander_A`) while Bauzer plays `Seq_Intro`
  (5 Bauzer beats + 1 line from the chosen astronaut, with their portrait), then eases back to the diorama pose.
  `SKIP >>` (top right) closes the intro dialogue and snaps the camera home.
- Day 1 onboarding (`Day1Onboarding`), Next Day locked until step 3 (`NextDayFlow.ExternalLock`):
  1. Highlight Space Weather widget + Bauzer (2 beats) -> highlight solar field, waits for a real click on it (popup opens).
  2. Highlight rover + Bauzer (2 beats) -> waits until the rover is dispatched from the map (hint hides while the map is open).
  3. Highlight Next Day + Bauzer -> waits for the Day -> Night transition and the Day 2 morning cards, then a "training done" line.
- End screen (`EndScreenView`, after `StoryDirector.LeaderboardHandoffRequested`, i.e. after Bauzer's `Seq_MissionEnd_*`):
  `MISSION COMPLETE` / `MISSION EXTENDED`, 1-3 Kenney stars, days, Bauzer grade line (by stars, special line for a win
  by day 12), histogram of days 1-30 + `EXT` (Day Cap) bar with the player's bar in amber, status line, PLAY AGAIN.
- Play Again reloads the scene: start screen again (prefilled), no intro cutscene and no onboarding on replays in the same
  session (one Bauzer "welcome back" line instead).
- Leaderboard (`LeaderboardClient`, `UnityWebRequest` coroutines): `POST /rest/v1/runs` then
  `GET /rest/v1/run_histogram?select=days_taken,end_reason,runs`, publishable key in `_Config/Leaderboard/LeaderboardConfig.asset`.
  Empty config, timeout or HTTP error -> "Leaderboard offline. Showing your run only." with only the player's own bar. Never
  blocks, never logs warnings/errors (plain `Debug.Log`). No seed data.

### Copy
- All 24 `DialogueSequence` assets rewritten in English, <= 110 chars per beat, `{playerName}` kept; renamed without
  `_[PLACEHOLDER]` via `AssetDatabase.RenameAsset` (GUIDs kept). `Speaker_A` -> `Speaker_Bauzer`, `Speaker_B` -> `Speaker_Player`
  (`PlayerRunProfile.playerSpeaker` still points at it). `grep PLACEHOLDER` over assets/scenes/scripts: 0 hits.
- New copy in `_Config/UI/ScreensConfig.asset` (onboarding, end screen, grades) and `GameplayUIConfig` (Day 25 and final-day
  Daily Report lines; the Daily Report, demand, DONKI choice and tip copy from 3B was kept).

### Small edits outside new files
- `StoryDirector`: `playDay1IntroOnStart` flag, `Schedule` getter, Day Cap stars now 0..3 (was clamped to min 1; GDD says 0-2).
- `DialogueRunner.SkipCurrentSequence()`, `NextDayFlow.ExternalLock`, `WorldClickRouter.Clicked` (static event),
  `GameplayHud.Cinematic` + `topBar`, `MorningSequenceController` Day 25 / final day line.
- Scene: `AmbientMood.mood` was saved as `Storm` in `HandMadeBase.unity`, so Day 1 (calm) started with storm particles and dark
  lighting. Set back to `Normal`.

### Duplicates checked
- Alert badge: the scene pills have only one `AlertBadge` (3B's pulsing red `!`); the 3C `ResourcePill.prefab` is not
  instanced in this scene, so nothing to remove.
- Weather: kept 3B's `SpaceWeatherWidgetView` (wired, working) and gave it the 3C themed icons. `WeatherBadge.prefab` is unused.

### WebGL
- No threads, `System.IO`, sync network, sockets or `Task.Wait` in runtime scripts (grep); networking is `UnityWebRequest` coroutines.
- Build Settings: `HandMadeBase.unity` index 0 (enabled); `Base.unity` kept in the list but disabled.
- No WebGL build was run (would block the Editor).

## Definition of Done (Play Mode via MCP; clicks simulated through the same code paths, not real mouse input)
- [x] Start (NOVA..KAI + nickname "Lucas") -> intro cutscene (and Skip tested separately) -> onboarding steps 1-3
  (solar click via `WorldClickRouter.Click`, rover map `OnSiteClicked`, `NextDayFlow.OnNextDayClicked` + transition `Skip()`)
  -> Day 2 Daily Report -> resources added + 3 buildings built via `BuildingManager.TryBuildNext` -> Next Day -> `EarlyWin`
  3-star end screen -> Play Again reloaded cleanly (Day 1, 0 stars, Power 60, start screen prefilled).
- [x] Day Cap: day forced to 30 by reflection, 1 building, Next Day -> "Mission Extended" end screen, 1 star, no game over.
  (The Day pill still read "Day 1" in that test because the day was set by reflection.)
- [x] No `[PLACEHOLDER]` text left in assets, scenes or scripts.
- [x] Leaderboard offline: empty config and a bogus host (`.invalid`) both show "Leaderboard offline", zero errors.
- [x] `references.md` updated (packs, fonts, sounds, NASA DONKI/CCMC + the 5 event dates, AI assets, Dr. Claudia Maria Bauzer Medeiros, Supabase).
- [x] Console: zero errors/warnings from our scripts (only MCP-FOR-UNITY port notices and URP "memoryless" info lines).
- [x] Scene saved, Play Mode stopped, commits on `feat/screens-polish` (Lucas's identity, no AI attribution).

## Screenshots (uncommitted: git-lfs is missing in the VM; commit from the Mac)
`Assets/Docs/screenshots/run4-start-screen.png`, `run4-intro-cutscene-day.png`, `run4-onboarding-step1-weather-day.png`,
`run4-onboarding-step1-solar.png`, `run4-onboarding-step2-map.png`, `run4-onboarding-step3-nextday.png`, `run4-day2-report.png`,
`run4-end-earlywin-offline.png`, `run4-end-daycap-extended.png` (+ `.meta`). `run4-intro-a.png`, `run4-intro-cutscene.png`,
`run4-onboarding-step1-weather.png` show the old storm-mood bug and can be deleted;
`run4-start-screen-check.png` and `run4-intro-check.png` are re-checks after an Editor reload.

## Supabase
No Artemis X project exists in the connected Supabase account, so the config is empty and the game runs offline.
To go online: create a project, run the SQL below, then paste the Project URL and the publishable (anon) key into
`Assets/_Config/Leaderboard/LeaderboardConfig.asset`.

```sql
create table public.runs (
  id bigint generated always as identity primary key,
  created_at timestamptz not null default now(),
  nickname text not null check (char_length(nickname) between 1 and 12),
  character_index smallint not null check (character_index between 0 and 3),
  days_taken smallint not null check (days_taken between 1 and 30),
  stars smallint not null check (stars between 0 and 3),
  end_reason text not null check (end_reason in ('early_win', 'day_cap'))
);
alter table public.runs enable row level security;
create policy "anon insert runs" on public.runs for insert to anon with check (true);
create policy "anon read runs" on public.runs for select to anon using (true);
grant insert, select on public.runs to anon;

create view public.run_histogram with (security_invoker = true) as
  select days_taken, end_reason, count(*)::int as runs
  from public.runs
  group by days_taken, end_reason;
grant select on public.run_histogram to anon;
```

## Not done
- No WebGL build (API check only).
- No in-house Sun/Storm/Radiation icons (3C AI icons used).
- The chosen astronaut does not change the 3D walker model (Crew walkers are fixed); only portrait + name follow the choice.
- `AudioManager.prefab` (Run 3D) is not in `HandMadeBase.unity`; 3B's `GameplayHud` sounds still play. Not touched.
- Intro camera framing was tuned from two screenshots only.

## Unsure (my picks)
1. Play Again skips the intro and onboarding for the rest of the session (GDD says onboarding is mandatory on Day 1; replaying it felt like a chore).
2. Step 1 completes when the solar field is clicked (Day 1 is calm, so no retract is required).
3. Onboarding does not block other actions (building, help); only Next Day is locked until step 3.
4. Offline histogram shows only the player's own bar (real data, not seed data).
5. Supabase column names are snake_case (`days_taken`, `end_reason`, `character_index` instead of `character`).
6. Start screen gets a small "AI-generated art" caption; the AI images still have no visible watermark (SynthID only).
   If the rules need a mark on each image, Lucas must add it to the PNGs.
7. Day Cap end screen can show 0 stars (GDD: 0-2); before this run it was forced to at least 1.

## Heads-up for Run 5 / Lucas
- Verify the 5 DONKI dates in `SpaceWeatherConfig` (written from memory in Run 3A).
- `PlayerRunProfile.asset` and `Speaker_Player.asset` are written at runtime by `ApplySelection` (3C design). If the Editor
  saves assets after a Play session, they may show up as modified: don't commit those changes.
- The MissionCard checklist labels look empty in screenshots (dark text on a white card?); not investigated.
- A transient `.git/index.lock` appeared once during this run (another git process on this checkout).
- Mid-run the MCP instance list changed (an `ArtemisX-ui-polish` instance disappeared; the main `ArtemisX` Editor remained and
  was the one used, `Application.dataPath` = main checkout). After that reload, saving the scene rewrote layout-driven
  RectTransform values (zeros) and one Text max size; layouts were re-checked in Play Mode and look right
  (commit `chore(scene): resave base scene after editor reload`).
