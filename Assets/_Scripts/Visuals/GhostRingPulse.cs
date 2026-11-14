using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Gently pulses a ghost slot's ring so empty building slots feel alive.</summary>
    public class GhostRingPulse : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private LineRenderer ring;
        [Range(0f, 1f)] [SerializeField] private float phase;

        private Vector3 baseScale;
        private Color baseStart;
        private Color baseEnd;

        private void Awake()
        {
            if (ring == null) ring = GetComponent<LineRenderer>();
            baseScale = transform.localScale;
            if (ring != null)
            {
                baseStart = ring.startColor;
                baseEnd = ring.endColor;
            }
        }

        private void Update()
        {
            if (config == null) return;
            float wave = 0.5f + 0.5f * Mathf.Sin((Time.time / config.ringPulsePeriod + phase) * Mathf.PI * 2f);
            transform.localScale = baseScale * (1f + config.ringPulseScale * wave);
            if (ring == null) return;
            float alpha = 1f - config.ringPulseAlphaDip * (1f - wave);
            ring.startColor = new Color(baseStart.r, baseStart.g, baseStart.b, baseStart.a * alpha);
            ring.endColor = new Color(baseEnd.r, baseEnd.g, baseEnd.b, baseEnd.a * alpha);
        }
    }
}
