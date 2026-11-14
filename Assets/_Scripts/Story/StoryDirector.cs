using System;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Maps day transitions, debriefs, milestones, and event cards onto the DialogueRunner.</summary>
    public class StoryDirector : MonoBehaviour
    {
        [SerializeField] private DayManager dayManager;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private StorySchedule schedule;

        private bool introPlayed;

        private void OnEnable()
        {
            if (dayManager != null)
                dayManager.DayAdvanced += OnDayAdvanced;
        }

        private void OnDisable()
        {
            if (dayManager != null)
                dayManager.DayAdvanced -= OnDayAdvanced;
        }

        /// <summary>Plays the evening debrief sequence, then runs onDone before advancing the day.</summary>
        public void PlayDebriefThen(Action onDone)
        {
            if (runner != null && schedule != null && schedule.defaultEveningDebrief != null)
            {
                runner.Enqueue(schedule.defaultEveningDebrief, onDone);
                return;
            }

            onDone?.Invoke();
        }

        /// <summary>Milestone hook for the first completed building.</summary>
        public void TriggerFirstBuilding()
        {
            if (runner != null && schedule != null && schedule.firstBuildingMilestone != null)
                runner.Enqueue(schedule.firstBuildingMilestone);
        }

        /// <summary>Milestone hook for an Earth supply ship arrival.</summary>
        public void TriggerSupplyShipArrived()
        {
            if (runner != null && schedule != null && schedule.supplyShipArrivedMilestone != null)
                runner.Enqueue(schedule.supplyShipArrivedMilestone);
        }

        /// <summary>Milestone hook for mission completion.</summary>
        public void TriggerMissionEnd()
        {
            if (runner != null && schedule != null && schedule.missionEndMilestone != null)
                runner.Enqueue(schedule.missionEndMilestone);
        }

        /// <summary>Queues a specific event card sequence.</summary>
        public void TriggerEventCard(DialogueSequence eventCard)
        {
            if (runner != null && eventCard != null)
                runner.Enqueue(eventCard);
        }

        /// <summary>Queues the schedule's default event card (used by debug triggers).</summary>
        public void TriggerDefaultEventCard()
        {
            if (schedule != null && schedule.defaultEventCard != null)
                TriggerEventCard(schedule.defaultEventCard);
        }

        private void OnDayAdvanced(int day)
        {
            if (runner == null || schedule == null) return;

            if (day == 1 && !introPlayed && schedule.introMilestone != null)
            {
                introPlayed = true;
                runner.Enqueue(schedule.introMilestone);
            }

            var briefing = schedule.GetBriefingForDay(day);
            if (briefing != null)
                runner.Enqueue(briefing);
        }
    }
}
