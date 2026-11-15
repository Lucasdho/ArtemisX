using System.Collections.Generic;
using System.IO;
using ArtemisX.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArtemisX.EditorTools
{
    /// <summary>
    /// Builds the lunar ground mesh and texture from LunarTerrainConfig, then swaps it into the Base scene.
    /// Menu: Artemis X / Build Lunar Terrain.
    /// </summary>
    public static class LunarTerrainBuilder
    {
        const string ConfigPath = "Assets/_Config/Visuals/LunarTerrainConfig.asset";
        const string MeshPath = "Assets/_Materials/LunarGround.asset";
        const string TexturePath = "Assets/_Materials/T_LunarGround.png";
        const string MaterialPath = "Assets/_Materials/M_LunarGround.mat";

        struct Sample
        {
            public float height, bowl, rim, hill, edge;
        }

        class Shape
        {
            public LunarTerrainConfig cfg;
            public List<LunarTerrainConfig.Crater> craters = new List<LunarTerrainConfig.Crater>();
            public Vector2 zoneMin, zoneMax;
            public float offX, offZ;

            public Sample Evaluate(float x, float z)
            {
                var s = new Sample();
                float dx = Mathf.Max(zoneMin.x - x, 0f, x - zoneMax.x);
                float dz = Mathf.Max(zoneMin.y - z, 0f, z - zoneMax.y);
                float away = Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(dx * dx + dz * dz) / cfg.flatBlend);

                float rolling = (Mathf.PerlinNoise(x * cfg.rollingScale + offX, z * cfg.rollingScale + offZ) - 0.5f) * 2f;
                float detail = (Mathf.PerlinNoise(x * cfg.detailScale + offZ, z * cfg.detailScale + offX) - 0.5f) * 2f;
                float h = (rolling * cfg.rollingAmplitude + detail * cfg.detailAmplitude) * away;

                Vector2 rel = new Vector2(x - cfg.center.x, z - cfg.center.y);
                float r = rel.magnitude;
                float northness = Mathf.Lerp(cfg.hillSouthFactor, 1f, Mathf.Clamp01(rel.y / Mathf.Max(r, 0.001f) * 0.5f + 0.5f));
                float hillBand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(cfg.hillStartRadius, cfg.hillEndRadius, r));
                float hillNoise = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.09f + offZ, z * 0.09f + offX);
                s.hill = hillBand * northness * hillNoise;
                h += cfg.hillHeight * s.hill * away;

                foreach (var p in cfg.plateaus)
                {
                    float px = Mathf.Max(p.area.xMin - x, 0f, x - p.area.xMax);
                    float pz = Mathf.Max(p.area.yMin - z, 0f, z - p.area.yMax);
                    float lift = p.height * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Sqrt(px * px + pz * pz) / cfg.plateauBlend));
                    h = Mathf.Max(h, lift);
                }

                foreach (var c in craters)
                {
                    float d = Vector2.Distance(new Vector2(x, z), c.center);
                    float t = d / c.radius;
                    if (t < 1f)
                    {
                        float bowl = (1f - t * t);
                        bowl *= bowl;
                        h -= c.depth * bowl;
                        s.bowl = Mathf.Max(s.bowl, bowl);
                    }
                    float rimT = (t - 1.05f) / 0.28f;
                    float rim = Mathf.Exp(-rimT * rimT);
                    h += c.rimHeight * rim;
                    s.rim = Mathf.Max(s.rim, rim);
                }

                float angle = Mathf.Atan2(rel.y, rel.x);
                float wobble = (Mathf.PerlinNoise(Mathf.Cos(angle) * 1.6f + 20f, Mathf.Sin(angle) * 1.6f + 20f) - 0.5f) * 2f;
                float edgeRadius = cfg.islandRadius + wobble * cfg.islandWobble;
                s.edge = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edgeRadius - cfg.edgeWidth, edgeRadius, r));
                h = Mathf.Lerp(h, -cfg.edgeDrop, s.edge);
                s.height = h;
                return s;
            }

            public bool BeyondIsland(float x, float z)
            {
                Vector2 rel = new Vector2(x - cfg.center.x, z - cfg.center.y);
                float angle = Mathf.Atan2(rel.y, rel.x);
                float wobble = (Mathf.PerlinNoise(Mathf.Cos(angle) * 1.6f + 20f, Mathf.Sin(angle) * 1.6f + 20f) - 0.5f) * 2f;
                return rel.magnitude > cfg.islandRadius + wobble * cfg.islandWobble + 0.2f;
            }
        }

        [MenuItem("Artemis X/Build Lunar Terrain")]
        public static void Build()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<LunarTerrainConfig>(ConfigPath);
            if (cfg == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                cfg = ScriptableObject.CreateInstance<LunarTerrainConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }

            var env = GameObject.Find("Environment");
            if (env == null) { Debug.LogError("Environment root not found. Open Base.unity first."); return; }

            var shape = new Shape
            {
                cfg = cfg,
                zoneMin = cfg.flatZone.min,
                zoneMax = cfg.flatZone.max,
                offX = 100f + cfg.seed * 13.7f,
                offZ = 100f + cfg.seed * 7.3f,
            };
            shape.craters.AddRange(cfg.craters);
            shape.craters.AddRange(cfg.pits);
            AddSmallCraters(shape);

            var mesh = BuildMesh(shape);
            SaveAsset(mesh, MeshPath);
            BuildTexture(shape);
            ApplyToScene(env.transform, shape, mesh);
            AssetDatabase.SaveAssets();
            Debug.Log($"Lunar terrain built: {mesh.vertexCount} verts, {mesh.triangles.Length / 3} tris, {shape.craters.Count} craters.");
        }

        static void AddSmallCraters(Shape shape)
        {
            var cfg = shape.cfg;
            var rng = new System.Random(cfg.seed);
            int placed = 0, tries = 0;
            while (placed < cfg.smallCraterCount && tries++ < 2000)
            {
                float x = cfg.center.x + ((float)rng.NextDouble() * 2f - 1f) * (cfg.islandRadius - 4f);
                float z = cfg.center.y + ((float)rng.NextDouble() * 2f - 1f) * (cfg.islandRadius - 4f);
                if (Vector2.Distance(new Vector2(x, z), cfg.center) > cfg.islandRadius - 5f) continue;
                float r = Mathf.Lerp(cfg.smallCraterRadius.x, cfg.smallCraterRadius.y, (float)rng.NextDouble());
                float margin = r + cfg.smallCraterZoneMargin;
                if (x > shape.zoneMin.x - margin && x < shape.zoneMax.x + margin &&
                    z > shape.zoneMin.y - margin && z < shape.zoneMax.y + margin) continue;
                bool overlaps = false;
                foreach (var c in shape.craters)
                    if (Vector2.Distance(c.center, new Vector2(x, z)) < c.radius + r + 0.8f) { overlaps = true; break; }
                if (overlaps) continue;
                shape.craters.Add(new LunarTerrainConfig.Crater
                {
                    center = new Vector2(x, z), radius = r, depth = cfg.smallCraterDepth, rimHeight = cfg.smallCraterDepth * 0.3f,
                });
                placed++;
            }
        }

        static Mesh BuildMesh(Shape shape)
        {
            var cfg = shape.cfg;
            int n = Mathf.RoundToInt(cfg.halfSize * 2f / cfg.cellSize);
            float minX = cfg.center.x - cfg.halfSize, minZ = cfg.center.y - cfg.halfSize, size = cfg.halfSize * 2f;
            var verts = new Vector3[(n + 1) * (n + 1)];
            var uvs = new Vector2[verts.Length];
            var beyond = new bool[verts.Length];
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float x = minX + i * cfg.cellSize, z = minZ + j * cfg.cellSize;
                    int k = j * (n + 1) + i;
                    verts[k] = new Vector3(x, shape.Evaluate(x, z).height, z);
                    uvs[k] = new Vector2((x - minX) / size, (z - minZ) / size);
                    beyond[k] = shape.BeyondIsland(x, z);
                }

            var tris = new List<int>(n * n * 6);
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i, b = a + 1, c = a + (n + 1), d = c + 1;
                    if (beyond[a] && beyond[b] && beyond[c] && beyond[d]) continue;
                    // Alternate the diagonal so the low-poly facets do not all lean one way.
                    if (((i + j) & 1) == 0) { tris.Add(a); tris.Add(c); tris.Add(d); tris.Add(a); tris.Add(d); tris.Add(b); }
                    else { tris.Add(a); tris.Add(c); tris.Add(b); tris.Add(b); tris.Add(c); tris.Add(d); }
                }

            var mesh = new Mesh { name = "LunarGround", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void BuildTexture(Shape shape)
        {
            var cfg = shape.cfg;
            int size = cfg.textureSize;
            float minX = cfg.center.x - cfg.halfSize, minZ = cfg.center.y - cfg.halfSize, world = cfg.halfSize * 2f;
            var pixels = new Color32[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = minX + (i + 0.5f) / size * world, z = minZ + (j + 0.5f) / size * world;
                    var s = shape.Evaluate(x, z);
                    Color c = cfg.plainColor;
                    c = Color.Lerp(c, cfg.hillColor, Mathf.Clamp01(s.hill * 1.3f));
                    c = Color.Lerp(c, cfg.craterFloorColor, Mathf.Clamp01(s.bowl * 1.2f));
                    c = Color.Lerp(c, cfg.rimColor, Mathf.Clamp01(s.rim) * 0.8f);
                    c = Color.Lerp(c, cfg.edgeColor, Mathf.Clamp01(s.edge * 1.6f));
                    float n = Mathf.PerlinNoise(x * 0.8f + shape.offX, z * 0.8f + shape.offZ) * 0.6f
                            + Mathf.PerlinNoise(x * 3.1f + shape.offZ, z * 3.1f + shape.offX) * 0.4f;
                    c *= 1f + (n - 0.5f) * 2f * cfg.colorNoise;
                    if (Hash(i, j) > 0.9965f) c *= 0.72f;
                    c.a = 1f;
                    pixels[j * size + i] = c;
                }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0xFFFFFF;
        }

        static void SaveAsset(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { existing.Clear(); EditorUtility.CopySerialized(mesh, existing); }
        }

        static void ApplyToScene(Transform env, Shape shape, Mesh mesh)
        {
            var mesh2 = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", Vector2.one);
            mat.SetTextureOffset("_BaseMap", Vector2.zero);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Smoothness", 0.04f);
            EditorUtility.SetDirty(mat);

            var ground = env.Find("LunarGround");
            ground.gameObject.SetActive(true);
            ground.position = Vector3.zero;
            ground.localScale = Vector3.one;
            ground.GetComponent<MeshFilter>().sharedMesh = mesh2;
            ground.GetComponent<MeshCollider>().sharedMesh = mesh2;
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;

            // The old KayKit tile blocks are gone for good: the generated mesh now covers ground, plateaus and pit.
            var terrain = env.Find("Terrain");
            if (terrain != null) Object.DestroyImmediate(terrain.gameObject);

            var outer = env.Find("TerrainOuter");
            if (outer != null) Object.DestroyImmediate(outer.gameObject);

            EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
        }
    }
}
