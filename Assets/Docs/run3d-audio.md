# Run 3D — Audio System & Kenney Interface Sounds (Report)

Branch `feat/audio`, created in worktree `/Users/lucasoliveira/Developer/ArtemisX-audio` from `develop`.
Nothing merged. `Assets/Scenes/HandMadeBase.unity`, `_Scripts/Simulation/**`, `_Scripts/UI/**`, `_Prefabs/UI/**`, and `Assets/GameAssets/kenney_interface-sounds/**` were not touched.

---

## 1. What Was Done

### Scripts (`Assets/_Scripts/Audio/`, namespace `ArtemisX.Audio`)
- **`SoundCue.cs`**: Defines the 16 named sound cues:
  `UiClick`, `UiOpen`, `UiClose`, `ToggleSwitch`, `BuildSuccess`, `StarEarned`, `RoverDispatch`, `RoverReturn`, `SupplyCollect`, `StormAlert`, `ComponentBroken`, `ComponentRepaired`, `ActionBlocked`, `EventPrompt`, `DayAdvance`, `MissionWin`.
- **`AudioConfig.cs`**: `ScriptableObject` (`[CreateAssetMenu(fileName = "AudioConfig", menuName = "Artemis X/Audio Config")]`) storing `masterVolume` (`[Range(0f, 1f)]`), `muted` (`bool`), and `CueEntry[] entries` (`SoundCue cue`, `AudioClip clip`, `[Range(0f, 1f)] float volume`, `[Range(0.8f, 1.2f)] float pitchMin`, `[Range(0.8f, 1.2f)] float pitchMax`).
- **`AudioManager.cs`**:
  - WebGL-safe 2D `AudioSource` pool (default pool size `6`) on the manager GameObject so overlapping cues (e.g., `BuildSuccess` + `StarEarned`) never overwrite each other's randomized pitch or cut off playback.
  - Preloads all assigned `AudioClip` data in `Awake()` (`clip.LoadAudioData()`) so `.ogg` clips with `preloadAudioData: 0` in the read-only Kenney pack never stall on first play in WebGL.
  - Static helper `AudioManager.Play(SoundCue cue)` (graceful no-op if `AudioManager.Instance` is absent).
  - Public instance methods: `PlayCue(SoundCue)`, `PlayClick()`, `PlayConfirm()`, `PlayCancel()`, `PlayOpen()`, `PlayBlocked()`, `ToggleMute()`, `SetMuted(bool)`, `SetMasterVolume(float)`, plus `IsMuted`, `MasterVolume`, `LastPlayedCue`, `LastPlayedClip`, `PlayCount`, `CuePlayed`, and `MuteChanged`.
- **`SimulationAudioBridge.cs`**:
  - Subscribes in `OnEnable()` and unsubscribes in `OnDisable()` to all present simulation and story managers (`BuildingManager`, `RoverExpeditionManager`, `SolarFieldManager`, `SupplyManager`, `SpaceWeatherManager`, `DayManager`, `UnexpectedDemandManager`, `StoryDirector`, `DialogueRunner`).
  - Auto-resolves any unassigned manager references via `FindAnyObjectByType<T>()` when `autoFindMissingManagers` is enabled (default `true`), and ignores initial `Start()` state broadcasts (such as `StarsChanged(0)`, `DayAdvanced(1)` on Day 1 start, and initial `SolarStateChanged`) so no spurious SFX play on scene load.
- **`AudioDebugPanel.cs`**:
  - Drives the audition Canvas in `Assets/Scenes/Dev_Audio.unity`, wiring all 16 `SoundCue` buttons and the `MuteButton` toggle with live status feedback.
- **`Editor/AudioTests.cs`**:
  - EditMode NUnit test suite verifying `AudioConfig.asset`, `AudioManager.prefab`, `AudioManager` playback/mute/static no-op, and `SimulationAudioBridge` event forwarding across all simulation and story managers.

---

## 2. Configured Sound Cues (`Assets/_Config/Audio/AudioConfig.asset`)

All 16 `SoundCue` entries are populated with real clips from `Assets/GameAssets/kenney_interface-sounds/Audio/`:

| `SoundCue` | Clip (`kenney_interface-sounds/Audio/`) | Volume | Pitch Range | Trigger / Usage |
| :--- | :--- | :---: | :---: | :--- |
| `UiClick` | `click_001.ogg` | `0.85` | `0.96 – 1.04` | UI button taps, dialogue typewriter skip/advance, demand choice |
| `UiOpen` | `open_001.ogg` | `0.85` | `0.97 – 1.03` | Opening pop-up cards, Daily Report, standard dialogue open |
| `UiClose` | `back_004.ogg` | `0.85` | `0.97 – 1.03` | Closing/cancelling pop-ups, back from Rover map, dialogue finish |
| `ToggleSwitch` | `toggle_001.ogg` | `0.85` | `0.95 – 1.05` | Exposed solar field Active/Retracted toggle, Space Weather choice |
| `BuildSuccess` | `confirmation_002.ogg` | `0.95` | `0.98 – 1.02` | `BuildingManager.BuildingCompleted`, `PlayConfirm()` |
| `StarEarned` | `confirmation_003.ogg` | `1.00` | `1.00 – 1.05` | `BuildingManager.StarsChanged` (when a new star is earned) |
| `RoverDispatch` | `maximize_004.ogg` | `0.90` | `0.96 – 1.04` | `RoverExpeditionManager.RoverDispatched` |
| `RoverReturn` | `drop_002.ogg` | `0.90` | `0.96 – 1.04` | `RoverExpeditionManager.RoverReturned` (intact) & `SupplyManager.SupplyDropped` |
| `SupplyCollect` | `pluck_001.ogg` | `0.95` | `0.95 – 1.06` | `SupplyManager.SupplyCollected` |
| `StormAlert` | `error_006.ogg` | `0.95` | `0.96 – 1.02` | `SpaceWeatherManager.StormIncoming` on DONKI event mornings |
| `ComponentBroken` | `error_008.ogg` | `0.95` | `0.94 – 1.00` | `RoverExpeditionManager.ComponentBroken` & `SolarFieldManager.ComponentBroken` |
| `ComponentRepaired` | `select_002.ogg` | `0.90` | `0.98 – 1.04` | `RoverExpeditionManager.ComponentRepaired` & `SolarFieldManager.ComponentRepaired` |
| `ActionBlocked` | `bong_001.ogg` | `0.85` | `0.95 – 1.02` | Locked/unaffordable action feedback (`AudioManager.Play(SoundCue.ActionBlocked)` / `PlayBlocked()`) |
| `EventPrompt` | `question_004.ogg` | `0.90` | `0.98 – 1.03` | `UnexpectedDemandManager.DemandRolled`, choice-bearing dialogue cards |
| `DayAdvance` | `switch_002.ogg` | `0.85` | `0.96 – 1.04` | `DayManager.DayAdvanced` (when advancing to Day 2+) |
| `MissionWin` | `confirmation_004.ogg` | `1.00` | `1.00 – 1.06` | `StoryDirector.MissionEnded` on `EarlyWin` / 3 stars |

---

## 3. Wiring Instructions for `HandMadeBase.unity`

Integrating audio into `Assets/Scenes/HandMadeBase.unity` takes under a minute:

1. **Drop Prefab into Scene**:
   Drag `Assets/_Prefabs/Audio/AudioManager.prefab` into the root of `HandMadeBase.unity`.
2. **Automatic Simulation & Story Event Wiring (`SimulationAudioBridge`)**:
   - `SimulationAudioBridge` on `AudioManager.prefab` has `autoFindMissingManagers = true` enabled by default, so it automatically discovers `BuildingManager`, `RoverExpeditionManager`, `SolarFieldManager`, `SupplyManager`, `SpaceWeatherManager`, `DayManager`, `UnexpectedDemandManager`, `StoryDirector`, and `DialogueRunner` in `Awake()`/`OnEnable()` and subscribes to their C# events.
   - *(Optional Inspector Wiring)*: For explicit scene references without runtime lookup, drag the scene's `Managers` / `Story` components into the matching fields on `SimulationAudioBridge` in the Inspector.
3. **UI Button & Blocked-Action Hooks (Optional One-Liners)**:
   - Any UI script can call `AudioManager.Play(SoundCue.UiClick)` (or `SoundCue.UiOpen`, `SoundCue.UiClose`, `SoundCue.ActionBlocked`) directly; if `AudioManager` is absent in a test scene, `AudioManager.Play(...)` is a safe no-op.
   - Unity `Button.onClick` events in the Inspector can also bind directly to `AudioManager` instance methods: `PlayClick()`, `PlayConfirm()`, `PlayCancel()`, `PlayOpen()`, `PlayBlocked()`, and `ToggleMute()`.

---

## 4. What Was Not Done

- `HandMadeBase.unity`, `_Scripts/UI/**`, and `_Prefabs/UI/**` were intentionally left untouched per parallel track ownership rules.
- `Dev_Audio.unity` was kept out of `EditorBuildSettings.scenes` per instructions.
- Ambient background music / looping drone was not added (only Kenney interface SFX are in scope for Run 3D).

## 5. Unsure / Design Notes

- **`SupplyDropped` vs `RoverReturn`**: Both use `SoundCue.RoverReturn` (`drop_002.ogg`) when the supply capsule lands in the morning, while collecting the pile plays `SoundCue.SupplyCollect` (`pluck_001.ogg`), matching the prompt's pairing of `drop_002.ogg..drop_004.ogg / pluck_001.ogg (Supply capsule drop & collect)`.
- **Preloading Kenney `.ogg` clips**: The read-only `.ogg.meta` files in `Assets/GameAssets/kenney_interface-sounds/Audio/` have `preloadAudioData: 0`. Rather than modifying read-only `.meta` files, `AudioManager.Awake()` calls `clip.LoadAudioData()` on all configured clips so they are decoded into memory immediately for WebGL safety.

## 6. Missing Assets

- None. All 16 `SoundCue` entries use imported `.ogg` clips already present in `Assets/GameAssets/kenney_interface-sounds/Audio/`.
