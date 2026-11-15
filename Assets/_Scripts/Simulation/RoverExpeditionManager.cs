using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>The single rover: dispatch to a site (Power paid now), return next morning with a haul or broken, repair with Parts.</summary>
    public class RoverExpeditionManager : MonoBehaviour
    {
        /// <summary>What happened when the rover came back.</summary>
        public struct ReturnResult
        {
            public bool returned;
            public string siteId;
            public bool isBroken;
            public bool stormHit;
            public int water;
            public int parts;
        }

        [SerializeField] private ExpeditionSiteConfig config;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private ResourceManager resources;
        [Tooltip("Optional. Locks dispatch on Solar Radiation days.")]
        [SerializeField] private SpaceWeatherManager weather;

        public event Action<string> RoverDispatched;
        /// <summary>(siteId, water, parts, isBroken). Water and parts are 0 when broken.</summary>
        public event Action<string, int, int, bool> RoverReturned;
        public event Action<RoverStatus> RoverStatusChanged;
        public event Action<BaseComponent> ComponentBroken;
        public event Action<BaseComponent> ComponentRepaired;

        /// <summary>Returns a value in [0, 1). Replace in tests for deterministic rolls.</summary>
        public Func<float> RandomValue { get; set; } = () => UnityEngine.Random.value;

        public ExpeditionSiteConfig Config => config;
        /// <summary>Ready / OnMission / Broken, or LockedByRadiation when Ready but docked by today's radiation.</summary>
        public RoverStatus Status =>
            baseStatus == RoverStatus.Ready && weather != null && weather.IsRoverLockedToday ? RoverStatus.LockedByRadiation : baseStatus;

        private RoverStatus baseStatus = RoverStatus.Ready;
        private bool subscribed;
        public string CurrentSiteId { get; private set; }
        public bool StormHitWhileOut { get; private set; }
        public bool InputLocked { get; set; }
        public string RoverName => config != null ? config.roverName : "Rover";
        public int RepairCost => gameConfig != null ? gameConfig.repairPartsCost : 5;

        public void Initialize(ExpeditionSiteConfig siteConfig, GameConfig baseConfig, ResourceManager resourceManager,
            SpaceWeatherManager weatherManager = null)
        {
            Unsubscribe();
            config = siteConfig;
            gameConfig = baseConfig;
            resources = resourceManager;
            weather = weatherManager;
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Subscribe()
        {
            if (subscribed || weather == null) return;
            weather.RoverLockChanged += OnRoverLockChanged;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || weather == null) return;
            weather.RoverLockChanged -= OnRoverLockChanged;
            subscribed = false;
        }

        private void OnRoverLockChanged(bool _)
        {
            if (baseStatus == RoverStatus.Ready) RoverStatusChanged?.Invoke(Status);
        }

        private void Awake()
        {
            if (config == null || gameConfig == null || resources == null)
                Debug.LogError("RoverExpeditionManager needs ExpeditionSiteConfig, GameConfig and ResourceManager.", this);
        }

        /// <summary>Why the rover can't go to this site right now, or None.</summary>
        public ActionBlockReason CanDispatch(string siteId)
        {
            if (InputLocked) return ActionBlockReason.InputLocked;
            if (resources != null && resources.IsCrewRestricted) return ActionBlockReason.CrewRestricted;
            if (baseStatus == RoverStatus.OnMission) return ActionBlockReason.RoverOnMission;
            if (baseStatus == RoverStatus.Broken) return ActionBlockReason.RoverBroken;
            if (weather != null && weather.IsRoverLockedToday) return ActionBlockReason.RoverLockedByRadiation;
            if (config == null || !config.TryGetSite(siteId, out var site)) return ActionBlockReason.UnknownSite;
            if (resources == null || !resources.Has(ResourceType.Power, site.powerCost)) return ActionBlockReason.NotEnoughPower;
            return ActionBlockReason.None;
        }

        /// <summary>Pays the site's Power now and sends the rover out until next morning. Cannot be cancelled.</summary>
        public bool TryDispatchRover(string siteId)
        {
            if (CanDispatch(siteId) != ActionBlockReason.None) return false;
            config.TryGetSite(siteId, out var site);
            resources.Add(ResourceType.Power, -site.powerCost);
            CurrentSiteId = siteId;
            StormHitWhileOut = false;
            SetStatus(RoverStatus.OnMission);
            RoverDispatched?.Invoke(siteId);
            return true;
        }

        /// <summary>Called during night resolution when a storm hits.</summary>
        public void NotifyStorm()
        {
            if (baseStatus == RoverStatus.OnMission) StormHitWhileOut = true;
        }

        /// <summary>Brings the rover home (next morning). Storm: broken, no haul. Calm: roll the site's risk.</summary>
        public ReturnResult ResolveReturn()
        {
            var result = new ReturnResult();
            if (baseStatus != RoverStatus.OnMission) return result;

            config.TryGetSite(CurrentSiteId, out var site);
            float breakChance = StormHitWhileOut ? config.stormBreakdownChance : site.calmBreakdownChance;
            bool broken = breakChance > 0f && RandomValue() < breakChance;

            result.returned = true;
            result.siteId = CurrentSiteId;
            result.isBroken = broken;
            result.stormHit = StormHitWhileOut;
            if (!broken)
            {
                result.water = site.waterReward;
                result.parts = site.partsReward;
                if (result.water != 0) resources.Add(ResourceType.Water, result.water);
                if (result.parts != 0) resources.Add(ResourceType.Parts, result.parts);
            }

            string siteId = CurrentSiteId;
            CurrentSiteId = null;
            StormHitWhileOut = false;
            SetStatus(broken ? RoverStatus.Broken : RoverStatus.Ready);
            RoverReturned?.Invoke(siteId, result.water, result.parts, broken);
            if (broken) ComponentBroken?.Invoke(BaseComponent.Rover);
            return result;
        }

        public ActionBlockReason CanRepair()
        {
            if (InputLocked) return ActionBlockReason.InputLocked;
            if (baseStatus != RoverStatus.Broken) return ActionBlockReason.NothingToRepair;
            if (resources == null || !resources.Has(ResourceType.Parts, RepairCost)) return ActionBlockReason.NotEnoughParts;
            return ActionBlockReason.None;
        }

        /// <summary>Pays the repair Parts and makes the rover Ready.</summary>
        public bool TryRepairRover()
        {
            if (CanRepair() != ActionBlockReason.None) return false;
            resources.Add(ResourceType.Parts, -RepairCost);
            SetStatus(RoverStatus.Ready);
            ComponentRepaired?.Invoke(BaseComponent.Rover);
            return true;
        }

        private void SetStatus(RoverStatus status)
        {
            if (baseStatus == status) return;
            baseStatus = status;
            RoverStatusChanged?.Invoke(Status);
        }
    }
}
