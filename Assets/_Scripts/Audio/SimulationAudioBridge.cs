using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.Audio
{
    /// <summary>
    /// Subscribes to simulation and story C# events in OnEnable/OnDisable and forwards them to AudioManager.
    /// Dropping AudioManager.prefab into HandMadeBase.unity wires gameplay audio automatically.
    /// </summary>
    public class SimulationAudioBridge : MonoBehaviour
    {
        [SerializeField] private AudioManager audioManager;

        [Header("Simulation Managers (Optional - auto-found if unassigned)")]
        [SerializeField] private BuildingManager buildingManager;
        [SerializeField] private RoverExpeditionManager roverExpeditionManager;
        [SerializeField] private SolarFieldManager solarFieldManager;
        [SerializeField] private SupplyManager supplyManager;
        [SerializeField] private SpaceWeatherManager spaceWeatherManager;
        [SerializeField] private DayManager dayManager;
        [SerializeField] private UnexpectedDemandManager unexpectedDemandManager;

        [Header("Story Managers (Optional - auto-found if unassigned)")]
        [SerializeField] private StoryDirector storyDirector;
        [SerializeField] private DialogueRunner dialogueRunner;

        [Header("Discovery")]
        [SerializeField] private bool autoFindMissingManagers = true;

        private bool subscribed;
        private int lastKnownDay = 1;
        private int lastKnownStars;
        private SolarFieldState lastSolarState = SolarFieldState.Active;
        private bool suppressNextWeatherChoiceCue;
        private DialogueSequence promptPlayedForSequence;

        public void Initialize(
            AudioManager audio,
            BuildingManager buildings = null,
            RoverExpeditionManager rover = null,
            SolarFieldManager solar = null,
            SupplyManager supply = null,
            SpaceWeatherManager weather = null,
            DayManager days = null,
            StoryDirector story = null,
            DialogueRunner runner = null,
            UnexpectedDemandManager demands = null)
        {
            Unsubscribe();
            audioManager = audio;
            buildingManager = buildings;
            roverExpeditionManager = rover;
            solarFieldManager = solar;
            supplyManager = supply;
            spaceWeatherManager = weather;
            dayManager = days;
            storyDirector = story;
            dialogueRunner = runner;
            unexpectedDemandManager = demands;
            Subscribe();
        }

        private void Awake()
        {
            if (audioManager == null)
                audioManager = GetComponent<AudioManager>();

            if (autoFindMissingManagers)
                ResolveMissingManagers();
        }

        private void OnEnable()
        {
            if (autoFindMissingManagers)
                ResolveMissingManagers();

            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        public void ResolveMissingManagers()
        {
            if (audioManager == null)
                audioManager = GetComponent<AudioManager>();
            if (buildingManager == null)
                buildingManager = FindAnyObjectByType<BuildingManager>();
            if (roverExpeditionManager == null)
                roverExpeditionManager = FindAnyObjectByType<RoverExpeditionManager>();
            if (solarFieldManager == null)
                solarFieldManager = FindAnyObjectByType<SolarFieldManager>();
            if (supplyManager == null)
                supplyManager = FindAnyObjectByType<SupplyManager>();
            if (spaceWeatherManager == null)
                spaceWeatherManager = FindAnyObjectByType<SpaceWeatherManager>();
            if (dayManager == null)
                dayManager = FindAnyObjectByType<DayManager>();
            if (unexpectedDemandManager == null)
                unexpectedDemandManager = FindAnyObjectByType<UnexpectedDemandManager>();
            if (storyDirector == null)
                storyDirector = FindAnyObjectByType<StoryDirector>();
            if (dialogueRunner == null)
                dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        public void Subscribe()
        {
            if (subscribed)
                return;

            if (buildingManager != null)
            {
                lastKnownStars = buildingManager.Stars;
                buildingManager.BuildingCompleted += OnBuildingCompleted;
                buildingManager.StarsChanged += OnStarsChanged;
            }

            if (roverExpeditionManager != null)
            {
                roverExpeditionManager.RoverDispatched += OnRoverDispatched;
                roverExpeditionManager.RoverReturned += OnRoverReturned;
                roverExpeditionManager.ComponentBroken += OnComponentBroken;
                roverExpeditionManager.ComponentRepaired += OnComponentRepaired;
            }

            if (solarFieldManager != null)
            {
                lastSolarState = solarFieldManager.State;
                solarFieldManager.SolarStateChanged += OnSolarStateChanged;
                solarFieldManager.ComponentBroken += OnComponentBroken;
                solarFieldManager.ComponentRepaired += OnComponentRepaired;
            }

            if (supplyManager != null)
            {
                supplyManager.SupplyDropped += OnSupplyDropped;
                supplyManager.SupplyCollected += OnSupplyCollected;
            }

            if (spaceWeatherManager != null)
            {
                spaceWeatherManager.StormIncoming += OnStormIncoming;
                spaceWeatherManager.WeatherChoiceApplied += OnWeatherChoiceApplied;
            }

            if (dayManager != null)
            {
                lastKnownDay = dayManager.CurrentDay;
                dayManager.DayAdvanced += OnDayAdvanced;
            }

            if (unexpectedDemandManager != null)
            {
                unexpectedDemandManager.DemandRolled += OnDemandRolled;
                unexpectedDemandManager.DemandResolved += OnDemandResolved;
            }

            if (storyDirector != null)
            {
                storyDirector.MissionEnded += OnMissionEnded;
            }

            if (dialogueRunner != null)
            {
                dialogueRunner.DialogueStarted += OnDialogueStarted;
                dialogueRunner.StepFeedbackTriggered += OnDialogueStepFeedback;
                dialogueRunner.ChoicesPresented += OnDialogueChoicesPresented;
                dialogueRunner.DialogueFinished += OnDialogueFinished;
            }

            subscribed = true;
        }

        public void Unsubscribe()
        {
            if (!subscribed)
                return;

            if (buildingManager != null)
            {
                buildingManager.BuildingCompleted -= OnBuildingCompleted;
                buildingManager.StarsChanged -= OnStarsChanged;
            }

            if (roverExpeditionManager != null)
            {
                roverExpeditionManager.RoverDispatched -= OnRoverDispatched;
                roverExpeditionManager.RoverReturned -= OnRoverReturned;
                roverExpeditionManager.ComponentBroken -= OnComponentBroken;
                roverExpeditionManager.ComponentRepaired -= OnComponentRepaired;
            }

            if (solarFieldManager != null)
            {
                solarFieldManager.SolarStateChanged -= OnSolarStateChanged;
                solarFieldManager.ComponentBroken -= OnComponentBroken;
                solarFieldManager.ComponentRepaired -= OnComponentRepaired;
            }

            if (supplyManager != null)
            {
                supplyManager.SupplyDropped -= OnSupplyDropped;
                supplyManager.SupplyCollected -= OnSupplyCollected;
            }

            if (spaceWeatherManager != null)
            {
                spaceWeatherManager.StormIncoming -= OnStormIncoming;
                spaceWeatherManager.WeatherChoiceApplied -= OnWeatherChoiceApplied;
            }

            if (dayManager != null)
            {
                dayManager.DayAdvanced -= OnDayAdvanced;
            }

            if (unexpectedDemandManager != null)
            {
                unexpectedDemandManager.DemandRolled -= OnDemandRolled;
                unexpectedDemandManager.DemandResolved -= OnDemandResolved;
            }

            if (storyDirector != null)
            {
                storyDirector.MissionEnded -= OnMissionEnded;
            }

            if (dialogueRunner != null)
            {
                dialogueRunner.DialogueStarted -= OnDialogueStarted;
                dialogueRunner.StepFeedbackTriggered -= OnDialogueStepFeedback;
                dialogueRunner.ChoicesPresented -= OnDialogueChoicesPresented;
                dialogueRunner.DialogueFinished -= OnDialogueFinished;
            }

            subscribed = false;
        }

        private void TriggerCue(SoundCue cue)
        {
            if (audioManager != null)
                audioManager.PlayCue(cue);
            else
                AudioManager.Play(cue);
        }

        private void OnBuildingCompleted(int _) => TriggerCue(SoundCue.BuildSuccess);

        private void OnStarsChanged(int stars)
        {
            if (stars > lastKnownStars && stars > 0)
                TriggerCue(SoundCue.StarEarned);
            lastKnownStars = stars;
        }

        private void OnRoverDispatched(string _) => TriggerCue(SoundCue.RoverDispatch);

        private void OnRoverReturned(string _, int __, int ___, bool isBroken)
        {
            if (!isBroken)
                TriggerCue(SoundCue.RoverReturn);
        }

        private void OnSolarStateChanged(SolarFieldState state)
        {
            SolarFieldState previous = lastSolarState;
            if (state == previous)
                return;

            lastSolarState = state;
            if (previous != SolarFieldState.Broken && state != SolarFieldState.Broken)
            {
                if (spaceWeatherManager != null && spaceWeatherManager.NeedsChoiceToday)
                    suppressNextWeatherChoiceCue = true;
                TriggerCue(SoundCue.ToggleSwitch);
            }
        }

        private void OnComponentBroken(BaseComponent _) => TriggerCue(SoundCue.ComponentBroken);

        private void OnComponentRepaired(BaseComponent _) => TriggerCue(SoundCue.ComponentRepaired);

        private void OnSupplyDropped(int _, int __, int ___) => TriggerCue(SoundCue.RoverReturn);

        private void OnSupplyCollected(int _, int __) => TriggerCue(SoundCue.SupplyCollect);

        private void OnStormIncoming(int _, WeatherEventType weather)
        {
            suppressNextWeatherChoiceCue = false;
            if (weather != WeatherEventType.Calm)
                TriggerCue(SoundCue.StormAlert);
        }

        private void OnWeatherChoiceApplied(WeatherChoice _)
        {
            if (suppressNextWeatherChoiceCue)
            {
                suppressNextWeatherChoiceCue = false;
                return;
            }

            TriggerCue(SoundCue.ToggleSwitch);
        }

        private void OnDayAdvanced(int day)
        {
            suppressNextWeatherChoiceCue = false;
            if (day != lastKnownDay && day > 1)
                TriggerCue(SoundCue.DayAdvance);
            lastKnownDay = day;
        }

        private void OnDemandRolled(UnexpectedDemandConfig.Demand _) => TriggerCue(SoundCue.EventPrompt);

        private void OnDemandResolved(string _, int __) => TriggerCue(SoundCue.UiClick);

        private void OnMissionEnded(MissionEndReason reason, int stars)
        {
            if (reason == MissionEndReason.EarlyWin || stars >= 3)
                TriggerCue(SoundCue.MissionWin);
            else
                TriggerCue(SoundCue.UiClose);
        }

        private void OnDialogueStarted(DialogueSequence sequence)
        {
            if (sequence != null && sequence.HasChoices && sequence.lines != null && sequence.lines.Length <= 1)
            {
                promptPlayedForSequence = sequence;
                TriggerCue(SoundCue.EventPrompt);
            }
            else
            {
                promptPlayedForSequence = null;
                TriggerCue(SoundCue.UiOpen);
            }
        }

        private void OnDialogueStepFeedback() => TriggerCue(SoundCue.UiClick);

        private void OnDialogueChoicesPresented(DialogueSequence.DialogueChoice[] _)
        {
            DialogueSequence current = dialogueRunner != null ? dialogueRunner.CurrentSequence : null;
            if (current != null && current == promptPlayedForSequence)
            {
                promptPlayedForSequence = null;
                return;
            }

            TriggerCue(SoundCue.EventPrompt);
        }

        private void OnDialogueFinished(DialogueSequence _)
        {
            promptPlayedForSequence = null;
            TriggerCue(SoundCue.UiClose);
        }
    }
}
