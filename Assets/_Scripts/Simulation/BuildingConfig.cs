using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>The 3 linear self-sufficiency buildings. Each one built is one Star.</summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "Artemis X/Simulation/Building Config")]
    public class BuildingConfig : ScriptableObject
    {
        [Serializable]
        public struct Building
        {
            public string buildingId;
            public string displayName;
            [Min(0)] public int partsCost;
            [Min(0)] public int powerCost;
            [Tooltip("Passive gain per day once built.")]
            public ResourceAmount[] dailyOutput;
        }

        [Tooltip("Strict build order: index 0 first.")]
        public Building[] buildings =
        {
            new Building
            {
                buildingId = "water_o2_plant", displayName = "Water & O2 Plant", partsCost = 15, powerCost = 20,
                dailyOutput = new[] { new ResourceAmount(ResourceType.Water, 10), new ResourceAmount(ResourceType.Oxygen, 12) }
            },
            new Building
            {
                buildingId = "greenhouse", displayName = "Greenhouse", partsCost = 25, powerCost = 25,
                dailyOutput = new[] { new ResourceAmount(ResourceType.Food, 12) }
            },
            new Building
            {
                buildingId = "fuel_reserve", displayName = "Fuel Reserve", partsCost = 35, powerCost = 30,
                dailyOutput = new ResourceAmount[0]
            },
        };

        public int Count => buildings != null ? buildings.Length : 0;
    }
}
