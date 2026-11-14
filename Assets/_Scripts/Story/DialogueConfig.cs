using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Tunable timing, mission cadence, and dialogue copy settings.</summary>
    [CreateAssetMenu(fileName = "DialogueConfig", menuName = "Artemis X/Story/Dialogue Config")]
    public class DialogueConfig : ScriptableObject
    {
        [Header("Typewriter")]
        [Min(1f)] public float charactersPerSecond = 45f;
        [Tooltip("Seconds to wait before auto-advancing a completed line; 0 disables auto-advance.")]
        [Min(0f)] public float autoAdvanceDelay = 0f;

        [Header("Input")]
        [Min(0f)] public float inputCooldown = 0.05f;

        [Header("Mission & Roster")]
        public string defaultPlayerName = "Commander";
        [Min(1)] public int missionLength = 30;
        [Min(1)] public int supplyInterval = 5;

        [Header("Thresholds")]
        [Min(0)] public int lowPowerThreshold = 25;
        [Min(0)] public int lowWaterThreshold = 25;

        [Header("Authoring Constraints")]
        [Min(40)] public int maxCharactersPerLine = 110;
        [Min(1)] public int maxLinesPerBeat = 2;
    }
}
