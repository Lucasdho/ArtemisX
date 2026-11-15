using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.Story
{
    /// <summary>
    /// Coordinates Next Day clicks and input blocking. With a DayResolutionController in the scene, Next Day runs the
    /// simulation's Day -> Storm -> Night resolution (Bauzer reports it in the Morning Briefing). Without one it falls
    /// back to the Run 2A evening debrief + day advance.
    /// </summary>
    public class NextDayFlow : MonoBehaviour
    {
        [SerializeField] private DayManager dayManager;
        [SerializeField] private StoryDirector storyDirector;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Button nextDayButton;
        [Tooltip("Optional. Found in the scene if left empty.")]
        [SerializeField] private DayResolutionController dayResolution;

        private bool isTransitioning;
        private bool externalLock;

        /// <summary>Keeps Next Day locked (e.g. during the Day 1 onboarding until its last step).</summary>
        public bool ExternalLock
        {
            get => externalLock;
            set { externalLock = value; RefreshButtonState(); }
        }

        private void Awake()
        {
            if (dayResolution == null) dayResolution = FindAnyObjectByType<DayResolutionController>();
        }

        private void OnEnable()
        {
            if (dayResolution != null)
            {
                dayResolution.NightResolved += OnNightResolved;
                dayResolution.MorningReady += OnMorningReady;
                dayResolution.EndDayAvailabilityChanged += RefreshButtonState;
            }

            if (dialogueRunner != null)
            {
                dialogueRunner.DialogueStarted += OnDialogueStateChanged;
                dialogueRunner.DialogueFinished += OnDialogueStateChanged;
            }

            if (storyDirector != null)
                storyDirector.MissionEnded += OnMissionEnded;

            RefreshButtonState();
        }

        private void OnDisable()
        {
            if (dayResolution != null)
            {
                dayResolution.NightResolved -= OnNightResolved;
                dayResolution.MorningReady -= OnMorningReady;
                dayResolution.EndDayAvailabilityChanged -= RefreshButtonState;
            }

            if (dialogueRunner != null)
            {
                dialogueRunner.DialogueStarted -= OnDialogueStateChanged;
                dialogueRunner.DialogueFinished -= OnDialogueStateChanged;
            }

            if (storyDirector != null)
                storyDirector.MissionEnded -= OnMissionEnded;
        }

        /// <summary>Plays evening debrief, then advances DayManager if not yet at the mission cap.</summary>
        public void OnNextDayClicked()
        {
            if (isTransitioning || externalLock) return;
            if (dialogueRunner != null && dialogueRunner.IsOpen) return;
            if (storyDirector != null && storyDirector.IsMissionEnded) return;

            isTransitioning = true;
            RefreshButtonState();

            if (dayResolution != null)
            {
                if (!dayResolution.EndDay())
                {
                    isTransitioning = false;
                    RefreshButtonState();
                }
                return;
            }

            if (storyDirector != null)
            {
                storyDirector.PlayDebriefThen(CompleteDayAdvance);
            }
            else
            {
                CompleteDayAdvance();
            }
        }

        private void CompleteDayAdvance()
        {
            isTransitioning = false;
            bool canAdvance = dayManager != null &&
                (storyDirector == null || (!storyDirector.IsMissionEnded && dayManager.CurrentDay < storyDirector.MissionLength));
            if (canAdvance)
                dayManager.AdvanceDay();

            RefreshButtonState();
        }

        private void OnNightResolved(DailyReportData _)
        {
            if (dayResolution != null && dayResolution.IsMissionOver)
            {
                isTransitioning = false;
                RefreshButtonState();
            }
        }

        private void OnMorningReady(DailyReportData _)
        {
            isTransitioning = false;
            RefreshButtonState();
        }

        private void OnDialogueStateChanged(DialogueSequence _) => RefreshButtonState();
        private void OnMissionEnded(MissionEndReason _, int __) => RefreshButtonState();

        private void RefreshButtonState()
        {
            if (nextDayButton == null) return;
            bool blocked = isTransitioning || externalLock ||
                (dialogueRunner != null && dialogueRunner.IsOpen) ||
                (storyDirector != null && storyDirector.IsMissionEnded) ||
                (dayResolution != null && dayResolution.CanEndDay() != ActionBlockReason.None);
            nextDayButton.interactable = !blocked;
        }
    }
}
