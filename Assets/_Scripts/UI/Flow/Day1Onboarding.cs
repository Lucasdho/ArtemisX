using System;
using System.Collections;
using ArtemisX.Simulation;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>
    /// Mandatory Day 1 tutorial: 1) space weather + solar field, 2) rover map dispatch, 3) Next Day.
    /// Each step highlights its target, Bauzer explains, then the step waits for the real action.
    /// </summary>
    public class Day1Onboarding : MonoBehaviour
    {
        [SerializeField] private ScreensConfig config;
        [SerializeField] private GameplayUIConfig uiConfig;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private NextDayFlow nextDay;
        [SerializeField] private DayManager days;
        [SerializeField] private DayResolutionController resolution;
        [SerializeField] private RoverExpeditionManager rover;
        [SerializeField] private MorningSequenceController morning;
        [SerializeField] private Visuals.DayNightTransitionPlayer transition;
        [SerializeField] private TutorialHighlightView highlight;
        [SerializeField] private RectTransform missionCard;
        [SerializeField] private Transform firstBuildingSlot;
        [SerializeField] private RectTransform weatherWidget;
        [SerializeField] private RectTransform nextDayButton;
        [SerializeField] private RectTransform roverButton;
        [SerializeField] private Transform solarField;
        [SerializeField] private Speaker playerSpeaker;

        public bool IsRunning { get; private set; }
        public event Action Finished;

        private bool solarClicked;
        private MissionCardView card;
        private DialogueSequence runtimeSequence;

        private void LateUpdate()
        {
            if (highlight != null) highlight.Suppressed = hud != null && hud.RoverMap != null && hud.RoverMap.IsOpen;
        }

        private void OnEnable() => WorldClickRouter.Clicked += OnWorldClicked;
        private void OnDisable() => WorldClickRouter.Clicked -= OnWorldClicked;

        private void OnDestroy()
        {
            if (runtimeSequence != null) Destroy(runtimeSequence);
        }

        public void Begin()
        {
            if (IsRunning) return;
            StartCoroutine(Run());
        }

        private void OnWorldClicked(WorldInteractable target)
        {
            if (target is SolarFieldInteractable) solarClicked = true;
        }

        private IEnumerator Run()
        {
            IsRunning = true;
            if (nextDay != null) nextDay.ExternalLock = true;

            card = missionCard != null ? missionCard.GetComponent<MissionCardView>() : null;
            if (card != null) card.ShowTutorial(new[] { "Click the solar field", "Send the rover out", "Press Next Day" });

            // Step 1: what is the goal (Mission Card).
            if (missionCard != null)
            {
                if (highlight != null) highlight.ShowUi(missionCard, config.goalHint);
                yield return Say(config.goalLines);
            }

            // Step 2: where to build next (Processing Plant ghost ring).
            if (firstBuildingSlot != null)
            {
                if (highlight != null) highlight.ShowWorld(firstBuildingSlot, hud != null ? hud.ViewCamera : null, config.buildNextHint);
                yield return Say(config.buildNextLines);
            }

            // Step 3: how to get Power (space weather + solar field).
            if (highlight != null) highlight.ShowUi(weatherWidget, config.step1WeatherHint);
            yield return Say(config.step1Lines);
            solarClicked = false;
            if (highlight != null) highlight.ShowWorld(solarField, hud != null ? hud.ViewCamera : null, config.step1Hint);
            yield return new WaitUntil(() => solarClicked);
            if (card != null) card.CompleteTutorialTask(0);
            yield return WaitIdle();

            // Step 4: how to get Water & Parts (rover map dispatch).
            if (rover == null || rover.Status == RoverStatus.Ready)
            {
                // The bottom HUD buttons are hidden while Bauzer talks, so frame the Rover button once he is done.
                if (highlight != null) highlight.Hide();
                yield return Say(config.step2Lines);
                if (highlight != null) highlight.ShowUi(roverButton, config.step2Hint);
                yield return new WaitUntil(() => rover == null || rover.Status != RoverStatus.Ready);
                yield return WaitIdle();
            }
            if (card != null) card.CompleteTutorialTask(1);

            // Step 5: Next Day.
            if (highlight != null) highlight.ShowUi(nextDayButton, config.step3Hint);
            yield return Say(config.step3Lines);
            if (nextDay != null) nextDay.ExternalLock = false;
            int startDay = days != null ? days.CurrentDay : 1;
            yield return new WaitUntil(() => (resolution != null && resolution.IsResolving) || (days != null && days.CurrentDay > startDay)
                || (resolution != null && resolution.IsMissionOver));
            if (highlight != null) highlight.Hide();
            yield return new WaitUntil(() => (days == null || days.CurrentDay > startDay) && IsIdle());
            if (card != null) card.CompleteTutorialTask(2);
            yield return new WaitForSeconds(0.8f);
            if (card != null) card.ShowMissions();
            yield return Say(config.doneLines);

            IsRunning = false;
            Finished?.Invoke();
        }

        private IEnumerator WaitIdle()
        {
            yield return null;
            yield return new WaitUntil(IsIdle);
        }

        private bool IsIdle() =>
            (runner == null || !runner.IsOpen) &&
            (hud == null || hud.Popup == null || !hud.Popup.IsOpen) &&
            (hud == null || hud.RoverMap == null || !hud.RoverMap.IsOpen) &&
            (morning == null || !morning.IsShowing) &&
            (transition == null || !transition.IsPlaying) &&
            (resolution == null || !resolution.IsResolving);

        /// <summary>Plays Bauzer (or player) lines through the dialogue box and waits until it closes.</summary>
        private IEnumerator Say(ScreensConfig.Line[] lines)
        {
            if (runner == null || lines == null || lines.Length == 0) yield break;
            yield return new WaitUntil(() => !runner.IsOpen);
            if (runtimeSequence == null)
            {
                runtimeSequence = ScriptableObject.CreateInstance<DialogueSequence>();
                runtimeSequence.name = "runtime_onboarding";
                runtimeSequence.sequenceId = "onboarding";
            }
            runtimeSequence.lines = BuildLines(lines, uiConfig != null ? uiConfig.bauzer : null, playerSpeaker);
            bool done = false;
            runner.Enqueue(runtimeSequence, () => done = true, DialogueQueueSlot.Intro);
            yield return new WaitUntil(() => done);
        }

        public static DialogueSequence.DialogueLine[] BuildLines(ScreensConfig.Line[] lines, Speaker bauzer, Speaker player)
        {
            var result = new DialogueSequence.DialogueLine[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                result[i] = new DialogueSequence.DialogueLine
                {
                    speaker = lines[i].player && player != null ? player : bauzer,
                    mood = lines[i].mood,
                    text = lines[i].text
                };
            }
            return result;
        }
    }
}
