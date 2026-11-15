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
    /// Builds rock prefabs and scatters them deterministically over LunarGround under Environment/RockScatter.
    /// Menu: Artemis X / Scatter Rocks.
    /// </summary>
    public static class RockScatterer
    {
        const string ConfigPath = "Assets/_Config/Visuals/RockScatterConfig.asset";
        const string TerrainConfigPath = "Assets/_Config/Visuals/LunarTerrainConfig.asset";
        const string LightMaterialPath = "Assets/_Materials/M_LunarTerrain.mat";
        const string DarkMaterialPath = "Assets/_Materials/M_LunarRockDark.mat";
        const string PrefabFolder = "Assets/_Prefabs/Environment";
        const string ScatterRootName = "RockScatter";
        const int MaxAllowedRocks = 250;

        readonly struct RockSpec
        {
            public readonly string prefabName;
            public readonly string fbxPath;
            public readonly float modelScale;
            public readonly bool useDarkMaterial;

            public RockSpec(string prefabName, string fbxPath, float modelScale, bool useDarkMaterial)
            {
                this.prefabName = prefabName;
                this.fbxPath = fbxPath;
                this.modelScale = modelScale;
                this.useDarkMaterial = useDarkMaterial;
            }
        }

        struct ExclusionBox
        {
            public float minX, maxX, minZ, maxZ;
            public bool isBuilding;

            public float DistanceTo(float x, float z)
            {
                float dx = Mathf.Max(minX - x, 0f, x - maxX);
                float dz = Mathf.Max(minZ - z, 0f, z - maxZ);
                return Mathf.Sqrt(dx * dx + dz * dz);
            }
        }

        struct Segment2D
        {
            public Vector2 a, b;

            public float DistanceTo(Vector2 p)
            {
                Vector2 ab = b - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                return Vector2.Distance(p, a + ab * t);
            }
        }

        struct FlatPad
        {
            public Vector2 center;
            public float radius;
        }

        struct PlacedRock
        {
            public Vector2 pos;
            public float radius;
            public int groupId;
        }

        struct PrefabMetrics
        {
            public GameObject prefab;
            public float baseRadius;
            public float baseHeight;
        }

        static readonly RockSpec[] RockSpecs =
        {
            new RockSpec("Rock_KayKit_A", "Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/rock_A.fbx", 1f, false),
            new RockSpec("Rock_KayKit_B", "Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/rock_B.fbx", 1f, false),
            new RockSpec("Rocks_KayKit_A", "Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/rocks_A.fbx", 0.85f, false),
            new RockSpec("Rocks_KayKit_B", "Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/rocks_B.fbx", 0.65f, true),
            new RockSpec("Rock_Quat_1", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_1.fbx", 14f, false),
            new RockSpec("Rock_Quat_2", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_2.fbx", 16f, false),
            new RockSpec("Rock_Quat_3", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_3.fbx", 13f, false),
            new RockSpec("Rock_Quat_4", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_4.fbx", 13f, false),
            new RockSpec("Rock_Quat_Large_1", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_Large_1.fbx", 14f, true),
            new RockSpec("Rock_Quat_Large_2", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_Large_2.fbx", 10f, true),
            new RockSpec("Rock_Quat_Large_3", "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Environment/FBX/Rock_Large_3.fbx", 10f, true),
        };

        [MenuItem("Artemis X/Scatter Rocks")]
        public static void Scatter()
        {
            var env = GameObject.Find("Environment");
            if (env == null)
            {
                Debug.LogError("Environment root not found. Open Base.unity first.");
                return;
            }

            var groundTf = env.transform.Find("LunarGround");
            var groundCollider = groundTf != null ? groundTf.GetComponent<MeshCollider>() : null;
            if (groundCollider == null)
            {
                Debug.LogError("Environment/LunarGround MeshCollider not found.");
                return;
            }

            var terrainCfg = AssetDatabase.LoadAssetAtPath<LunarTerrainConfig>(TerrainConfigPath);
            if (terrainCfg == null)
            {
                Debug.LogError($"LunarTerrainConfig not found at {TerrainConfigPath}.");
                return;
            }

            var cfg = EnsureAssets();
            if (cfg == null) return;

            var metrics = MeasurePrefabs(cfg.rockPrefabs);
            if (metrics.Count == 0)
            {
                Debug.LogError("No valid rock prefabs found in RockScatterConfig.");
                return;
            }

            var craters = CollectAllCraters(terrainCfg);
            var keepClearBoxes = CollectKeepClearBoxes();
            var existingRockBoxes = CollectExistingRockBoxes();
            var routeSegments = CollectRouteSegments();

            var oldScatter = env.transform.Find(ScatterRootName);
            if (oldScatter != null)
                Object.DestroyImmediate(oldScatter.gameObject);

            var scatterRoot = new GameObject(ScatterRootName);
            scatterRoot.transform.SetParent(env.transform, false);
            GameObjectUtility.SetStaticEditorFlags(scatterRoot, StaticEditorFlags.BatchingStatic);

            var rng = new System.Random(cfg.seed);
            var placed = new List<PlacedRock>();
            var groupCenters = new List<(Vector2 pos, bool isCluster)>();
            var styleBag = new List<int>();
            var usedStyles = new HashSet<int>();

            int targetTotal = Mathf.Clamp(cfg.targetRockCount, 1, MaxAllowedRocks - 1);
            int innerQuota = Mathf.Clamp(Mathf.RoundToInt(targetTotal * cfg.innerIslandQuotaFraction), 0, targetTotal);
            int clusterCount = 0;
            int singleCount = 0;
            int groupId = 0;
            int attempts = 0;
            int maxAttempts = 8000;

            float offX = 100f + terrainCfg.seed * 13.7f;
            float offZ = 100f + terrainCfg.seed * 7.3f;

            while (placed.Count < targetTotal && attempts++ < maxAttempts)
            {
                bool innerPass = placed.Count < innerQuota;
                float maxSampleR = innerPass
                    ? Mathf.Min(cfg.innerIslandRadius, terrainCfg.islandRadius - cfg.islandEdgeMargin)
                    : terrainCfg.islandRadius - cfg.islandEdgeMargin;
                float minSampleR = innerPass ? 6.5f : 5.5f;

                Vector2 dir = RandomUnitVector2(rng);
                float dist = Mathf.Sqrt(Mathf.Lerp(minSampleR * minSampleR, maxSampleR * maxSampleR, (float)rng.NextDouble()));
                Vector2 center = terrainCfg.center + dir * dist;

                if (EdgeFactor(terrainCfg, center.x, center.y) > 0.02f) continue;

                float density = EvaluateDensity(cfg, terrainCfg, craters, keepClearBoxes, center.x, center.y, offX, offZ);
                if ((float)rng.NextDouble() > density) continue;

                bool wantsCluster = (float)rng.NextDouble() < cfg.clusterProbability
                                    && targetTotal - placed.Count >= cfg.clusterSizeRange.x;
                float requiredGroupGap = wantsCluster ? cfg.minClusterGap : cfg.minSingleGap;
                if (!HasGroupClearance(groupCenters, center, requiredGroupGap)) continue;

                int remaining = targetTotal - placed.Count;
                int desiredCount = wantsCluster
                    ? Mathf.Min(remaining, rng.Next(cfg.clusterSizeRange.x, cfg.clusterSizeRange.y + 1))
                    : 1;

                int placedInGroup = 0;
                int memberTries = 0;
                int maxMemberTries = desiredCount * 10;
                float anchorRadius = 0f;

                while (placedInGroup < desiredCount && memberTries++ < maxMemberTries && placed.Count < targetTotal)
                {
                    int styleIdx = DrawStyleIndex(styleBag, metrics.Count, rng);
                    var pref = metrics[styleIdx];
                    if (wantsCluster && placedInGroup > 0)
                    {
                        for (int r = 0; r < metrics.Count && IsTallRock(pref.prefab.name); r++)
                        {
                            styleIdx = DrawStyleIndex(styleBag, metrics.Count, rng);
                            pref = metrics[styleIdx];
                        }
                    }

                    float u = (float)rng.NextDouble();
                    float scaleT = Mathf.Pow(u, cfg.scaleBiasPower);
                    if (wantsCluster && placedInGroup > 0)
                        scaleT *= 0.72f;
                    float scale = Mathf.Lerp(cfg.scaleRange.x, cfg.scaleRange.y, scaleT);

                    float worldRadius = pref.baseRadius * scale;
                    float worldHeight = pref.baseHeight * scale;

                    Vector2 pos;
                    if (!wantsCluster || placedInGroup == 0)
                    {
                        pos = center;
                    }
                    else
                    {
                        float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                        float spread = anchorRadius + worldRadius
                            + Mathf.Lerp(cfg.clusterSpreadRadius.x, cfg.clusterSpreadRadius.y, (float)rng.NextDouble());
                        pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spread;
                    }

                    if (!IsValidRockPlacement(
                            cfg, terrainCfg, groundCollider,
                            keepClearBoxes, existingRockBoxes, routeSegments, placed,
                            pos, worldRadius, worldHeight, groupId,
                            out RaycastHit centerHit, out float minFootY))
                    {
                        if (placedInGroup == 0) break;
                        continue;
                    }

                    Quaternion rot = ComputeRockRotation(cfg, centerHit.normal, rng);
                    float sink = ComputeSink(cfg, rng, centerHit.point.y, minFootY, worldRadius, worldHeight, rot, out bool tooSteep);
                    if (tooSteep)
                    {
                        if (placedInGroup == 0) break;
                        continue;
                    }

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(pref.prefab, scatterRoot.transform);
                    instance.name = $"{pref.prefab.name}_{placed.Count + 1:000}";
                    instance.transform.SetPositionAndRotation(new Vector3(pos.x, centerHit.point.y - sink, pos.y), rot);
                    instance.transform.localScale = Vector3.one * scale;
                    SetStaticRecursive(instance, StaticEditorFlags.BatchingStatic);

                    if (placedInGroup == 0) anchorRadius = worldRadius;
                    placed.Add(new PlacedRock { pos = pos, radius = worldRadius, groupId = groupId });
                    usedStyles.Add(styleIdx);
                    placedInGroup++;
                }

                if (placedInGroup > 0)
                {
                    groupCenters.Add((center, placedInGroup >= 2));
                    if (placedInGroup >= 2) clusterCount++;
                    else singleCount++;
                    groupId++;
                }
            }

            EditorSceneManager.MarkSceneDirty(groundCollider.gameObject.scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"Scattered {placed.Count} rocks ({clusterCount} clusters, {singleCount} singles, {usedStyles.Count}/{metrics.Count} styles) under Environment/{ScatterRootName}.");
        }

        static RockScatterConfig EnsureAssets()
        {
            var lightMat = AssetDatabase.LoadAssetAtPath<Material>(LightMaterialPath);
            if (lightMat == null)
            {
                Debug.LogError($"Material not found at {LightMaterialPath}.");
                return null;
            }

            var darkMat = AssetDatabase.LoadAssetAtPath<Material>(DarkMaterialPath);
            if (darkMat == null)
            {
                darkMat = new Material(lightMat);
                darkMat.name = Path.GetFileNameWithoutExtension(DarkMaterialPath);
                var darkGrey = new Color(0.60f, 0.60f, 0.64f, 1f);
                if (darkMat.HasProperty("_BaseColor")) darkMat.SetColor("_BaseColor", darkGrey);
                if (darkMat.HasProperty("_Color")) darkMat.SetColor("_Color", darkGrey);
                AssetDatabase.CreateAsset(darkMat, DarkMaterialPath);
            }

            EnsureFolder(PrefabFolder);

            var builtPrefabs = new GameObject[RockSpecs.Length];
            for (int i = 0; i < RockSpecs.Length; i++)
            {
                var spec = RockSpecs[i];
                string prefabPath = $"{PrefabFolder}/{spec.prefabName}.prefab";
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (existing != null)
                {
                    var existingModel = existing.transform.Find("Model");
                    if (existingModel != null && Mathf.Abs(existingModel.localScale.x - spec.modelScale) > 0.001f)
                    {
                        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
                        var modelTf = contents.transform.Find("Model");
                        if (modelTf != null)
                        {
                            modelTf.localPosition = Vector3.zero;
                            modelTf.localScale = Vector3.one * spec.modelScale;
                            Bounds updatedBounds = ComputeWorldMeshBounds(contents);
                            modelTf.localPosition = new Vector3(-updatedBounds.center.x, -updatedBounds.min.y, -updatedBounds.center.z);
                            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                        }
                        PrefabUtility.UnloadPrefabContents(contents);
                        existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    }
                    builtPrefabs[i] = existing;
                    continue;
                }

                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(spec.fbxPath);
                if (fbx == null)
                {
                    Debug.LogError($"Rock FBX missing at {spec.fbxPath}.");
                    return null;
                }

                var root = new GameObject(spec.prefabName);
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
                model.name = "Model";
                model.transform.localPosition = Vector3.zero;
                model.transform.localScale = Vector3.one * spec.modelScale;

                foreach (var col in model.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);

                var targetMat = spec.useDarkMaterial ? darkMat : lightMat;
                foreach (var mr in model.GetComponentsInChildren<MeshRenderer>(true))
                {
                    int slotCount = Mathf.Max(1, mr.sharedMaterials.Length);
                    var mats = new Material[slotCount];
                    for (int m = 0; m < slotCount; m++) mats[m] = targetMat;
                    mr.sharedMaterials = mats;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    mr.receiveShadows = true;
                }

                Bounds b = ComputeWorldMeshBounds(root);
                model.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);
                SetStaticRecursive(root, StaticEditorFlags.BatchingStatic);

                builtPrefabs[i] = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }

            var cfg = AssetDatabase.LoadAssetAtPath<RockScatterConfig>(ConfigPath);
            if (cfg == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                cfg = ScriptableObject.CreateInstance<RockScatterConfig>();
                cfg.rockPrefabs = builtPrefabs;
                AssetDatabase.CreateAsset(cfg, ConfigPath);
            }
            else if (HasMissingPrefabs(cfg.rockPrefabs))
            {
                cfg.rockPrefabs = builtPrefabs;
                EditorUtility.SetDirty(cfg);
            }

            return cfg;
        }

        static bool HasMissingPrefabs(GameObject[] prefabs)
        {
            if (prefabs == null || prefabs.Length == 0) return true;
            foreach (var p in prefabs)
                if (p == null) return true;
            return false;
        }

        static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;
            string parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        static Bounds ComputeWorldMeshBounds(GameObject root)
        {
            var filters = root.GetComponentsInChildren<MeshFilter>(true);
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;
            foreach (var mf in filters)
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var l2w = mf.transform.localToWorldMatrix;
                var verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 w = l2w.MultiplyPoint3x4(verts[i]);
                    min = Vector3.Min(min, w);
                    max = Vector3.Max(max, w);
                    any = true;
                }
            }
            if (!any) return new Bounds(root.transform.position, Vector3.zero);
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        static List<PrefabMetrics> MeasurePrefabs(GameObject[] prefabs)
        {
            var list = new List<PrefabMetrics>();
            if (prefabs == null) return list;
            foreach (var p in prefabs)
            {
                if (p == null) continue;
                var temp = (GameObject)PrefabUtility.InstantiatePrefab(p);
                temp.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                temp.transform.localScale = Vector3.one;
                Bounds b = ComputeWorldMeshBounds(temp);
                Object.DestroyImmediate(temp);

                list.Add(new PrefabMetrics
                {
                    prefab = p,
                    baseRadius = Mathf.Max(0.15f, Mathf.Max(b.extents.x, b.extents.z)),
                    baseHeight = Mathf.Max(0.1f, b.size.y),
                });
            }
            return list;
        }

        static List<LunarTerrainConfig.Crater> CollectAllCraters(LunarTerrainConfig terrainCfg)
        {
            var craters = new List<LunarTerrainConfig.Crater>();
            craters.AddRange(terrainCfg.craters);
            craters.AddRange(terrainCfg.pits);

            // Replicate LunarTerrainBuilder.CollectFlats & AddSmallCraters so small craters match the baked mesh.
            var flats = new List<FlatPad>();
            foreach (var rootName in new[] { "Base", "Slots", "Props", "IceField", "Rocks" })
            {
                var root = GameObject.Find(rootName);
                if (root == null) continue;
                foreach (Transform child in root.transform)
                {
                    var renderers = child.GetComponentsInChildren<Renderer>();
                    float radius = 0.6f;
                    Vector3 center = child.position;
                    if (renderers.Length > 0)
                    {
                        var bounds = renderers[0].bounds;
                        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                        center = bounds.center;
                        radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
                    }
                    radius = Mathf.Min(radius + terrainCfg.padMargin, terrainCfg.maxPadRadius);
                    flats.Add(new FlatPad { center = new Vector2(center.x, center.z), radius = radius });
                }
            }

            var rng = new System.Random(terrainCfg.seed);
            int placed = 0, tries = 0;
            while (placed < terrainCfg.smallCraterCount && tries++ < 2000)
            {
                float x = terrainCfg.center.x + ((float)rng.NextDouble() * 2f - 1f) * (terrainCfg.islandRadius - 4f);
                float z = terrainCfg.center.y + ((float)rng.NextDouble() * 2f - 1f) * (terrainCfg.islandRadius - 4f);
                if (Vector2.Distance(new Vector2(x, z), terrainCfg.center) > terrainCfg.islandRadius - 5f) continue;
                float r = Mathf.Lerp(terrainCfg.smallCraterRadius.x, terrainCfg.smallCraterRadius.y, (float)rng.NextDouble());

                float flatDist = float.MaxValue;
                var p = new Vector2(x, z);
                foreach (var f in flats)
                {
                    float d = Vector2.Distance(p, f.center) - f.radius;
                    if (d < flatDist) flatDist = d;
                }
                if (Mathf.Max(flatDist, 0f) < r + terrainCfg.smallCraterZoneMargin) continue;

                bool overlaps = false;
                foreach (var c in craters)
                {
                    if (Vector2.Distance(c.center, p) < c.radius + r + 0.8f)
                    {
                        overlaps = true;
                        break;
                    }
                }
                if (overlaps) continue;

                craters.Add(new LunarTerrainConfig.Crater
                {
                    center = p,
                    radius = r,
                    depth = terrainCfg.smallCraterDepth,
                    rimHeight = terrainCfg.smallCraterDepth * 0.3f,
                });
                placed++;
            }

            return craters;
        }

        static List<ExclusionBox> CollectKeepClearBoxes()
        {
            var boxes = new List<ExclusionBox>();
            AddChildBoxes(boxes, "Base", true);
            AddChildBoxes(boxes, "Slots", true);
            AddChildBoxes(boxes, "Environment/Props", false);
            AddChildBoxes(boxes, "Environment/IceField", false);
            AddChildBoxes(boxes, "Astronauts", false);

            var living = GameObject.Find("LivingBase");
            if (living != null)
            {
                foreach (Transform child in living.transform)
                {
                    if (child.name == "Paths" || child.name == "EmoteBubbles"
                        || child.name == "AmbientMood" || child.name == "MoodFX")
                        continue;
                    AddTransformBox(boxes, child, false, 0.25f);
                }
            }

            return boxes;
        }

        static List<ExclusionBox> CollectExistingRockBoxes()
        {
            var boxes = new List<ExclusionBox>();
            AddChildBoxes(boxes, "Environment/Rocks", false);
            return boxes;
        }

        static void AddChildBoxes(List<ExclusionBox> boxes, string hierarchyPath, bool isBuilding)
        {
            var root = GameObject.Find(hierarchyPath);
            if (root == null) return;
            foreach (Transform child in root.transform)
                AddTransformBox(boxes, child, isBuilding, 0.35f);
        }

        static void AddTransformBox(List<ExclusionBox> boxes, Transform target, bool isBuilding, float fallbackExtent)
        {
            Bounds b = default;
            bool hasBounds = false;
            foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                if (!hasBounds)
                {
                    b = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    b.Encapsulate(r.bounds);
                }
            }

            if (hasBounds)
            {
                boxes.Add(new ExclusionBox
                {
                    minX = b.min.x,
                    maxX = b.max.x,
                    minZ = b.min.z,
                    maxZ = b.max.z,
                    isBuilding = isBuilding,
                });
            }
            else
            {
                boxes.Add(new ExclusionBox
                {
                    minX = target.position.x - fallbackExtent,
                    maxX = target.position.x + fallbackExtent,
                    minZ = target.position.z - fallbackExtent,
                    maxZ = target.position.z + fallbackExtent,
                    isBuilding = isBuilding,
                });
            }
        }

        static List<Segment2D> CollectRouteSegments()
        {
            var segments = new List<Segment2D>();
            var samples = new List<Vector3>();
            var distances = new List<float>();

            foreach (var wp in Object.FindObjectsByType<WaypointPath>(FindObjectsSortMode.None))
            {
                int n = wp.Count;
                if (n < 2) continue;

                for (int i = 0; i < n; i++)
                {
                    Vector3 a = wp.GetPoint(i);
                    Vector3 b = wp.GetPoint(i + 1);
                    segments.Add(new Segment2D
                    {
                        a = new Vector2(a.x, a.z),
                        b = new Vector2(b.x, b.z),
                    });
                }

                wp.BuildSmoothLoop(8, samples, distances);
                for (int i = 0; i < samples.Count - 1; i++)
                {
                    Vector3 a = samples[i];
                    Vector3 b = samples[i + 1];
                    segments.Add(new Segment2D
                    {
                        a = new Vector2(a.x, a.z),
                        b = new Vector2(b.x, b.z),
                    });
                }
            }

            return segments;
        }

        static float LocalEdgeRadius(LunarTerrainConfig terrainCfg, float x, float z)
        {
            Vector2 rel = new Vector2(x - terrainCfg.center.x, z - terrainCfg.center.y);
            float angle = Mathf.Atan2(rel.y, rel.x);
            float wobble = (Mathf.PerlinNoise(Mathf.Cos(angle) * 1.6f + 20f, Mathf.Sin(angle) * 1.6f + 20f) - 0.5f) * 2f;
            return terrainCfg.islandRadius + wobble * terrainCfg.islandWobble;
        }

        static float EdgeFactor(LunarTerrainConfig terrainCfg, float x, float z)
        {
            Vector2 rel = new Vector2(x - terrainCfg.center.x, z - terrainCfg.center.y);
            float r = rel.magnitude;
            float edgeRadius = LocalEdgeRadius(terrainCfg, x, z);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edgeRadius - terrainCfg.edgeWidth, edgeRadius, r));
        }

        static float EvaluateDensity(
            RockScatterConfig cfg,
            LunarTerrainConfig terrainCfg,
            List<LunarTerrainConfig.Crater> craters,
            List<ExclusionBox> keepClearBoxes,
            float x,
            float z,
            float offX,
            float offZ)
        {
            var p = new Vector2(x, z);
            Vector2 rel = p - terrainCfg.center;
            float r = rel.magnitude;

            float northness = Mathf.Lerp(
                terrainCfg.hillSouthFactor,
                1f,
                Mathf.Clamp01(rel.y / Mathf.Max(r, 0.001f) * 0.5f + 0.5f));
            float hillBand = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(terrainCfg.hillStartRadius, terrainCfg.hillEndRadius, r));
            float hillNoise = 0.55f + 0.45f * Mathf.PerlinNoise(x * 0.09f + offZ, z * 0.09f + offX);
            float hill = Mathf.Clamp01(hillBand * northness * hillNoise * 1.15f);

            float maxRim = 0f;
            float maxBowl = 0f;
            foreach (var c in craters)
            {
                float d = Vector2.Distance(p, c.center);
                float t = d / c.radius;
                if (t < 0.85f)
                {
                    float bowl = 1f - t * t;
                    bowl *= bowl;
                    maxBowl = Mathf.Max(maxBowl, bowl);
                }
                float rimT = (t - 1.05f) / 0.30f;
                float rim = Mathf.Exp(-rimT * rimT);
                maxRim = Mathf.Max(maxRim, rim);
            }

            float w = cfg.plainDensity;
            w = Mathf.Max(w, Mathf.Lerp(cfg.plainDensity, cfg.hillDensity, hill));
            w = Mathf.Max(w, Mathf.Lerp(cfg.plainDensity, cfg.craterRimDensity, maxRim));
            if (maxBowl > 0.05f)
                w = Mathf.Max(w, Mathf.Lerp(cfg.plainDensity, cfg.craterBowlDensity, maxBowl));

            float minBuildingDist = float.MaxValue;
            foreach (var box in keepClearBoxes)
            {
                if (!box.isBuilding) continue;
                float bd = box.DistanceTo(x, z);
                if (bd < minBuildingDist) minBuildingDist = bd;
            }
            if (minBuildingDist < cfg.buildingKeepClearMargin + cfg.nearBuildingSoftBand)
                w *= cfg.nearBuildingDensityFactor;

            return Mathf.Clamp01(w);
        }

        static bool HasGroupClearance(List<(Vector2 pos, bool isCluster)> groups, Vector2 candidate, float gap)
        {
            foreach (var g in groups)
                if (Vector2.Distance(g.pos, candidate) < gap) return false;
            return true;
        }

        static bool IsValidRockPlacement(
            RockScatterConfig cfg,
            LunarTerrainConfig terrainCfg,
            MeshCollider groundCollider,
            List<ExclusionBox> keepClearBoxes,
            List<ExclusionBox> existingRockBoxes,
            List<Segment2D> routeSegments,
            List<PlacedRock> placed,
            Vector2 pos,
            float worldRadius,
            float worldHeight,
            int groupId,
            out RaycastHit centerHit,
            out float minFootY)
        {
            centerHit = default;
            minFootY = float.MaxValue;

            float distFromCenter = Vector2.Distance(pos, terrainCfg.center);
            float allowedRadius = Mathf.Min(terrainCfg.islandRadius, LocalEdgeRadius(terrainCfg, pos.x, pos.y))
                                  - cfg.islandEdgeMargin;
            if (distFromCenter + worldRadius > allowedRadius) return false;
            if (EdgeFactor(terrainCfg, pos.x, pos.y) > 0.02f) return false;

            foreach (var box in keepClearBoxes)
                if (box.DistanceTo(pos.x, pos.y) < cfg.buildingKeepClearMargin + worldRadius) return false;

            foreach (var box in existingRockBoxes)
                if (box.DistanceTo(pos.x, pos.y) < cfg.existingRockMargin + worldRadius) return false;

            foreach (var seg in routeSegments)
                if (seg.DistanceTo(pos) < cfg.routeKeepClearMargin + worldRadius) return false;

            foreach (var other in placed)
            {
                float clearance = other.groupId == groupId ? cfg.minIntraClusterClearance : cfg.minSingleGap * 0.5f;
                if (Vector2.Distance(other.pos, pos) < other.radius + worldRadius + clearance)
                    return false;
            }

            if (!groundCollider.Raycast(new Ray(new Vector3(pos.x, 50f, pos.y), Vector3.down), out centerHit, 120f))
                return false;

            float sampleR = Mathf.Max(0.15f, worldRadius * 0.45f);
            Vector2[] offsets =
            {
                new Vector2(sampleR, 0f),
                new Vector2(-sampleR, 0f),
                new Vector2(0f, sampleR),
                new Vector2(0f, -sampleR),
            };

            foreach (var off in offsets)
            {
                float sx = pos.x + off.x;
                float sz = pos.y + off.y;
                if (EdgeFactor(terrainCfg, sx, sz) > 0.05f) return false;
                if (!groundCollider.Raycast(new Ray(new Vector3(sx, 50f, sz), Vector3.down), out RaycastHit footHit, 120f))
                    return false;
                if (footHit.point.y < minFootY) minFootY = footHit.point.y;
            }

            return true;
        }

        static Quaternion ComputeRockRotation(RockScatterConfig cfg, Vector3 surfaceNormal, System.Random rng)
        {
            Vector3 alignedUp = Vector3.Slerp(Vector3.up, surfaceNormal.normalized, cfg.normalAlignBlend).normalized;
            Quaternion alignRot = Quaternion.FromToRotation(Vector3.up, alignedUp);
            float yaw = (float)rng.NextDouble() * 360f;
            float tiltAngle = (float)rng.NextDouble() * cfg.maxTiltDegrees;
            float tiltAxisAngle = (float)rng.NextDouble() * Mathf.PI * 2f;
            Vector3 tiltAxis = new Vector3(Mathf.Cos(tiltAxisAngle), 0f, Mathf.Sin(tiltAxisAngle));
            return Quaternion.AngleAxis(tiltAngle, tiltAxis) * alignRot * Quaternion.AngleAxis(yaw, Vector3.up);
        }

        static float ComputeSink(
            RockScatterConfig cfg,
            System.Random rng,
            float centerGroundY,
            float minFootY,
            float worldRadius,
            float worldHeight,
            Quaternion rot,
            out bool tooSteep)
        {
            float baseSink = Mathf.Lerp(cfg.sinkFractionRange.x, cfg.sinkFractionRange.y, (float)rng.NextDouble()) * worldHeight;
            float slopeDrop = Mathf.Max(0f, centerGroundY - minFootY);
            float sampleR = Mathf.Max(0.15f, worldRadius * 0.45f);
            float tiltDeg = Vector3.Angle(Vector3.up, rot * Vector3.up);
            float tiltRise = sampleR * Mathf.Sin(tiltDeg * Mathf.Deg2Rad);

            float neededSink = Mathf.Max(baseSink, slopeDrop + tiltRise + 0.02f * worldHeight);
            tooSteep = neededSink > worldHeight * 0.35f;
            return Mathf.Min(neededSink, worldHeight * 0.28f);
        }

        static bool IsTallRock(string name)
        {
            return name == "Rocks_KayKit_B" || name == "Rock_Quat_Large_2" || name == "Rock_Quat_Large_3";
        }

        static int DrawStyleIndex(List<int> bag, int count, System.Random rng)
        {
            if (bag.Count == 0)
            {
                for (int i = 0; i < count; i++) bag.Add(i);
                for (int i = bag.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (bag[i], bag[j]) = (bag[j], bag[i]);
                }
            }
            int idx = bag[bag.Count - 1];
            bag.RemoveAt(bag.Count - 1);
            return idx;
        }

        static Vector2 RandomUnitVector2(System.Random rng)
        {
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        static void SetStaticRecursive(GameObject go, StaticEditorFlags flags)
        {
            GameObjectUtility.SetStaticEditorFlags(go, flags);
            foreach (Transform child in go.transform)
                SetStaticRecursive(child.gameObject, flags);
        }
    }
}
