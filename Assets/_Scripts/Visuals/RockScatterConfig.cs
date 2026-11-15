using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Tunable numbers for the lunar rock scatter and resource hotspots tool. One asset lives in Assets/_Config/Visuals/.</summary>
    [CreateAssetMenu(fileName = "RockScatterConfig", menuName = "Artemis X/Rock Scatter Config")]
    public class RockScatterConfig : ScriptableObject
    {
        [Header("Prefabs & Seed")]
        public int seed = 42;
        public GameObject[] rockPrefabs = { };

        [Header("Counts & Clusters")]
        [Range(50, 245)] public int targetRockCount = 215;
        public Vector2Int clusterSizeRange = new Vector2Int(3, 7);
        [Range(0f, 1f)] public float clusterProbability = 0.62f;
        public Vector2 clusterSpreadRadius = new Vector2(0.2f, 1.8f);
        [Min(0.5f)] public float minClusterGap = 4.6f;
        [Min(0.5f)] public float minSingleGap = 2.0f;
        [Min(0.05f)] public float minIntraClusterClearance = 0.12f;

        [Header("Base Center Dressing")]
        [Range(0f, 0.5f)] public float baseCenterQuotaFraction = 0.20f;
        [Min(6f)] public float baseCenterRadius = 15.5f;
        [Min(1f)] public float baseCenterMinRadius = 2.0f;
        [Range(0.4f, 2f)] public float baseCenterMaxScale = 1.30f;
        [Min(0.5f)] public float baseCenterClusterGap = 2.8f;
        [Min(0.4f)] public float baseCenterSingleGap = 1.25f;

        [Header("Scale & Orientation")]
        public Vector2 scaleRange = new Vector2(0.4f, 2.5f);
        [Tooltip("Higher values bias random scale towards smaller rocks with a few big ones.")]
        [Min(1f)] public float scaleBiasPower = 2.4f;
        [Range(0f, 25f)] public float maxTiltDegrees = 10f;
        [Range(0f, 1f)] public float normalAlignBlend = 0.3f;
        [Tooltip("Fraction of rock height sunk into the lunar surface.")]
        public Vector2 sinkFractionRange = new Vector2(0.06f, 0.14f);

        [Header("Density Weights")]
        [Range(0f, 1f)] public float plainDensity = 0.16f;
        [Range(0f, 1f)] public float craterRimDensity = 1.0f;
        [Range(0f, 1f)] public float craterBowlDensity = 0.28f;
        [Range(0f, 1f)] public float hillDensity = 0.82f;
        [Tooltip("Ensures inner-island craters and slopes visible from home view get balanced coverage.")]
        [Range(0f, 1f)] public float innerIslandQuotaFraction = 0.26f;
        [Min(5f)] public float innerIslandRadius = 26f;

        [Header("Keep-Clear Margins")]
        [Min(1f)] public float islandEdgeMargin = 8f;
        [Min(0.4f)] public float buildingKeepClearMargin = 1.45f;
        [Min(0.4f)] public float propKeepClearMargin = 0.95f;
        [Min(0f)] public float nearBuildingSoftBand = 1.2f;
        [Range(0f, 1f)] public float nearBuildingDensityFactor = 0.68f;
        [Min(0.5f)] public float existingRockMargin = 1.4f;
        [Min(0.5f)] public float routeKeepClearMargin = 1.35f;
        [Min(1f)] public float hotspotKeepClearMargin = 3.4f;

        [Header("Resource Hotspots")]
        public GameObject[] icePrefabs = { };
        public GameObject[] mineralPrefabs = { };
        public GameObject[] quarryChunkPrefabs = { };
        public GameObject beaconPrefab;
        public Material ringMaterial;
        public HotspotEntry[] hotspots = CreateDefaultHotspots();

        /// <summary>Default 5 lunar resource hotspots spaced cleanly around the base.</summary>
        public static HotspotEntry[] CreateDefaultHotspots()
        {
            return new[]
            {
                new HotspotEntry
                {
                    id = "Hotspot_Ice_SW",
                    displayName = "Southwest Crater Ice Deposit",
                    primaryResource = HotspotResourceKind.FrozenIce,
                    center = new Vector2(-11.0f, -11.5f),
                    depositRadius = 2.1f,
                    roverStopDistance = 2.6f,
                    primaryNodeCount = 6,
                    borderRockCount = 5,
                },
                new HotspotEntry
                {
                    id = "Hotspot_Minerals_SSE",
                    displayName = "South Basin Mineral Vein",
                    primaryResource = HotspotResourceKind.Minerals,
                    center = new Vector2(10.0f, -12.5f),
                    depositRadius = 2.1f,
                    roverStopDistance = 2.6f,
                    primaryNodeCount = 7,
                    borderRockCount = 5,
                },
                new HotspotEntry
                {
                    id = "Hotspot_Rocks_East",
                    displayName = "East Regolith Quarry",
                    primaryResource = HotspotResourceKind.Rocks,
                    center = new Vector2(18.5f, -2.5f),
                    depositRadius = 2.2f,
                    roverStopDistance = 2.7f,
                    primaryNodeCount = 6,
                    borderRockCount = 6,
                },
                new HotspotEntry
                {
                    id = "Hotspot_Minerals_NE",
                    displayName = "Northeast Ridge Ore Deposit",
                    primaryResource = HotspotResourceKind.Minerals,
                    center = new Vector2(14.5f, 13.5f),
                    depositRadius = 2.0f,
                    roverStopDistance = 2.5f,
                    primaryNodeCount = 7,
                    borderRockCount = 5,
                },
                new HotspotEntry
                {
                    id = "Hotspot_Ice_NW",
                    displayName = "Northwest Shadow Ice Pocket",
                    primaryResource = HotspotResourceKind.FrozenIce,
                    center = new Vector2(-12.5f, 11.0f),
                    depositRadius = 2.0f,
                    roverStopDistance = 2.5f,
                    primaryNodeCount = 6,
                    borderRockCount = 5,
                },
            };
        }
    }
}
