using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Tunable timing and input parameters for the dialogue system.</summary>
    [CreateAssetMenu(fileName = "DialogueConfig", menuName = "Artemis X/Story/Dialogue Config")]
    public class DialogueConfig : ScriptableObject
    {
        [Header("Typewriter")]
        [Min(1f)] public float charactersPerSecond = 45f;
        [Tooltip("Seconds to wait before auto-advancing a completed line; 0 disables auto-advance.")]
        [Min(0f)] public float autoAdvanceDelay = 0f;

        [Header("Input")]
        [Min(0f)] public float inputCooldown = 0.05f;
    }
}
