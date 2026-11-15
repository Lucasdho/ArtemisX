using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtemisX.Audio
{
    /// <summary>WebGL-safe pooled SFX player for UI and simulation SoundCues.</summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        public static AudioManager Instance => instance != null ? instance : null;

        [SerializeField] private AudioConfig config;
        [SerializeField] private AudioSource primarySource;
        [SerializeField, Range(1, 16)] private int poolSize = 6;
        [SerializeField] private bool persistAcrossScenes;

        private readonly List<AudioSource> sourcePool = new List<AudioSource>();
        private int nextPoolIndex;
        private bool isMuted;
        private float masterVolume = 1f;

        public event Action<SoundCue, AudioClip> CuePlayed;
        public event Action<bool> MuteChanged;

        public AudioConfig Config => config;
        public bool IsMuted => isMuted;
        public float MasterVolume => masterVolume;
        public SoundCue? LastPlayedCue { get; private set; }
        public AudioClip LastPlayedClip { get; private set; }
        public int PlayCount { get; private set; }

        /// <summary>Static helper to trigger a SoundCue if an AudioManager is present in the scene.</summary>
        public static void Play(SoundCue cue)
        {
            if (Instance != null)
                Instance.PlayCue(cue);
        }

        public void Initialize(AudioConfig audioConfig)
        {
            instance = this;
            config = audioConfig;
            SyncFromConfig();
            EnsurePool();
            PreloadClips();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                if (persistAcrossScenes)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            instance = this;
            if (persistAcrossScenes && transform.parent == null)
                DontDestroyOnLoad(gameObject);

            SyncFromConfig();
            EnsurePool();
            PreloadClips();
        }

        private void OnEnable()
        {
            if (Instance == null)
                instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>Plays a SoundCue using a pooled 2D AudioSource with randomized pitch.</summary>
        public void PlayCue(SoundCue cue)
        {
            if (config == null || !config.TryGetEntry(cue, out var entry) || entry.clip == null)
                return;

            LastPlayedCue = cue;
            LastPlayedClip = entry.clip;

            if (isMuted)
                return;

            float volume = Mathf.Clamp01(entry.volume * masterVolume);
            if (volume <= 0f)
                return;

            EnsurePool();
            AudioSource source = GetNextSource();
            if (source == null)
                return;

            float pMin = entry.pitchMin > 0f ? entry.pitchMin : 1f;
            float pMax = entry.pitchMax > 0f ? entry.pitchMax : 1f;
            if (pMax < pMin)
            {
                float temp = pMin;
                pMin = pMax;
                pMax = temp;
            }

            source.pitch = Mathf.Approximately(pMin, pMax) ? pMin : UnityEngine.Random.Range(pMin, pMax);
            source.mute = isMuted;
            source.PlayOneShot(entry.clip, volume);
            PlayCount++;
            CuePlayed?.Invoke(cue, entry.clip);
        }

        public void PlayClick() => PlayCue(SoundCue.UiClick);
        public void PlayConfirm() => PlayCue(SoundCue.BuildSuccess);
        public void PlayCancel() => PlayCue(SoundCue.UiClose);
        public void PlayOpen() => PlayCue(SoundCue.UiOpen);
        public void PlayBlocked() => PlayCue(SoundCue.ActionBlocked);

        public void ToggleMute() => SetMuted(!isMuted);

        public void SetMuted(bool muted)
        {
            if (isMuted == muted)
                return;

            isMuted = muted;
            for (int i = 0; i < sourcePool.Count; i++)
            {
                if (sourcePool[i] != null)
                {
                    sourcePool[i].mute = isMuted;
                    if (isMuted && sourcePool[i].isPlaying)
                        sourcePool[i].Stop();
                }
            }

            MuteChanged?.Invoke(isMuted);
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        private void SyncFromConfig()
        {
            if (config == null)
                return;

            masterVolume = Mathf.Clamp01(config.masterVolume);
            isMuted = config.muted;
        }

        private void EnsurePool()
        {
            sourcePool.RemoveAll(s => s == null);

            if (primarySource == null)
                primarySource = GetComponent<AudioSource>();

            if (primarySource == null)
                primarySource = gameObject.AddComponent<AudioSource>();

            ConfigureSource(primarySource);
            if (!sourcePool.Contains(primarySource))
                sourcePool.Add(primarySource);

            int targetCount = Mathf.Max(1, poolSize);
            while (sourcePool.Count < targetCount)
            {
                AudioSource extra = gameObject.AddComponent<AudioSource>();
                ConfigureSource(extra);
                sourcePool.Add(extra);
            }
        }

        private void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.mute = isMuted;
        }

        private AudioSource GetNextSource()
        {
            if (sourcePool.Count == 0)
                return null;

            for (int i = 0; i < sourcePool.Count; i++)
            {
                int idx = (nextPoolIndex + i) % sourcePool.Count;
                AudioSource candidate = sourcePool[idx];
                if (candidate != null && !candidate.isPlaying)
                {
                    nextPoolIndex = (idx + 1) % sourcePool.Count;
                    return candidate;
                }
            }

            AudioSource fallback = sourcePool[nextPoolIndex % sourcePool.Count];
            nextPoolIndex = (nextPoolIndex + 1) % sourcePool.Count;
            return fallback;
        }

        private void PreloadClips()
        {
            if (config == null || config.entries == null)
                return;

            for (int i = 0; i < config.entries.Length; i++)
            {
                AudioClip clip = config.entries[i].clip;
                if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                    clip.LoadAudioData();
            }
        }
    }
}
