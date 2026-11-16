using System.Collections;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Keeps the dropship flying in a wide, continuous orbit around the base.</summary>
    public class DropshipPatrol : MonoBehaviour
    {
        [Header("Orbit")]
        [Tooltip("Optional center to circle around. Uses Base Center Fallback when empty.")]
        [SerializeField] private Transform orbitCenter;
        [SerializeField] private Vector3 baseCenterFallback = new Vector3(-1.5f, 0f, 0.5f);
        [Min(2f)] [SerializeField] private float orbitRadius = 17f;
        [Min(1f)] [SerializeField] private float cruiseHeight = 9f;
        [Min(0.5f)] [SerializeField] private float flySpeed = 6f;
        [SerializeField] private bool clockwise = true;

        [Header("Feel")]
        [Min(0.2f)] [SerializeField] private float takeoffSeconds = 2.4f;
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
            // Orbit the base all the time, starting from wherever the dropship is placed in the scene.
            Vector3 center = CenterPoint();
            Vector3 fromCenter = transform.position - center;
            fromCenter.y = 0f;
            float angle = fromCenter.sqrMagnitude > 0.01f ? Mathf.Atan2(fromCenter.z, fromCenter.x) : 0f;

            // Ease onto the orbit ring first so the start has no snap.
            yield return MoveToPoint(OrbitPoint(center, angle), takeoffSeconds, 0.35f);

            float dir = clockwise ? -1f : 1f;
            float bank = clockwise ? -bankDegrees : bankDegrees;
            while (true)
            {
                float dt = Time.deltaTime;
                angle += (flySpeed / Mathf.Max(1f, orbitRadius)) * dir * dt;

                Vector3 nextPos = OrbitPoint(center, angle);
                nextPos.y += Mathf.Sin(Time.time * idleBobSpeed) * idleBobHeight;
                Vector3 velocity = (nextPos - transform.position) / Mathf.Max(dt, 0.0001f);
                transform.position = nextPos;
                SteerAlong(velocity, bank, dt);
                yield return null;
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
