using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Rolls at most one Unexpected Demand per normal morning and applies the chosen answer.</summary>
    public class UnexpectedDemandManager : MonoBehaviour
    {
        [SerializeField] private UnexpectedDemandConfig config;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private DayManager dayManager;
        [SerializeField] private SpaceWeatherManager weather;

        /// <summary>A demand card should be shown.</summary>
        public event Action<UnexpectedDemandConfig.Demand> DemandRolled;
        /// <summary>(demandId, choiceIndex) after the choice was applied.</summary>
        public event Action<string, int> DemandResolved;

        /// <summary>Returns a value in [0, 1). Replace in tests for deterministic rolls.</summary>
        public Func<float> RandomValue { get; set; } = () => UnityEngine.Random.value;

        public UnexpectedDemandConfig Config => config;
        public bool HasActiveDemand { get; private set; }
        public UnexpectedDemandConfig.Demand ActiveDemand { get; private set; }

        private int lastRollDay = -1;

        public void Initialize(UnexpectedDemandConfig demandConfig, ResourceManager resourceManager, DayManager days, SpaceWeatherManager weatherManager)
        {
            config = demandConfig;
            resources = resourceManager;
            dayManager = days;
            weather = weatherManager;
        }

        private void Awake()
        {
            if (config == null || resources == null || dayManager == null)
                Debug.LogError("UnexpectedDemandManager needs UnexpectedDemandConfig, ResourceManager and DayManager.", this);
        }

        /// <summary>
        /// Rolls today's demand once per day. Skipped before firstDay and on DONKI event days (the weather card has priority).
        /// Returns true if a demand is now active.
        /// </summary>
        public bool TryRollMorningDemand()
        {
            int day = dayManager != null ? dayManager.CurrentDay : 1;
            if (config == null || lastRollDay == day) return HasActiveDemand;
            lastRollDay = day;
            HasActiveDemand = false;

            if (day < config.firstDay) return false;
            if (weather != null && weather.IsEventToday) return false;
            if (config.demands == null || config.demands.Length == 0) return false;
            if (RandomValue() >= config.dailyChance) return false;

            ActiveDemand = Pick(RandomValue());
            HasActiveDemand = true;
            DemandRolled?.Invoke(ActiveDemand);
            return true;
        }

        /// <summary>Why this choice can't be picked now, or None.</summary>
        public ActionBlockReason CanChoose(int choiceIndex)
        {
            if (!HasActiveDemand || ActiveDemand.choices == null || choiceIndex < 0 || choiceIndex >= ActiveDemand.choices.Length)
                return ActionBlockReason.NoActiveDemand;
            var costs = ActiveDemand.choices[choiceIndex].costs;
            if (costs != null)
            {
                foreach (var c in costs)
                {
                    if (resources.Has(c.resource, c.amount)) continue;
                    return c.resource == ResourceType.Parts ? ActionBlockReason.NotEnoughParts : ActionBlockReason.NotEnoughPower;
                }
            }
            return ActionBlockReason.None;
        }

        /// <summary>Pays the choice's costs, applies its effects and closes the demand.</summary>
        public bool ResolveDemandChoice(int choiceIndex)
        {
            if (CanChoose(choiceIndex) != ActionBlockReason.None) return false;
            var choice = ActiveDemand.choices[choiceIndex];
            if (choice.costs != null)
                foreach (var c in choice.costs) resources.Add(c.resource, -c.amount);
            if (choice.effects != null)
                foreach (var e in choice.effects) resources.Add(e.resource, e.amount);

            HasActiveDemand = false;
            DemandResolved?.Invoke(ActiveDemand.demandId, choiceIndex);
            return true;
        }

        /// <summary>Drops an unanswered demand. Not used by End Day any more: a pending demand blocks End Day instead.</summary>
        public void ClearActiveDemand() => HasActiveDemand = false;

        private UnexpectedDemandConfig.Demand Pick(float roll)
        {
            float total = 0f;
            foreach (var d in config.demands) total += Mathf.Max(0f, d.weight);
            if (total <= 0f) return config.demands[0];

            float target = roll * total;
            foreach (var d in config.demands)
            {
                target -= Mathf.Max(0f, d.weight);
                if (target < 0f) return d;
            }
            return config.demands[config.demands.Length - 1];
        }
    }
}
