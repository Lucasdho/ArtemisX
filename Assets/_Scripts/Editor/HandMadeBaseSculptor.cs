using System.Collections.Generic;
using System.IO;
using ArtemisX.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtemisX.EditorTools
{
    /// <summary>
    /// Sculpts the HandMadeBase scene's ground:
    ///  - extra relief where the base sits,
    ///  - level pads for the buildings (processing plant, electrolyser + H2 generator, food farm),
    ///  - rover lanes from the processing plant out to every DepositRing (smoothed lane + tyre tracks),
    ///    including a trench under the garage tunnel so the hover fits underneath,
    ///  - while keeping the ground under every hand-placed building exactly as it is.
    /// Always starts from the shared LunarGround mesh/texture and writes HandMadeBase copies, so Base.unity is untouched.
    /// Safe to re-run after tweaking the numbers below. Menu: Artemis X / Sculpt HandMadeBase Terrain.
    /// </summary>
    public static class HandMadeBaseSculptor
    {
        const string SourceMesh = "Assets/_Materials/LunarGround.asset";
        const string SourceTex = "Assets/_Materials/T_LunarGround.png";
        const string SourceMat = "Assets/_Materials/M_LunarGround.mat";
        const string OutMesh = "Assets/_Materials/LunarGround_HandMadeBase.asset";
        const string OutTex = "Assets/_Materials/T_LunarGround_HandMadeBase.png";
        const string OutMat = "Assets/_Materials/M_LunarGround_HandMadeBase.mat";

        // ---------- Base relief (don't change these once buildings are placed: the ground under them depends on it) ----------
        static readonly Vector2 BaseCenter = Vector2.zero;
        const float ReliefInner = 8f, ReliefOuter = 15f;
        const float ReliefAmp = 1.5f, ReliefScale = 0.11f;
        const float ReliefDetailAmp = 0.45f, ReliefDetailScale = 0.28f;
        const int ReliefSeed = 3;
        static readonly Vector4[] Features = // x, z, radius, height
        {
            new Vector4(-4.5f, 4.5f, 4.5f, 1.1f),
            new Vector4(3.5f, 6.5f, 3f, 0.6f),
            new Vector4(5f, -3.5f, 3.5f, -0.6f),
        };

        // ---------- Building pads ----------
        struct Pad
        {
            public string name;      // root object the pad belongs to (used to seat it); may not exist yet
            public Vector2 center;
            public Vector2 halfSize; // level rectangle half extents (rotated by angle)
            public float angle;      // degrees, around Y
            public float level;      // NaN = average of the ground under it
            public bool seatChildren;// move the root's direct children so they sit on the pad
        }

        const float PadBlend = 1.3f;
        static readonly Pad[] Pads =
        {
            new Pad { name = "ProcessingPlant", center = new Vector2(1.0f, -1.4f), halfSize = new Vector2(2.3f, 2.0f), level = float.NaN },
            new Pad { name = "Electrolyser_H2Generator", center = new Vector2(-1.0f, -7.8f), halfSize = new Vector2(2.6f, 2.2f), level = float.NaN },
            new Pad { name = "foodFarm", center = new Vector2(6.9f, -4.6f), halfSize = new Vector2(2.7f, 2.6f), level = float.NaN, seatChildren = true },
        };

        // ---------- Lanes ----------
        static readonly Vector2 HubCenter = new Vector2(1.0f, -1.4f); // processing plant: every lane starts here
        const float HubStartRadius = 2.6f;
        const float RingClearance = 2.2f;
        const float LaneHalfWidth = 0.9f, LaneShoulder = 1.6f, LaneSink = 0.05f, ProfileSmooth = 3f;
        const float LaneTint = 0.93f, TrackTint = 0.8f, PadTint = 0.95f;
        const float TrackOffset = 0.42f, TrackWidth = 0.16f;
        const float RockClearance = 1.2f;
        const float RouteNodeSpacing = 2.5f;

        struct Ceiling { public Vector2 at; public float radius, maxY; }

        class LaneDef
        {
            public Vector2[] vias = new Vector2[0];
            public Ceiling[] ceilings = new Ceiling[0];
            public float halfWidth = LaneHalfWidth, shoulder = LaneShoulder;
        }

        static readonly Dictionary<string, LaneDef> LaneDefs = new Dictionary<string, LaneDef>
        {
            // Through the gap between the garage and the gold-dome building, under the raised tunnel.
            ["Hotspot_Ice_NW"] = new LaneDef
            {
                vias = new[] { new Vector2(-3.36f, 1.04f), new Vector2(-4.76f, 2.48f), new Vector2(-6.16f, 3.92f) },
                ceilings = new[] { new Ceiling { at = new Vector2(-4.76f, 2.48f), radius = 2.2f, maxY = 0.9f } },
                halfWidth = 0.8f, shoulder = 0.9f,
            },
            ["Hotspot_Ice_SW"] = new LaneDef { vias = new[] { new Vector2(-4.6f, -5.8f) } },
            ["Hotspot_Minerals_SSE"] = new LaneDef { vias = new[] { new Vector2(3.6f, -7.0f) } },
            ["Hotspot_Minerals_NE"] = new LaneDef { vias = new[] { new Vector2(4.6f, 0.9f) } },
            ["Hotspot_Rocks_East"] = new LaneDef(),
        };

        // Hand-placed roots whose ground must not move. Everything else at the scene root that sits on the ground is protected too.
        static readonly HashSet<string> NotProtected = new HashSet<string>
        {
            "-----------SETUP----------------", "---------MANAGERS-------------", "------------HUD-----------------",
            "-------ENVIRONMENT-----------", "OLDBASE", "ExplorationHover", "foodFarm", "ProcessingPlant", "Electrolyser_H2Generator",
        };
        const float ProtectMargin = 0.45f, ProtectBlend = 0.7f; // margin > grid cell (0.6 m diagonal/2) so no triangle under a building tilts

        class Path
        {
            public string name;
            public LaneDef def;
            public Vector2 target;
            public readonly List<Vector2> pts = new List<Vector2>();
            public readonly List<float> cum = new List<float>();
            public readonly List<float> prof = new List<float>();
            public Rect bounds;
            public float Length => cum[cum.Count - 1];

            public float Nearest(Vector2 p, out float s, out float height, out Vector2 closest)
            {
                float best = float.MaxValue; s = 0f; height = 0f; closest = pts[0];
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    Vector2 a = pts[i], ab = pts[i + 1] - a;
                    float len2 = ab.sqrMagnitude;
                    float t = len2 > 1e-8f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                    Vector2 c = a + ab * t;
                    float d = (p - c).sqrMagnitude;
                    if (d < best)
                    {
                        best = d; closest = c;
                        s = Mathf.Lerp(cum[i], cum[i + 1], t);
                        height = Mathf.Lerp(prof[i], prof[i + 1], t);
                    }
                }
                return Mathf.Sqrt(best);
            }
        }

        class Grid
        {
            public float minX, minZ, cell;
            public int row;
            public float[] h;

            public float Sample(float x, float z)
            {
                float fx = Mathf.Clamp((x - minX) / cell, 0f, row - 1.001f);
                float fz = Mathf.Clamp((z - minZ) / cell, 0f, row - 1.001f);
                int i = (int)fx, j = (int)fz;
                float tx = fx - i, tz = fz - j;
                float a = h[j * row + i], b = h[j * row + i + 1], c = h[(j + 1) * row + i], d = h[(j + 1) * row + i + 1];
                return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), tz);
            }
        }

        [MenuItem("Artemis X/Sculpt HandMadeBase Terrain")]
        public static void Sculpt()
        {
            var scene = EditorSceneManager.GetActiveScene();
            GameObject env = null;
            foreach (var root in scene.GetRootGameObjects())
                if (root.name.Contains("ENVIRONMENT")) env = root;
            if (env == null) { Debug.LogError("[Sculptor] ENVIRONMENT root not found — open HandMadeBase.unity."); return; }
            var ground = env.transform.Find("LunarGround")?.gameObject;
            if (ground == null) { Debug.LogError("[Sculptor] LunarGround not found."); return; }
            var rockScatter = env.transform.Find("RockScatter");
            var collider = ground.GetComponent<MeshCollider>();

            var src = AssetDatabase.LoadAssetAtPath<Mesh>(SourceMesh);
            var verts = src.vertices;
            var grid = BuildGrid(verts);

            // Current heights (what the hand-placed buildings are standing on).
            var currentMesh = ground.GetComponent<MeshFilter>().sharedMesh;
            float[] current = null;
            if (currentMesh != null && currentMesh.vertexCount == verts.Length)
            {
                var cv = currentMesh.vertices;
                current = new float[cv.Length];
                for (int k = 0; k < cv.Length; k++) current[k] = cv[k].y;
            }

            // 1) Relief.
            for (int k = 0; k < verts.Length; k++) grid.h[k] += Relief(verts[k].x, verts[k].z);

            // 2) Pads.
            var levels = ApplyPads(grid, verts);

            // 3) Lanes.
            var paths = BuildPaths(grid);
            CarveLanes(grid, verts, paths);

            // 4) Level the ground under every hand-placed building to its base (no floating corners, no buried sides).
            var protect = CollectProtected(scene, collider);
            int protectedVerts = 0;
            // (levels come from the buildings themselves)
                for (int k = 0; k < verts.Length; k++)
                {
                    float w = ProtectWeight(protect, verts[k].x, verts[k].z, out float baseLevel);
                    if (w <= 0f) continue;
                    grid.h[k] = Mathf.Lerp(grid.h[k], baseLevel, w);
                    protectedVerts++;
                }

            // Tunnels win over protection: the lane centre under a ceiling is always at or below it.
            EnforceCeilings(grid, verts, paths);

            for (int k = 0; k < verts.Length; k++) verts[k].y = grid.h[k];

            // Remember ground under everything we re-seat, before the mesh changes.
            var seat = new List<Transform>();
            if (rockScatter != null) foreach (Transform c in rockScatter) seat.Add(c);
            var hotspots = env.transform.Find("ResourceHotspots");
            if (hotspots != null) foreach (Transform hs in hotspots) foreach (Transform c in hs) seat.Add(c);
            var hover = FindRoot(scene, "ExplorationHover");
            if (hover != null) seat.Add(hover.transform);
            var before = new float[seat.Count];
            for (int i = 0; i < seat.Count; i++) before[i] = GroundY(collider, seat[i].position);

            var undoObjects = new List<Object>(seat.ConvertAll(t => (Object)t));
            undoObjects.Add(ground.GetComponent<MeshFilter>());
            undoObjects.Add(collider);
            undoObjects.Add(ground.GetComponent<MeshRenderer>());
            foreach (var pad in Pads)
            {
                var r = FindRoot(scene, pad.name);
                if (r != null) foreach (Transform c in r.transform) undoObjects.Add(c);
            }
            Undo.RecordObjects(undoObjects.ToArray(), "Sculpt HandMadeBase Terrain");

            // Push scatter rocks off lanes and pads.
            int pushed = 0;
            if (rockScatter != null)
                foreach (Transform rock in rockScatter)
                {
                    var p = new Vector2(rock.position.x, rock.position.z);
                    var start = p;
                    foreach (var path in paths)
                    {
                        if (!path.bounds.Contains(p)) continue;
                        float d = path.Nearest(p, out _, out _, out var c);
                        float clear = RockClearance + (path.def.halfWidth - LaneHalfWidth);
                        if (d >= clear) continue;
                        Vector2 dir = d > 1e-3f ? (p - c) / d : Vector2.right;
                        p = c + dir * (clear + 0.2f);
                    }
                    foreach (var pad in Pads)
                    {
                        var local = ToPad(pad, p);
                        var hs = pad.halfSize + Vector2.one * 0.4f;
                        if (Mathf.Abs(local.x) > hs.x || Mathf.Abs(local.y) > hs.y) continue;
                        // Push out through the nearest side.
                        if (hs.x - Mathf.Abs(local.x) < hs.y - Mathf.Abs(local.y)) local.x = Mathf.Sign(local.x == 0 ? 1 : local.x) * (hs.x + 0.3f);
                        else local.y = Mathf.Sign(local.y == 0 ? 1 : local.y) * (hs.y + 0.3f);
                        p = FromPad(pad, local);
                    }
                    if (p != start) { rock.position = new Vector3(p.x, rock.position.y, p.y); pushed++; }
                }

            // 5) Save mesh, texture and material copies, and swap them in.
            var mesh = new Mesh { name = "LunarGround_HandMadeBase", indexFormat = src.indexFormat };
            mesh.vertices = verts;
            mesh.uv = src.uv;
            mesh.triangles = src.triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var saved = SaveMesh(mesh);
            var tex = PaintTexture(src, paths);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OutMat);
            if (mat == null) { AssetDatabase.CopyAsset(SourceMat, OutMat); mat = AssetDatabase.LoadAssetAtPath<Material>(OutMat); }
            mat.SetTexture("_BaseMap", tex);
            EditorUtility.SetDirty(mat);

            ground.GetComponent<MeshFilter>().sharedMesh = saved;
            collider.sharedMesh = null;
            collider.sharedMesh = saved;
            ground.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Physics.SyncTransforms();

            for (int i = 0; i < seat.Count; i++)
            {
                if (float.IsNaN(before[i])) continue;
                float after = GroundY(collider, seat[i].position);
                if (float.IsNaN(after)) continue;
                seat[i].position += Vector3.up * (after - before[i]);
            }

            // 6) Seat pad owners that ask for it (food farm) flat on their pad.
            for (int p = 0; p < Pads.Length; p++)
            {
                if (!Pads[p].seatChildren) continue;
                var r = FindRoot(scene, Pads[p].name);
                if (r == null) continue;
                foreach (Transform child in r.transform)
                {
                    var rs = child.GetComponentsInChildren<Renderer>();
                    if (rs.Length == 0) continue;
                    float minY = float.MaxValue;
                    foreach (var rr in rs) minY = Mathf.Min(minY, rr.bounds.min.y);
                    child.position += Vector3.up * (levels[p] - minY);
                }
            }

            // 7) Rover routes following the lanes.
            BuildRoutes(env.transform, paths, collider);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            var padInfo = "";
            for (int p = 0; p < Pads.Length; p++) padInfo += $" {Pads[p].name}@{levels[p]:F2}";
            Debug.Log($"[Sculptor] Done: {paths.Count} lanes, pads:{padInfo}, {protect.Count} protected footprints ({protectedVerts} verts kept), {pushed} rocks pushed, {seat.Count} objects re-seated.");
        }

        // ---------------------------------------------------------------- helpers

        static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var r in scene.GetRootGameObjects()) if (r.name == name) return r;
            return null;
        }

        static Grid BuildGrid(Vector3[] v)
        {
            var g = new Grid { minX = v[0].x, minZ = v[0].z, cell = v[1].x - v[0].x };
            int row = 0;
            while (row < v.Length && Mathf.Approximately(v[row].z, v[0].z)) row++;
            g.row = row;
            g.h = new float[v.Length];
            for (int k = 0; k < v.Length; k++) g.h[k] = v[k].y;
            return g;
        }

        static float Relief(float x, float z)
        {
            float r = Vector2.Distance(new Vector2(x, z), BaseCenter);
            float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ReliefInner, ReliefOuter, r));
            if (w <= 0f) return 0f;
            return (RawRelief(x, z) - RawRelief(BaseCenter.x, BaseCenter.y)) * w;
        }

        static float RawRelief(float x, float z)
        {
            float o = ReliefSeed * 31.7f + 500f;
            float big = (Mathf.PerlinNoise(x * ReliefScale + o, z * ReliefScale + o * 0.7f) - 0.5f) * 2f;
            float fine = (Mathf.PerlinNoise(x * ReliefDetailScale + o * 0.3f, z * ReliefDetailScale + o) - 0.5f) * 2f;
            float h = big * ReliefAmp + fine * ReliefDetailAmp;
            foreach (var f in Features)
            {
                float d = Vector2.Distance(new Vector2(x, z), new Vector2(f.x, f.y)) / f.z;
                if (d < 1f) { float b = 1f - d * d; h += f.w * b * b; }
            }
            return h;
        }

        static Vector2 ToPad(Pad pad, Vector2 p)
        {
            float a = -pad.angle * Mathf.Deg2Rad;
            Vector2 d = p - pad.center;
            return new Vector2(d.x * Mathf.Cos(a) - d.y * Mathf.Sin(a), d.x * Mathf.Sin(a) + d.y * Mathf.Cos(a));
        }

        static Vector2 FromPad(Pad pad, Vector2 local)
        {
            float a = pad.angle * Mathf.Deg2Rad;
            return pad.center + new Vector2(local.x * Mathf.Cos(a) - local.y * Mathf.Sin(a), local.x * Mathf.Sin(a) + local.y * Mathf.Cos(a));
        }

        /// <summary>Distance outside the pad rectangle (0 inside).</summary>
        static float PadDistance(Pad pad, Vector2 p)
        {
            var l = ToPad(pad, p);
            float dx = Mathf.Max(Mathf.Abs(l.x) - pad.halfSize.x, 0f), dz = Mathf.Max(Mathf.Abs(l.y) - pad.halfSize.y, 0f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static float[] ApplyPads(Grid grid, Vector3[] verts)
        {
            var levels = new float[Pads.Length];
            for (int p = 0; p < Pads.Length; p++)
            {
                float level = Pads[p].level;
                if (float.IsNaN(level))
                {
                    float sum = 0f; int n = 0;
                    for (int k = 0; k < verts.Length; k++)
                        if (PadDistance(Pads[p], new Vector2(verts[k].x, verts[k].z)) <= 0f) { sum += grid.h[k]; n++; }
                    level = n > 0 ? Mathf.Round(sum / n * 20f) / 20f : 0f;
                }
                levels[p] = level;
                for (int k = 0; k < verts.Length; k++)
                {
                    float d = PadDistance(Pads[p], new Vector2(verts[k].x, verts[k].z));
                    if (d >= PadBlend) continue;
                    float w = 1f - Mathf.SmoothStep(0f, 1f, d / PadBlend);
                    grid.h[k] = Mathf.Lerp(grid.h[k], level, w);
                }
            }
            return levels;
        }

        static List<Path> BuildPaths(Grid grid)
        {
            var paths = new List<Path>();
            foreach (var lr in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
            {
                if (lr.name != "DepositRing") continue;
                var ring = lr.transform;
                string name = ring.parent != null ? ring.parent.name : ring.name;
                if (!LaneDefs.TryGetValue(name, out var def)) def = new LaneDef();
                var path = new Path { name = name, def = def, target = new Vector2(ring.position.x, ring.position.z) };

                var ctrl = new List<Vector2>();
                Vector2 firstAim = def.vias.Length > 0 ? def.vias[0] : path.target;
                ctrl.Add(HubCenter + (firstAim - HubCenter).normalized * HubStartRadius);
                ctrl.AddRange(def.vias);
                Vector2 lastFrom = ctrl[ctrl.Count - 1];
                ctrl.Add(path.target - (path.target - lastFrom).normalized * RingClearance);

                // Catmull-Rom through the control points.
                for (int i = 0; i < ctrl.Count - 1; i++)
                {
                    Vector2 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, ctrl.Count - 1)];
                    int steps = Mathf.Max(4, Mathf.CeilToInt(Vector2.Distance(p1, p2) / 0.25f));
                    for (int s = (i == 0 ? 0 : 1); s <= steps; s++)
                    {
                        float t = s / (float)steps, t2 = t * t, t3 = t2 * t;
                        Vector2 p = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                        path.cum.Add(path.pts.Count == 0 ? 0f : path.cum[path.cum.Count - 1] + Vector2.Distance(path.pts[path.pts.Count - 1], p));
                        path.pts.Add(p);
                    }
                }

                // Smoothed height profile, capped under ceilings (tunnels).
                var raw = new float[path.pts.Count];
                for (int i = 0; i < raw.Length; i++) raw[i] = Mathf.Min(grid.Sample(path.pts[i].x, path.pts[i].y), CeilingAt(def, path.pts[i]));
                float sigma = ProfileSmooth * 0.5f;
                for (int i = 0; i < raw.Length; i++)
                {
                    float sum = 0f, wsum = 0f;
                    for (int j = 0; j < raw.Length; j++)
                    {
                        float d = path.cum[j] - path.cum[i];
                        if (Mathf.Abs(d) > ProfileSmooth) continue;
                        float w = Mathf.Exp(-(d * d) / (2f * sigma * sigma));
                        sum += raw[j] * w; wsum += w;
                    }
                    path.prof.Add(Mathf.Min(sum / wsum, CeilingAt(def, path.pts[i])));
                }

                float pad = def.halfWidth + def.shoulder + 0.5f;
                float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
                foreach (var p in path.pts) { minX = Mathf.Min(minX, p.x); minZ = Mathf.Min(minZ, p.y); maxX = Mathf.Max(maxX, p.x); maxZ = Mathf.Max(maxZ, p.y); }
                path.bounds = Rect.MinMaxRect(minX - pad, minZ - pad, maxX + pad, maxZ + pad);
                paths.Add(path);
            }
            paths.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return paths;
        }

        static float CeilingAt(LaneDef def, Vector2 p)
        {
            float cap = float.MaxValue;
            foreach (var c in def.ceilings)
                if (Vector2.Distance(p, c.at) <= c.radius) cap = Mathf.Min(cap, c.maxY);
            return cap;
        }

static void EnforceCeilings(Grid grid, Vector3[] verts, List<Path> paths)
        {
            foreach (var path in paths)
            {
                if (path.def.ceilings.Length == 0) continue;
                float core = path.def.halfWidth * 0.75f;
                for (int k = 0; k < verts.Length; k++)
                {
                    var p = new Vector2(verts[k].x, verts[k].z);
                    float cap = CeilingAt(path.def, p);
                    if (cap == float.MaxValue || grid.h[k] <= cap) continue;
                    float d = path.Nearest(p, out _, out _, out _);
                    if (d > core + 0.35f) continue;
                    float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(core, core + 0.35f, d));
                    grid.h[k] = Mathf.Lerp(grid.h[k], cap, w);
                }
            }
        }


        static void CarveLanes(Grid grid, Vector3[] verts, List<Path> paths)
        {
            var orig = (float[])grid.h.Clone();
            for (int k = 0; k < verts.Length; k++)
            {
                var p = new Vector2(verts[k].x, verts[k].z);
                float bestW = 0f, bestH = 0f;
                foreach (var path in paths)
                {
                    if (!path.bounds.Contains(p)) continue;
                    float d = path.Nearest(p, out _, out float laneH, out _);
                    float hw = path.def.halfWidth;
                    float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hw, hw + path.def.shoulder, d));
                    if (w > bestW)
                    {
                        float core = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, hw, d));
                        bestW = w; bestH = laneH - LaneSink * core;
                    }
                }
                if (bestW > 0f) grid.h[k] = Mathf.Lerp(orig[k], bestH, bestW);
            }
        }

        /// <summary>Footprints (xz rects) of every renderer of a hand-placed root that rests on the ground.</summary>
/// <summary>Footprints (convex hull of the mesh, in xz) of every renderer of a hand-placed root that rests on the ground.</summary>
/// <summary>Footprints (convex hull of the mesh, in xz) of every renderer of a hand-placed root that rests on the ground, with its base height.</summary>
        static List<Footprint> CollectProtected(UnityEngine.SceneManagement.Scene scene, MeshCollider col)
        {
            var list = new List<Footprint>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (NotProtected.Contains(root.name) || !root.activeInHierarchy) continue;
                foreach (var r in root.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;
                    var b = r.bounds;
                    float g = GroundY(col, b.center);
                    if (float.IsNaN(g) || b.min.y > g + 0.45f) continue; // floating (raised tunnels, dropship)
                    var pts = new List<Vector2>();
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                    {
                        var m = r.transform.localToWorldMatrix;
                        foreach (var v in mf.sharedMesh.vertices) { var w = m.MultiplyPoint3x4(v); pts.Add(new Vector2(w.x, w.z)); }
                    }
                    if (pts.Count < 3)
                    {
                        pts.Clear();
                        pts.Add(new Vector2(b.min.x, b.min.z)); pts.Add(new Vector2(b.max.x, b.min.z));
                        pts.Add(new Vector2(b.max.x, b.max.z)); pts.Add(new Vector2(b.min.x, b.max.z));
                    }
                    var hull = ConvexHull(pts);
                    if (hull.Length < 3) continue;
                    var fp = new Footprint { hull = hull, level = b.min.y - 0.02f };
                    fp.box = Rect.MinMaxRect(b.min.x, b.min.z, b.max.x, b.max.z);
                    list.Add(fp);
                }
            }
            return list;
        }

        class Footprint { public Vector2[] hull; public Rect box; public float level; }

        static Vector2[] ConvexHull(List<Vector2> pts)
        {
            pts.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            var h = new Vector2[pts.Count * 2];
            int k = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                while (k >= 2 && Cross(h[k - 2], h[k - 1], pts[i]) <= 0f) k--;
                h[k++] = pts[i];
            }
            for (int i = pts.Count - 2, t = k + 1; i >= 0; i--)
            {
                while (k >= t && Cross(h[k - 2], h[k - 1], pts[i]) <= 0f) k--;
                h[k++] = pts[i];
            }
            var res = new Vector2[Mathf.Max(k - 1, 0)];
            System.Array.Copy(h, res, res.Length);
            return res;
        }

        static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

/// <summary>How strongly (0..1) the ground at (x, z) is pulled to a building's base, and to which height.</summary>
        static float ProtectWeight(List<Footprint> fps, float x, float z, out float level)
        {
            var p = new Vector2(x, z);
            float bestW = 0f, bestD = float.MaxValue; level = 0f;
            float reach = ProtectMargin + ProtectBlend;
            foreach (var fp in fps)
            {
                if (x < fp.box.xMin - reach || x > fp.box.xMax + reach || z < fp.box.yMin - reach || z > fp.box.yMax + reach) continue;
                bool inside = true;
                float edge = float.MaxValue;
                var hull = fp.hull;
                for (int i = 0; i < hull.Length; i++)
                {
                    Vector2 a = hull[i], b = hull[(i + 1) % hull.Length];
                    if (Cross(a, b, p) < 0f) inside = false;
                    Vector2 ab = b - a;
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(ab.sqrMagnitude, 1e-8f));
                    edge = Mathf.Min(edge, Vector2.Distance(p, a + ab * t));
                }
                float d = inside ? 0f : edge;
                float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - ProtectMargin) / ProtectBlend));
                if (w <= 0f) continue;
                // Highest weight wins; then the footprint the point is closest to (neighbours' margins overlap);
                // only where the point is inside several footprints (a crate on a pad) does the lowest base win.
                bool better = w > bestW + 1e-3f
                    || (Mathf.Abs(w - bestW) <= 1e-3f && (d < bestD - 1e-3f || (Mathf.Abs(d - bestD) <= 1e-3f && fp.level < level)));
                if (better) { bestW = w; bestD = d; level = fp.level; }
            }
            return bestW;
        }

        static Texture2D PaintTexture(Mesh src, List<Path> paths)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(SourceTex));
            int size = tex.width;
            var px = tex.GetPixels32();
            var b = src.bounds;
            float minX = b.min.x, minZ = b.min.z, world = b.size.x;

            // Pads: compacted, slightly darker regolith.
            foreach (var pad in Pads)
            {
                float reach = Mathf.Max(pad.halfSize.x, pad.halfSize.y) * 1.5f + 1f;
                ForPixels(size, minX, minZ, world, Rect.MinMaxRect(pad.center.x - reach, pad.center.y - reach, pad.center.x + reach, pad.center.y + reach), (k, x, z) =>
                {
                    float d = PadDistance(pad, new Vector2(x, z));
                    if (d > 0.6f) return;
                    float w = 1f - Mathf.SmoothStep(0f, 1f, d / 0.6f);
                    Mul(px, k, Mathf.Lerp(1f, PadTint, w));
                });
            }

            foreach (var path in paths)
            {
                float L = path.Length, hw = path.def.halfWidth;
                ForPixels(size, minX, minZ, world, path.bounds, (k, x, z) =>
                {
                    float d = path.Nearest(new Vector2(x, z), out float s, out _, out _);
                    if (d > hw + 0.4f) return;
                    float ends = Mathf.Clamp01(Mathf.Min(s, L - s) / 0.8f);
                    float lane = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(hw - 0.2f, hw + 0.4f, d));
                    float mul = Mathf.Lerp(1f, LaneTint, lane);
                    float track = 1f - Mathf.Clamp01((Mathf.Abs(d - TrackOffset) - TrackWidth * 0.5f) / 0.06f);
                    mul *= Mathf.Lerp(1f, TrackTint, track * ends);
                    Mul(px, k, mul);
                });
            }

            tex.SetPixels32(px);
            File.WriteAllBytes(OutTex, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(OutTex, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(OutTex);
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 8;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(OutTex);
        }

        static void ForPixels(int size, float minX, float minZ, float world, Rect area, System.Action<int, float, float> fn)
        {
            int i0 = Mathf.Clamp(Mathf.FloorToInt((area.xMin - minX) / world * size), 0, size - 1);
            int i1 = Mathf.Clamp(Mathf.CeilToInt((area.xMax - minX) / world * size), 0, size - 1);
            int j0 = Mathf.Clamp(Mathf.FloorToInt((area.yMin - minZ) / world * size), 0, size - 1);
            int j1 = Mathf.Clamp(Mathf.CeilToInt((area.yMax - minZ) / world * size), 0, size - 1);
            for (int j = j0; j <= j1; j++)
                for (int i = i0; i <= i1; i++)
                    fn(j * size + i, minX + (i + 0.5f) / size * world, minZ + (j + 0.5f) / size * world);
        }

        static void Mul(Color32[] px, int k, float mul)
        {
            var c = px[k];
            px[k] = new Color32((byte)(c.r * mul), (byte)(c.g * mul), (byte)(c.b * mul), c.a);
        }

        static Mesh SaveMesh(Mesh mesh)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(OutMesh);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, OutMesh); return mesh; }
            existing.Clear();
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static float GroundY(MeshCollider col, Vector3 p)
        {
            var ray = new Ray(new Vector3(p.x, 60f, p.z), Vector3.down);
            return col.Raycast(ray, out var hit, 200f) ? hit.point.y : float.NaN;
        }

        static void BuildRoutes(Transform env, List<Path> paths, MeshCollider col)
        {
            var old = env.Find("RoverRoutes");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            var root = new GameObject("RoverRoutes");
            Undo.RegisterCreatedObjectUndo(root, "Sculpt HandMadeBase Terrain");
            root.transform.SetParent(env, false);

            foreach (var path in paths)
            {
                var go = new GameObject("Route_" + path.name);
                go.transform.SetParent(root.transform, false);
                var wp = go.AddComponent<WaypointPath>();

                var outward = new List<Transform>();
                int count = Mathf.Max(2, Mathf.CeilToInt(path.Length / RouteNodeSpacing) + 1);
                for (int n = 0; n < count; n++)
                {
                    Vector2 p = PointAt(path, path.Length * n / (count - 1));
                    float y = GroundY(col, new Vector3(p.x, 0f, p.y));
                    var node = new GameObject($"P{n:00}").transform;
                    node.SetParent(go.transform, false);
                    node.position = new Vector3(p.x, float.IsNaN(y) ? 0f : y, p.y);
                    outward.Add(node);
                }

                var nodes = new List<WaypointPath.Node>();
                for (int n = 0; n < outward.Count; n++)
                    nodes.Add(new WaypointPath.Node { point = outward[n], stopSeconds = n == 0 ? 1f : n == outward.Count - 1 ? 2f : 0f });
                for (int n = outward.Count - 2; n >= 1; n--)
                    nodes.Add(new WaypointPath.Node { point = outward[n], stopSeconds = 0f });
                wp.SetNodes(nodes);
                EditorUtility.SetDirty(wp);
            }
        }

        static Vector2 PointAt(Path path, float s)
        {
            for (int i = 0; i < path.cum.Count - 1; i++)
                if (s <= path.cum[i + 1])
                {
                    float seg = path.cum[i + 1] - path.cum[i];
                    return Vector2.Lerp(path.pts[i], path.pts[i + 1], seg > 1e-6f ? (s - path.cum[i]) / seg : 0f);
                }
            return path.pts[path.pts.Count - 1];
        }
    }
}
