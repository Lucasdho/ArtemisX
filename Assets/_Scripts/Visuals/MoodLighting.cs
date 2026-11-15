using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ArtemisX.Visuals
{
    /// <summary>Changes the light and plays particle effects when the ambient mood changes. Normal is whatever the scene starts with.</summary>
    public class MoodLighting : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private AmbientMood ambientMood;
        [SerializeField] private Light sun;
        [SerializeField] private Light rimLight;
        [SerializeField] private ParticleSystem nightParticles;
        [SerializeField] private ParticleSystem stormParticles;

        private Color normalSunColor;
        private float normalSunIntensity;
        private Quaternion normalSunRotation;
        private float normalShadowStrength;
        private Color normalAmbient;
        private bool normalFogEnabled;
        private FogMode normalFogMode;
        private Color normalFogColor;
        private float normalFogDensity;
        private float normalBloomIntensity = 0.25f;
        private float normalBloomThreshold = 1f;
        private float normalVignetteIntensity = 0.2f;
        private Color normalVignetteColor = Color.black;
        private float normalContrast;
        private float normalSaturation;
        private float normalPostExposure;
        private float normalChromaticAberration;

        private Color currentSunColor;
        private float baseIntensity;
        private float baseRimIntensity;
        private float baseBloomIntensity;
        private float baseChromaticAberration;
        private bool storm;
        private Tween colorTween;
        private Tween intensityTween;
        private Tween shadowTween;
        private Tween rimTween;
        private Tween bloomTween;
        private Tween bloomThresholdTween;
        private Tween vignetteTween;
        private Tween vignetteColorTween;
        private Tween contrastTween;
        private Tween saturationTween;
        private Tween exposureTween;
        private Tween chromaticTween;
        private Tween fogDensityTween;
        private Tween fogColorTween;
        private NightLightPool[] nightPools;
        private Bloom bloom;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;
        private ChromaticAberration chromaticAberration;

        private void Awake()
        {
            if (sun != null)
            {
                normalSunColor = sun.color;
                normalSunIntensity = sun.intensity;
                normalSunRotation = sun.transform.rotation;
                normalShadowStrength = sun.shadowStrength;
                currentSunColor = normalSunColor;
                baseIntensity = normalSunIntensity;
            }
            normalAmbient = RenderSettings.ambientLight;
            normalFogEnabled = RenderSettings.fog;
            normalFogMode = RenderSettings.fogMode;
            normalFogColor = RenderSettings.fogColor;
            normalFogDensity = RenderSettings.fogDensity;
            nightPools = FindObjectsByType<NightLightPool>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            CachePostProcessing();
            EnsureRimLight();
            ConfigureStormParticles();
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
            if (config == null || sun == null) return;

            Color sunColor = normalSunColor;
            Color ambient = normalAmbient;
            float intensity = normalSunIntensity;
            Quaternion sunRotation = normalSunRotation;
            float shadowStrength = normalShadowStrength;
            float rimGoal = 0f;
            float bloomGoal = normalBloomIntensity;
            float bloomThresholdGoal = normalBloomThreshold;
            float vignetteGoal = normalVignetteIntensity;
            Color vignetteColorGoal = normalVignetteColor;
            float contrastGoal = normalContrast;
            float saturationGoal = normalSaturation;
            float exposureGoal = normalPostExposure;
            float chromaticGoal = normalChromaticAberration;
            bool fogGoalEnabled = normalFogEnabled;
            Color fogColorGoal = normalFogColor;
            float fogDensityGoal = normalFogEnabled ? normalFogDensity : 0f;

            if (mood == MoodState.Night)
            {
                sunColor = config.nightSunColor;
                ambient = config.nightAmbient;
                intensity = normalSunIntensity * config.nightSunIntensityScale;
                rimGoal = config.nightRimIntensity;
                bloomGoal = normalBloomIntensity * 1.35f;
            }
            else if (mood == MoodState.Storm)
            {
                sunColor = config.stormSunColor;
                ambient = config.stormAmbient;
                intensity = normalSunIntensity * config.stormSunIntensityScale;
                sunRotation = Quaternion.Euler(config.stormSunEuler);
                shadowStrength = config.stormShadowStrength;
                rimGoal = config.stormRimIntensity;
                bloomGoal = config.stormBloomIntensity;
                bloomThresholdGoal = config.stormBloomThreshold;
                vignetteGoal = config.stormVignetteIntensity;
                vignetteColorGoal = config.stormVignetteColor;
                contrastGoal = config.stormContrast;
                saturationGoal = config.stormSaturation;
                exposureGoal = config.stormPostExposure;
                chromaticGoal = config.stormChromaticAberration;
                fogGoalEnabled = config.stormFogEnabled;
                fogColorGoal = config.stormFogColor;
                fogDensityGoal = config.stormFogEnabled ? config.stormFogDensity : 0f;
            }
            storm = mood == MoodState.Storm;

            float seconds = storm ? config.stormTransitionSeconds : config.moodTransitionSeconds;

            sun.DOKill();
            sun.transform.DOKill();
            sun.transform.DORotateQuaternion(sunRotation, seconds).SetEase(Ease.OutCubic).SetLink(gameObject);

            colorTween?.Kill();
            colorTween = DOTween.To(() => currentSunColor, v => currentSunColor = v, sunColor, seconds).SetLink(gameObject);

            intensityTween?.Kill();
            intensityTween = DOTween.To(() => baseIntensity, v => baseIntensity = v, intensity, seconds).SetLink(gameObject);

            shadowTween?.Kill();
            shadowTween = DOTween.To(() => sun.shadowStrength, v => sun.shadowStrength = v, shadowStrength, seconds).SetLink(gameObject);

            DOTween.Kill(this);
            DOTween.To(() => RenderSettings.ambientLight, v => RenderSettings.ambientLight = v, ambient, seconds).SetTarget(this).SetLink(gameObject);

            fogDensityTween?.Kill();
            fogColorTween?.Kill();
            if (fogGoalEnabled)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }
            fogColorTween = DOTween.To(() => RenderSettings.fogColor, v => RenderSettings.fogColor = v, fogColorGoal, seconds).SetLink(gameObject);
            fogDensityTween = DOTween.To(() => RenderSettings.fogDensity, v => RenderSettings.fogDensity = v, fogDensityGoal, seconds)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    if (!fogGoalEnabled)
                    {
                        RenderSettings.fog = false;
                        RenderSettings.fogMode = normalFogMode;
                    }
                });

            if (rimLight != null)
            {
                rimTween?.Kill();
                rimLight.color = config.stormRimColor;
                rimLight.transform.rotation = Quaternion.Euler(config.stormRimEuler);
                if (rimGoal > 0.001f) rimLight.enabled = true;
                rimTween = DOTween.To(() => baseRimIntensity, v => baseRimIntensity = v, rimGoal, seconds)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        if (rimLight != null && baseRimIntensity <= 0.001f) rimLight.enabled = false;
                    });
            }

            if (bloom != null)
            {
                bloomTween?.Kill();
                bloomTween = DOTween.To(() => baseBloomIntensity, v => baseBloomIntensity = v, bloomGoal, seconds).SetLink(gameObject);
                bloomThresholdTween?.Kill();
                bloomThresholdTween = DOTween.To(() => bloom.threshold.value, v => bloom.threshold.value = v, bloomThresholdGoal, seconds).SetLink(gameObject);
            }

            if (vignette != null)
            {
                vignetteTween?.Kill();
                vignetteTween = DOTween.To(() => vignette.intensity.value, v => vignette.intensity.value = v, vignetteGoal, seconds).SetLink(gameObject);
                vignetteColorTween?.Kill();
                vignetteColorTween = DOTween.To(() => vignette.color.value, v => vignette.color.value = v, vignetteColorGoal, seconds).SetLink(gameObject);
            }

            if (colorAdjustments != null)
            {
                contrastTween?.Kill();
                contrastTween = DOTween.To(() => colorAdjustments.contrast.value, v => colorAdjustments.contrast.value = v, contrastGoal, seconds).SetLink(gameObject);
                saturationTween?.Kill();
                saturationTween = DOTween.To(() => colorAdjustments.saturation.value, v => colorAdjustments.saturation.value = v, saturationGoal, seconds).SetLink(gameObject);
                exposureTween?.Kill();
                exposureTween = DOTween.To(() => colorAdjustments.postExposure.value, v => colorAdjustments.postExposure.value = v, exposureGoal, seconds).SetLink(gameObject);
            }

            if (chromaticAberration != null)
            {
                chromaticTween?.Kill();
                chromaticTween = DOTween.To(() => baseChromaticAberration, v => baseChromaticAberration = v, chromaticGoal, seconds).SetLink(gameObject);
            }

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
            if (sun == null || config == null) return;

            if (storm)
            {
                float noise = (Mathf.PerlinNoise(Time.time * config.stormFlickerSpeed, 0.5f) - 0.5f) * 2f;
                float flicker = 1f + config.stormFlicker * noise;
                float surgePrimary = Mathf.PerlinNoise(Time.time * config.stormFlashFrequency, 7.3f);
                float surgeSecondary = Mathf.PerlinNoise(Time.time * config.stormFlashFrequency * 2.15f, 19.1f);
                float surgeCombined = surgePrimary * 0.65f + surgeSecondary * 0.35f;
                float flash = Mathf.Pow(Mathf.Clamp01((surgeCombined - 0.50f) / 0.30f), 2.2f);

                sun.intensity = Mathf.Max(0.25f, baseIntensity * flicker + flash * config.stormFlashIntensity);
                sun.color = Color.Lerp(currentSunColor, config.stormFlashColor, Mathf.Clamp01(flash * 0.78f));
                if (rimLight != null) rimLight.intensity = baseRimIntensity * (1f + config.stormRimFlashBoost * flash);
                if (bloom != null) bloom.intensity.value = baseBloomIntensity + flash * 0.38f;
                if (chromaticAberration != null) chromaticAberration.intensity.value = baseChromaticAberration + flash * config.stormFlashChromaticBoost;
            }
            else
            {
                sun.intensity = baseIntensity;
                sun.color = currentSunColor;
                if (rimLight != null) rimLight.intensity = baseRimIntensity;
                if (bloom != null) bloom.intensity.value = baseBloomIntensity;
                if (chromaticAberration != null) chromaticAberration.intensity.value = baseChromaticAberration;
            }
        }

        private void OnDestroy()
        {
            RenderSettings.ambientLight = normalAmbient;
            RenderSettings.fog = normalFogEnabled;
            RenderSettings.fogMode = normalFogMode;
            RenderSettings.fogColor = normalFogColor;
            RenderSettings.fogDensity = normalFogDensity;
        }

        private void CachePostProcessing()
        {
            var volumes = FindObjectsByType<Volume>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < volumes.Length; i++)
            {
                var vol = volumes[i];
                if (vol == null || !vol.isGlobal || vol.sharedProfile == null) continue;
                var profile = Application.isPlaying ? vol.profile : vol.sharedProfile;
                if (profile == null) continue;

                if (profile.TryGet(out Bloom b))
                {
                    bloom = b;
                    normalBloomIntensity = b.intensity.value;
                    normalBloomThreshold = b.threshold.value;
                    baseBloomIntensity = normalBloomIntensity;
                }
                if (profile.TryGet(out Vignette vg))
                {
                    vignette = vg;
                    normalVignetteIntensity = vg.intensity.value;
                    normalVignetteColor = vg.color.value;
                    vg.color.overrideState = true;
                }

                if (!profile.TryGet(out ColorAdjustments ca) && Application.isPlaying)
                {
                    ca = profile.Add<ColorAdjustments>(false);
                }
                if (ca != null)
                {
                    colorAdjustments = ca;
                    normalContrast = ca.contrast.value;
                    normalSaturation = ca.saturation.value;
                    normalPostExposure = ca.postExposure.value;
                    ca.active = true;
                    ca.contrast.overrideState = true;
                    ca.saturation.overrideState = true;
                    ca.postExposure.overrideState = true;
                }

                if (!profile.TryGet(out ChromaticAberration ch) && Application.isPlaying)
                {
                    ch = profile.Add<ChromaticAberration>(false);
                }
                if (ch != null)
                {
                    chromaticAberration = ch;
                    normalChromaticAberration = ch.intensity.value;
                    baseChromaticAberration = normalChromaticAberration;
                    ch.active = true;
                    ch.intensity.overrideState = true;
                }
                break;
            }
        }

        private void EnsureRimLight()
        {
            if (config == null) return;
            if (rimLight == null)
            {
                Transform child = transform.Find("StormRimLight");
                if (child != null) rimLight = child.GetComponent<Light>();
                if (rimLight == null)
                {
                    var go = child != null ? child.gameObject : new GameObject("StormRimLight");
                    go.transform.SetParent(transform, false);
                    rimLight = go.AddComponent<Light>();
                }
            }
            rimLight.type = LightType.Directional;
            rimLight.shadows = LightShadows.None;
            rimLight.color = config.stormRimColor;
            rimLight.transform.rotation = Quaternion.Euler(config.stormRimEuler);
            rimLight.intensity = 0f;
            rimLight.enabled = false;
        }

        private struct LayerSpec
        {
            public Material material;
            public Vector3 localOffset;
            public int maxParticles;
            public float emissionRate;
            public int burstCount;
            public Vector2 lifetime;
            public Vector2 size;
            public Color colorMin;
            public Color colorMax;
            public Vector3 boxScale;
            public Vector2 velX;
            public Vector2 velY;
            public Vector2 velZ;
            public float noiseStrength;
            public float noiseFreq;
            public ParticleSystemRenderMode renderMode;
            public float stretchVelScale;
            public float stretchLenScale;
            public bool randomRotation;
        }

        /// <summary>Applies the VisualsConfig storm particle parameters and ensures multi-layered storm particles.</summary>
        public void ConfigureStormParticles()
        {
            if (stormParticles == null || config == null) return;

            bool wasPlaying = stormParticles.isPlaying;
            if (wasPlaying) stormParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            stormParticles.transform.localPosition = config.stormBoxCenter;

            Material fallbackMat = null;
            var mainRenderer = stormParticles.GetComponent<ParticleSystemRenderer>();
            if (mainRenderer != null) fallbackMat = mainRenderer.sharedMaterial;
            Material sparkMat = config.stormSparkMaterial != null ? config.stormSparkMaterial : fallbackMat;
            Material dustMat = config.stormDustMaterial != null ? config.stormDustMaterial : fallbackMat;

            ApplyLayer(stormParticles, new LayerSpec
            {
                material = sparkMat,
                localOffset = Vector3.zero,
                maxParticles = Mathf.Max(config.stormMaxParticles, ComputeCapacity(config.stormEmissionRate, config.stormParticleLifetime.y, config.stormBurstCount)),
                emissionRate = config.stormEmissionRate,
                burstCount = config.stormBurstCount,
                lifetime = config.stormParticleLifetime,
                size = config.stormParticleSize,
                colorMin = config.stormParticleColorMin,
                colorMax = config.stormParticleColorMax,
                boxScale = config.stormBoxScale,
                velX = config.stormVelocityX,
                velY = config.stormVelocityY,
                velZ = config.stormVelocityZ,
                noiseStrength = config.stormNoiseStrength,
                noiseFreq = config.stormNoiseFrequency,
                renderMode = ParticleSystemRenderMode.Stretch,
                stretchVelScale = config.stormStretchVelocityScale,
                stretchLenScale = config.stormStretchLengthScale
            });

            var galeLifetime = new Vector2(0.45f, 0.90f);
            int galeBurst = Mathf.RoundToInt(config.stormBurstCount * 0.65f);
            var gale = GetOrCreateChildParticleSystem(stormParticles.transform, "StormGaleStreaks");
            ApplyLayer(gale, new LayerSpec
            {
                material = sparkMat,
                localOffset = Vector3.zero,
                maxParticles = ComputeCapacity(config.stormGaleEmissionRate, galeLifetime.y, galeBurst),
                emissionRate = config.stormGaleEmissionRate,
                burstCount = galeBurst,
                lifetime = galeLifetime,
                size = new Vector2(0.025f, 0.065f),
                colorMin = config.stormIonStreakColorMin,
                colorMax = config.stormIonStreakColorMax,
                boxScale = config.stormBoxScale,
                velX = config.stormVelocityX * 1.35f,
                velY = config.stormVelocityY * 1.15f,
                velZ = config.stormVelocityZ * 1.25f,
                noiseStrength = config.stormNoiseStrength * 0.65f,
                noiseFreq = config.stormNoiseFrequency * 0.85f,
                renderMode = ParticleSystemRenderMode.Stretch,
                stretchVelScale = config.stormStretchVelocityScale * 1.4f,
                stretchLenScale = config.stormStretchLengthScale * 1.6f
            });

            var debrisLifetime = new Vector2(0.85f, 1.65f);
            int debrisBurst = Mathf.RoundToInt(config.stormBurstCount * 0.45f);
            var debris = GetOrCreateChildParticleSystem(stormParticles.transform, "StormSwirlDebris");
            ApplyLayer(debris, new LayerSpec
            {
                material = sparkMat,
                localOffset = new Vector3(0f, -0.8f, 0f),
                maxParticles = ComputeCapacity(config.stormDustEmissionRate, debrisLifetime.y, debrisBurst),
                emissionRate = config.stormDustEmissionRate,
                burstCount = debrisBurst,
                lifetime = debrisLifetime,
                size = new Vector2(0.05f, 0.13f),
                colorMin = config.stormParticleColorMin,
                colorMax = config.stormParticleColorMax,
                boxScale = new Vector3(config.stormBoxScale.x, config.stormBoxScale.y * 0.65f, config.stormBoxScale.z),
                velX = config.stormVelocityX * 0.72f,
                velY = new Vector2(-1.6f, 0.9f),
                velZ = config.stormVelocityZ * 0.72f,
                noiseStrength = config.stormNoiseStrength * 1.95f,
                noiseFreq = config.stormNoiseFrequency * 1.35f,
                renderMode = ParticleSystemRenderMode.Stretch,
                stretchVelScale = config.stormStretchVelocityScale * 0.28f,
                stretchLenScale = config.stormStretchLengthScale * 0.55f
            });

            var fgLifetime = new Vector2(0.45f, 0.85f);
            int fgBurst = Mathf.RoundToInt(config.stormBurstCount * 0.4f);
            var fg = GetOrCreateChildParticleSystem(stormParticles.transform, "StormForegroundGale");
            ApplyLayer(fg, new LayerSpec
            {
                material = sparkMat,
                localOffset = config.stormForegroundCenterOffset,
                maxParticles = ComputeCapacity(config.stormForegroundEmissionRate, fgLifetime.y, fgBurst),
                emissionRate = config.stormForegroundEmissionRate,
                burstCount = fgBurst,
                lifetime = fgLifetime,
                size = new Vector2(0.03f, 0.08f),
                colorMin = config.stormParticleColorMin,
                colorMax = config.stormIonStreakColorMax,
                boxScale = config.stormForegroundBoxScale,
                velX = config.stormVelocityX * 1.18f,
                velY = config.stormVelocityY * 1.1f,
                velZ = config.stormVelocityZ * 1.1f,
                noiseStrength = config.stormNoiseStrength * 0.9f,
                noiseFreq = config.stormNoiseFrequency * 1.1f,
                renderMode = ParticleSystemRenderMode.Stretch,
                stretchVelScale = config.stormStretchVelocityScale * 0.65f,
                stretchLenScale = config.stormStretchLengthScale * 0.9f
            });

            var cloudLifetime = new Vector2(1.1f, 1.9f);
            int cloudBurst = Mathf.RoundToInt(config.stormDustCloudEmissionRate * 0.35f);
            var clouds = GetOrCreateChildParticleSystem(stormParticles.transform, "StormDustClouds");
            ApplyLayer(clouds, new LayerSpec
            {
                material = dustMat,
                localOffset = new Vector3(0f, -1.9f, -1.0f),
                maxParticles = ComputeCapacity(config.stormDustCloudEmissionRate, cloudLifetime.y, cloudBurst),
                emissionRate = config.stormDustCloudEmissionRate,
                burstCount = cloudBurst,
                lifetime = cloudLifetime,
                size = config.stormDustCloudSize,
                colorMin = config.stormDustCloudColorMin,
                colorMax = config.stormDustCloudColorMax,
                boxScale = new Vector3(config.stormBoxScale.x * 0.85f, 2.8f, config.stormBoxScale.z * 0.85f),
                velX = config.stormVelocityX * 0.48f,
                velY = new Vector2(-0.35f, 0.55f),
                velZ = config.stormVelocityZ * 0.48f,
                noiseStrength = config.stormNoiseStrength * 1.1f,
                noiseFreq = config.stormNoiseFrequency * 0.45f,
                renderMode = ParticleSystemRenderMode.Billboard,
                stretchVelScale = 0f,
                stretchLenScale = 1f,
                randomRotation = true
            });

            if (wasPlaying) stormParticles.Play(true);
        }

        private static int ComputeCapacity(float emissionRate, float maxLifetime, int burstCount)
        {
            return Mathf.Max(400, Mathf.CeilToInt(emissionRate * maxLifetime * 1.35f) + burstCount);
        }

        private static ParticleSystem GetOrCreateChildParticleSystem(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            GameObject go = child != null ? child.gameObject : new GameObject(childName);
            if (child == null) go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var ps = go.GetComponent<ParticleSystem>();
            return ps != null ? ps : go.AddComponent<ParticleSystem>();
        }

        private static void ApplyLayer(ParticleSystem ps, LayerSpec spec)
        {
            if (ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (ps.transform.parent != null && ps.transform.parent.name == "StormParticles")
            {
                ps.transform.localPosition = spec.localOffset;
            }

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = spec.maxParticles;
            main.startLifetime = new ParticleSystem.MinMaxCurve(spec.lifetime.x, spec.lifetime.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f);
            main.startSize = new ParticleSystem.MinMaxCurve(spec.size.x, spec.size.y);
            main.startRotation = spec.randomRotation
                ? new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f)
                : new ParticleSystem.MinMaxCurve(0f);
            main.startColor = new ParticleSystem.MinMaxGradient(spec.colorMin, spec.colorMax);
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = spec.emissionRate > 0f || spec.burstCount > 0;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(spec.emissionRate);
            if (spec.burstCount > 0)
            {
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Min(spec.burstCount, short.MaxValue)) });
            }
            else
            {
                emission.SetBursts(System.Array.Empty<ParticleSystem.Burst>());
            }

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = spec.boxScale;

            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(spec.velX.x, spec.velX.y);
            vol.y = new ParticleSystem.MinMaxCurve(spec.velY.x, spec.velY.y);
            vol.z = new ParticleSystem.MinMaxCurve(spec.velZ.x, spec.velZ.y);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.14f),
                    new GradientAlphaKey(1f, 0.80f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var noise = ps.noise;
            noise.enabled = spec.noiseStrength > 0.001f;
            noise.strength = new ParticleSystem.MinMaxCurve(spec.noiseStrength);
            noise.frequency = spec.noiseFreq;
            noise.scrollSpeed = 1.9f;
            noise.damping = true;

            var psr = ps.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                if (spec.material != null) psr.sharedMaterial = spec.material;
                psr.renderMode = spec.renderMode;
                psr.velocityScale = spec.stretchVelScale;
                psr.lengthScale = spec.stretchLenScale;
                psr.shadowCastingMode = ShadowCastingMode.Off;
                psr.receiveShadows = false;
                psr.enableGPUInstancing = true;
            }
        }

        private static void SetPlaying(ParticleSystem system, bool on)
        {
            if (system == null) return;
            if (on && !system.isPlaying) system.Play(true);
            else if (!on && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
