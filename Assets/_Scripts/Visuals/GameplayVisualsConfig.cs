using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>Timings and sizes for the Run 3B world visuals: day-to-night, rover trips, supply capsule, solar rocks, building pop.</summary>
    [CreateAssetMenu(fileName = "GameplayVisualsConfig", menuName = "Artemis X/Visuals/Gameplay Visuals Config")]
    public class GameplayVisualsConfig : ScriptableObject
    {
        [Header("Next Day transition (about 1.5-2 s total)")]
        [Min(0f)] public float stormSeconds = 1.8f;
        [Min(0.05f)] public float nightSeconds = 1.0f;
        [Tooltip("Night length when there is no storm or radiation phase.")]
        [Min(0.05f)] public float calmNightSeconds = 1.5f;

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
