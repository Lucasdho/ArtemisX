using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Tunable numbers for the lunar rock scatter tool. One asset lives in Assets/_Config/Visuals/.</summary>
    [CreateAssetMenu(fileName = "RockScatterConfig", menuName = "Artemis X/Rock Scatter Config")]
    public class RockScatterConfig : ScriptableObject
    {
        [Header("Prefabs & Seed")]
        public int seed = 42;
        public GameObject[] rockPrefabs = { };

        [Header("Counts & Clusters")]
        [Range(50, 245)] public int targetRockCount = 235;
        public Vector2Int clusterSizeRange = new Vector2Int(3, 7);
        [Range(0f, 1f)] public float clusterProbability = 0.62f;
        public Vector2 clusterSpreadRadius = new Vector2(0.2f, 1.8f);
        [Min(0.5f)] public float minClusterGap = 4.8f;
        [Min(0.5f)] public float minSingleGap = 2.2f;
        [Min(0.05f)] public float minIntraClusterClearance = 0.12f;

        [Header("Scale & Orientation")]
        public Vector2 scaleRange = new Vector2(0.4f, 2.5f);
        [Tooltip("Higher values bias random scale towards smaller rocks with a few big ones.")]
        [Min(1f)] public float scaleBiasPower = 2.4f;
        [Range(0f, 25f)] public float maxTiltDegrees = 10f;
        [Range(0f, 1f)] public float normalAlignBlend = 0.3f;
        [Tooltip("Fraction of rock height sunk into the lunar surface.")]
        public Vector2 sinkFractionRange = new Vector2(0.06f, 0.14f);

        [Header("Density Weights")]
        [Range(0f, 1f)] public float plainDensity = 0.15f;
        [Range(0f, 1f)] public float craterRimDensity = 1.0f;
        [Range(0f, 1f)] public float craterBowlDensity = 0.28f;
        [Range(0f, 1f)] public float hillDensity = 0.82f;
        [Tooltip("Ensures inner-island craters and slopes visible from home view get balanced coverage.")]
        [Range(0f, 1f)] public float innerIslandQuotaFraction = 0.28f;
        [Min(5f)] public float innerIslandRadius = 26f;

        [Header("Keep-Clear Margins")]
        [Min(1f)] public float islandEdgeMargin = 8f;
        [Min(0.5f)] public float buildingKeepClearMargin = 2.5f;
        [Min(0f)] public float nearBuildingSoftBand = 3.2f;
        [Range(0f, 1f)] public float nearBuildingDensityFactor = 0.12f;
        [Min(0.5f)] public float existingRockMargin = 1.5f;
        [Min(0.5f)] public float routeKeepClearMargin = 1.6f;
    }
}
