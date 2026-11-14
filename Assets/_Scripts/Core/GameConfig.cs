using System;
using UnityEngine;

namespace ArtemisX.Core
{
    /// <summary>All tunable numbers for the game. One asset lives in Assets/_Config/.</summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Artemis X/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Serializable]
        public struct ResourceSettings
        {
            public ResourceType type;
            [Min(0)] public int startValue;
            [Min(1)] public int maxValue;
        }

        [Header("Mission")]
        [Min(1)] public int startDay = 1;

        [Header("Resources")]
        public ResourceSettings[] resources =
        {
            new ResourceSettings { type = ResourceType.Power,  startValue = 60, maxValue = 100 },
            new ResourceSettings { type = ResourceType.Water,  startValue = 40, maxValue = 100 },
            new ResourceSettings { type = ResourceType.Oxygen, startValue = 70, maxValue = 100 },
            new ResourceSettings { type = ResourceType.Food,   startValue = 50, maxValue = 100 },
            new ResourceSettings { type = ResourceType.Parts,  startValue = 20, maxValue = 50 },
        };
    }
}
