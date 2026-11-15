using System.Collections;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Periodically launches the dropship to orbit the base, then returns it to its parked hover pose.</summary>
    public class DropshipPatrol : MonoBehaviour
    {
        [Header("Orbit")]
        [Tooltip("Optional center to circle around. Uses Base Center Fallback when empty.")]
        [SerializeField] private Transform orbitCenter;
        [SerializeField] private Vector3 baseCenterFallback = new Vector3(-1.5f, 0f, 0.5f);
        [Min(2f)] [SerializeField] private float orbitRadius = 10.5f;
        [Min(1f)] [SerializeField] private float cruiseHeight = 7.5f;
        [Min(0.5f)] [SerializeField] private float flySpeed = 5.5f;
        [SerializeField] private bool clockwise = true;

        [Header("Timing & Feel")]
        [SerializeField] private Vector2 waitRangeSeconds = new Vector2(10f, 24f);
        [Min(0.2f)] [SerializeField] private float takeoffSeconds = 2.4f;
        [Min(2f)] [SerializeField] private float orbitSeconds = 16f;
        [Range(0f, 45f)] [SerializeField] private float bankDegrees = 18f;
        [Min(1f)] [SerializeField] private float turnSharpness = 4f;
        [Min(0f)] [SerializeField] private float idleBobHeight = 0.12f;
        [Min(0.1f)] [SerializeField] private float idleBobSpeed = 1.4f;

        private Vector3 homePos;
        private Quaternion homeRot;
        private Coroutine patrolRoutine;

        private void Awake()
        {
            homePos = transform.position;
            homeRot = transform.rotation;
        }

        private void OnEnable()
        {
            patrolRoutine = StartCoroutine(PatrolLoop());
        }

        private void OnDisable()
        {
            if (patrolRoutine != null)
            {
                StopCoroutine(patrolRoutine);
                patrolRoutine = null;
            }
        }

        private IEnumerator PatrolLoop()
        {
            while (enabled)
            {
                yield return IdleHover(Random.Range(waitRangeSeconds.x, waitRangeSeconds.y));

                Vector3 center = CenterPoint();
                Vector3 toHome = homePos - center;
                toHome.y = 0f;
                float angle = toHome.sqrMagnitude > 0.01f ? Mathf.Atan2(toHome.z, toHome.x) : 0f;

                // 1. Lift off from hover pose onto the orbit ring
                yield return MoveToPoint(OrbitPoint(center, angle), takeoffSeconds, 0.35f);

                // 2. Orbit smoothly around the base, banking into the turn
                float dir = clockwise ? -1f : 1f;
                float angularSpeed = (flySpeed / Mathf.Max(1f, orbitRadius)) * dir;
                float bank = clockwise ? -bankDegrees : bankDegrees;
                float elapsed = 0f;

                while (elapsed < orbitSeconds)
                {
                    float dt = Time.deltaTime;
                    elapsed += dt;
                    angle += angularSpeed * dt;

                    Vector3 nextPos = OrbitPoint(center, angle);
                    Vector3 velocity = (nextPos - transform.position) / Mathf.Max(dt, 0.0001f);
                    transform.position = nextPos;
                    SteerAlong(velocity, bank, dt);
                    yield return null;
                }

                // 3. Return to the parked hover position
                float returnDuration = Mathf.Max(1.5f, Vector3.Distance(transform.position, homePos) / Mathf.Max(0.5f, flySpeed));
                yield return MoveToPoint(homePos, returnDuration, 0.25f);

                // 4. Settle cleanly back to initial orientation
                float settle = 0f;
                Quaternion fromRot = transform.rotation;
                while (settle < 1f)
                {
                    settle += Time.deltaTime / 1.2f;
                    transform.rotation = Quaternion.Slerp(fromRot, homeRot, Mathf.SmoothStep(0f, 1f, settle));
                    yield return null;
                }
                transform.SetPositionAndRotation(homePos, homeRot);
            }
        }

        private IEnumerator IdleHover(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                float bob = Mathf.Sin(elapsed * idleBobSpeed) * idleBobHeight;
                transform.position = homePos + Vector3.up * bob;
                yield return null;
            }
        }

        private IEnumerator MoveToPoint(Vector3 target, float duration, float bankScale)
        {
            Vector3 start = transform.position;
            float t = 0f;
            duration = Mathf.Max(0.2f, duration);
            float bank = (clockwise ? -bankDegrees : bankDegrees) * bankScale;

            while (t < 1f)
            {
                float dt = Time.deltaTime;
                t += dt / duration;
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 nextPos = Vector3.Lerp(start, target, eased);
                Vector3 velocity = (nextPos - transform.position) / Mathf.Max(dt, 0.0001f);
                transform.position = nextPos;
                SteerAlong(velocity, bank, dt);
                yield return null;
            }
        }

        private void SteerAlong(Vector3 velocity, float bank, float dt)
        {
            if (velocity.sqrMagnitude < 0.0001f) return;
            Quaternion look = Quaternion.LookRotation(velocity.normalized, Vector3.up);
            Quaternion rolled = look * Quaternion.Euler(0f, 0f, bank);
            transform.rotation = Quaternion.Slerp(transform.rotation, rolled, turnSharpness * dt);
        }

        private Vector3 CenterPoint() => orbitCenter != null ? orbitCenter.position : baseCenterFallback;

        private Vector3 OrbitPoint(Vector3 center, float angleRad)
        {
            return new Vector3(
                center.x + Mathf.Cos(angleRad) * orbitRadius,
                center.y + cruiseHeight,
                center.z + Mathf.Sin(angleRad) * orbitRadius
            );
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = CenterPoint();
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.7f);
            const int segs = 32;
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * Mathf.PI * 2f / segs;
                float a1 = (i + 1) * Mathf.PI * 2f / segs;
                Gizmos.DrawLine(OrbitPoint(center, a0), OrbitPoint(center, a1));
            }
        }
    }
}
