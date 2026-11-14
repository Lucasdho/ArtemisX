using System;
using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Maps day transitions, state pools, debrief templates, simulation event cards, and mission milestones onto DialogueRunner.</summary>
    public class StoryDirector : MonoBehaviour
    {
        [SerializeField] private DayManager dayManager;
        [SerializeField] private ResourceManager resourceManager;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private StorySchedule schedule;

        public event Action<MissionEndReason, int> MissionEnded;

        public bool HasBrokenComponent { get; private set; }
        public bool HasTriggeredEventToday => lastEventCardDay == CurrentDay;
        public DialogueConfig Config => runner != null ? runner.Config : null;

        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;
        private readonly int[] startOfDayResources = new int[ResourceTypeCount];

        private bool introPlayed;
        private bool firstStormPlayed;
        private bool firstBrokenComponentPlayed;
        private bool missionEnded;
        private int lastEventCardDay = -1;
        private DebriefOutcomeType todayOutcome = DebriefOutcomeType.Normal;
        private string todayEventName = "nominal shift";
        private string todayOutcomeSummary = "Steady reserves today traded speed for safety margins.";

        private int CurrentDay => dayManager != null ? dayManager.CurrentDay : 1;
        private int MissionLength => Config != null ? Mathf.Max(1, Config.missionLength) : 30;
        private int SupplyInterval => Config != null ? Mathf.Max(1, Config.supplyInterval) : 5;
        private int LowPowerThreshold => Config != null ? Config.lowPowerThreshold : 25;
        private int LowWaterThreshold => Config != null ? Config.lowWaterThreshold : 25;

        private void OnEnable()
        {
            if (dayManager != null)
                dayManager.DayAdvanced += OnDayAdvanced;
            if (runner != null)
                runner.ChoiceMade += OnChoiceMade;
        }

        private void OnDisable()
        {
            if (dayManager != null)
                dayManager.DayAdvanced -= OnDayAdvanced;
            if (runner != null)
                runner.ChoiceMade -= OnChoiceMade;
        }

        /// <summary>Plays the evening debrief template matching today's outcome, then runs onDone before advancing the day.</summary>
        public void PlayDebriefThen(Action onDone)
        {
            if (runner == null || schedule == null)
            {
                onDone?.Invoke();
                return;
            }

            SyncTemplateTokens();
            var outcome = ResolveDebriefOutcome();
            var debrief = schedule.GetDebriefForOutcome(outcome);
            if (debrief == null)
            {
                onDone?.Invoke();
                return;
            }

            if (CurrentDay >= MissionLength && !missionEnded)
                TriggerMissionEnd(MissionEndReason.DayCap, 1);

            runner.Enqueue(debrief, onDone, DialogueQueueSlot.EveningDebrief);
        }

        /// <summary>Milestone hook for the first completed building.</summary>
        public void TriggerFirstBuilding()
        {
            if (runner != null && schedule != null && schedule.firstBuildingMilestone != null)
                runner.Enqueue(schedule.firstBuildingMilestone, null, DialogueQueueSlot.MorningBriefing);
        }

        /// <summary>Queues Bauzer's supply ship arrival announcement in the SupplyArrival slot.</summary>
        public void TriggerSupplyShipArrived()
        {
            if (runner == null || schedule == null || schedule.supplyShipArrivedMilestone == null) return;
            SyncTemplateTokens();
            runner.Enqueue(schedule.supplyShipArrivedMilestone, null, DialogueQueueSlot.SupplyArrival);
        }

        /// <summary>Milestone hook for mission completion with default early-win 3-star parameters.</summary>
        public void TriggerMissionEnd() => TriggerMissionEnd(MissionEndReason.EarlyWin, 3);

        /// <summary>Queues the MissionEnd sequence last and emits MissionEnded(reason, stars).</summary>
        public void TriggerMissionEnd(MissionEndReason reason, int achievedGoalsCount)
        {
            if (missionEnded) return;
            missionEnded = true;

            bool allGoalsMet = achievedGoalsCount >= 3 || reason == MissionEndReason.EarlyWin;
            int stars = reason == MissionEndReason.EarlyWin ? 3 : Mathf.Clamp(achievedGoalsCount, 1, 3);

            SyncTemplateTokens();
            if (runner != null)
                runner.SetTemplateValue("stars", stars.ToString());

            var sequence = schedule != null ? schedule.GetMissionEndSequence(reason, allGoalsMet) : null;
            if (runner != null && sequence != null)
                runner.Enqueue(sequence, null, DialogueQueueSlot.MissionEnd);

            MissionEnded?.Invoke(reason, stars);
        }

        /// <summary>End-of-day check: triggers EarlyWin if all 3 goals are met, or DayCap at missionLength.</summary>
        public bool CheckEndOfDayMissionEnd(int achievedGoalsCount)
        {
            if (missionEnded) return true;

            if (achievedGoalsCount >= 3)
            {
                TriggerMissionEnd(MissionEndReason.EarlyWin, achievedGoalsCount);
                return true;
            }

            if (CurrentDay >= MissionLength)
            {
                TriggerMissionEnd(MissionEndReason.DayCap, achievedGoalsCount);
                return true;
            }

            return false;
        }

        /// <summary>Triggers a simulation event card (max 1 event card per day) with optional first-time intro beat.</summary>
        public bool TriggerSimulationEvent(SimulationEventType eventType)
        {
            if (runner == null || schedule == null) return false;
            if (lastEventCardDay == CurrentDay) return false;

            lastEventCardDay = CurrentDay;

            if (eventType == SimulationEventType.StormIncoming)
            {
                todayEventName = "dust storm";
                if (!firstStormPlayed && schedule.firstStormBriefing != null)
                {
                    firstStormPlayed = true;
                    runner.Enqueue(schedule.firstStormBriefing, null, DialogueQueueSlot.MorningBriefing);
                }
            }
            else if (eventType == SimulationEventType.ComponentBroken)
            {
                HasBrokenComponent = true;
                todayOutcome = DebriefOutcomeType.ComponentBroken;
                todayEventName = "broken component";
                if (!firstBrokenComponentPlayed && schedule.firstBrokenComponentBriefing != null)
                {
                    firstBrokenComponentPlayed = true;
                    runner.Enqueue(schedule.firstBrokenComponentBriefing, null, DialogueQueueSlot.MorningBriefing);
                }
            }

            SyncTemplateTokens();
            var card = schedule.GetEventCard(eventType);
            if (card == null) return false;

            runner.Enqueue(card, null, DialogueQueueSlot.EventCard);
            return true;
        }

        /// <summary>Queues a specific event card sequence if today has not already used its 1-per-day event slot.</summary>
        public void TriggerEventCard(DialogueSequence eventCard)
        {
            if (runner == null || eventCard == null) return;
            if (lastEventCardDay == CurrentDay) return;

            lastEventCardDay = CurrentDay;
            todayEventName = string.IsNullOrEmpty(eventCard.sequenceId) ? eventCard.name : eventCard.sequenceId;
            SyncTemplateTokens();
            runner.Enqueue(eventCard, null, DialogueQueueSlot.EventCard);
        }

        /// <summary>Queues the schedule's storm event card (used by debug triggers).</summary>
        public void TriggerDefaultEventCard()
        {
            TriggerSimulationEvent(SimulationEventType.StormIncoming);
        }

        private void OnDayAdvanced(int day)
        {
            if (runner == null || schedule == null) return;

            SnapshotStartOfDayResources();
            todayOutcome = DebriefOutcomeType.Normal;
            todayEventName = "nominal shift";
            todayOutcomeSummary = "Steady operations today traded speed for safe reserves.";
            SyncTemplateTokens();

            if (day == 1 && !introPlayed && schedule.introMilestone != null)
            {
                introPlayed = true;
                runner.Enqueue(schedule.introMilestone, null, DialogueQueueSlot.Intro);
            }

            var briefingState = EvaluateBriefingState();
            var briefing = schedule.GetBriefingForDay(day, MissionLength, SupplyInterval, briefingState);
            if (briefing != null)
                runner.Enqueue(briefing, null, DialogueQueueSlot.MorningBriefing);

            if (day > 0 && SupplyInterval > 0 && day % SupplyInterval == 0)
                TriggerSupplyShipArrived();
        }

        private void OnChoiceMade(string sequenceId, string choiceId)
        {
            var seq = runner != null ? runner.CurrentSequence : null;
            if (seq == null || seq.choices == null) return;

            for (int i = 0; i < seq.choices.Length; i++)
            {
                var choice = seq.choices[i];
                if (!string.Equals(choice.choiceId, choiceId, StringComparison.Ordinal)) continue;

                ApplyChoiceConsequences(choice);
                return;
            }
        }

        private void ApplyChoiceConsequences(DialogueSequence.DialogueChoice choice)
        {
            if (resourceManager != null && choice.resourceDeltas != null)
            {
                for (int i = 0; i < choice.resourceDeltas.Length; i++)
                {
                    var d = choice.resourceDeltas[i];
                    if (d.delta != 0)
                        resourceManager.Add(d.resource, d.delta);
                }
            }

            if (choice.fixesBrokenComponent)
                HasBrokenComponent = false;

            todayOutcome = HasBrokenComponent ? DebriefOutcomeType.ComponentBroken : DebriefOutcomeType.EventResolved;
            if (!string.IsNullOrEmpty(choice.outcomeSummary))
                todayOutcomeSummary = choice.outcomeSummary;

            if (runner != null)
                runner.SetTemplateValue("choice_label", choice.label ?? choice.choiceId);

            SyncTemplateTokens();
        }

        private void SnapshotStartOfDayResources()
        {
            if (resourceManager == null) return;

            for (int i = 0; i < ResourceTypeCount; i++)
                startOfDayResources[i] = resourceManager.Get((ResourceType)i);
        }

        private BaseBriefingState EvaluateBriefingState()
        {
            if (resourceManager == null) return BaseBriefingState.Ok;

            if (resourceManager.Get(ResourceType.Power) <= LowPowerThreshold)
                return BaseBriefingState.LowPower;

            if (resourceManager.Get(ResourceType.Water) <= LowWaterThreshold)
                return BaseBriefingState.LowWater;

            return BaseBriefingState.Ok;
        }

        private DebriefOutcomeType ResolveDebriefOutcome()
        {
            if (HasBrokenComponent)
                return DebriefOutcomeType.ComponentBroken;

            if (todayOutcome == DebriefOutcomeType.EventResolved)
                return DebriefOutcomeType.EventResolved;

            if (EvaluateBriefingState() != BaseBriefingState.Ok)
                return DebriefOutcomeType.ResourceLow;

            return todayOutcome;
        }

        private void SyncTemplateTokens()
        {
            if (runner == null) return;

            runner.SetTemplateValue("day", CurrentDay.ToString());
            runner.SetTemplateValue("mission_length", MissionLength.ToString());
            runner.SetTemplateValue("supply_interval", SupplyInterval.ToString());
            runner.SetTemplateValue("event_name", todayEventName);
            runner.SetTemplateValue("outcome_summary", todayOutcomeSummary);

            SetDeltaToken("power_delta", ResourceType.Power);
            SetDeltaToken("water_delta", ResourceType.Water);
            SetDeltaToken("oxygen_delta", ResourceType.Oxygen);
            SetDeltaToken("food_delta", ResourceType.Food);
            SetDeltaToken("parts_delta", ResourceType.Parts);
        }

        private void SetDeltaToken(string key, ResourceType resource)
        {
            int delta = 0;
            if (resourceManager != null)
                delta = resourceManager.Get(resource) - startOfDayResources[(int)resource];

            string formatted = delta >= 0 ? "+" + delta : delta.ToString();
            runner.SetTemplateValue(key, formatted);
        }
    }
}
