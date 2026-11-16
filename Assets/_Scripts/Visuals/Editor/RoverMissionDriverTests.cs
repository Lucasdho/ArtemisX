using System;
using System.Collections.Generic;
using System.Reflection;
using ArtemisX.Core;
using ArtemisX.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArtemisX.Visuals.Tests
{
    public class RoverMissionDriverTests
    {
        private const string WheelDustPrefabPath = "Assets/_Prefabs/FX/WheelDust.prefab";
        private const string SiteId = "Hotspot_Ice_SW";

        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        private sealed class Harness
        {
            public GameObject root;
            public GameConfig gameConfig;
            public SpaceWeatherConfig weatherConfig;
            public ExpeditionSiteConfig siteConfig;
            public DayManager days;
            public ResourceManager resources;
            public SpaceWeatherManager weather;
            public SolarFieldManager solar;
            public RoverExpeditionManager rover;
            public GameObject roverGo;
            public RoverMissionDriver driver;
            public ParticleSystem[] dustSystems;
            public Vector3 homePosition;
            public Quaternion homeRotation;
            public Vector3 targetPosition;
            public List<Vector3> outwardPoints;
        }

        private Harness CreateHarness(bool mirroredRoute = true, int nodeCount = 3, float calmBreakdownChance = 0f, int[] radiationDays = null)
        {
            var h = new Harness
            {
                root = Track(new GameObject("RoverTestRoot")),
                homePosition = new Vector3(1f, 0f, 2f),
                homeRotation = Quaternion.Euler(0f, 35f, 0f),
                targetPosition = new Vector3(10f, 0f, 20f)
            };

            h.gameConfig = Track(ScriptableObject.CreateInstance<GameConfig>());
            h.weatherConfig = Track(ScriptableObject.CreateInstance<SpaceWeatherConfig>());
            h.weatherConfig.days = new SpaceWeatherConfig.WeatherDay[5];
            if (radiationDays != null)
                foreach (int d in radiationDays)
                    h.weatherConfig.days[d - 1].weather = WeatherEventType.SolarRadiation;

            h.siteConfig = Track(ScriptableObject.CreateInstance<ExpeditionSiteConfig>());
            h.siteConfig.sites = new[]
            {
                new ExpeditionSiteConfig.Site
                {
                    siteId = SiteId,
                    displayName = "Test Site",
                    distance = SiteDistance.Far,
                    powerCost = 5,
                    waterReward = 10,
                    partsReward = 4,
                    calmBreakdownChance = calmBreakdownChance
                }
            };

            h.days = h.root.AddComponent<DayManager>();
            h.resources = h.root.AddComponent<ResourceManager>();
            h.weather = h.root.AddComponent<SpaceWeatherManager>();
            h.solar = h.root.AddComponent<SolarFieldManager>();
            h.rover = h.root.AddComponent<RoverExpeditionManager>();

            h.resources.Initialize(h.gameConfig);
            h.resources.Add(ResourceType.Power, 100);
            h.resources.Add(ResourceType.Parts, 50);
            h.solar.Initialize(h.gameConfig, h.resources);
            h.weather.Initialize(h.weatherConfig, h.days, h.resources, h.solar);
            h.rover.Initialize(h.siteConfig, h.gameConfig, h.resources, h.weather);
            h.rover.RandomValue = () => 0.999f;

            // Route
            var routeGo = Track(new GameObject("Route_" + SiteId));
            var wp = routeGo.AddComponent<WaypointPath>();
            h.outwardPoints = new List<Vector3>();
            var nodes = new List<WaypointPath.Node>();
            var outwardTransforms = new List<Transform>();
            for (int i = 0; i < nodeCount; i++)
            {
                float t = nodeCount > 1 ? i / (float)(nodeCount - 1) : 1f;
                Vector3 pos = Vector3.Lerp(new Vector3(2f, 0f, 4f), new Vector3(9f, 0f, 18f), t);
                if (!mirroredRoute && i == nodeCount - 1) pos = new Vector3(11f, 0f, 17f);
                var pt = Track(new GameObject($"P{i:00}")).transform;
                pt.SetParent(routeGo.transform, false);
                pt.position = pos;
                outwardTransforms.Add(pt);
                h.outwardPoints.Add(pos);
                nodes.Add(new WaypointPath.Node { point = pt });
            }
            if (mirroredRoute && nodeCount > 2)
            {
                for (int i = nodeCount - 2; i >= 1; i--)
                    nodes.Add(new WaypointPath.Node { point = outwardTransforms[i] });
            }
            wp.SetNodes(nodes);

            // Hotspot
            var hotspotGo = Track(new GameObject(SiteId));
            var hotspot = hotspotGo.AddComponent<ResourceHotspot>();
            hotspot.hotspotId = SiteId;
            var stopGo = Track(new GameObject("RoverTarget"));
            stopGo.transform.SetParent(hotspotGo.transform, false);
            stopGo.transform.position = h.targetPosition;
            hotspot.roverStopPoint = stopGo.transform;

            // Rover + WheelDust
            h.roverGo = Track(new GameObject("ExplorationHover"));
            h.roverGo.transform.SetPositionAndRotation(h.homePosition, h.homeRotation);

            var wheelDustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WheelDustPrefabPath);
            Assert.IsNotNull(wheelDustPrefab, "Missing WheelDust prefab at " + WheelDustPrefabPath);
            var wheelDustGo = Track((GameObject)PrefabUtility.InstantiatePrefab(wheelDustPrefab, h.roverGo.transform));
            wheelDustGo.name = "WheelDust";
            h.dustSystems = wheelDustGo.GetComponentsInChildren<ParticleSystem>(true);
            Assert.AreEqual(3, h.dustSystems.Length, "WheelDust.prefab must contain Dust, WheelGrit, and DustWake.");

            // Simulate playOnAwake active emission on all 3 systems before driver initializes
            foreach (var ps in h.dustSystems)
            {
                ps.Play(false);
                ps.Simulate(0.15f, false, true);
            }

            var primaryDust = wheelDustGo.transform.Find("Dust").GetComponent<ParticleSystem>();
            var visConfig = Track(ScriptableObject.CreateInstance<GameplayVisualsConfig>());
            visConfig.roverDriveSpeed = 10f;

            h.driver = h.roverGo.AddComponent<RoverMissionDriver>();
            var so = new SerializedObject(h.driver);
            so.FindProperty("rover").objectReferenceValue = h.rover;
            so.FindProperty("config").objectReferenceValue = visConfig;
            so.FindProperty("wheelDust").objectReferenceValue = primaryDust;
            var routesProp = so.FindProperty("routes");
            routesProp.arraySize = 1;
            routesProp.GetArrayElementAtIndex(0).objectReferenceValue = wp;
            var hotspotsProp = so.FindProperty("hotspots");
            hotspotsProp.arraySize = 1;
            hotspotsProp.GetArrayElementAtIndex(0).objectReferenceValue = hotspot;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeLifecycle(h.driver, "Awake");
            InvokeLifecycle(h.driver, "OnEnable");
            InvokeLifecycle(h.driver, "Start");
            return h;
        }

        private static void InvokeLifecycle(object target, string methodName)
        {
            var m = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(m, $"Missing method {methodName}");
            m.Invoke(target, null);
        }

        private static void AssertAllDustStoppedAndClear(Harness h)
        {
            foreach (var ps in h.dustSystems)
            {
                Assert.IsFalse(ps.emission.enabled, $"{ps.name} emission should be disabled.");
                Assert.IsFalse(ps.isEmitting, $"{ps.name} should not be emitting.");
                Assert.IsFalse(ps.isPlaying, $"{ps.name} should not be playing.");
                Assert.AreEqual(0, ps.particleCount, $"{ps.name} should have 0 particles.");
            }
        }

        private static void AssertParkedHome(Harness h)
        {
            Assert.IsFalse(h.driver.IsDriving, "Rover should not be driving.");
            Assert.Less(Vector3.Distance(h.roverGo.transform.position, h.homePosition), 0.001f, "Rover should be parked at homePosition.");
            Assert.Less(Quaternion.Angle(h.roverGo.transform.rotation, h.homeRotation), 0.1f, "Rover should face homeRotation.");
            AssertAllDustStoppedAndClear(h);
        }

        [Test]
        public void InitialReadyState_ParksAtHomeAndStopsAllThreeWheelDustSystems()
        {
            var h = CreateHarness();
            AssertParkedHome(h);

            for (int i = 0; i < 10; i++) h.driver.Step(0.1f);
            AssertParkedHome(h);
        }

        [Test]
        public void Dispatch_DrivesOneWayAlongMirroredRouteAndEnablesAllDust()
        {
            var h = CreateHarness(mirroredRoute: true, nodeCount: 4);
            Assert.IsTrue(h.rover.TryDispatchRover(SiteId));
            Assert.IsTrue(h.driver.IsDriving);

            var visitedPositions = new List<Vector3>();
            for (int i = 0; i < 100 && h.driver.IsDriving; i++)
            {
                h.driver.Step(0.05f);
                visitedPositions.Add(h.roverGo.transform.position);
                if (h.driver.IsDriving)
                {
                    foreach (var ps in h.dustSystems)
                    {
                        Assert.IsTrue(ps.emission.enabled, $"{ps.name} emission should be enabled while driving.");
                        Assert.IsTrue(ps.isPlaying, $"{ps.name} should be playing while driving.");
                    }
                }
            }

            Assert.IsFalse(h.driver.IsDriving, "Rover should have reached hotspot target.");
            Assert.Less(Vector3.Distance(h.roverGo.transform.position, h.targetPosition), 0.01f, "Rover should finish one-way drive at hotspot target.");

            // Verify distance from home stays monotonic once past P0 (did not double back to P0)
            float maxDistFromHome = 0f;
            foreach (var p in visitedPositions)
            {
                float d = Vector3.Distance(p, h.homePosition);
                Assert.GreaterOrEqual(d + 0.01f, maxDistFromHome, "One-way route should not turn around and drive back toward home.");
                maxDistFromHome = Mathf.Max(maxDistFromHome, d);
            }

            foreach (var ps in h.dustSystems)
            {
                Assert.IsFalse(ps.emission.enabled, $"{ps.name} emission should disable upon arrival.");
                Assert.IsFalse(ps.isEmitting, $"{ps.name} should stop emitting upon arrival.");
            }
        }

        [Test]
        public void BrokenReturn_WhileDrivingOutbound_ParksImmediatelyAndClearsAllDust()
        {
            var h = CreateHarness(calmBreakdownChance: 1f);
            h.rover.RandomValue = () => 0f;

            Assert.IsTrue(h.rover.TryDispatchRover(SiteId));
            h.driver.Step(0.25f);
            Assert.IsTrue(h.driver.IsDriving);
            Assert.Greater(Vector3.Distance(h.roverGo.transform.position, h.homePosition), 0.1f);
            foreach (var ps in h.dustSystems) ps.Simulate(0.1f, false, false);

            var result = h.rover.ResolveReturn();
            Assert.IsTrue(result.isBroken);
            Assert.AreEqual(RoverStatus.Broken, h.rover.Status);

            AssertParkedHome(h);

            for (int i = 0; i < 20; i++) h.driver.Step(0.1f);
            AssertParkedHome(h);
        }

        [Test]
        public void BrokenReturn_AfterArrivingAtHotspot_ParksImmediatelyAndClearsAllDust()
        {
            var h = CreateHarness(calmBreakdownChance: 1f);
            h.rover.RandomValue = () => 0f;

            Assert.IsTrue(h.rover.TryDispatchRover(SiteId));
            for (int i = 0; i < 100 && h.driver.IsDriving; i++) h.driver.Step(0.05f);
            Assert.IsFalse(h.driver.IsDriving);
            Assert.Less(Vector3.Distance(h.roverGo.transform.position, h.targetPosition), 0.01f);

            var result = h.rover.ResolveReturn();
            Assert.IsTrue(result.isBroken);
            AssertParkedHome(h);

            for (int i = 0; i < 10; i++) h.driver.Step(0.1f);
            AssertParkedHome(h);
        }

        [Test]
        public void IntactReturn_ParksImmediatelyAtHomeAndClearsAllDust()
        {
            var h = CreateHarness(calmBreakdownChance: 0f);
            Assert.IsTrue(h.rover.TryDispatchRover(SiteId));
            h.driver.Step(0.3f);
            foreach (var ps in h.dustSystems) ps.Simulate(0.1f, false, false);

            var result = h.rover.ResolveReturn();
            Assert.IsFalse(result.isBroken);
            Assert.AreEqual(RoverStatus.Ready, h.rover.Status);
            AssertParkedHome(h);

            for (int i = 0; i < 10; i++) h.driver.Step(0.1f);
            AssertParkedHome(h);
        }

        [Test]
        public void RadiationLockAndRepair_RemainStationaryWithNoDust()
        {
            var h = CreateHarness(calmBreakdownChance: 1f, radiationDays: new[] { 2 });
            h.rover.RandomValue = () => 0f;

            Assert.IsTrue(h.rover.TryDispatchRover(SiteId));
            h.driver.Step(0.2f);
            h.rover.ResolveReturn();
            Assert.AreEqual(RoverStatus.Broken, h.rover.Status);
            AssertParkedHome(h);

            Assert.IsTrue(h.rover.TryRepairRover());
            Assert.AreEqual(RoverStatus.Ready, h.rover.Status);
            AssertParkedHome(h);

            h.days.AdvanceDay();
            Assert.AreEqual(RoverStatus.LockedByRadiation, h.rover.Status);
            for (int i = 0; i < 10; i++) h.driver.Step(0.1f);
            AssertParkedHome(h);
        }

        [Test]
        public void NonRoverComponentBroken_DoesNotParkRoverDuringMission()
        {
            var h = CreateHarness();
            Assert.IsTrue(h.rover.TryDispatchRover(SiteId));
            h.driver.Step(0.2f);
            Assert.IsTrue(h.driver.IsDriving);

            var onComponentBroken = typeof(RoverMissionDriver).GetMethod("OnComponentBroken", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(onComponentBroken);
            onComponentBroken.Invoke(h.driver, new object[] { BaseComponent.SolarField });

            Assert.IsTrue(h.driver.IsDriving, "Solar breakdown should not park the rover while OnMission.");
        }

        [Test]
        public void NonMirroredOneWayRoute_AndTwoNodeRoute_VisitAllNodes()
        {
            var nonMirrored = CreateHarness(mirroredRoute: false, nodeCount: 4);
            Assert.IsTrue(nonMirrored.rover.TryDispatchRover(SiteId));
            bool visitedLastRouteNode = false;
            Vector3 lastRouteNode = nonMirrored.outwardPoints[3];
            for (int i = 0; i < 100 && nonMirrored.driver.IsDriving; i++)
            {
                nonMirrored.driver.Step(0.05f);
                if (Vector3.Distance(nonMirrored.roverGo.transform.position, lastRouteNode) < 0.5f)
                    visitedLastRouteNode = true;
            }
            Assert.IsTrue(visitedLastRouteNode, "Non-mirrored one-way path should visit its final waypoint.");

            var twoNode = CreateHarness(mirroredRoute: true, nodeCount: 2);
            Assert.IsTrue(twoNode.rover.TryDispatchRover(SiteId));
            for (int i = 0; i < 100 && twoNode.driver.IsDriving; i++) twoNode.driver.Step(0.05f);
            Assert.IsFalse(twoNode.driver.IsDriving);
            Assert.Less(Vector3.Distance(twoNode.roverGo.transform.position, twoNode.targetPosition), 0.01f);
        }
    }
}
