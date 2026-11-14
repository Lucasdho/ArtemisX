using System.Collections.Generic;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// World-space emote bubbles. Call <c>EmoteBubble.Show(target, kind, seconds)</c> from anywhere;
    /// one bubble per target, pooled.
    /// </summary>
    public class EmoteBubble : MonoBehaviour
    {
        [SerializeField] private VisualsConfig config;
        [SerializeField] private EmoteBubbleView viewPrefab;

        private static EmoteBubble instance;
        private readonly List<EmoteBubbleView> views = new List<EmoteBubbleView>();

        private void Awake() => instance = this;

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        /// <summary>Pops an emote over the target for the given time. Does nothing when no EmoteBubble is in the scene.</summary>
        public static void Show(Transform target, EmoteKind kind, float seconds)
        {
            if (instance != null) instance.ShowOn(target, kind, seconds, instance.config.emoteHeight);
        }

        /// <summary>Same as <see cref="Show(Transform, EmoteKind, float)"/> with a custom height above the target's origin.</summary>
        public static void Show(Transform target, EmoteKind kind, float seconds, float heightOffset)
        {
            if (instance != null) instance.ShowOn(target, kind, seconds, heightOffset);
        }

        private void ShowOn(Transform target, EmoteKind kind, float seconds, float heightOffset)
        {
            if (target == null) return;
            Texture2D texture = config.PickEmote(kind);
            if (texture == null)
            {
                Debug.LogWarning($"No emote texture configured for {kind}.", this);
                return;
            }

            EmoteBubbleView view = null;
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i].Target == target) { view = views[i]; break; }
                if (view == null && views[i].IsFree) view = views[i];
            }
            if (view == null)
            {
                view = Instantiate(viewPrefab, transform);
                views.Add(view);
            }
            view.Play(target, texture, seconds, config.emoteWorldSize, heightOffset, config.emotePopSeconds);
        }
    }
}
