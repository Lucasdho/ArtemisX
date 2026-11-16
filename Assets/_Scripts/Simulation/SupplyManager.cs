using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Earth supply ship: every supplyInterval days a clickable pile (Food + Parts) waits to be collected.</summary>
    public class SupplyManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private DayManager dayManager;

        /// <summary>(day, food, parts) of the pile waiting at the base.</summary>
        public event Action<int, int, int> SupplyDropped;
        /// <summary>(food, parts) added to the stock.</summary>
        public event Action<int, int> SupplyCollected;

        public int PendingDrops { get; private set; }
        public bool HasPendingSupply => PendingDrops > 0;
        public int PendingFood => PendingDrops * (config != null ? config.supplyFood : 0);
        public int PendingParts => PendingDrops * (config != null ? config.supplyParts : 0);
        public bool InputLocked { get; set; }
        public int Interval => config != null ? Mathf.Max(1, config.supplyInterval) : 5;

        private bool subscribed;

        public void Initialize(GameConfig gameConfig, ResourceManager resourceManager, DayManager days)
        {
            Unsubscribe();
            config = gameConfig;
            resources = resourceManager;
            dayManager = days;
            Subscribe();
        }

        private void Awake()
        {
            if (config == null || resources == null)
                Debug.LogError("SupplyManager needs GameConfig and ResourceManager.", this);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private bool starterDropped;

        public bool StarterDropped => starterDropped;

        public static bool IsSupplyDay(int day, int interval) => day > 0 && interval > 0 && day % interval == 0;

        /// <summary>Spawns a one-time starter supply crate for Day 1 onboarding without altering the 5-day cadence.</summary>
        public bool DropStarterSupply(int day = 1)
        {
            if (starterDropped || HasPendingSupply) return false;
            starterDropped = true;
            PendingDrops++;
            SupplyDropped?.Invoke(day, PendingFood, PendingParts);
            return true;
        }

        /// <summary>Morning of a new day: drops a pile on supply days.</summary>
        public void OnDayStarted(int day)
        {
            if (!IsSupplyDay(day, Interval)) return;
            PendingDrops++;
            SupplyDropped?.Invoke(day, PendingFood, PendingParts);
        }

        /// <summary>Adds the waiting pile to the stock and clears it.</summary>
        public bool CollectSupply()
        {
            if (InputLocked || !HasPendingSupply) return false;
            int food = PendingFood;
            int parts = PendingParts;
            PendingDrops = 0;
            resources.Add(ResourceType.Food, food);
            resources.Add(ResourceType.Parts, parts);
            SupplyCollected?.Invoke(food, parts);
            return true;
        }

        private void OnDayAdvanced(int day) => OnDayStarted(day);

        private void Subscribe()
        {
            if (subscribed || dayManager == null) return;
            dayManager.DayAdvanced += OnDayAdvanced;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || dayManager == null) return;
            dayManager.DayAdvanced -= OnDayAdvanced;
            subscribed = false;
        }
    }
}
