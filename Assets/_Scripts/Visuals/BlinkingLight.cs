using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Softly blinks a small unlit lamp mesh between two colours. No real light, so it stays cheap on WebGL.</summary>
    public class BlinkingLight : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private VisualsConfig config;
        [SerializeField] private Renderer lamp;
        [SerializeField] private Color onColor = new Color(1f, 0.85f, 0.4f);
        [SerializeField] private Color offColor = new Color(0.25f, 0.18f, 0.08f);
        [Range(0f, 1f)] [SerializeField] private float phase;

        private MaterialPropertyBlock block;

        private void Update()
        {
            if (config == null || lamp == null) return;
            float t = Mathf.Repeat(Time.time / config.lightBlinkPeriod + phase, 1f);
            // Soft edges instead of a hard on/off flip.
            float edge = 0.08f;
            float on = Mathf.SmoothStep(0f, 1f, t / edge) * (1f - Mathf.SmoothStep(0f, 1f, (t - config.lightBlinkDuty) / edge));
            block ??= new MaterialPropertyBlock();
            lamp.GetPropertyBlock(block);
            block.SetColor(BaseColor, Color.Lerp(offColor, onColor, on));
            lamp.SetPropertyBlock(block);
        }
    }
}
