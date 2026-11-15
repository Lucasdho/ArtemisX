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
            [TextArea(2, 4)] public string scienceWhy;
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
                buildingId = "processing_plant", displayName = "Processing Plant",
                scienceWhy = "Melts lunar ice into Water and refines moon rocks into Parts (ISRU), cutting supply trips from Earth.",
                partsCost = 15, powerCost = 20,
                dailyOutput = new[] { new ResourceAmount(ResourceType.Water, 10), new ResourceAmount(ResourceType.Parts, 4) }
            },
            new Building
            {
                buildingId = "electrolyser", displayName = "Electrolyser",
                scienceWhy = "Splits Water (H2O) with solar electricity into breathable Oxygen (O2) and Hydrogen (H2) rocket fuel.",
                partsCost = 25, powerCost = 25,
                dailyOutput = new[] { new ResourceAmount(ResourceType.Oxygen, 12) }
            },
            new Building
            {
                buildingId = "terraforming", displayName = "Terraforming Dome",
                scienceWhy = "Uses spare Power for grow lights and Water for hydroponics to grow fresh Food on the Moon.",
                partsCost = 35, powerCost = 30,
                dailyOutput = new[] { new ResourceAmount(ResourceType.Food, 12) }
            },
        };

        public int Count => buildings != null ? buildings.Length : 0;
    }
}
