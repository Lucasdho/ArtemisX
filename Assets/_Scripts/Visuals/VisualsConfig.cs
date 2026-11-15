using System;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>All tunable numbers for the living-base visuals. One asset lives in Assets/_Config/Visuals/.</summary>
    [CreateAssetMenu(fileName = "VisualsConfig", menuName = "Artemis X/Visuals Config")]
    public class VisualsConfig : ScriptableObject
    {
        [Serializable]
        public struct EmoteSet
        {
            public EmoteKind kind;
            public Texture2D[] textures;
        }

        [Header("Rover")]
        [Min(0.01f)] public float roverSpeed = 1.4f;
        [Min(0.01f)] public float roverAcceleration = 0.9f;
        [Min(1f)] public float roverTurnSharpness = 6f;
        [Min(0.01f)] public float roverParkDistance = 0.08f;
        public float roverBounceHeight = 0.012f;
        public float roverBounceFrequency = 6f;
        public float roverWobbleDegrees = 2.5f;
        public float roverBrakePitchDegrees = 3f;

        [Header("Astronaut gait")]
        [Min(0.01f)] public float walkSpeed = 0.42f;
        [Min(0f)] public float hopHeight = 0.14f;
        [Min(0.05f)] public float hopAirSeconds = 0.85f;
        [Min(0.01f)] public float hopGroundSeconds = 0.16f;
        [Range(0f, 1f)] public float groundSpeedFactor = 0.25f;
        [Range(0f, 0.6f)] public float squashAmount = 0.22f;
        [Min(1f)] public float walkerTurnDegreesPerSecond = 300f;
        [Min(0.001f)] public float arriveDistance = 0.03f;
        [Min(0)] public int landingDustCount = 4;

        [Header("Astronaut pauses and actions")]
        [Range(0f, 1f)] public float pauseChance = 0.4f;
        public Vector2 pauseSeconds = new Vector2(0.8f, 2f);
        public string idleState = "Idle";
        public string walkState = "Walk";
        public string[] actionStates = { "Wave", "Jump", "Yes" };
        [Min(0f)] public float animationCrossFade = 0.15f;
        [Min(0.1f)] public float fallbackActionSeconds = 1.5f;
        [Tooltip("Action state that also triggers big floaty hops on the spot.")]
        public string jumpActionState = "Jump";
        [Min(0f)] public float jumpHeight = 0.38f;
        [Min(0.1f)] public float jumpAirSeconds = 1.15f;
        [Min(1)] public int jumpCount = 2;

        [Header("Mood")]
        [Min(0.05f)] public float nightSpeedMultiplier = 0.45f;
        [Min(0.05f)] public float stormSpeedMultiplier = 1.6f;
        [Min(0f)] public float shelterSpread = 0.35f;
        [Tooltip("Two astronauts closer than this stop and greet each other.")]
        [Min(0.1f)] public float greetDistance = 0.9f;
        [Min(1f)] public float greetCooldownSeconds = 14f;
        public string greetState = "Wave";
        public Vector2 sleepEmoteInterval = new Vector2(3f, 5f);
        public Vector2 stormEmoteInterval = new Vector2(2.5f, 4f);

        [Header("Mood lighting")]
        [Min(0.05f)] public float moodTransitionSeconds = 1.5f;
        [Min(0.05f)] public float stormTransitionSeconds = 0.45f;
        public Color nightSunColor = new Color(0.24f, 0.31f, 0.62f);
        [Range(0f, 2f)] public float nightSunIntensityScale = 0.15f;
        public Color nightAmbient = new Color(0.03f, 0.04f, 0.09f);
        [Range(0f, 1f)] public float nightRimIntensity = 0.18f;
        public Color stormSunColor = new Color(1f, 0.30f, 0.08f);
        [Range(0f, 4f)] public float stormSunIntensityScale = 2.05f;
        public Color stormAmbient = new Color(0.035f, 0.042f, 0.085f);
        public Vector3 stormSunEuler = new Vector3(13f, 242f, 0f);
        [Range(0f, 1f)] public float stormShadowStrength = 0.96f;
        [Range(0f, 0.8f)] public float stormFlicker = 0.42f;
        [Min(0.1f)] public float stormFlickerSpeed = 13f;
        public Color stormFlashColor = new Color(0.92f, 0.95f, 1f);
        [Range(0f, 5f)] public float stormFlashIntensity = 2.4f;
        [Min(0.1f)] public float stormFlashFrequency = 2.9f;
        public Color stormRimColor = new Color(0.18f, 0.64f, 1f);
        [Range(0f, 3f)] public float stormRimIntensity = 1.25f;
        [Range(0f, 3f)] public float stormRimFlashBoost = 0.85f;
        public Vector3 stormRimEuler = new Vector3(15f, 62f, 0f);
        [Range(0f, 3f)] public float stormBloomIntensity = 1.28f;
        [Range(0f, 2f)] public float stormBloomThreshold = 0.82f;
        [Range(0f, 1f)] public float stormVignetteIntensity = 0.44f;
        public Color stormVignetteColor = new Color(0.06f, 0.02f, 0.05f);
        [Range(-100f, 100f)] public float stormContrast = 24f;
        [Range(-100f, 100f)] public float stormSaturation = 16f;
        [Range(-2f, 2f)] public float stormPostExposure = 0.08f;
        [Range(0f, 1f)] public float stormChromaticAberration = 0.14f;
        [Range(0f, 1f)] public float stormFlashChromaticBoost = 0.24f;
        public bool stormFogEnabled = true;
        public Color stormFogColor = new Color(0.14f, 0.065f, 0.10f);
        [Range(0f, 0.08f)] public float stormFogDensity = 0.017f;

        [Header("Storm particles")]
        public Material stormSparkMaterial;
        public Material stormDustMaterial;
        [Min(100)] public int stormMaxParticles = 14000;
        [Min(50f)] public float stormEmissionRate = 7500f;
        [Min(0)] public int stormBurstCount = 2400;
        public Vector3 stormBoxCenter = new Vector3(-2.5f, 3.6f, -1.5f);
        public Vector3 stormBoxScale = new Vector3(30f, 7.5f, 26f);
        public Vector2 stormParticleLifetime = new Vector2(0.65f, 1.25f);
        public Vector2 stormParticleSize = new Vector2(0.06f, 0.18f);
        public Vector2 stormVelocityX = new Vector2(20f, 32f);
        public Vector2 stormVelocityY = new Vector2(-3.5f, -1.0f);
        public Vector2 stormVelocityZ = new Vector2(-8.0f, -3.0f);
        [ColorUsage(true, true)] public Color stormParticleColorMin = new Color(1.35f, 0.42f, 0.10f, 0.95f);
        [ColorUsage(true, true)] public Color stormParticleColorMax = new Color(1.85f, 1.32f, 0.62f, 1f);
        [Min(0f)] public float stormNoiseStrength = 1.6f;
        [Min(0.01f)] public float stormNoiseFrequency = 0.72f;
        [Min(0.005f)] public float stormStretchVelocityScale = 0.062f;
        [Min(0.1f)] public float stormStretchLengthScale = 2.6f;
        [Min(0f)] public float stormGaleEmissionRate = 4500f;
        [ColorUsage(true, true)] public Color stormIonStreakColorMin = new Color(0.35f, 0.88f, 1.65f, 0.75f);
        [ColorUsage(true, true)] public Color stormIonStreakColorMax = new Color(1.65f, 0.92f, 0.42f, 0.92f);
        [Min(0f)] public float stormDustEmissionRate = 3500f;
        [Min(0f)] public float stormForegroundEmissionRate = 3400f;
        public Vector3 stormForegroundCenterOffset = new Vector3(1.5f, -0.5f, -4.5f);
        public Vector3 stormForegroundBoxScale = new Vector3(22f, 5.5f, 18f);
        [Min(0f)] public float stormDustCloudEmissionRate = 650f;
        public Vector2 stormDustCloudSize = new Vector2(1.4f, 3.2f);
        public Color stormDustCloudColorMin = new Color(0.68f, 0.30f, 0.20f, 0.12f);
        public Color stormDustCloudColorMax = new Color(0.96f, 0.52f, 0.28f, 0.20f);

        [Header("Emote bubbles")]
        [Min(0.01f)] public float emoteWorldSize = 0.55f;
        public float emoteHeight = 0.65f;
        [Min(0.01f)] public float emotePopSeconds = 0.25f;
        [Min(0.1f)] public float emoteSeconds = 2.2f;
        public EmoteSet[] emotes = Array.Empty<EmoteSet>();

        [Header("Ambient")]
        [Min(0.1f)] public float lightBlinkPeriod = 2.4f;
        [Range(0f, 1f)] public float lightBlinkDuty = 0.5f;
        public float antennaDegreesPerSecond = 25f;
        [Min(0.1f)] public float ringPulsePeriod = 2.6f;
        [Range(0f, 0.5f)] public float ringPulseScale = 0.06f;
        [Range(0f, 1f)] public float ringPulseAlphaDip = 0.45f;

        public Texture2D PickEmote(EmoteKind kind)
        {
            for (int i = 0; i < emotes.Length; i++)
            {
                if (emotes[i].kind != kind) continue;
                Texture2D[] set = emotes[i].textures;
                if (set == null || set.Length == 0) return null;
                return set[UnityEngine.Random.Range(0, set.Length)];
            }
            return null;
        }
    }
}
