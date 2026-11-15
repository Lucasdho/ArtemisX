using System;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Tunable numbers for the generated lunar ground. One asset lives in Assets/_Config/Visuals/.</summary>
    [CreateAssetMenu(fileName = "LunarTerrainConfig", menuName = "Artemis X/Lunar Terrain Config")]
    public class LunarTerrainConfig : ScriptableObject
    {
        [Serializable]
        public struct Crater
        {
            public Vector2 center;
            [Min(0.1f)] public float radius;
            [Min(0f)] public float depth;
            [Min(0f)] public float rimHeight;
        }

        [Header("Mesh")]
        public Vector2 center = new Vector2(0f, 2f);
        [Min(5f)] public float halfSize = 34f;
        [Min(0.1f)] public float cellSize = 0.5f;
        public int seed = 7;

        [Header("Build zone (kept dead flat so buildings sit level)")]
        [Tooltip("x, z of the lower-left corner, then width and depth.")]
        public Rect flatZone = new Rect(-12f, -9f, 24f, 22f);
        [Min(0.1f)] public float flatBlend = 6f;

        [Header("Relief")]
        public float rollingAmplitude = 0.5f;
        public float rollingScale = 0.07f;
        public float detailAmplitude = 0.07f;
        public float detailScale = 0.45f;
        [Tooltip("Rim of hills that wraps the basin, tallest on the far (north) side.")]
        public float hillHeight = 2.6f;
        public float hillStartRadius = 17f;
        public float hillEndRadius = 25f;
        [Range(0f, 1f)] public float hillSouthFactor = 0.35f;

        [Header("Island edge")]
        [Min(5f)] public float islandRadius = 31f;
        public float islandWobble = 2.5f;
        [Min(0.5f)] public float edgeWidth = 4f;
        public float edgeDrop = 4.5f;

        [Header("Craters")]
        public Crater[] craters =
        {
            new Crater { center = new Vector2(-17f, -6f), radius = 4.2f, depth = 1.1f, rimHeight = 0.35f },
            new Crater { center = new Vector2(16f, -9f), radius = 4.8f, depth = 1.3f, rimHeight = 0.4f },
            new Crater { center = new Vector2(0f, -17f), radius = 5.2f, depth = 1.4f, rimHeight = 0.45f },
            new Crater { center = new Vector2(20f, 9f), radius = 3.6f, depth = 1.0f, rimHeight = 0.3f },
            new Crater { center = new Vector2(-19f, 12f), radius = 3.4f, depth = 0.9f, rimHeight = 0.3f },
            new Crater { center = new Vector2(-9f, -18f), radius = 2.6f, depth = 0.8f, rimHeight = 0.25f },
            new Crater { center = new Vector2(11f, -19f), radius = 2.4f, depth = 0.7f, rimHeight = 0.25f },
            new Crater { center = new Vector2(24f, -3f), radius = 2.4f, depth = 0.7f, rimHeight = 0.25f },
        };
        [Min(0)] public int smallCraterCount = 16;
        public Vector2 smallCraterRadius = new Vector2(0.7f, 1.5f);
        public float smallCraterDepth = 0.25f;
        [Tooltip("Small craters stay at least this far outside the flat zone.")]
        public float smallCraterZoneMargin = 3f;

        [Header("Ground texture")]
        [Min(256)] public int textureSize = 1024;
        public Color plainColor = new Color(0.66f, 0.58f, 0.51f);
        public Color hillColor = new Color(0.76f, 0.70f, 0.63f);
        public Color craterFloorColor = new Color(0.42f, 0.37f, 0.34f);
        public Color rimColor = new Color(0.82f, 0.76f, 0.69f);
        public Color edgeColor = new Color(0.30f, 0.26f, 0.25f);
        [Range(0f, 0.4f)] public float colorNoise = 0.1f;
    }
}
