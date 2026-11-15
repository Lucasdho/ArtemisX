using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Holds the current value of every resource and the crew restriction state. Starting values come from GameConfig.</summary>
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        /// <summary>Raised whenever a resource changes (type, value, max), and once per resource on Start.</summary>
        public event Action<ResourceType, int, int> ResourceChanged;

        /// <summary>Raised when a resource's passive net change per day changes, and once per resource on Start.</summary>
        public event Action<ResourceType, int> NetRateChanged;

        /// <summary>Raised when the crew restriction turns on or off. The type is the resource at zero (Oxygen or Food), or the one restored.</summary>
        public event Action<bool, ResourceType> CrewRestrictionChanged;

        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;

        private readonly int[] current = new int[ResourceTypeCount];
        private readonly int[] max = new int[ResourceTypeCount];
        private readonly int[] netRate = new int[ResourceTypeCount];

        public GameConfig Config => config;

        /// <summary>True while Oxygen or Food is at 0. Blocks building and rover dispatch, never ends the game.</summary>
        public bool IsCrewRestricted { get; private set; }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("ResourceManager has no GameConfig assigned.", this);
                return;
            }
            Initialize(config);
        }

        /// <summary>Loads start and max values from the config. Called from Awake; tests call it directly.</summary>
        public void Initialize(GameConfig gameConfig)
        {
            config = gameConfig;
            foreach (var r in config.resources)
            {
                max[(int)r.type] = r.maxValue;
                current[(int)r.type] = Mathf.Clamp(r.startValue, 0, r.maxValue);
                netRate[(int)r.type] = r.dailyBaseChange;
            }
            IsCrewRestricted = ComputeRestricted();
        }

        private void Start()
        {
            // Listeners subscribe in OnEnable, so broadcasting here gives every bar its starting value.
            for (int i = 0; i < ResourceTypeCount; i++)
            {
                ResourceChanged?.Invoke((ResourceType)i, current[i], max[i]);
                NetRateChanged?.Invoke((ResourceType)i, netRate[i]);
            }
        }

        public int GetNetRate(ResourceType type) => netRate[(int)type];

        /// <summary>Sets the passive net change per day (gains minus base requirements). Excludes supply and rover hauls.</summary>
        public void SetNetRate(ResourceType type, int perDay)
        {
            int i = (int)type;
            if (netRate[i] == perDay) return;
            netRate[i] = perDay;
            NetRateChanged?.Invoke(type, perDay);
        }

        public int Get(ResourceType type) => current[(int)type];
        public int GetMax(ResourceType type) => max[(int)type];

        public bool Has(ResourceType type, int amount) => current[(int)type] >= amount;

        /// <summary>Adds (or, with a negative amount, removes) a resource, clamped to [0, max].</summary>
        public void Add(ResourceType type, int amount)
        {
            int i = (int)type;
            int next = Mathf.Clamp(current[i] + amount, 0, max[i]);
            if (next == current[i]) return;
            current[i] = next;
            ResourceChanged?.Invoke(type, current[i], max[i]);
            UpdateCrewRestriction(type);
        }

        /// <summary>Removes the amount only if there is enough of it.</summary>
        public bool TrySpend(ResourceType type, int amount)
        {
            if (!Has(type, amount)) return false;
            Add(type, -amount);
            return true;
        }

        /// <summary>Removes both amounts only if there is enough of both.</summary>
        public bool TrySpend(ResourceType typeA, int amountA, ResourceType typeB, int amountB)
        {
            if (!Has(typeA, amountA) || !Has(typeB, amountB)) return false;
            Add(typeA, -amountA);
            Add(typeB, -amountB);
            return true;
        }

        private bool ComputeRestricted() =>
            current[(int)ResourceType.Oxygen] <= 0 || current[(int)ResourceType.Food] <= 0;

        private void UpdateCrewRestriction(ResourceType changed)
        {
            bool restricted = ComputeRestricted();
            if (restricted == IsCrewRestricted) return;
            IsCrewRestricted = restricted;

            ResourceType cause = changed;
            if (restricted)
                cause = current[(int)ResourceType.Oxygen] <= 0 ? ResourceType.Oxygen : ResourceType.Food;
            CrewRestrictionChanged?.Invoke(restricted, cause);
        }
    }
}
