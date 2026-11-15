using DG.Tweening;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Marks a local Light (e.g. lamp post spot) that fades on during Night mood.</summary>
    [RequireComponent(typeof(Light))]
    public class NightLightPool : MonoBehaviour
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private float nightIntensity = -1f;
        [SerializeField] private float stormFactor = 0.35f;

        private float maxIntensity;

        private void Awake()
        {
            if (targetLight == null) targetLight = GetComponent<Light>();
            maxIntensity = nightIntensity >= 0f ? nightIntensity : (targetLight != null ? targetLight.intensity : 0f);
            if (targetLight != null) targetLight.intensity = 0f;
        }

        /// <summary>Smoothly fades this light pool for the given mood.</summary>
        public void SetMood(MoodState mood, float seconds)
        {
            if (targetLight == null) return;
            float factor = mood == MoodState.Night ? 1f : mood == MoodState.Storm ? stormFactor : 0f;
            float goal = maxIntensity * factor;
            targetLight.DOKill();
            if (goal > 0.001f) targetLight.enabled = true;
            Tween tween = targetLight.DOIntensity(goal, seconds).SetLink(gameObject);
            if (goal <= 0.001f)
                tween.OnComplete(() => { if (targetLight != null && targetLight.intensity <= 0.001f) targetLight.enabled = false; });
        }
    }
}
