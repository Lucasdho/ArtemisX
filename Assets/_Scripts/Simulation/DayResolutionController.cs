using System;
using ArtemisX.Core;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>
    /// Runs End Day: locks input, lets visuals play Daylight -> Storm/Radiation -> Night, then resolves the night
    /// (storm damage, passive deltas, win / day cap), brings the rover home, starts the next morning, rolls an
    /// Unexpected Demand and hands a DailyReportData to the morning cards.
    /// </summary>
    public class DayResolutionController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private DayManager dayManager;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private SpaceWeatherManager weather;
        [SerializeField] private RoverExpeditionManager rover;
        [SerializeField] private SolarFieldManager solar;
        [SerializeField] private BuildingManager buildings;
        [SerializeField] private SupplyManager supply;
        [SerializeField] private UnexpectedDemandManager demands;
        [Tooltip("Optional. Receives mission end, first building and overnight delta hooks.")]
        [SerializeField] private StoryDirector storyDirector;

        /// <summary>
        /// End Day was pressed (argument: today's DONKI weather). Only the visual day-to-night player should subscribe:
        /// it must call CompleteNightResolution() when its transition ends. With no subscriber the night resolves at once.
        /// </summary>
        public event Action<WeatherEventType> DayResolvingStarted;

        /// <summary>The night has been resolved (raised before the day advances, and also on the final night).</summary>
        public event Action<DailyReportData> NightResolved;

        /// <summary>
        /// The new day has started and input is unlocked. Show, in order: Daily Report, Unexpected Demand
        /// (if report.demandRolled), Space Weather Choice (if report.todayWeather is not Calm).
        /// </summary>
        public event Action<DailyReportData> MorningReady;

        /// <summary>The run is over: (reason, stars). StoryDirector.TriggerMissionEnd is also called if wired.</summary>
        public event Action<MissionEndReason, int> MissionFinished;

        /// <summary>CanEndDay() may have changed (an Unexpected Demand was rolled or answered).</summary>
        public event Action EndDayAvailabilityChanged;

        private static readonly int ResourceTypeCount = Enum.GetValues(typeof(ResourceType)).Length;

        public bool IsResolving { get; private set; }
        public bool IsMissionOver { get; private set; }
        public DailyReportData LastReport { get; private set; }
        public int CurrentDay => dayManager != null ? dayManager.CurrentDay : 1;
        public int MissionLength => config != null ? config.missionLength : 30;
        public ResourceManager Resources => resources;

        private bool subscribed;

        /// <summary>Sets every reference. Tests call this directly; in scenes the serialized fields are used.</summary>
        public void Initialize(GameConfig gameConfig, DayManager days, ResourceManager resourceManager,
            SpaceWeatherManager weatherManager, RoverExpeditionManager roverManager, SolarFieldManager solarManager,
            BuildingManager buildingManager, SupplyManager supplyManager, UnexpectedDemandManager demandManager,
            StoryDirector story = null)
        {
            Unsubscribe();
            config = gameConfig;
            dayManager = days;
            resources = resourceManager;
            weather = weatherManager;
            rover = roverManager;
            solar = solarManager;
            buildings = buildingManager;
            supply = supplyManager;
            demands = demandManager;
            storyDirector = story;
            Subscribe();
            RefreshNetRates();
        }

        private void Awake()
        {
            if (config == null || dayManager == null || resources == null)
                Debug.LogError("DayResolutionController needs GameConfig, DayManager and ResourceManager.", this);
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void Start() => RefreshNetRates();

        /// <summary>Passive change per day for one resource: base change + solar field + buildings.</summary>
        public int GetPassiveDelta(ResourceType type)
        {
            int delta = config != null ? config.GetResource(type).dailyBaseChange : 0;
            if (type == ResourceType.Power && solar != null) delta += solar.PowerPerDay;
            if (buildings != null) delta += buildings.GetDailyOutput(type);
            return delta;
        }

        /// <summary>Pushes the passive net rates to ResourceManager (NetRateChanged). Excludes supply and rover hauls.</summary>
        public void RefreshNetRates()
        {
            if (resources == null) return;
            for (int i = 0; i < ResourceTypeCount; i++)
                resources.SetNetRate((ResourceType)i, GetPassiveDelta((ResourceType)i));
        }

        /// <summary>Why End Day can't run now: InputLocked while resolving or after the run, DemandPending until the demand is answered.</summary>
        public ActionBlockReason CanEndDay()
        {
            if (IsResolving || IsMissionOver || dayManager == null || resources == null) return ActionBlockReason.InputLocked;
            if (demands != null && demands.HasActiveDemand) return ActionBlockReason.DemandPending;
            return ActionBlockReason.None;
        }

        /// <summary>Player pressed End Day / Next Day. Returns false if a night is resolving, the run is over or a demand is unanswered.</summary>
        public bool EndDay()
        {
            if (CanEndDay() != ActionBlockReason.None) return false;

            IsResolving = true;
            SetInputLocked(true);
            var today = weather != null ? weather.TodayWeather : WeatherEventType.Calm;

            var handler = DayResolvingStarted;
            if (handler == null) CompleteNightResolution();
            else handler(today);
            return true;
        }

        /// <summary>Called when the day-to-night visuals finish (or right away with no visual listener).</summary>
        public void CompleteNightResolution()
        {
            if (!IsResolving) return;

            int day = CurrentDay;
            var summary = new DailyReportData { day = day, newDay = day };
            var before = SnapshotResources();

            summary.nightWeather = weather != null ? weather.TodayWeather : WeatherEventType.Calm;
            summary.stormHit = summary.nightWeather == WeatherEventType.SolarStorm;
            if (summary.stormHit)
            {
                if (solar != null) summary.solarBroken = solar.ResolveStorm();
                if (rover != null) rover.NotifyStorm();
            }

            for (int i = 0; i < ResourceTypeCount; i++)
            {
                int passive = GetPassiveDelta((ResourceType)i);
                summary.passiveDeltas[i] = passive;
                if (passive != 0) resources.Add((ResourceType)i, passive);
            }
            RefreshNetRates();

            int stars = buildings != null ? buildings.Stars : 0;
            int goal = buildings != null && buildings.Config != null ? buildings.Config.Count : 3;
            bool earlyWin = stars >= goal;
            bool dayCap = !earlyWin && day >= MissionLength;

            if (earlyWin || dayCap)
            {
                summary.missionEnded = true;
                FinishSummary(summary, before);
                IsMissionOver = true;
                IsResolving = false;
                NightResolved?.Invoke(summary);

                var reason = earlyWin ? MissionEndReason.EarlyWin : MissionEndReason.DayCap;
                MissionFinished?.Invoke(reason, stars);
                if (storyDirector != null) storyDirector.TriggerMissionEnd(reason, stars);
                return;
            }

            // Next morning: the rover drives home before Bauzer's briefing so its haul is part of the overnight report.
            if (rover != null) summary.rover = rover.ResolveReturn();
            FinishSummary(summary, before);
            NightResolved?.Invoke(summary);

            SetInputLocked(false);
            // A retracted field deploys again by itself each morning (broken stays broken).
            if (solar != null) summary.solarRedeployed = solar.AutoRedeploy();
            if (storyDirector != null) storyDirector.SetOvernightBaseline(before);
            dayManager.AdvanceDay(); // Weather, supply and StoryDirector listen to DayAdvanced.

            summary.newDay = CurrentDay;
            if (supply != null && SupplyManager.IsSupplyDay(summary.newDay, supply.Interval))
            {
                summary.supplyArrived = true;
                summary.supplyFood = config.supplyFood;
                summary.supplyParts = config.supplyParts;
            }
            summary.todayWeather = weather != null ? weather.TodayWeather : WeatherEventType.Calm;
            summary.demandRolled = demands != null && demands.TryRollMorningDemand();
            summary.bauzerSummaryKey = PickSummaryKey(summary);

            IsResolving = false;
            MorningReady?.Invoke(summary);
        }

        private void FinishSummary(DailyReportData summary, int[] before)
        {
            for (int i = 0; i < ResourceTypeCount; i++)
                summary.resourceDeltas[i] = resources.Get((ResourceType)i) - before[i];
            summary.crewRestricted = resources.IsCrewRestricted;
            summary.bauzerSummaryKey = PickSummaryKey(summary);
            LastReport = summary;
        }

        private static string PickSummaryKey(DailyReportData r)
        {
            if (r.missionEnded) return "mission_end";
            if (r.crewRestricted) return "crew_restricted";
            if (r.solarBroken) return "solar_broken";
            if (r.rover.returned && r.rover.isBroken) return "rover_broken";
            if (r.supplyArrived) return "supply_arrived";
            if (r.rover.returned) return "rover_haul";
            if (r.nightWeather != WeatherEventType.Calm) return "storm_safe";
            return "calm";
        }

        private int[] SnapshotResources()
        {
            var values = new int[ResourceTypeCount];
            for (int i = 0; i < ResourceTypeCount; i++)
                values[i] = resources.Get((ResourceType)i);
            return values;
        }

        private void SetInputLocked(bool locked)
        {
            if (rover != null) rover.InputLocked = locked;
            if (solar != null) solar.InputLocked = locked;
            if (buildings != null) buildings.InputLocked = locked;
            if (supply != null) supply.InputLocked = locked;
        }

        private void OnBuildingCompleted(int index)
        {
            RefreshNetRates();
            if (storyDirector != null) storyDirector.TriggerBuildingCompleted(index);
        }

        private void OnSolarStateChanged(SolarFieldState _) => RefreshNetRates();
        private void OnDemandRolled(UnexpectedDemandConfig.Demand _) => EndDayAvailabilityChanged?.Invoke();
        private void OnDemandResolved(string _, int __) => EndDayAvailabilityChanged?.Invoke();

        private void Subscribe()
        {
            if (subscribed) return;
            if (buildings != null) buildings.BuildingCompleted += OnBuildingCompleted;
            if (solar != null) solar.SolarStateChanged += OnSolarStateChanged;
            if (demands != null)
            {
                demands.DemandRolled += OnDemandRolled;
                demands.DemandResolved += OnDemandResolved;
            }
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            if (buildings != null) buildings.BuildingCompleted -= OnBuildingCompleted;
            if (solar != null) solar.SolarStateChanged -= OnSolarStateChanged;
            if (demands != null)
            {
                demands.DemandRolled -= OnDemandRolled;
                demands.DemandResolved -= OnDemandResolved;
            }
            subscribed = false;
        }
    }
}
