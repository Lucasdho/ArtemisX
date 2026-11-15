using System;
using UnityEngine;

namespace ArtemisX.Core
{
    /// <summary>All tunable base numbers for the simulation. One asset lives in Assets/_Config/.</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Artemis X/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Serializable]
        public struct ResourceSettings
        {
            public ResourceType type;
            [Min(0)] public int startValue;
            [Min(1)] public int maxValue;
            [Tooltip("Flat change applied every night before buildings and the solar field: crew consumption, or dome power for Power.")]
            public int dailyBaseChange;
        }

        [Header("Mission")]
        [Min(1)] public int startDay = 1;
        [Tooltip("Hard day cap. Ending this day without all 3 buildings is DayCap.")]
        [Min(1)] public int missionLength = 30;

        [Header("Resources")]
        public ResourceSettings[] resources =
        {
            new ResourceSettings { type = ResourceType.Power,  startValue = 60, maxValue = 100, dailyBaseChange = 5 },
            new ResourceSettings { type = ResourceType.Water,  startValue = 40, maxValue = 100, dailyBaseChange = -8 },
            new ResourceSettings { type = ResourceType.Oxygen, startValue = 70, maxValue = 100, dailyBaseChange = -8 },
            new ResourceSettings { type = ResourceType.Food,   startValue = 50, maxValue = 100, dailyBaseChange = -8 },
            new ResourceSettings { type = ResourceType.Parts,  startValue = 20, maxValue = 50,  dailyBaseChange = 0 },
        };

        [Header("Exposed Solar Field")]
        [Tooltip("Extra Power per day while the exposed solar field is Active (on top of dome power).")]
        [Min(0)] public int solarFieldPowerPerDay = 20;
        [Tooltip("Chance the Active solar field breaks when a storm hits during the night.")]
        [Range(0f, 1f)] public float solarStormBreakChance = 0.75f;

        [Header("Repairs")]
        [Tooltip("Parts cost to repair the Rover or the solar field.")]
        [Min(0)] public int repairPartsCost = 5;

        [Header("Earth Supply Ship")]
        [Min(1)] public int supplyInterval = 5;
        [Min(0)] public int supplyFood = 30;
        [Min(0)] public int supplyParts = 15;

        /// <summary>Settings for one resource, or a zeroed entry if it is missing.</summary>
        public ResourceSettings GetResource(ResourceType type)
        {
            if (resources != null)
            {
                for (int i = 0; i < resources.Length; i++)
                    if (resources[i].type == type) return resources[i];
            }
            return new ResourceSettings { type = type, maxValue = 1 };
        }
    }
}
