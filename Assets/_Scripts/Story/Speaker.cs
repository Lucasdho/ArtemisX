using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Display metadata for a character or narrator speaking in dialogue.</summary>
    [CreateAssetMenu(fileName = "Speaker", menuName = "Artemis X/Story/Speaker")]
    public class Speaker : ScriptableObject
    {
        public string speakerId = "speaker_a";
        public string displayName = "Speaker A";
        public Sprite portrait;
        public Color accentColor = new Color(0.24f, 0.72f, 0.96f, 1f);
    }
}
