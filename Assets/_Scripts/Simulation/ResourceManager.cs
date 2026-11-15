using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Holds the current value of every resource. Starting values come from GameConfig.</summary>
    public class ResourceManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        /// <summary>Raised whenever a resource changes, and once per resource on Start.</summary>
        public event Action<ResourceType, int, int> ResourceChanged;

        /// <summary>Raised when a resource's passive net change per day changes, and once per resource on Start.</summary>
        public event Action<ResourceType, int> NetRateChanged;

        private readonly int[] current = new int[ResourceTypeCount];
        private readonly int[] max = new int[ResourceTypeCount];
        private readonly int[] netRate = new int[ResourceTypeCount];

        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("ResourceManager has no GameConfig assigned.", this);
                return;
            }

            foreach (var r in config.resources)
            {
                max[(int)r.type] = r.maxValue;
                current[(int)r.type] = Mathf.Clamp(r.startValue, 0, r.maxValue);
                netRate[(int)r.type] = r.netPerDay;
            }
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

        /// <summary>Sets the passive net change per day (gains minus base requirements and running costs).</summary>
        public void SetNetRate(ResourceType type, int perDay)
        {
            int i = (int)type;
            if (netRate[i] == perDay) return;
            netRate[i] = perDay;
            NetRateChanged?.Invoke(type, perDay);
        }

        public int Get(ResourceType type) => current[(int)type];
        public int GetMax(ResourceType type) => max[(int)type];

        /// <summary>Adds (or, with a negative amount, removes) a resource, clamped to [0, max].</summary>
        public void Add(ResourceType type, int amount)
        {
            int i = (int)type;
            int next = Mathf.Clamp(current[i] + amount, 0, max[i]);
            if (next == current[i]) return;
            current[i] = next;
            ResourceChanged?.Invoke(type, current[i], max[i]);
        }
    }
}
