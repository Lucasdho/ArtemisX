using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Timings and sizes for the Run 3B world visuals: day-to-night, rover trips, supply capsule, solar rocks, building pop.</summary>
    [CreateAssetMenu(fileName = "GameplayVisualsConfig", menuName = "Artemis X/Visuals/Gameplay Visuals Config")]
    public class GameplayVisualsConfig : ScriptableObject
    {
        [Header("Next Day transition: Daylight -> Storm/Radiation -> Night -> Dawn")]
        [Tooltip("Whole transition when the player doesn't skip (GDD: about 10 s).")]
        [Min(0.5f)] public float transitionSeconds = 10f;
        [Tooltip("Event day split (weights, normalised): daylight hold, storm/radiation, night, dawn.")]
        public Vector4 eventDaySplit = new Vector4(0.1f, 0.3f, 0.35f, 0.25f);
        [Tooltip("Calm day split (weights, normalised): daylight hold, unused, night, dawn.")]
        public Vector4 calmDaySplit = new Vector4(0.15f, 0f, 0.55f, 0.3f);

        [Header("Rover trips")]
        [Min(0.1f)] public float roverDriveSpeed = 3.2f;
        [Min(1f)] public float roverTurnSharpness = 8f;
        [Min(0.5f)] public float brokenAlertInterval = 3f;

        [Header("Supply capsule")]
        [Min(1f)] public float capsuleDropHeight = 14f;
        [Min(0.1f)] public float capsuleDropSeconds = 1.4f;

        [Header("Solar field")]
        [Tooltip("Vertical scale of each panel while retracted (protected).")]
        [Range(0.1f, 1f)] public float retractedPanelScale = 0.35f;
        [Min(0.05f)] public float retractSeconds = 0.4f;
        [Min(1)] public int damagedPanelCount = 4;
        public Vector2 rockScaleRange = new Vector2(0.65f, 0.9f);

        [Header("Buildings")]
        [Min(0.05f)] public float buildPopSeconds = 0.5f;
        [Tooltip("Ghost ring radius = footprint radius times this.")]
        [Min(0.1f)] public float ghostRingRadiusScale = 0.75f;
        public Color ghostRingColor = new Color(0.35f, 0.85f, 1f, 0.9f);
    }
}
