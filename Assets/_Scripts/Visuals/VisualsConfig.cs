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
        public Color nightSunColor = new Color(0.55f, 0.66f, 1f);
        [Range(0f, 2f)] public float nightSunIntensityScale = 0.45f;
        public Color nightAmbient = new Color(0.09f, 0.11f, 0.22f);
        public Color stormSunColor = new Color(1f, 0.7f, 0.48f);
        [Range(0f, 2f)] public float stormSunIntensityScale = 0.9f;
        public Color stormAmbient = new Color(0.3f, 0.15f, 0.12f);
        [Range(0f, 0.5f)] public float stormFlicker = 0.18f;
        [Min(0.1f)] public float stormFlickerSpeed = 7f;

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
