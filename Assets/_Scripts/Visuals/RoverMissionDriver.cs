using System.Collections.Generic;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Drives the rover out along its hot spot's RoverRoutes path while OnMission and parks it at home
    /// whenever it is not on a mission (Ready, Broken, LockedByRadiation). Shows cargo or a broken alert over it.
    /// Replaces the decorative RoverDriver loop.
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
        private float alertTimer;
        private ParticleSystem[] dustSystems;
        private bool homeCaptured;

        public bool IsDriving => nextWaypoint < waypoints.Count;
        private bool CanDrive => rover == null || rover.Status == RoverStatus.OnMission;

        private void Awake()
        {
            CaptureHomeIfNeeded();
            EnsureDustSystems();
            SetDust(false, clear: true);
        }

        private void Start()
        {
            if (!CanDrive) ParkHome();
            else if (!IsDriving) SetDust(false, clear: true);
        }

        private void OnEnable()
        {
            CaptureHomeIfNeeded();
            if (rover != null)
            {
                rover.RoverDispatched += OnDispatched;
                rover.RoverReturned += OnReturned;
                rover.RoverStatusChanged += OnStatusChanged;
                rover.ComponentBroken += OnComponentBroken;
                rover.ComponentRepaired += OnRepaired;
            }

            if (!CanDrive) ParkHome();
            else SetDust(IsDriving, clear: true);
        }

        private void OnDisable()
        {
            if (rover != null)
            {
                rover.RoverDispatched -= OnDispatched;
                rover.RoverReturned -= OnReturned;
                rover.RoverStatusChanged -= OnStatusChanged;
                rover.ComponentBroken -= OnComponentBroken;
                rover.ComponentRepaired -= OnRepaired;
            }
            SetDust(false, clear: true);
        }

        private void CaptureHomeIfNeeded()
        {
            if (homeCaptured) return;
            homePosition = transform.position;
            homeRotation = transform.rotation;
            homeCaptured = true;
        }

        private void ParkHome()
        {
            CaptureHomeIfNeeded();
            waypoints.Clear();
            nextWaypoint = 0;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            SetDust(false, clear: true);
        }

        private void OnDispatched(string siteId)
        {
            if (!CanDrive)
            {
                ParkHome();
                return;
            }
            BuildRoute(siteId);
        }

        private void OnReturned(string siteId, int water, int parts, bool broken)
        {
            ParkHome();
            alertTimer = 0f;
            if (!broken && (water > 0 || parts > 0)) EmoteBubble.Show(transform, EmoteKind.Happy, 3f, 1.4f);
        }

        private void OnStatusChanged(RoverStatus status)
        {
            if (status != RoverStatus.OnMission)
                ParkHome();
        }

        private void OnComponentBroken(BaseComponent component)
        {
            if (component != BaseComponent.Rover) return;
            ParkHome();
            alertTimer = 0f;
        }

        private void OnRepaired(BaseComponent component)
        {
            if (component != BaseComponent.Rover) return;
            ParkHome();
            EmoteBubble.Show(transform, EmoteKind.Happy, 2f, 1.4f);
        }

        private void BuildRoute(string siteId)
        {
            CaptureHomeIfNeeded();
            waypoints.Clear();
            nextWaypoint = 0;
            WaypointPath path = FindRoute(siteId);
            Vector3 target = FindTarget(siteId);

            Vector3 last = transform.position;
            if (path != null)
            {
                int outwardCount = GetOutwardPointCount(path);
                for (int i = 0; i < outwardCount; i++)
                {
                    Vector3 pt = path.GetPoint(i);
                    if ((pt - last).sqrMagnitude <= 0.0001f) continue;
                    waypoints.Add(pt);
                    last = pt;
                }
            }
            if ((target != Vector3.zero || path == null) && (target - last).sqrMagnitude > 0.0001f)
            {
                waypoints.Add(target);
            }
        }

        private static int GetOutwardPointCount(WaypointPath path)
        {
            int count = path.Count;
            if (count <= 2) return count;
            int mid = count / 2;
            if (count % 2 == 0 && (path.GetPoint(mid - 1) - path.GetPoint(mid + 1)).sqrMagnitude < 0.0001f)
                return mid + 1;
            return count;
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

        private void Update() => Step(Time.deltaTime);

        public void Step(float dt)
        {
            if (dt < 0f) dt = 0f;
            if (!CanDrive)
            {
                if (IsDriving
                    || (transform.position - homePosition).sqrMagnitude > 0.0001f
                    || Quaternion.Angle(transform.rotation, homeRotation) > 0.01f)
                {
                    ParkHome();
                }
                else
                {
                    SetDust(false, clear: true);
                }
            }
            else if (IsDriving)
            {
                Vector3 target = waypoints[nextWaypoint];
                Vector3 to = target - transform.position;
                Vector3 flat = new Vector3(to.x, 0f, to.z);
                float step = (config != null ? config.roverDriveSpeed : 3f) * dt;
                if (to.magnitude <= step)
                {
                    transform.position = target;
                    nextWaypoint++;
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
                SetDust(IsDriving, clear: false);
            }
            else
            {
                SetDust(false, clear: false);
            }

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

        private void EnsureDustSystems()
        {
            if (dustSystems != null && dustSystems.Length > 0) return;
            if (wheelDust == null)
            {
                dustSystems = System.Array.Empty<ParticleSystem>();
                return;
            }

            Transform container = wheelDust.transform.parent != null && wheelDust.transform.parent != transform
                ? wheelDust.transform.parent
                : wheelDust.transform;
            dustSystems = container.GetComponentsInChildren<ParticleSystem>(true);
        }

        private void SetDust(bool on, bool clear = true)
        {
            EnsureDustSystems();
            for (int i = 0; i < dustSystems.Length; i++)
            {
                ParticleSystem ps = dustSystems[i];
                if (ps == null) continue;
                var emission = ps.emission;
                if (emission.enabled != on) emission.enabled = on;
                if (on)
                {
                    if (!ps.isPlaying) ps.Play(false);
                }
                else if (ps.isEmitting || (clear && (ps.isPlaying || ps.particleCount > 0)))
                {
                    ps.Stop(false, clear ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }
    }
}
