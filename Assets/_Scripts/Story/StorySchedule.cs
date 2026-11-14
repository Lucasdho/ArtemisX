using System;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Maps mission days, debriefs, milestones, and fallback event cards to dialogue sequences.</summary>
    [CreateAssetMenu(fileName = "StorySchedule", menuName = "Artemis X/Story/Story Schedule")]
    public class StorySchedule : ScriptableObject
    {
        [Serializable]
        public struct DayBriefingEntry
        {
            [Min(1)] public int day;
            public DialogueSequence sequence;
        }

        [Header("Morning Briefings")]
        public DayBriefingEntry[] dayBriefings = Array.Empty<DayBriefingEntry>();
        public DialogueSequence defaultMorningBriefing;

        [Header("Evening Debrief")]
        public DialogueSequence defaultEveningDebrief;

        [Header("Milestones")]
        public DialogueSequence introMilestone;
        public DialogueSequence firstBuildingMilestone;
        public DialogueSequence supplyShipArrivedMilestone;
        public DialogueSequence missionEndMilestone;

        [Header("Event Cards")]
        public DialogueSequence defaultEventCard;

        /// <summary>Returns the sequence configured for the given day, or the default morning briefing.</summary>
        public DialogueSequence GetBriefingForDay(int day)
        {
            if (dayBriefings != null)
            {
                for (int i = 0; i < dayBriefings.Length; i++)
                {
                    if (dayBriefings[i].day == day && dayBriefings[i].sequence != null)
                        return dayBriefings[i].sequence;
                }
            }

            return defaultMorningBriefing;
        }
    }
}
