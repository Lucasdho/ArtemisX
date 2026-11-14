using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Portrait mood variant displayed for a dialogue line.</summary>
    public enum SpeakerMood
    {
        Neutral = 0,
        Happy = 1,
        Worried = 2
    }

    /// <summary>Display metadata and mood portraits for a character or AI speaking in dialogue.</summary>
    [CreateAssetMenu(fileName = "Speaker", menuName = "Artemis X/Story/Speaker")]
    public class Speaker : ScriptableObject
    {
        public string speakerId = "bauzer";
        public string displayName = "Bauzer";
        [Tooltip("When enabled, substitutes the active player's name when available.")]
        public bool usePlayerName;

        [Header("Portraits")]
        public Sprite portrait;
        public Sprite happyPortrait;
        public Sprite worriedPortrait;

        public Color accentColor = new Color(0.24f, 0.72f, 0.96f, 1f);

        /// <summary>Returns the mood-specific portrait sprite, falling back to the default portrait.</summary>
        public Sprite GetPortrait(SpeakerMood mood)
        {
            switch (mood)
            {
                case SpeakerMood.Happy:
                    return happyPortrait != null ? happyPortrait : portrait;
                case SpeakerMood.Worried:
                    return worriedPortrait != null ? worriedPortrait : portrait;
                default:
                    return portrait;
            }
        }

        /// <summary>Returns the display label for the speaker banner.</summary>
        public string GetDisplayName(string playerName)
        {
            if (usePlayerName && !string.IsNullOrWhiteSpace(playerName))
                return playerName;

            return displayName;
        }
    }
}
