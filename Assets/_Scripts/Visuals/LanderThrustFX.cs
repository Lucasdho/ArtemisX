using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Modulates lander/dropship thruster plumes, sparks, and pad ground-wash dust based on movement speed and altitude.
    /// </summary>
    public class LanderThrustFX : MonoBehaviour
    {
        [Header("Thrusters")]
        [SerializeField] private ParticleSystem[] vtolThrusters = new ParticleSystem[0];
        [SerializeField] private ParticleSystem[] rearThrusters = new ParticleSystem[0];

        [Header("Ground Wash")]
        [SerializeField] private ParticleSystem groundWashDust;
        [SerializeField] private float groundWashMaxHeight = 4.2f;
        [SerializeField] private float groundWashBaseRate = 16f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Response")]
        [Range(0.1f, 1f)] [SerializeField] private float idleThrustScale = 0.5f;
        [Min(0.1f)] [SerializeField] private float fullHorizontalSpeed = 4.5f;
        [Min(0.1f)] [SerializeField] private float fullVerticalSpeed = 2.0f;
        [Min(1f)] [SerializeField] private float smoothing = 7f;

        private struct ThrusterDefaults
        {
            public ParticleSystem ps;
            public float baseRate;
            public float baseSpeedMin;
            public float baseSpeedMax;
            public float baseSizeMin;
            public float baseSizeMax;
        }

        private ThrusterDefaults[] vtolDefaults;
        private ThrusterDefaults[] rearDefaults;
        private Vector3 lastPosition;
        private float currentVtol;
        private float currentRear;
        private bool hasLastPosition;

        private void Awake()
        {
            vtolDefaults = CaptureDefaults(vtolThrusters);
            rearDefaults = CaptureDefaults(rearThrusters);
            lastPosition = transform.position;
            hasLastPosition = true;
            currentVtol = idleThrustScale;
            currentRear = 0f;
            ApplyScale(vtolDefaults, currentVtol);
            ApplyScale(rearDefaults, currentRear);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            Vector3 pos = transform.position;
            Vector3 velocity = hasLastPosition ? (pos - lastPosition) / dt : Vector3.zero;
            lastPosition = pos;
            hasLastPosition = true;

            float horizSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float vertSpeed = Mathf.Abs(velocity.y);

            float targetVtol = Mathf.Clamp(
                idleThrustScale + (vertSpeed / fullVerticalSpeed) * 0.45f + (horizSpeed / fullHorizontalSpeed) * 0.15f,
                idleThrustScale,
                1.0f);
            float targetRear = Mathf.Clamp01((horizSpeed - 0.25f) / fullHorizontalSpeed);

            float blend = 1f - Mathf.Exp(-smoothing * dt);
            currentVtol = Mathf.Lerp(currentVtol, targetVtol, blend);
            currentRear = Mathf.Lerp(currentRear, targetRear, blend);

            ApplyScale(vtolDefaults, currentVtol);
            ApplyScale(rearDefaults, currentRear);
            UpdateGroundWash(pos);
        }

        private void UpdateGroundWash(Vector3 shipPos)
        {
            if (groundWashDust == null) return;

            var emission = groundWashDust.emission;
            Ray ray = new Ray(shipPos + Vector3.up * 0.2f, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, groundWashMaxHeight + 0.5f, groundMask, QueryTriggerInteraction.Ignore))
            {
                float height = Mathf.Max(0f, hit.distance - 0.2f);
                float proximity = 1f - Mathf.Clamp01(height / groundWashMaxHeight);
                if (proximity > 0.02f)
                {
                    groundWashDust.transform.position = hit.point + Vector3.up * 0.06f;
                    groundWashDust.transform.rotation = Quaternion.LookRotation(Vector3.forward, hit.normal);
                    emission.enabled = true;
                    emission.rateOverTime = new ParticleSystem.MinMaxCurve(groundWashBaseRate * proximity * currentVtol);
                    return;
                }
            }
            emission.enabled = false;
        }

        private static ThrusterDefaults[] CaptureDefaults(ParticleSystem[] systems)
        {
            if (systems == null || systems.Length == 0) return new ThrusterDefaults[0];
            var result = new ThrusterDefaults[systems.Length];
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null) continue;
                var main = ps.main;
                var em = ps.emission;
                result[i] = new ThrusterDefaults
                {
                    ps = ps,
                    baseRate = em.rateOverTime.constantMax > 0f ? em.rateOverTime.constantMax : em.rateOverTime.constant,
                    baseSpeedMin = main.startSpeed.constantMin,
                    baseSpeedMax = main.startSpeed.constantMax,
                    baseSizeMin = main.startSize.constantMin,
                    baseSizeMax = main.startSize.constantMax
                };
            }
            return result;
        }

        private static void ApplyScale(ThrusterDefaults[] list, float scale)
        {
            if (list == null) return;
            bool emit = scale > 0.03f;
            float sizeScale = Mathf.Lerp(0.65f, 1.15f, Mathf.Clamp01(scale));
            float speedScale = Mathf.Lerp(0.65f, 1.2f, Mathf.Clamp01(scale));

            for (int i = 0; i < list.Length; i++)
            {
                ThrusterDefaults d = list[i];
                if (d.ps == null) continue;

                var em = d.ps.emission;
                em.enabled = emit;
                if (emit) em.rateOverTime = new ParticleSystem.MinMaxCurve(d.baseRate * scale);

                var main = d.ps.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(d.baseSpeedMin * speedScale, d.baseSpeedMax * speedScale);
                main.startSize = new ParticleSystem.MinMaxCurve(d.baseSizeMin * sizeScale, d.baseSizeMax * sizeScale);
            }
        }
    }
}
