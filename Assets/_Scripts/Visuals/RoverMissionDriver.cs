using System.Collections.Generic;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Drives the rover out along its hot spot's RoverRoutes path when dispatched and back home when it returns
    /// next morning. Shows cargo or a broken alert over it. Replaces the decorative RoverDriver loop.
    /// </summary>
    public class RoverMissionDriver : MonoBehaviour
    {
        [SerializeField] private RoverExpeditionManager rover;
        [SerializeField] private GameplayVisualsConfig config;
        [Tooltip("Paths named Route_<siteId>, from the base outwards.")]
        [SerializeField] private WaypointPath[] routes = new WaypointPath[0];
        [SerializeField] private ResourceHotspot[] hotspots = new ResourceHotspot[0];
        [SerializeField] private ParticleSystem wheelDust;

        private readonly List<Vector3> waypoints = new List<Vector3>();
        private int nextWaypoint;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private bool parkAtEnd;
        private float alertTimer;

        public bool IsDriving => nextWaypoint < waypoints.Count;

        private void Awake()
        {
            homePosition = transform.position;
            homeRotation = transform.rotation;
            SetDust(false);
        }

        private void OnEnable()
        {
            if (rover == null) return;
            rover.RoverDispatched += OnDispatched;
            rover.RoverReturned += OnReturned;
            rover.ComponentRepaired += OnRepaired;
        }

        private void OnDisable()
        {
            if (rover == null) return;
            rover.RoverDispatched -= OnDispatched;
            rover.RoverReturned -= OnReturned;
            rover.ComponentRepaired -= OnRepaired;
        }

        private void OnDispatched(string siteId)
        {
            BuildRoute(siteId, outbound: true);
            parkAtEnd = false;
        }

        private void OnReturned(string siteId, int water, int parts, bool broken)
        {
            // The rover spent the night at the site: start the drive home from there.
            BuildRoute(siteId, outbound: false);
            if (waypoints.Count > 0) transform.position = waypoints[0];
            nextWaypoint = Mathf.Min(1, waypoints.Count);
            parkAtEnd = true;
            alertTimer = 0f;
            if (!broken && (water > 0 || parts > 0)) EmoteBubble.Show(transform, EmoteKind.Happy, 3f, 1.4f);
        }

        private void OnRepaired(BaseComponent component)
        {
            if (component == BaseComponent.Rover) EmoteBubble.Show(transform, EmoteKind.Happy, 2f, 1.4f);
        }

        private void BuildRoute(string siteId, bool outbound)
        {
            waypoints.Clear();
            nextWaypoint = 0;
            WaypointPath path = FindRoute(siteId);
            Vector3 target = FindTarget(siteId);

            var points = new List<Vector3> { homePosition };
            if (path != null)
                for (int i = 0; i < path.Count; i++) points.Add(path.GetPoint(i));
            if (target != Vector3.zero || path == null) points.Add(target);

            if (!outbound) points.Reverse();
            if (outbound) waypoints.Add(transform.position);
            waypoints.AddRange(points);
        }

        private WaypointPath FindRoute(string siteId)
        {
            foreach (var r in routes)
                if (r != null && r.name == "Route_" + siteId) return r;
            return null;
        }

        private Vector3 FindTarget(string siteId)
        {
            foreach (var h in hotspots)
            {
                if (h == null || h.hotspotId != siteId) continue;
                if (h.roverStopPoint != null) return h.roverStopPoint.position;
                var child = h.transform.Find("RoverTarget");
                return child != null ? child.position : h.transform.position;
            }
            return Vector3.zero;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (IsDriving)
            {
                Vector3 target = waypoints[nextWaypoint];
                Vector3 to = target - transform.position;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                float step = (config != null ? config.roverDriveSpeed : 3f) * dt;
                if (to.magnitude <= step)
                {
                    transform.position = target;
                    nextWaypoint++;
                    if (!IsDriving && parkAtEnd)
                    {
                        transform.rotation = homeRotation;
                    }
                }
                else
                {
                    transform.position += to.normalized * step;
                    if (flat.sqrMagnitude > 0.0001f)
                    {
                        float sharp = config != null ? config.roverTurnSharpness : 8f;
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat, Vector3.up), 1f - Mathf.Exp(-sharp * dt));
                    }
                }
            }
            SetDust(IsDriving);

            if (rover != null && rover.Status == RoverStatus.Broken && !IsDriving)
            {
                alertTimer -= dt;
                if (alertTimer <= 0f)
                {
                    alertTimer = config != null ? config.brokenAlertInterval : 3f;
                    EmoteBubble.Show(transform, EmoteKind.Alert, alertTimer * 0.8f, 1.4f);
                }
            }
        }

        private void SetDust(bool on)
        {
            if (wheelDust == null) return;
            var emission = wheelDust.emission;
            if (emission.enabled != on) emission.enabled = on;
        }
    }
}
