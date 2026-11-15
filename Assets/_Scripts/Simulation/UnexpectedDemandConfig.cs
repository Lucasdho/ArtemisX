using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Random morning demands: 2-choice cards rolled on normal (non-DONKI) days.</summary>
    [CreateAssetMenu(fileName = "UnexpectedDemandConfig", menuName = "Artemis X/Simulation/Unexpected Demand Config")]
    public class UnexpectedDemandConfig : ScriptableObject
    {
        [Serializable]
        public struct Choice
        {
            public string label;
            [Tooltip("Must be affordable to pick this choice; paid at once.")]
            public ResourceAmount[] costs;
            [Tooltip("Applied at once (clamped to 0..max). Negative values are losses that don't need to be affordable.")]
            public ResourceAmount[] effects;
        }

        [Serializable]
        public struct Demand
        {
            public string demandId;
            [Tooltip("Pool the demand belongs to, e.g. Leak / Clogged Filter or Earth Science Request.")]
            public string pool;
            public string title;
            [Min(0f)] public float weight;
            public Choice[] choices;
        }

        [Tooltip("Chance to roll one demand on a normal morning.")]
        [Range(0f, 1f)] public float dailyChance = 0.27f;
        [Tooltip("First day a demand can appear (Day 1 is onboarding).")]
        [Min(1)] public int firstDay = 2;

        public Demand[] demands =
        {
            new Demand
            {
                demandId = "water_leak", pool = "Leak / Clogged Filter", title = "Water Leak", weight = 1f,
                choices = new[]
                {
                    new Choice { label = "Fix Filter (-5 Parts)", costs = new[] { new ResourceAmount(ResourceType.Parts, 5) }, effects = new ResourceAmount[0] },
                    new Choice { label = "Patch Later (-6 Water)", costs = new ResourceAmount[0], effects = new[] { new ResourceAmount(ResourceType.Water, -6) } },
                }
            },
            new Demand
            {
                demandId = "clogged_filter", pool = "Leak / Clogged Filter", title = "Clogged Air Filter", weight = 1f,
                choices = new[]
                {
                    new Choice { label = "Fix Filter (-5 Parts)", costs = new[] { new ResourceAmount(ResourceType.Parts, 5) }, effects = new ResourceAmount[0] },
                    new Choice { label = "Patch Later (-6 Oxygen)", costs = new ResourceAmount[0], effects = new[] { new ResourceAmount(ResourceType.Oxygen, -6) } },
                }
            },
            new Demand
            {
                demandId = "science_food", pool = "Earth Science Request", title = "Earth Science Request: Plant Study", weight = 1f,
                choices = new[]
                {
                    new Choice { label = "Run Science (-12 Power, +15 Food)", costs = new[] { new ResourceAmount(ResourceType.Power, 12) }, effects = new[] { new ResourceAmount(ResourceType.Food, 15) } },
                    new Choice { label = "Skip Today", costs = new ResourceAmount[0], effects = new ResourceAmount[0] },
                }
            },
            new Demand
            {
                demandId = "science_parts", pool = "Earth Science Request", title = "Earth Science Request: Moon Rock Study", weight = 1f,
                choices = new[]
                {
                    new Choice { label = "Run Science (-12 Power, +10 Parts)", costs = new[] { new ResourceAmount(ResourceType.Power, 12) }, effects = new[] { new ResourceAmount(ResourceType.Parts, 10) } },
                    new Choice { label = "Skip Today", costs = new ResourceAmount[0], effects = new ResourceAmount[0] },
                }
            },
        };
    }
}
