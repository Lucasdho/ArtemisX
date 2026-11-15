using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.Story
{
    /// <summary>Coordinates Next Day clicks: evening debrief, day advance up to missionLength, and input blocking.</summary>
    public class NextDayFlow : MonoBehaviour
    {
        [SerializeField] private DayManager dayManager;
        [SerializeField] private StoryDirector storyDirector;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private Button nextDayButton;

        private bool isTransitioning;

        private void OnEnable()
        {
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
            if (isTransitioning) return;
            if (dialogueRunner != null && dialogueRunner.IsOpen) return;
            if (storyDirector != null && storyDirector.IsMissionEnded) return;

            isTransitioning = true;
            RefreshButtonState();

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

        private void OnDialogueStateChanged(DialogueSequence _) => RefreshButtonState();
        private void OnMissionEnded(MissionEndReason _, int __) => RefreshButtonState();

        private void RefreshButtonState()
        {
            if (nextDayButton == null) return;
            bool blocked = isTransitioning ||
                (dialogueRunner != null && dialogueRunner.IsOpen) ||
                (storyDirector != null && storyDirector.IsMissionEnded);
            nextDayButton.interactable = !blocked;
        }
    }
}
