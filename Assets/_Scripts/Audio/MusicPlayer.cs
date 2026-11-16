using UnityEngine;

namespace ArtemisX.Audio
{
    /// <summary>
    /// Plays the background track on a loop. Volume fades in at the start of every pass
    /// and fades out over the tail, so the loop point is never a hard cut.
    /// Tunables live in <see cref="AudioConfig"/>.
    /// </summary>
    [RequireComponent(typeof(AudioManager))]
    public class MusicPlayer : MonoBehaviour
    {
        [SerializeField] private AudioManager manager;

        private AudioSource source;
        private bool stopping;
        private float stopFadeStartVolume;
        private float stopFadeElapsed;

        private void Awake()
        {
            if (manager == null)
                manager = GetComponent<AudioManager>();

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
        }

        private void Start()
        {
            AudioConfig config = manager != null ? manager.Config : null;
            if (config == null || config.musicClip == null)
                return;

            source.clip = config.musicClip;
            source.Play();
        }

        private void Update()
        {
            AudioConfig config = manager != null ? manager.Config : null;
            if (config == null || source.clip == null || !source.isPlaying)
                return;

            if (stopping)
            {
                stopFadeElapsed += Time.unscaledDeltaTime;
                float duration = Mathf.Max(0.01f, config.musicFadeOutSeconds);
                source.volume = Mathf.Lerp(stopFadeStartVolume, 0f, Mathf.Clamp01(stopFadeElapsed / duration));
                if (stopFadeElapsed >= duration)
                    source.Stop();
                return;
            }

            float length = source.clip.length;
            float t = source.time;
            float envelope = 1f;

            if (config.musicFadeInSeconds > 0f)
                envelope = Mathf.Min(envelope, Mathf.Clamp01(t / config.musicFadeInSeconds));

            if (config.musicFadeOutSeconds > 0f)
                envelope = Mathf.Min(envelope, Mathf.Clamp01((length - t) / config.musicFadeOutSeconds));

            source.volume = config.musicVolume * manager.MasterVolume * envelope;
        }

        /// <summary>Fades the music out over the configured fade-out time, then stops it.</summary>
        public void FadeOutAndStop()
        {
            if (stopping || !source.isPlaying)
                return;

            stopping = true;
            stopFadeStartVolume = source.volume;
            stopFadeElapsed = 0f;
        }
    }
}
