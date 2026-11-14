using System;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Ambient mood of the base (Normal / Night / Storm). Purely visual: it drives walker speed, sheltering
    /// and emote bubbles. Toggle it from the component's context menu, or call <see cref="SetMood"/> from other code.
    /// </summary>
    public class AmbientMood : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private MoodState mood = MoodState.Normal;
        [Tooltip("Where astronauts gather in a storm, e.g. the habitat door.")]
        [SerializeField] private Transform shelter;
        [SerializeField] private MoonWalker[] walkers = Array.Empty<MoonWalker>();

        private float[] nextEmote = Array.Empty<float>();

        public event Action<MoodState> MoodChanged;

        public MoodState Mood => mood;

        private void Start()
        {
            nextEmote = new float[walkers.Length];
            Apply();
        }

        public void SetMood(MoodState newMood)
        {
            if (mood == newMood) return;
            mood = newMood;
            if (Application.isPlaying) Apply();
            MoodChanged?.Invoke(mood);
        }

        [ContextMenu("Mood: Normal")]
        private void SetNormal() => SetMood(MoodState.Normal);

        [ContextMenu("Mood: Night")]
        private void SetNight() => SetMood(MoodState.Night);

        [ContextMenu("Mood: Storm")]
        private void SetStorm() => SetMood(MoodState.Storm);

        private void Apply()
        {
            for (int i = 0; i < walkers.Length; i++)
            {
                MoonWalker walker = walkers[i];
                if (walker == null) continue;

                switch (mood)
                {
                    case MoodState.Night:
                        walker.SpeedMultiplier = config.nightSpeedMultiplier;
                        walker.LeaveShelter();
                        break;
                    case MoodState.Storm:
                        walker.SpeedMultiplier = config.stormSpeedMultiplier;
                        if (shelter != null) walker.GoToShelter(ShelterPoint(i));
                        EmoteBubble.Show(walker.transform, EmoteKind.Alert, config.emoteSeconds);
                        break;
                    default:
                        walker.SpeedMultiplier = 1f;
                        walker.LeaveShelter();
                        break;
                }
                // Stagger the first emote so the bubbles don't all pop on the same frame.
                nextEmote[i] = Time.time + (mood == MoodState.Storm ? config.emoteSeconds : 0.3f) + i * 0.35f;
            }
        }

        private Vector3 ShelterPoint(int index)
        {
            float angle = walkers.Length > 0 ? index * Mathf.PI * 2f / walkers.Length : 0f;
            return shelter.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * config.shelterSpread;
        }

        private void Update()
        {
            for (int i = 0; i < walkers.Length; i++)
            {
                if (walkers[i] == null || Time.time < nextEmote[i]) continue;

                EmoteKind kind;
                Vector2 interval;
                switch (mood)
                {
                    case MoodState.Night:
                        kind = EmoteKind.Sleep;
                        interval = config.sleepEmoteInterval;
                        break;
                    case MoodState.Storm:
                        kind = UnityEngine.Random.value < 0.5f ? EmoteKind.Scared : EmoteKind.Alert;
                        interval = config.stormEmoteInterval;
                        break;
                    default:
                        kind = UnityEngine.Random.value < config.ideaChance ? EmoteKind.Idea : EmoteKind.Happy;
                        interval = config.happyEmoteInterval;
                        break;
                }
                EmoteBubble.Show(walkers[i].transform, kind, config.emoteSeconds);
                nextEmote[i] = Time.time + UnityEngine.Random.Range(interval.x, interval.y);
            }
        }
    }
}
