using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtemisX.EditorTools
{
    /// <summary>
    /// Builds the two new Kenney Space Kit buildings on their terraformed pads in HandMadeBase:
    ///  - ProcessingPlant: where rovers drop regolith / water ice; melts ice into water.
    ///  - Electrolyser_H2Generator: splits water into O2 (breathing) and H2, and burns H2 for power.
    /// Plus the water pipeline between them. Re-running rebuilds the visuals (keeps the root objects and
    /// any components on them, e.g. ClickableBuilding). Menu: Artemis X / Build Kenney Buildings.
    /// </summary>
    public static class KenneyBuildingsBuilder
    {
        const string Kit = "Assets/GameAssets/kenney_space-kit/Models/FBX format/";
        const string VisualsName = "Visuals";

        struct Part
        {
            public string model, name;
            public Vector3 pos;
            public float rotY;
            public Vector3 scale;
            public Part(string model, string name, float x, float y, float z, float rotY = 0f, float s = 1f)
            { this.model = model; this.name = name; pos = new Vector3(x, y, z); this.rotY = rotY; scale = Vector3.one * s; }
        }

        struct Glow { public string name; public Vector3 pos; public Color color; public float range, intensity; }

        static readonly Color Water = new Color(0.35f, 0.85f, 1f);
        static readonly Color Hydrogen = new Color(1f, 0.55f, 0.2f);

        // ---------- Processing plant (pad centre 1.0, -1.4) ----------
        static readonly Vector2 PlantPad = new Vector2(1.0f, -1.4f);
        static readonly Part[] PlantParts =
        {
            new Part("hangar_roundB", "ProcessingHall", -0.45f, 0f, 0.45f),
            new Part("platform_large", "UnloadingDeck", 1.35f, 0f, -0.95f),
            new Part("machine_barrelLarge", "IntakeHopper", 1.45f, 0.1f, -0.9f, 0f, 1.6f),
            new Part("pipe_ringHigh", "ConveyorArch", 0.55f, 0.1f, -0.55f, 90f, 0.9f),
            new Part("rocket_sidesB", "WaterSilo_Lower", -1.75f, 0f, -1.3f, 0f, 0.8f),
            new Part("rocket_sidesB", "WaterSilo_Upper", -1.75f, 0.8f, -1.3f, 0f, 0.8f),
            new Part("rocket_fuelA", "WaterSilo_Cap", -1.75f, 1.6f, -1.3f, 0f, 0.8f),
            new Part("chimney_detailed", "SteamStack", -1.85f, 0f, 1.5f),
            new Part("satelliteDish_large", "Dish", 1.85f, 0f, 1.25f, 210f),
            new Part("barrels", "Barrels", 2.15f, 0.1f, -1.75f, 15f),
        };
        static readonly Glow[] PlantGlows =
        {
            new Glow { name = "Glow_Intake", pos = new Vector3(1.45f, 1.3f, -0.9f), color = Water, range = 2.5f, intensity = 2.5f },
            new Glow { name = "Glow_Silo", pos = new Vector3(-1.75f, 2.0f, -1.3f), color = Water, range = 2f, intensity = 1.8f },
        };

        // ---------- Electrolyser + H2 generator (pad centre -1.0, -7.8) ----------
        static readonly Vector2 ElectroPad = new Vector2(-1.0f, -7.8f);
        static readonly Part[] ElectroParts =
        {
            new Part("platform_large", "Deck_West", -1.0f, 0f, 0.4f),
            new Part("platform_large", "Deck_East", 1.0f, 0f, 0.4f),
            new Part("machine_generatorLarge", "ElectrolyserStack", 0f, 0.1f, 0.55f, 90f, 1.5f),
            new Part("rocket_sidesA", "O2Tank_Lower", -1.45f, 0.1f, 0.6f),
            new Part("rocket_sidesA", "O2Tank_Upper", -1.45f, 1.1f, 0.6f),
            new Part("rocket_fuelA", "O2Tank_Cap", -1.45f, 2.1f, 0.6f),
            new Part("rocket_sidesB", "H2Tank_Lower", 1.45f, 0.1f, 0.6f, 0f, 0.85f),
            new Part("rocket_sidesB", "H2Tank_Upper", 1.45f, 0.95f, 0.6f, 0f, 0.85f),
            new Part("rocket_fuelA", "H2Tank_Cap", 1.45f, 1.8f, 0.6f, 0f, 0.85f),
            new Part("pipe_straight", "Pipe_ToO2", -0.8f, 0.1f, 0.55f, 90f, 0.55f),
            new Part("pipe_straight", "Pipe_ToH2", 0.8f, 0.1f, 0.55f, 90f, 0.55f),
            new Part("pipe_straight", "Pipe_H2ToGenerator", 1.45f, 0f, -0.75f, 0f, 0.55f),
            new Part("machine_generator", "H2Generator", 1.0f, 0f, -1.55f, 180f, 2.0f),
            new Part("chimney_detailed", "GeneratorExhaust", 2.2f, 0f, -1.65f, 0f, 0.8f),
            new Part("machine_wirelessCable", "PowerUplink", -1.5f, 0f, -1.5f, 160f, 1.4f),
        };
        static readonly Glow[] ElectroGlows =
        {
            new Glow { name = "Glow_Electrolyser", pos = new Vector3(0f, 1.4f, 0.55f), color = Water, range = 2.5f, intensity = 2.5f },
            new Glow { name = "Glow_H2", pos = new Vector3(1.2f, 1.2f, -1.4f), color = Hydrogen, range = 2.5f, intensity = 2.5f },
        };

        [MenuItem("Artemis X/Build Kenney Buildings")]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var ground = FindGround(scene);
            if (ground == null) { Debug.LogError("[KenneyBuildings] LunarGround not found — open HandMadeBase.unity."); return; }

            var plant = BuildOne(scene, ground, "ProcessingPlant", PlantPad, PlantParts, PlantGlows);
            var electro = BuildOne(scene, ground, "Electrolyser_H2Generator", ElectroPad, ElectroParts, ElectroGlows);
            BuildPipeline(plant.transform, ground,
                new Vector2(-0.3f, PlantPad.y - 1.75f), new Vector2(-0.3f, ElectroPad.y + 2.05f));

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[KenneyBuildings] Built ProcessingPlant and Electrolyser_H2Generator.");
        }

        static GameObject BuildOne(UnityEngine.SceneManagement.Scene scene, MeshCollider ground, string rootName, Vector2 pad, Part[] parts, Glow[] glows)
        {
            GameObject root = null;
            foreach (var r in scene.GetRootGameObjects()) if (r.name == rootName) root = r;
            if (root == null)
            {
                root = new GameObject(rootName);
                Undo.RegisterCreatedObjectUndo(root, "Build Kenney Buildings");
            }
            float y = GroundY(ground, pad);
            Undo.RecordObject(root.transform, "Build Kenney Buildings");
            root.transform.SetPositionAndRotation(new Vector3(pad.x, y, pad.y), Quaternion.identity);
            root.transform.localScale = Vector3.one;

            var old = root.transform.Find(VisualsName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var visuals = new GameObject(VisualsName);
            Undo.RegisterCreatedObjectUndo(visuals, "Build Kenney Buildings");
            visuals.transform.SetParent(root.transform, false);

            foreach (var p in parts) Spawn(p, visuals.transform);
            foreach (var g in glows)
            {
                var go = new GameObject(g.name);
                go.transform.SetParent(visuals.transform, false);
                go.transform.localPosition = g.pos;
                var l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = g.color;
                l.range = g.range;
                l.intensity = g.intensity;
                l.shadows = LightShadows.None;
            }

            // Click target for ClickableBuilding (keeps an existing collider's settings if one is there).
            var bounds = RendererBounds(visuals);
            var box = root.GetComponent<BoxCollider>();
            if (box == null) box = Undo.AddComponent<BoxCollider>(root);
            box.center = root.transform.InverseTransformPoint(bounds.center);
            box.size = bounds.size;
            return root;
        }

        static GameObject Spawn(Part p, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Kit + p.model + ".fbx");
            if (asset == null) { Debug.LogWarning("[KenneyBuildings] Missing model " + p.model); return null; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            go.name = p.name;
            go.transform.localPosition = p.pos;
            go.transform.localRotation = Quaternion.Euler(0f, p.rotY, 0f);
            go.transform.localScale = p.scale;
            return go;
        }

        /// <summary>Water line from the plant to the electrolyser: pipe segments on low supports, following the ground.</summary>
        static void BuildPipeline(Transform plant, MeshCollider ground, Vector2 from, Vector2 to)
        {
            var visuals = plant.Find(VisualsName);
            var line = new GameObject("WaterPipeline");
            line.transform.SetParent(visuals, true);
            float len = Vector2.Distance(from, to);
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.6f));
            Vector2 dir = (to - from).normalized;
            float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = from + dir * (len * i / n), b = from + dir * (len * (i + 1) / n), m = (a + b) * 0.5f;
                float ya = GroundY(ground, a), yb = GroundY(ground, b);
                float pitch = -Mathf.Atan2(yb - ya, Vector2.Distance(a, b)) * Mathf.Rad2Deg;
                var seg = Spawn(new Part("pipe_straight", $"Pipe_{i:00}", 0, 0, 0), line.transform);
                seg.transform.position = new Vector3(a.x, ya + 0.12f, a.y);
                seg.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                seg.transform.localScale = new Vector3(0.45f, 0.45f, Vector2.Distance(a, b) * 1.02f);
                // Pivot of pipe_straight is at one end, centred: shift so the segment spans a→b.
                var rb = RendererBounds(seg);
                Vector3 mid = new Vector3(m.x, (ya + yb) * 0.5f + 0.12f + rb.extents.y, m.y);
                seg.transform.position += mid - rb.center;
                if (i % 2 == 0)
                {
                    var sup = Spawn(new Part("pipe_supportLow", $"Support_{i:00}", 0, 0, 0), line.transform);
                    sup.transform.position = new Vector3(m.x, GroundY(ground, m), m.y);
                    sup.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    sup.transform.localScale = Vector3.one * 0.45f;
                }
            }
        }

        static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static MeshCollider FindGround(UnityEngine.SceneManagement.Scene scene)
        {
            foreach (var r in scene.GetRootGameObjects())
            {
                if (!r.name.Contains("ENVIRONMENT")) continue;
                var g = r.transform.Find("LunarGround");
                if (g != null) return g.GetComponent<MeshCollider>();
            }
            return null;
        }

        static float GroundY(MeshCollider col, Vector2 p)
        {
            var ray = new Ray(new Vector3(p.x, 60f, p.y), Vector3.down);
            return col.Raycast(ray, out var hit, 200f) ? hit.point.y : 0f;
        }
    }
}
