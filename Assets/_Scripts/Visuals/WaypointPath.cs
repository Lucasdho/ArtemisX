using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>A closed loop of waypoints. Walkers use the straight segments; the rover uses the smoothed samples.</summary>
    public class WaypointPath : MonoBehaviour
    {
        [Serializable]
        public struct Node
        {
            public Transform point;
            [Tooltip("Rover: stop time at this node. Walker: time spent here (indoors when Enter Building is on).")]
            [Min(0f)] public float stopSeconds;
            [Tooltip("Walkers only: the character goes inside here (disappears) for Stop Seconds, then comes back out.")]
            public bool enterBuilding;
        }

        [SerializeField] private List<Node> nodes = new List<Node>();
        [SerializeField] private Color gizmoColor = new Color(0.3f, 0.9f, 1f, 0.9f);

        public int Count => nodes.Count;

        public Vector3 GetPoint(int index) => nodes[Wrap(index)].point.position;

        public float GetStopSeconds(int index) => nodes[Wrap(index)].stopSeconds;

        public bool EntersBuilding(int index) => nodes[Wrap(index)].enterBuilding;

        public void SetNodes(List<Node> newNodes) => nodes = newNodes;

        public int Wrap(int index)
        {
            int n = nodes.Count;
            return ((index % n) + n) % n;
        }

        public int NearestIndex(Vector3 position)
        {
            int best = 0;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < nodes.Count; i++)
            {
                float sqr = (nodes[i].point.position - position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = i; }
            }
            return best;
        }

        /// <summary>Samples a closed Catmull-Rom curve through the nodes. Node i sits at sample i * samplesPerSegment.</summary>
        public void BuildSmoothLoop(int samplesPerSegment, List<Vector3> samples, List<float> distances)
        {
            samples.Clear();
            distances.Clear();
            int n = nodes.Count;
            float total = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 p0 = GetPoint(i - 1), p1 = GetPoint(i), p2 = GetPoint(i + 1), p3 = GetPoint(i + 2);
                for (int s = 0; s < samplesPerSegment; s++)
                {
                    Vector3 p = CatmullRom(p0, p1, p2, p3, s / (float)samplesPerSegment);
                    if (samples.Count > 0) total += Vector3.Distance(samples[samples.Count - 1], p);
                    samples.Add(p);
                    distances.Add(total);
                }
            }
            // Close the loop: the last entry repeats the first point at the full loop length.
            total += Vector3.Distance(samples[samples.Count - 1], samples[0]);
            samples.Add(samples[0]);
            distances.Add(total);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        private void OnDrawGizmos()
        {
            if (nodes == null || nodes.Count < 2) return;
            Gizmos.color = gizmoColor;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].point == null || nodes[Wrap(i + 1)].point == null) return;
                Gizmos.DrawLine(GetPoint(i), GetPoint(i + 1));
                Gizmos.color = nodes[i].enterBuilding ? new Color(1f, 0.6f, 0.1f, 0.9f) : gizmoColor;
                Gizmos.DrawSphere(GetPoint(i), nodes[i].stopSeconds > 0f ? 0.12f : 0.06f);
                Gizmos.color = gizmoColor;
            }
        }
    }
}
