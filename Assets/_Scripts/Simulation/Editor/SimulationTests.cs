using System;
using System.Collections.Generic;
using System.Linq;
using ArtemisX.Core;
using ArtemisX.Story;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtemisX.Simulation.Tests
{
    /// <summary>EditMode checks of the Run 3A simulation rules against the real _Config assets.</summary>
    public class SimulationTests
    {
        private const string GameConfigPath = "Assets/_Config/GameConfig.asset";
        private const string WeatherConfigPath = "Assets/_Config/Simulation/SpaceWeatherConfig.asset";
        private const string SiteConfigPath = "Assets/_Config/Simulation/ExpeditionSiteConfig.asset";
        private const string BuildingConfigPath = "Assets/_Config/Simulation/BuildingConfig.asset";
        private const string DemandConfigPath = "Assets/_Config/Simulation/UnexpectedDemandConfig.asset";

        private const string NearMinerals = "Hotspot_Minerals_SSE";
        private const string FarMinerals = "Hotspot_Minerals_NE";
        private const string NearIce = "Hotspot_Ice_NW";
        private const string FarIce = "Hotspot_Ice_SW";
        private const string Balanced = "Hotspot_Rocks_East";

        /// <summary>Every simulation component on one GameObject, wired like the scene will be.</summary>
        private sealed class Sim
        {
            public GameObject go;
            public GameConfig config;
            public SpaceWeatherConfig weatherConfig;
            public DayManager days;
            public ResourceManager res;
            public SpaceWeatherManager weather;
            public RoverExpeditionManager rover;
            public SolarFieldManager solar;
            public BuildingManager buildings;
            public SupplyManager supply;
            public UnexpectedDemandManager demand;
            public DayResolutionController ctrl;
            public MissionEndReason? endReason;
            public int endStars;
            public readonly List<Object> owned = new List<Object>();
        }

        private readonly List<Sim> sims = new List<Sim>();

        [TearDown]
        public void TearDown()
        {
            foreach (var s in sims)
            {
                Object.DestroyImmediate(s.go);
                foreach (var o in s.owned) Object.DestroyImmediate(o);
            }
            sims.Clear();
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Assert.IsNotNull(asset, "Missing config asset " + path);
            return asset;
        }

        /// <param name="stormDays">Solar Storm days for a test weather schedule; null uses the real DONKI asset.</param>
        private Sim Create(int[] stormDays = null, Func<float> roll = null, int[] radiationDays = null)
        {
            var s = new Sim { go = new GameObject("SimTest") };
            sims.Add(s);
            s.config = Load<GameConfig>(GameConfigPath);

            if (stormDays == null)
            {
                s.weatherConfig = Load<SpaceWeatherConfig>(WeatherConfigPath);
            }
            else
            {
                s.weatherConfig = ScriptableObject.CreateInstance<SpaceWeatherConfig>();
                s.owned.Add(s.weatherConfig);
                s.weatherConfig.days = new SpaceWeatherConfig.WeatherDay[30];
                s.weatherConfig.overclockShieldsPowerCost = Load<SpaceWeatherConfig>(WeatherConfigPath).overclockShieldsPowerCost;
                foreach (int d in stormDays) s.weatherConfig.days[d - 1].weather = WeatherEventType.SolarStorm;
                if (radiationDays != null)
                    foreach (int d in radiationDays) s.weatherConfig.days[d - 1].weather = WeatherEventType.SolarRadiation;
            }

            s.days = s.go.AddComponent<DayManager>();
            s.res = s.go.AddComponent<ResourceManager>();
            s.weather = s.go.AddComponent<SpaceWeatherManager>();
            s.rover = s.go.AddComponent<RoverExpeditionManager>();
            s.solar = s.go.AddComponent<SolarFieldManager>();
            s.buildings = s.go.AddComponent<BuildingManager>();
            s.supply = s.go.AddComponent<SupplyManager>();
            s.demand = s.go.AddComponent<UnexpectedDemandManager>();
            s.ctrl = s.go.AddComponent<DayResolutionController>();

            s.res.Initialize(s.config);
            s.weather.Initialize(s.weatherConfig, s.days, s.res, s.solar);
            s.rover.Initialize(Load<ExpeditionSiteConfig>(SiteConfigPath), s.config, s.res, s.weather);
            s.solar.Initialize(s.config, s.res);
            s.buildings.Initialize(Load<BuildingConfig>(BuildingConfigPath), s.res);
            s.supply.Initialize(s.config, s.res, s.days);
            s.demand.Initialize(Load<UnexpectedDemandConfig>(DemandConfigPath), s.res, s.days, s.weather);
            s.ctrl.Initialize(s.config, s.days, s.res, s.weather, s.rover, s.solar, s.buildings, s.supply, s.demand);

            var r = roll ?? (() => 0.999f); // default: nothing breaks on a calm night, storms still break the rover
            s.rover.RandomValue = r;
            s.solar.RandomValue = r;
            s.demand.RandomValue = () => 0.999f; // no demands unless a test asks for them
            s.ctrl.MissionFinished += (reason, stars) => { s.endReason = reason; s.endStars = stars; };
            return s;
        }

        // ---------- Config ----------

        [Test]
        public void Configs_MatchGddNumbers()
        {
            var s = Create(new int[0]);
            Assert.AreEqual(60, s.res.Get(ResourceType.Power));
            Assert.AreEqual(40, s.res.Get(ResourceType.Water));
            Assert.AreEqual(70, s.res.Get(ResourceType.Oxygen));
            Assert.AreEqual(50, s.res.Get(ResourceType.Food));
            Assert.AreEqual(20, s.res.Get(ResourceType.Parts));
            Assert.AreEqual(50, s.res.GetMax(ResourceType.Parts));
            Assert.AreEqual(25, s.ctrl.GetPassiveDelta(ResourceType.Power));
            Assert.AreEqual(-8, s.ctrl.GetPassiveDelta(ResourceType.Water));
            Assert.AreEqual(-8, s.ctrl.GetPassiveDelta(ResourceType.Oxygen));
            Assert.AreEqual(-8, s.ctrl.GetPassiveDelta(ResourceType.Food));
            Assert.AreEqual(0, s.ctrl.GetPassiveDelta(ResourceType.Parts));
        }

        [Test]
        public void DonkiSchedule_Has30Days_With4To5MixedEvents_FirstAroundDay4()
        {
            var w = Load<SpaceWeatherConfig>(WeatherConfigPath);
            Assert.AreEqual(30, w.days.Length);
            var events = Enumerable.Range(1, 30).Where(d => w.GetWeather(d) != WeatherEventType.Calm).ToList();
            Assert.That(events.Count, Is.InRange(4, 5));
            Assert.That(events[0], Is.InRange(3, 5));
            Assert.IsTrue(events.Any(d => w.GetWeather(d) == WeatherEventType.SolarStorm));
            Assert.IsTrue(events.Any(d => w.GetWeather(d) == WeatherEventType.SolarRadiation));
            foreach (int d in events)
                Assert.IsFalse(string.IsNullOrEmpty(w.GetDay(d).donkiDate), "Event day " + d + " needs DONKI metadata");
        }

        // ---------- Buildings ----------

        [Test]
        public void Buildings_AreLinear_AndDeductPartsAndPowerImmediately()
        {
            var s = Create(new int[0]);
            var completed = new List<int>();
            int stars = -1;
            s.buildings.BuildingCompleted += completed.Add;
            s.buildings.StarsChanged += v => stars = v;

            Assert.IsTrue(s.buildings.TryBuildNext());
            Assert.AreEqual(5, s.res.Get(ResourceType.Parts));
            Assert.AreEqual(40, s.res.Get(ResourceType.Power));
            Assert.AreEqual(1, stars);

            Assert.AreEqual(ActionBlockReason.NotEnoughParts, s.buildings.CanBuildNext());
            Assert.IsFalse(s.buildings.TryBuildNext());

            s.res.Add(ResourceType.Parts, 45); // 50
            Assert.IsTrue(s.buildings.TryBuildNext()); // Greenhouse: 25 Parts + 25 Power
            Assert.AreEqual(25, s.res.Get(ResourceType.Parts));
            Assert.AreEqual(15, s.res.Get(ResourceType.Power));

            s.res.Add(ResourceType.Parts, 25);
            Assert.AreEqual(ActionBlockReason.NotEnoughPower, s.buildings.CanBuildNext()); // Fuel Reserve needs 30 Power
            s.res.Add(ResourceType.Power, 20);
            Assert.IsTrue(s.buildings.TryBuildNext());
            Assert.AreEqual(ActionBlockReason.AllBuilt, s.buildings.CanBuildNext());

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, completed);
            Assert.AreEqual(3, stars);
        }

        [Test]
        public void Buildings_AddTheirPassiveOutputToNetRates()
        {
            var s = Create(new int[0]);
            s.buildings.TryBuildNext();
            Assert.AreEqual(2, s.res.GetNetRate(ResourceType.Water));
            Assert.AreEqual(4, s.res.GetNetRate(ResourceType.Oxygen));
            s.res.Add(ResourceType.Parts, 50);
            s.buildings.TryBuildNext();
            Assert.AreEqual(4, s.res.GetNetRate(ResourceType.Food));
        }

        // ---------- Rover ----------

        [Test]
        public void Rover_DeductsPowerImmediately_AndReturnsNextMorningWithHaul()
        {
            var s = Create(new int[0]);
            string dispatched = null;
            s.rover.RoverDispatched += id => dispatched = id;

            Assert.IsTrue(s.rover.TryDispatchRover(Balanced));
            Assert.AreEqual(46, s.res.Get(ResourceType.Power));
            Assert.AreEqual(RoverStatus.OnMission, s.rover.Status);
            Assert.AreEqual(Balanced, dispatched);
            Assert.AreEqual(ActionBlockReason.RoverOnMission, s.rover.CanDispatch(NearIce));

            int water = -1, parts = -1;
            bool broken = true;
            s.rover.RoverReturned += (id, w, p, b) => { water = w; parts = p; broken = b; };

            Assert.IsTrue(s.ctrl.EndDay());
            Assert.AreEqual(2, s.days.CurrentDay);
            Assert.AreEqual(RoverStatus.Ready, s.rover.Status);
            Assert.IsFalse(broken);
            Assert.AreEqual(16, water);
            Assert.AreEqual(8, parts);
            Assert.AreEqual(40 - 8 + 16, s.res.Get(ResourceType.Water));
            Assert.AreEqual(28, s.res.Get(ResourceType.Parts));
            Assert.IsTrue(s.ctrl.LastReport.rover.returned);
        }

        [Test]
        public void Rover_NeedsEnoughPower()
        {
            var s = Create(new int[0]);
            s.res.Add(ResourceType.Power, -51); // 9
            Assert.AreEqual(ActionBlockReason.NotEnoughPower, s.rover.CanDispatch(NearIce));
            Assert.IsFalse(s.rover.TryDispatchRover(NearIce));
            Assert.AreEqual(9, s.res.Get(ResourceType.Power));
        }

        [Test]
        public void Rover_AlwaysBreaksInStorm_NoHaul_RepairsFor5Parts()
        {
            var s = Create(new[] { 1 }, () => 0.999f);
            var brokenEvents = new List<BaseComponent>();
            s.rover.ComponentBroken += brokenEvents.Add;

            Assert.IsTrue(s.rover.TryDispatchRover(FarMinerals));
            s.ctrl.EndDay();

            Assert.AreEqual(RoverStatus.Broken, s.rover.Status);
            Assert.AreEqual(20, s.res.Get(ResourceType.Parts));
            CollectionAssert.Contains(brokenEvents, BaseComponent.Rover);
            Assert.AreEqual(ActionBlockReason.RoverBroken, s.rover.CanDispatch(NearIce));

            bool repaired = false;
            s.rover.ComponentRepaired += c => repaired = c == BaseComponent.Rover;
            Assert.IsTrue(s.rover.TryRepairRover());
            Assert.IsTrue(repaired);
            Assert.AreEqual(15, s.res.Get(ResourceType.Parts));
            Assert.AreEqual(RoverStatus.Ready, s.rover.Status);
        }

        [Test]
        public void Rover_CalmBreakdown_UsesSiteRisk()
        {
            var s = Create(new int[0], () => 0.10f); // 10%: above Near 5%, below Far 25%
            s.rover.TryDispatchRover(NearMinerals);
            s.ctrl.EndDay();
            Assert.AreEqual(RoverStatus.Ready, s.rover.Status);

            s.rover.TryDispatchRover(FarIce);
            s.ctrl.EndDay();
            Assert.AreEqual(RoverStatus.Broken, s.rover.Status);
        }

        // ---------- Solar field ----------

        [Test]
        public void Solar_BreaksOnlyWhenActiveDuringStorm_AndRepairsFor5Parts()
        {
            var s = Create(new[] { 2 }, () => 0f); // every roll "fails"

            s.ctrl.EndDay(); // Day 1 calm
            Assert.AreEqual(SolarFieldState.Active, s.solar.State);

            bool brokeEvent = false;
            s.solar.ComponentBroken += c => brokeEvent = c == BaseComponent.SolarField;
            s.ctrl.EndDay(); // Day 2 storm, Active
            Assert.AreEqual(SolarFieldState.Broken, s.solar.State);
            Assert.IsTrue(brokeEvent);
            Assert.IsTrue(s.ctrl.LastReport.solarBroken);
            Assert.AreEqual(5, s.res.GetNetRate(ResourceType.Power));
            Assert.IsFalse(s.solar.ToggleRetract());

            Assert.IsTrue(s.solar.TryRepairSolar());
            Assert.AreEqual(15, s.res.Get(ResourceType.Parts));
            Assert.AreEqual(SolarFieldState.Active, s.solar.State);
            Assert.AreEqual(25, s.res.GetNetRate(ResourceType.Power));
        }

        [Test]
        public void Solar_NeverBreaksWhenRetracted_AndRetractingCutsPowerTo5()
        {
            var s = Create(new[] { 1 }, () => 0f);
            Assert.IsTrue(s.solar.ToggleRetract());
            Assert.AreEqual(SolarFieldState.Retracted, s.solar.State);
            Assert.AreEqual(5, s.res.GetNetRate(ResourceType.Power));

            s.ctrl.EndDay(); // storm
            Assert.AreEqual(SolarFieldState.Retracted, s.solar.State);
            Assert.IsFalse(s.ctrl.LastReport.solarBroken);
            Assert.AreEqual(65, s.res.Get(ResourceType.Power));

            Assert.IsTrue(s.solar.ToggleRetract());
            Assert.AreEqual(25, s.res.GetNetRate(ResourceType.Power));
        }

        // ---------- Crew restriction ----------

        [Test]
        public void CrewRestriction_OnZeroOxygenOrFood_BlocksBuildAndRover_UntilRestored()
        {
            var s = Create(new int[0]);
            var changes = new List<(bool, ResourceType)>();
            s.res.CrewRestrictionChanged += (on, type) => changes.Add((on, type));

            s.res.Add(ResourceType.Oxygen, -100);
            Assert.IsTrue(s.res.IsCrewRestricted);
            Assert.AreEqual(ActionBlockReason.CrewRestricted, s.buildings.CanBuildNext());
            Assert.AreEqual(ActionBlockReason.CrewRestricted, s.rover.CanDispatch(NearIce));
            Assert.IsFalse(s.buildings.TryBuildNext());
            Assert.IsFalse(s.rover.TryDispatchRover(NearIce));
            Assert.AreEqual(60, s.res.Get(ResourceType.Power));

            s.res.Add(ResourceType.Oxygen, 10);
            Assert.IsFalse(s.res.IsCrewRestricted);
            Assert.AreEqual(ActionBlockReason.None, s.buildings.CanBuildNext());

            s.res.Add(ResourceType.Food, -100);
            Assert.IsTrue(s.res.IsCrewRestricted);
            Assert.AreEqual(ActionBlockReason.CrewRestricted, s.rover.CanDispatch(NearIce));
            s.res.Add(ResourceType.Food, 30);
            Assert.IsTrue(s.rover.TryDispatchRover(NearIce));

            Assert.AreEqual((true, ResourceType.Oxygen), changes[0]);
            Assert.AreEqual(false, changes[1].Item1);
            Assert.AreEqual((true, ResourceType.Food), changes[2]);
            Assert.AreEqual(4, changes.Count);
        }

        [Test]
        public void CrewRestriction_FromNightlyConsumption_IsSoft_GameKeepsRunning()
        {
            var s = Create(new int[0]);
            s.res.Add(ResourceType.Food, -42); // 8 left, -8 tonight
            s.ctrl.EndDay();
            Assert.IsTrue(s.res.IsCrewRestricted);
            Assert.IsTrue(s.ctrl.LastReport.crewRestricted);
            Assert.AreEqual(2, s.days.CurrentDay);
            Assert.IsFalse(s.ctrl.IsMissionOver);
        }

        // ---------- Supply ----------

        [Test]
        public void Supply_DropsEvery5Days_AndCollectAddsFoodAndParts()
        {
            var s = Create(new int[0]);
            var drops = new List<int>();
            s.supply.SupplyDropped += (day, f, p) => drops.Add(day);

            for (int i = 0; i < 4; i++) s.ctrl.EndDay(); // -> Day 5
            Assert.AreEqual(5, s.days.CurrentDay);
            CollectionAssert.AreEqual(new[] { 5 }, drops);
            Assert.IsTrue(s.supply.HasPendingSupply);

            int food = s.res.Get(ResourceType.Food);
            int parts = s.res.Get(ResourceType.Parts);
            Assert.IsTrue(s.supply.CollectSupply());
            Assert.AreEqual(food + 30, s.res.Get(ResourceType.Food));
            Assert.AreEqual(parts + 15, s.res.Get(ResourceType.Parts));
            Assert.IsFalse(s.supply.CollectSupply());
        }

        // ---------- Day resolution / end ----------

        [Test]
        public void EndDay_WaitsForVisualListener_ThenResolves()
        {
            var s = Create(new[] { 1 });
            WeatherEventType? stormArg = null;
            s.ctrl.DayResolvingStarted += w => stormArg = w;

            Assert.IsTrue(s.ctrl.EndDay());
            Assert.AreEqual(WeatherEventType.SolarStorm, stormArg);
            Assert.IsTrue(s.ctrl.IsResolving);
            Assert.IsFalse(s.solar.ToggleRetract(), "Input is locked while resolving");
            Assert.IsFalse(s.ctrl.EndDay());
            Assert.AreEqual(1, s.days.CurrentDay);

            int morning = 0;
            s.ctrl.MorningReady += report => morning = report.newDay;
            s.ctrl.CompleteNightResolution();
            Assert.AreEqual(2, morning);
            Assert.IsFalse(s.ctrl.IsResolving);
            Assert.IsTrue(s.ctrl.LastReport.stormHit);
        }

        [Test]
        public void ThreeStars_TriggerEarlyWin_AtEndDay()
        {
            var s = Create(new int[0]);
            s.res.Add(ResourceType.Parts, 30);
            s.res.Add(ResourceType.Power, 40);
            s.buildings.TryBuildNext();
            s.res.Add(ResourceType.Parts, 50);
            s.buildings.TryBuildNext();
            s.res.Add(ResourceType.Parts, 50);
            s.res.Add(ResourceType.Power, 100);
            Assert.IsTrue(s.buildings.TryBuildNext());
            Assert.IsNull(s.endReason, "EarlyWin is evaluated at End Day");

            s.ctrl.EndDay();
            Assert.AreEqual(MissionEndReason.EarlyWin, s.endReason);
            Assert.AreEqual(3, s.endStars);
            Assert.AreEqual(1, s.days.CurrentDay);
            Assert.IsTrue(s.ctrl.IsMissionOver);
            Assert.IsFalse(s.ctrl.EndDay());
        }

        [Test]
        public void Day30_WithoutAllBuildings_TriggersDayCap()
        {
            var s = Create(new int[0]);
            s.buildings.TryBuildNext();
            for (int i = 0; i < 40 && s.endReason == null; i++)
            {
                s.supply.CollectSupply();
                s.ctrl.EndDay();
            }
            Assert.AreEqual(MissionEndReason.DayCap, s.endReason);
            Assert.AreEqual(1, s.endStars);
            Assert.AreEqual(30, s.days.CurrentDay);
        }

        // ---------- Space weather choices ----------

        [Test]
        public void SolarStormChoice_ShieldRetracts_KeepFullPowerStaysActive()
        {
            var s = Create(new[] { 1, 2 }, () => 0f);
            Assert.IsTrue(s.weather.NeedsChoiceToday);
            Assert.AreEqual(ActionBlockReason.WrongWeather, s.weather.CanApplyWeatherChoice(WeatherChoice.OverclockShields));
            Assert.IsTrue(s.weather.ApplyWeatherChoice(WeatherChoice.ShieldAndRetract));
            Assert.AreEqual(SolarFieldState.Retracted, s.solar.State);
            Assert.AreEqual(ActionBlockReason.AlreadyChosen, s.weather.CanApplyWeatherChoice(WeatherChoice.KeepFullPower));
            s.ctrl.EndDay();
            Assert.AreEqual(SolarFieldState.Retracted, s.solar.State);

            Assert.IsTrue(s.weather.ApplyWeatherChoice(WeatherChoice.KeepFullPower));
            Assert.AreEqual(SolarFieldState.Active, s.solar.State);
            s.ctrl.EndDay();
            Assert.AreEqual(SolarFieldState.Broken, s.solar.State);
            Assert.AreEqual("solar_broken", s.ctrl.LastReport.bauzerSummaryKey);
        }

        [Test]
        public void SolarRadiation_LocksRover_UnlessOverclockPays15Power()
        {
            var s = Create(new int[0], null, new[] { 1, 2 });
            Assert.IsTrue(s.weather.IsRoverLockedToday);
            Assert.AreEqual(RoverStatus.LockedByRadiation, s.rover.Status);
            Assert.AreEqual(ActionBlockReason.RoverLockedByRadiation, s.rover.CanDispatch(NearIce));

            Assert.IsTrue(s.weather.ApplyWeatherChoice(WeatherChoice.ShelterCrewAndLockRover));
            Assert.IsFalse(s.rover.TryDispatchRover(NearIce));
            Assert.AreEqual(60, s.res.Get(ResourceType.Power));
            s.ctrl.EndDay();
            Assert.AreEqual(SolarFieldState.Active, s.solar.State, "Radiation never damages the solar field");

            Assert.AreEqual(RoverStatus.LockedByRadiation, s.rover.Status);
            var statuses = new List<RoverStatus>();
            s.rover.RoverStatusChanged += statuses.Add;
            Assert.IsTrue(s.weather.ApplyWeatherChoice(WeatherChoice.OverclockShields));
            Assert.AreEqual(85 - 15, s.res.Get(ResourceType.Power));
            Assert.IsFalse(s.weather.IsRoverLockedToday);
            Assert.AreEqual(RoverStatus.Ready, s.rover.Status);
            CollectionAssert.Contains(statuses, RoverStatus.Ready);
            Assert.IsTrue(s.rover.TryDispatchRover(NearIce));
            s.ctrl.EndDay();
            Assert.AreEqual(RoverStatus.Ready, s.rover.Status, "A shielded rover is not broken by radiation");
        }

        [Test]
        public void Overclock_NeedsEnoughPower()
        {
            var s = Create(new int[0], null, new[] { 1 });
            s.res.Add(ResourceType.Power, -50); // 10
            Assert.AreEqual(ActionBlockReason.NotEnoughPower, s.weather.CanApplyWeatherChoice(WeatherChoice.OverclockShields));
            Assert.IsFalse(s.weather.ApplyWeatherChoice(WeatherChoice.OverclockShields));
            Assert.IsTrue(s.weather.IsRoverLockedToday);
        }

        // ---------- Unexpected demands ----------

        [Test]
        public void Demand_RollsOnNormalDaysOnly_AndAppliesChoice()
        {
            var s = Create(new[] { 3 });
            s.demand.RandomValue = () => 0f; // always roll, always pick the first demand (Water Leak)

            Assert.IsFalse(s.demand.TryRollMorningDemand(), "No demand on Day 1");
            Assert.IsFalse(s.demand.HasActiveDemand);
            s.ctrl.EndDay(); // Day 2
            Assert.IsTrue(s.ctrl.LastReport.demandRolled);
            Assert.IsTrue(s.demand.HasActiveDemand);
            Assert.AreEqual("water_leak", s.demand.ActiveDemand.demandId);

            int parts = s.res.Get(ResourceType.Parts);
            Assert.IsTrue(s.demand.ResolveDemandChoice(0)); // Fix Filter -5 Parts
            Assert.AreEqual(parts - 5, s.res.Get(ResourceType.Parts));
            Assert.IsFalse(s.demand.HasActiveDemand);
            Assert.IsFalse(s.demand.ResolveDemandChoice(1));

            s.ctrl.EndDay(); // Day 3 is a DONKI event day: the weather card has priority
            Assert.IsFalse(s.ctrl.LastReport.demandRolled);
            Assert.AreEqual(WeatherEventType.SolarStorm, s.ctrl.LastReport.todayWeather);
        }

        [Test]
        public void Demand_PatchLater_LosesWater_AndScienceNeedsPower()
        {
            var s = Create(new int[0]);
            s.demand.RandomValue = () => 0f; // roll passes, first demand (Water Leak)
            s.ctrl.EndDay(); // Day 2
            Assert.IsTrue(s.demand.HasActiveDemand);
            int water = s.res.Get(ResourceType.Water);
            Assert.IsTrue(s.demand.ResolveDemandChoice(1)); // Patch Later
            Assert.AreEqual(water - 6, s.res.Get(ResourceType.Water));

            int calls = 0;
            s.demand.RandomValue = () => ++calls % 2 == 1 ? 0f : 0.99f; // roll passes, then pick the last demand
            s.ctrl.EndDay(); // Day 3
            Assert.IsTrue(s.demand.HasActiveDemand);
            Assert.AreEqual("science_parts", s.demand.ActiveDemand.demandId);
            s.res.Add(ResourceType.Power, -100);
            Assert.AreEqual(ActionBlockReason.NotEnoughPower, s.demand.CanChoose(0));
            Assert.IsFalse(s.demand.ResolveDemandChoice(0));
            Assert.IsTrue(s.demand.ResolveDemandChoice(1)); // Skip Today
        }

        // ---------- Full runs ----------

        public enum Strategy { Greedy, Cautious, Careless }

        /// <summary>Plays one run with a simple scripted player. Returns the day the run ended.</summary>
        private static int Play(Sim s, Strategy strategy)
        {
            for (int guard = 0; guard < 40 && s.endReason == null; guard++)
            {
                bool storm = s.weather.TodayWeather == WeatherEventType.SolarStorm;
                bool radiation = s.weather.TodayWeather == WeatherEventType.SolarRadiation;

                // Morning cards: demand first (take the first answer if affordable), then the weather choice.
                if (s.demand.HasActiveDemand && !s.demand.ResolveDemandChoice(0)) s.demand.ResolveDemandChoice(1);
                if (storm)
                    s.weather.ApplyWeatherChoice(strategy == Strategy.Careless ? WeatherChoice.KeepFullPower : WeatherChoice.ShieldAndRetract);
                if (radiation)
                {
                    bool overclock = strategy == Strategy.Greedy && s.res.Get(ResourceType.Power) >= 15 + 18;
                    if (!overclock || !s.weather.ApplyWeatherChoice(WeatherChoice.OverclockShields))
                        s.weather.ApplyWeatherChoice(WeatherChoice.ShelterCrewAndLockRover);
                }

                s.supply.CollectSupply();
                if (s.solar.State == SolarFieldState.Broken) s.solar.TryRepairSolar();
                if (!storm && s.solar.State == SolarFieldState.Retracted) s.solar.ToggleRetract();
                if (s.rover.Status == RoverStatus.Broken) s.rover.TryRepairRover();
                s.buildings.TryBuildNext();

                bool sendRover = s.rover.Status == RoverStatus.Ready && (strategy == Strategy.Careless || !storm);
                if (sendRover)
                {
                    bool needWater = s.res.Get(ResourceType.Water) < 24;
                    string site;
                    switch (strategy)
                    {
                        case Strategy.Greedy: site = needWater ? FarIce : FarMinerals; break;
                        case Strategy.Cautious: site = needWater ? NearIce : NearMinerals; break;
                        default: site = Balanced; break;
                    }
                    if (!s.rover.TryDispatchRover(site)) s.rover.TryDispatchRover(needWater ? NearIce : NearMinerals);
                }

                s.ctrl.EndDay();
            }
            return s.days.CurrentDay;
        }

        [Test]
        public void FullRuns_ReachEarlyWin_WithConfiguredNumbers([Values] Strategy strategy)
        {
            var winDays = new List<int>();
            for (int seed = 1; seed <= 25; seed++)
            {
                var rng = new System.Random(seed);
                var s = Create(null, () => (float)rng.NextDouble());
                s.demand.RandomValue = () => (float)rng.NextDouble();
                int day = Play(s, strategy);
                Assert.AreEqual(MissionEndReason.EarlyWin, s.endReason, strategy + " seed " + seed + " ended " + s.endReason + " on day " + day);
                winDays.Add(day);
            }
            winDays.Sort();
            string line = strategy + " EarlyWin days over 25 seeds: min " + winDays.First() + ", median " + winDays[winDays.Count / 2] +
                          ", max " + winDays.Last() + " (GDD target 12-18)";
            TestContext.WriteLine(line);
            Debug.Log("[Sim balance] " + line);
        }
    }
}
