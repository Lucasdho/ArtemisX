using System;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Ambient mood of the base (Normal / Night / Storm). Purely visual: it drives walker speed, sheltering,
    /// the rover's run to its garage and the emote bubbles. Toggle it from the component's context menu,
    /// or call <see cref="SetMood"/> and <see cref="Celebrate"/> from other code.
    /// </summary>
    public class AmbientMood : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private MoodState mood = MoodState.Normal;
        [Tooltip("Where astronauts gather in a storm, e.g. the habitat door.")]
        [SerializeField] private Transform shelter;
        [SerializeField] private MoonWalker[] walkers = Array.Empty<MoonWalker>();
        [SerializeField] private RoverDriver[] rovers = Array.Empty<RoverDriver>();
        [Tooltip("Where rovers park in a storm, e.g. the rover garage slot.")]
        [SerializeField] private Transform garage;

        private float[] nextEmote = Array.Empty<float>();
        private float[] nextGreet = Array.Empty<float>();

        public event Action<MoodState> MoodChanged;

        public MoodState Mood => mood;

        private void Start()
        {
            nextEmote = new float[walkers.Length];
            nextGreet = new float[walkers.Length * walkers.Length];
            Apply();
        }

        public void SetMood(MoodState newMood)
        {
            if (mood == newMood) return;
            mood = newMood;
            if (Application.isPlaying) Apply();
            MoodChanged?.Invoke(mood);
        }

        /// <summary>Good fortune: every astronaut cheers with a happy bubble.</summary>
        [ContextMenu("Good fortune")]
        public void Celebrate()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < walkers.Length; i++)
            {
                if (walkers[i] == null) continue;
                walkers[i].Cheer();
                EmoteBubble.Show(walkers[i].EmoteAnchor, EmoteKind.Happy, config.emoteSeconds);
            }
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
                        EmoteBubble.Show(walker.EmoteAnchor, EmoteKind.Alert, config.emoteSeconds);
                        break;
                    default:
                        walker.SpeedMultiplier = 1f;
                        walker.LeaveShelter();
                        break;
                }
                // Stagger the first emote so the bubbles don't all pop on the same frame.
                nextEmote[i] = Time.time + (mood == MoodState.Storm ? config.emoteSeconds : 0.3f) + i * 0.35f;
            }

            for (int i = 0; i < rovers.Length; i++)
            {
                if (rovers[i] == null) continue;
                if (mood == MoodState.Storm && garage != null) rovers[i].GoToGarage(garage.position);
                else rovers[i].ResumeLoop();
            }
        }

        private Vector3 ShelterPoint(int index)
        {
            float angle = walkers.Length > 0 ? index * Mathf.PI * 2f / walkers.Length : 0f;
            return shelter.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * config.shelterSpread;
        }

        private void Update()
        {
            if (mood == MoodState.Normal)
            {
                Greetings();
                return;
            }

            for (int i = 0; i < walkers.Length; i++)
            {
                if (walkers[i] == null || Time.time < nextEmote[i]) continue;

                bool night = mood == MoodState.Night;
                EmoteKind kind = night ? EmoteKind.Sleep : (UnityEngine.Random.value < 0.5f ? EmoteKind.Scared : EmoteKind.Alert);
                Vector2 interval = night ? config.sleepEmoteInterval : config.stormEmoteInterval;
                EmoteBubble.Show(walkers[i].EmoteAnchor, kind, config.emoteSeconds);
                nextEmote[i] = Time.time + UnityEngine.Random.Range(interval.x, interval.y);
            }
        }

        /// <summary>Two astronauts who come close stop, wave and show a greeting bubble.</summary>
        private void Greetings()
        {
            float sqr = config.greetDistance * config.greetDistance;
            for (int i = 0; i < walkers.Length; i++)
            {
                MoonWalker a = walkers[i];
                if (a == null || !a.CanGreet) continue;
                for (int j = i + 1; j < walkers.Length; j++)
                {
                    MoonWalker b = walkers[j];
                    int pair = i * walkers.Length + j;
                    if (b == null || !b.CanGreet || Time.time < nextGreet[pair]) continue;
                    if ((a.transform.position - b.transform.position).sqrMagnitude > sqr) continue;

                    nextGreet[pair] = Time.time + config.greetCooldownSeconds;
                    Vector3 aPos = a.transform.position, bPos = b.transform.position;
                    a.Greet(bPos);
                    b.Greet(aPos);
                    EmoteBubble.Show(a.EmoteAnchor, EmoteKind.Greet, config.emoteSeconds);
                    EmoteBubble.Show(b.EmoteAnchor, EmoteKind.Greet, config.emoteSeconds);
                    break;
                }
            }
        }
    }
}
