using System;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>State bucket used to pick a fallback morning briefing.</summary>
    public enum BaseBriefingState
    {
        Ok = 0,
        LowPower = 1,
        LowWater = 2
    }

    /// <summary>Outcome category used to pick an evening debrief template.</summary>
    public enum DebriefOutcomeType
    {
        Normal = 0,
        EventResolved = 1,
        ResourceLow = 2,
        ComponentBroken = 3
    }

    /// <summary>Simulation event types that trigger 2-choice event cards.</summary>
    public enum SimulationEventType
    {
        StormIncoming = 0,
        ComponentBroken = 1
    }

    /// <summary>Reasons the mission can conclude.</summary>
    public enum MissionEndReason
    {
        EarlyWin = 0,
        DayCap = 1
    }

    /// <summary>Maps mission days, state pools, debrief outcomes, event cards, and milestones to dialogue sequences.</summary>
    [CreateAssetMenu(fileName = "StorySchedule", menuName = "Artemis X/Story/Story Schedule")]
    public class StorySchedule : ScriptableObject
    {
        [Serializable]
        public struct DayBriefingEntry
        {
            [Min(1)] public int day;
            public DialogueSequence sequence;
        }

        [Header("Morning Briefings — Key Days")]
        public DayBriefingEntry[] dayBriefings = Array.Empty<DayBriefingEntry>();
        public DialogueSequence day1TutorialBriefing;
        public DialogueSequence supplyDayBriefing;
        public DialogueSequence firstStormBriefing;
        public DialogueSequence firstBrokenComponentBriefing;
        public DialogueSequence day25WarningBriefing;
        public DialogueSequence finalDayBriefing;
        public DialogueSequence defaultMorningBriefing;

        [Header("Morning Briefings — Fallback State Pools")]
        public DialogueSequence[] okBriefingPool = Array.Empty<DialogueSequence>();
        public DialogueSequence[] lowPowerBriefingPool = Array.Empty<DialogueSequence>();
        public DialogueSequence[] lowWaterBriefingPool = Array.Empty<DialogueSequence>();

        [Header("Evening Debriefs — Outcome Templates")]
        public DialogueSequence defaultEveningDebrief;
        public DialogueSequence eventResolvedDebrief;
        public DialogueSequence resourceLowDebrief;
        public DialogueSequence componentBrokenDebrief;

        [Header("Milestones")]
        public DialogueSequence introMilestone;
        public DialogueSequence firstBuildingMilestone;
        public DialogueSequence secondBuildingMilestone;
        public DialogueSequence thirdBuildingMilestone;
        public DialogueSequence supplyShipArrivedMilestone;
        public DialogueSequence missionEndMilestone;
        public DialogueSequence missionEndEarlyWinMilestone;
        public DialogueSequence missionEndDayCapExtendedMilestone;

        [Header("Event Cards")]
        public DialogueSequence stormIncomingEventCard;
        public DialogueSequence componentBrokenEventCard;
        public DialogueSequence defaultEventCard;

        /// <summary>Returns the briefing sequence for the given day assuming normal reserves.</summary>
        public DialogueSequence GetBriefingForDay(int day)
            => GetBriefingForDay(day, 30, 5, BaseBriefingState.Ok);

        /// <summary>Returns the authored key-day briefing or rotates from the matching state pool.</summary>
        public DialogueSequence GetBriefingForDay(int day, int missionLength, int supplyInterval, BaseBriefingState state)
        {
            if (dayBriefings != null)
            {
                for (int i = 0; i < dayBriefings.Length; i++)
                {
                    if (dayBriefings[i].day == day && dayBriefings[i].sequence != null)
                        return dayBriefings[i].sequence;
                }
            }

            if (day == 1 && day1TutorialBriefing != null)
                return day1TutorialBriefing;

            if (day >= missionLength && finalDayBriefing != null)
                return finalDayBriefing;

            if (day == 25 && day25WarningBriefing != null)
                return day25WarningBriefing;

            if (day > 1 && supplyInterval > 0 && day % supplyInterval == 0 && supplyDayBriefing != null)
                return supplyDayBriefing;

            var pooled = PickFromStatePool(day, state);
            return pooled != null ? pooled : defaultMorningBriefing;
        }

        /// <summary>Returns the evening debrief template matching today's outcome.</summary>
        public DialogueSequence GetDebriefForOutcome(DebriefOutcomeType outcome)
        {
            switch (outcome)
            {
                case DebriefOutcomeType.EventResolved:
                    return eventResolvedDebrief != null ? eventResolvedDebrief : defaultEveningDebrief;
                case DebriefOutcomeType.ResourceLow:
                    return resourceLowDebrief != null ? resourceLowDebrief : defaultEveningDebrief;
                case DebriefOutcomeType.ComponentBroken:
                    return componentBrokenDebrief != null ? componentBrokenDebrief : defaultEveningDebrief;
                default:
                    return defaultEveningDebrief;
            }
        }

        /// <summary>Returns the event card mapped to a simulation event.</summary>
        public DialogueSequence GetEventCard(SimulationEventType type)
        {
            switch (type)
            {
                case SimulationEventType.StormIncoming:
                    return stormIncomingEventCard != null ? stormIncomingEventCard : defaultEventCard;
                case SimulationEventType.ComponentBroken:
                    return componentBrokenEventCard != null ? componentBrokenEventCard : defaultEventCard;
                default:
                    return defaultEventCard;
            }
        }

        /// <summary>Returns the mission end sequence based on whether all self-sufficiency goals were met.</summary>
        public DialogueSequence GetMissionEndSequence(MissionEndReason reason, bool allGoalsMet)
        {
            if (reason == MissionEndReason.EarlyWin || allGoalsMet)
                return missionEndEarlyWinMilestone != null ? missionEndEarlyWinMilestone : missionEndMilestone;

            return missionEndDayCapExtendedMilestone != null ? missionEndDayCapExtendedMilestone : missionEndMilestone;
        }

        private DialogueSequence PickFromStatePool(int day, BaseBriefingState state)
        {
            DialogueSequence[] pool = state switch
            {
                BaseBriefingState.LowPower => lowPowerBriefingPool,
                BaseBriefingState.LowWater => lowWaterBriefingPool,
                _ => okBriefingPool
            };

            if (pool == null || pool.Length == 0)
                pool = okBriefingPool;

            if (pool == null || pool.Length == 0)
                return null;

            int index = Mathf.Abs(day - 1) % pool.Length;
            return pool[index];
        }
    }
}
