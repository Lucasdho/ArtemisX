using System;
using UnityEngine;

namespace ArtemisX.Audio
{
    /// <summary>Maps every SoundCue to a Kenney AudioClip with volume and pitch randomization.</summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Artemis X/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Serializable]
        public struct CueEntry
        {
            public SoundCue cue;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            [Range(0.8f, 1.2f)] public float pitchMin;
            [Range(0.8f, 1.2f)] public float pitchMax;
        }

        [Header("Master Controls")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        public bool muted;

        [Header("Sound Cues")]
        public CueEntry[] entries = Array.Empty<CueEntry>();

        /// <summary>Looks up the configured entry for a cue.</summary>
        public bool TryGetEntry(SoundCue cue, out CueEntry entry)
        {
            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].cue == cue)
                    {
                        entry = entries[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }
    }
}
