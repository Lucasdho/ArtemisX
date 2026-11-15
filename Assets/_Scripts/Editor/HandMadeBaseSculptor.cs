using System.Collections.Generic;
using System.IO;
using ArtemisX.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtemisX.EditorTools
{
    /// <summary>
    /// Sculpts the HandMadeBase scene's ground: extra relief where the base goes, and rover paths
    /// from the base out to every DepositRing (smoothed lane in the mesh + tyre tracks in the texture).
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

        // ---------- Base relief ----------
        static readonly Vector2 BaseCenter = Vector2.zero;
        const float ReliefInner = 8f;      // full strength inside this radius
        const float ReliefOuter = 15f;     // fades to nothing here
        const float ReliefAmp = 1.5f, ReliefScale = 0.11f;
        const float ReliefDetailAmp = 0.45f, ReliefDetailScale = 0.28f;
        const int ReliefSeed = 3;
        // Hand-placed features: x, z, radius, height (negative = dip).
        static readonly Vector4[] Features =
        {
            new Vector4(-4.5f, 4.5f, 4.5f, 1.1f),   // low rise to the north-west
            new Vector4(3.5f, 6.5f, 3f, 0.6f),      // small shoulder to the north
            new Vector4(5f, -3.5f, 3.5f, -0.6f),    // shallow dip to the south-east
        };

        // ---------- Paths ----------
        const float PathStartRadius = 3.5f;  // lanes start this far from the base centre
        const float RingClearance = 2.2f;    // and stop this far from each deposit centre (ring is ~2 m)
        const float PathBend = 0.12f;        // sideways bow as a fraction of length, alternating sides
        const float LaneHalfWidth = 0.9f;
        const float LaneShoulder = 1.6f;     // blend distance from lane edge back to natural ground
        const float LaneSink = 0.05f;
        const float ProfileSmooth = 3f;      // metres of along-path smoothing (removes bumps and small craters)
        const float LaneTint = 0.93f, TrackTint = 0.8f;
        const float TrackOffset = 0.42f, TrackWidth = 0.16f;
        const float RockClearance = 1.2f;    // scatter rocks closer than this to a lane centre get pushed out
        const float RouteNodeSpacing = 2.5f;

        class Path
        {
            public string name;
            public Vector2 target;
            public readonly List<Vector2> pts = new List<Vector2>();
            public readonly List<float> cum = new List<float>();
            public readonly List<float> prof = new List<float>();
            public Rect bounds;

            public float Length => cum[cum.Count - 1];

            /// <summary>Distance to the polyline, along-path distance and lane height at the nearest point.</summary>
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
            public int row; // vertices per row
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
            GameObject ground = null, env = null, rockScatter = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.Contains("ENVIRONMENT")) env = root;
            }
            if (env == null) { Debug.LogError("[Sculptor] ENVIRONMENT root not found — open HandMadeBase.unity."); return; }
            var gt = env.transform.Find("LunarGround");
            var rt = env.transform.Find("RockScatter");
            if (gt == null) { Debug.LogError("[Sculptor] LunarGround not found."); return; }
            ground = gt.gameObject;
            if (rt != null) rockScatter = rt.gameObject;

            var src = AssetDatabase.LoadAssetAtPath<Mesh>(SourceMesh);
            var verts = src.vertices;
            var grid = BuildGrid(verts);

            // 1) Relief where the base will be.
            for (int k = 0; k < verts.Length; k++)
                grid.h[k] += Relief(verts[k].x, verts[k].z);

            // 2) Paths to every DepositRing.
            var paths = BuildPaths(grid);
            CarveLanes(grid, verts, paths);

            for (int k = 0; k < verts.Length; k++) verts[k].y = grid.h[k];

            // Remember ground height under everything we'll re-seat, before the mesh changes.
            var collider = ground.GetComponent<MeshCollider>();
            var seat = new List<Transform>();
            if (rockScatter != null) foreach (Transform c in rockScatter.transform) seat.Add(c);
            var hotspots = env.transform.Find("ResourceHotspots");
            if (hotspots != null) foreach (Transform hs in hotspots) foreach (Transform c in hs) seat.Add(c);
            var before = new float[seat.Count];
            for (int i = 0; i < seat.Count; i++) before[i] = GroundY(collider, seat[i].position);

            Undo.RecordObjects(seat.ConvertAll(t => (Object)t).ToArray(), "Sculpt HandMadeBase Terrain");
            Undo.RecordObject(ground.GetComponent<MeshFilter>(), "Sculpt HandMadeBase Terrain");
            Undo.RecordObject(collider, "Sculpt HandMadeBase Terrain");
            Undo.RecordObject(ground.GetComponent<MeshRenderer>(), "Sculpt HandMadeBase Terrain");

            // Push scatter rocks off the lanes (hotspot rocks stay put — lanes stop at the ring).
            int pushed = 0;
            if (rockScatter != null)
                foreach (Transform rock in rockScatter.transform)
                {
                    var p = new Vector2(rock.position.x, rock.position.z);
                    foreach (var path in paths)
                    {
                        if (!path.bounds.Contains(p)) continue;
                        float d = path.Nearest(p, out _, out _, out var c);
                        if (d >= RockClearance) continue;
                        Vector2 dir = d > 1e-3f ? (p - c) / d : new Vector2(1f, 0f);
                        p = c + dir * (RockClearance + 0.2f);
                        rock.position = new Vector3(p.x, rock.position.y, p.y);
                        pushed++;
                    }
                }

            // 3) Save mesh, texture and material copies, and swap them in.
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

            // 4) Waypoint routes for rovers, following the lanes (out and back).
            BuildRoutes(env.transform, paths, collider);

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Sculptor] Done: {paths.Count} paths, {pushed} rocks pushed off lanes, {seat.Count} objects re-seated.");
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
            // Pin the base centre to y = 0 so the camera target and anything placed at the origin stay on the ground.
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

        static List<Path> BuildPaths(Grid grid)
        {
            var paths = new List<Path>();
            var rings = new List<Transform>();
            foreach (var lr in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
                if (lr.name == "DepositRing") rings.Add(lr.transform);
            rings.Sort((a, b) => Angle(a.position).CompareTo(Angle(b.position)));

            for (int r = 0; r < rings.Count; r++)
            {
                var ring = rings[r];
                var path = new Path
                {
                    name = ring.parent != null ? ring.parent.name : ring.name,
                    target = new Vector2(ring.position.x, ring.position.z),
                };
                Vector2 dir = (path.target - BaseCenter).normalized;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                Vector2 a = BaseCenter + dir * PathStartRadius;
                Vector2 b = path.target - dir * RingClearance;
                float len = Vector2.Distance(a, b);
                Vector2 m = (a + b) * 0.5f + perp * len * PathBend * ((r & 1) == 0 ? 1f : -1f);

                int steps = Mathf.Max(8, Mathf.CeilToInt(len / 0.25f));
                float total = 0f;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Vector2 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * m + t * t * b;
                    if (i > 0) total += Vector2.Distance(path.pts[i - 1], p);
                    path.pts.Add(p);
                    path.cum.Add(total);
                }

                // Smoothed height profile along the path.
                var raw = new float[path.pts.Count];
                for (int i = 0; i < raw.Length; i++) raw[i] = grid.Sample(path.pts[i].x, path.pts[i].y);
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
                    path.prof.Add(sum / wsum);
                }

                float pad = LaneHalfWidth + LaneShoulder + 0.5f;
                float minX = float.MaxValue, minZ = float.MaxValue, maxX = float.MinValue, maxZ = float.MinValue;
                foreach (var p in path.pts) { minX = Mathf.Min(minX, p.x); minZ = Mathf.Min(minZ, p.y); maxX = Mathf.Max(maxX, p.x); maxZ = Mathf.Max(maxZ, p.y); }
                path.bounds = Rect.MinMaxRect(minX - pad, minZ - pad, maxX + pad, maxZ + pad);
                paths.Add(path);
            }
            return paths;
        }

        static float Angle(Vector3 p) => Mathf.Atan2(p.z - BaseCenter.y, p.x - BaseCenter.x);

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
                    float w = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LaneHalfWidth, LaneHalfWidth + LaneShoulder, d));
                    if (w > bestW)
                    {
                        float core = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, LaneHalfWidth, d));
                        bestW = w; bestH = laneH - LaneSink * core;
                    }
                }
                if (bestW > 0f) grid.h[k] = Mathf.Lerp(orig[k], bestH, bestW);
            }
        }

        static Texture2D PaintTexture(Mesh src, List<Path> paths)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(SourceTex));
            int size = tex.width;
            var px = tex.GetPixels32();
            var b = src.bounds;
            float minX = b.min.x, minZ = b.min.z, world = b.size.x;

            foreach (var path in paths)
            {
                int i0 = Mathf.Clamp(Mathf.FloorToInt((path.bounds.xMin - minX) / world * size), 0, size - 1);
                int i1 = Mathf.Clamp(Mathf.CeilToInt((path.bounds.xMax - minX) / world * size), 0, size - 1);
                int j0 = Mathf.Clamp(Mathf.FloorToInt((path.bounds.yMin - minZ) / world * size), 0, size - 1);
                int j1 = Mathf.Clamp(Mathf.CeilToInt((path.bounds.yMax - minZ) / world * size), 0, size - 1);
                float L = path.Length;
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                    {
                        float x = minX + (i + 0.5f) / size * world, z = minZ + (j + 0.5f) / size * world;
                        float d = path.Nearest(new Vector2(x, z), out float s, out _, out _);
                        if (d > LaneHalfWidth + 0.4f) continue;
                        float ends = Mathf.Clamp01(Mathf.Min(s, L - s) / 0.8f);
                        float lane = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(LaneHalfWidth - 0.2f, LaneHalfWidth + 0.4f, d));
                        float mul = Mathf.Lerp(1f, LaneTint, lane);
                        float track = 1f - Mathf.Clamp01((Mathf.Abs(d - TrackOffset) - TrackWidth * 0.5f) / 0.06f);
                        mul *= Mathf.Lerp(1f, TrackTint, track * ends);
                        int k = j * size + i;
                        var c = px[k];
                        px[k] = new Color32((byte)(c.r * mul), (byte)(c.g * mul), (byte)(c.b * mul), c.a);
                    }
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
                    float s = path.Length * n / (count - 1);
                    Vector2 p = PointAt(path, s);
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
