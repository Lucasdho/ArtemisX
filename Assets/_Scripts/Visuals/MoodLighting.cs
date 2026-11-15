using DG.Tweening;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Changes the light and plays particle effects when the ambient mood changes. Normal is whatever the scene starts with.</summary>
    public class MoodLighting : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private AmbientMood ambientMood;
        [SerializeField] private Light sun;
        [SerializeField] private ParticleSystem nightParticles;
        [SerializeField] private ParticleSystem stormParticles;

        private Color normalSunColor;
        private float normalSunIntensity;
        private Color normalAmbient;
        private float baseIntensity;
        private bool storm;
        private Tween intensityTween;
        private NightLightPool[] nightPools;

        private void Awake()
        {
            normalSunColor = sun.color;
            normalSunIntensity = sun.intensity;
            normalAmbient = RenderSettings.ambientLight;
            baseIntensity = normalSunIntensity;
            nightPools = FindObjectsByType<NightLightPool>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            if (ambientMood != null) ambientMood.MoodChanged += OnMoodChanged;
        }

        private void OnDisable()
        {
            if (ambientMood != null) ambientMood.MoodChanged -= OnMoodChanged;
        }

        private void Start()
        {
            if (ambientMood != null && ambientMood.Mood != MoodState.Normal) OnMoodChanged(ambientMood.Mood);
        }

        private void OnMoodChanged(MoodState mood)
        {
            Color sunColor = normalSunColor;
            Color ambient = normalAmbient;
            float intensity = normalSunIntensity;
            if (mood == MoodState.Night)
            {
                sunColor = config.nightSunColor;
                ambient = config.nightAmbient;
                intensity = normalSunIntensity * config.nightSunIntensityScale;
            }
            else if (mood == MoodState.Storm)
            {
                sunColor = config.stormSunColor;
                ambient = config.stormAmbient;
                intensity = normalSunIntensity * config.stormSunIntensityScale;
            }
            storm = mood == MoodState.Storm;

            float seconds = config.moodTransitionSeconds;
            sun.DOKill();
            sun.DOColor(sunColor, seconds).SetLink(gameObject);
            intensityTween?.Kill();
            intensityTween = DOTween.To(() => baseIntensity, v => baseIntensity = v, intensity, seconds).SetLink(gameObject);
            DOTween.Kill(this);
            DOTween.To(() => RenderSettings.ambientLight, v => RenderSettings.ambientLight = v, ambient, seconds).SetTarget(this).SetLink(gameObject);

            SetPlaying(nightParticles, mood == MoodState.Night);
            SetPlaying(stormParticles, storm);

            if (nightPools != null)
            {
                for (int i = 0; i < nightPools.Length; i++)
                {
                    if (nightPools[i] != null) nightPools[i].SetMood(mood, seconds);
                }
            }
        }

        private void Update()
        {
            float flicker = storm ? 1f + config.stormFlicker * (Mathf.PerlinNoise(Time.time * config.stormFlickerSpeed, 0.5f) - 0.5f) * 2f : 1f;
            sun.intensity = baseIntensity * flicker;
        }

        private void OnDestroy()
        {
            RenderSettings.ambientLight = normalAmbient;
        }

        private static void SetPlaying(ParticleSystem system, bool on)
        {
            if (system == null) return;
            if (on && !system.isPlaying) system.Play();
            else if (!on && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
