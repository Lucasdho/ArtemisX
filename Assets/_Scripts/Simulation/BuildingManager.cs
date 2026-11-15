using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Strictly linear build order. Building pays Parts + Power now and awards its Star right away.</summary>
    public class BuildingManager : MonoBehaviour
    {
        [SerializeField] private BuildingConfig config;
        [SerializeField] private ResourceManager resources;

        /// <summary>Index (0-based) of the building just completed.</summary>
        public event Action<int> BuildingCompleted;
        public event Action<int> StarsChanged;

        public BuildingConfig Config => config;
        public int BuiltCount { get; private set; }
        public int Stars => BuiltCount;
        public int NextIndex => BuiltCount;
        public bool AllBuilt => config == null || BuiltCount >= config.Count;
        public bool InputLocked { get; set; }

        public void Initialize(BuildingConfig buildingConfig, ResourceManager resourceManager)
        {
            config = buildingConfig;
            resources = resourceManager;
        }

        private void Awake()
        {
            if (config == null || resources == null)
                Debug.LogError("BuildingManager needs BuildingConfig and ResourceManager.", this);
        }

        private void Start() => StarsChanged?.Invoke(Stars);

        public bool IsBuilt(int index) => index < BuiltCount;

        /// <summary>The next building to build. Only valid while AllBuilt is false.</summary>
        public BuildingConfig.Building NextBuilding => config.buildings[NextIndex];

        /// <summary>Why the next building can't be built now, or None.</summary>
        public ActionBlockReason CanBuildNext()
        {
            if (InputLocked) return ActionBlockReason.InputLocked;
            if (AllBuilt) return ActionBlockReason.AllBuilt;
            if (resources.IsCrewRestricted) return ActionBlockReason.CrewRestricted;
            var b = NextBuilding;
            if (!resources.Has(ResourceType.Parts, b.partsCost)) return ActionBlockReason.NotEnoughParts;
            if (!resources.Has(ResourceType.Power, b.powerCost)) return ActionBlockReason.NotEnoughPower;
            return ActionBlockReason.None;
        }

        /// <summary>Builds the next building in order: pays Parts + Power, adds a Star.</summary>
        public bool TryBuildNext()
        {
            if (CanBuildNext() != ActionBlockReason.None) return false;
            var b = NextBuilding;
            resources.TrySpend(ResourceType.Parts, b.partsCost, ResourceType.Power, b.powerCost);
            int index = BuiltCount;
            BuiltCount++;
            BuildingCompleted?.Invoke(index);
            StarsChanged?.Invoke(Stars);
            return true;
        }

        /// <summary>Sum of daily output of every completed building for one resource.</summary>
        public int GetDailyOutput(ResourceType type)
        {
            int total = 0;
            for (int i = 0; i < BuiltCount; i++)
            {
                var output = config.buildings[i].dailyOutput;
                if (output == null) continue;
                for (int j = 0; j < output.Length; j++)
                    if (output[j].resource == type) total += output[j].amount;
            }
            return total;
        }
    }
}
