using System;
using UnityEngine;

namespace ArtemisX.Audio
{
    /// <summary>Interactive audition UI for Dev_Audio.unity with buttons for every SoundCue and a Mute toggle.</summary>
    public class AudioDebugPanel : MonoBehaviour
    {
        [Serializable]
        public struct CueButtonBinding
        {
            public SoundCue cue;
            public UnityEngine.UI.Button button;
        }

        [SerializeField] private AudioManager audioManager;
        [SerializeField] private UnityEngine.UI.Button muteButton;
        [SerializeField] private UnityEngine.UI.Text muteButtonLabel;
        [SerializeField] private UnityEngine.UI.Image muteButtonBackground;
        [SerializeField] private UnityEngine.UI.Text statusLabel;
        [SerializeField] private CueButtonBinding[] cueButtons = Array.Empty<CueButtonBinding>();

        private static readonly Color UnmutedButtonColor = new Color(0.20f, 0.56f, 0.34f, 1f);
        private static readonly Color MutedButtonColor = new Color(0.75f, 0.24f, 0.20f, 1f);

        private void Awake()
        {
            if (audioManager == null)
                audioManager = FindAnyObjectByType<AudioManager>();
        }

        private void OnEnable()
        {
            if (audioManager == null)
                audioManager = FindAnyObjectByType<AudioManager>();

            if (muteButton != null)
                muteButton.onClick.AddListener(OnMuteButtonClicked);

            if (cueButtons != null)
            {
                for (int i = 0; i < cueButtons.Length; i++)
                {
                    SoundCue capturedCue = cueButtons[i].cue;
                    UnityEngine.UI.Button btn = cueButtons[i].button;
                    if (btn != null)
                        btn.onClick.AddListener(() => TriggerCue(capturedCue));
                }
            }

            if (audioManager != null)
            {
                audioManager.MuteChanged += OnMuteChanged;
                audioManager.CuePlayed += OnCuePlayed;
            }

            RefreshMuteVisuals();
            UpdateStatusText("Ready — Click any SoundCue button to audition.");
        }

        private void OnDisable()
        {
            if (muteButton != null)
                muteButton.onClick.RemoveListener(OnMuteButtonClicked);

            if (cueButtons != null)
            {
                for (int i = 0; i < cueButtons.Length; i++)
                {
                    if (cueButtons[i].button != null)
                        cueButtons[i].button.onClick.RemoveAllListeners();
                }
            }

            if (audioManager != null)
            {
                audioManager.MuteChanged -= OnMuteChanged;
                audioManager.CuePlayed -= OnCuePlayed;
            }
        }

        public void TriggerCue(SoundCue cue)
        {
            if (audioManager == null)
                audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindAnyObjectByType<AudioManager>();

            if (audioManager != null)
            {
                audioManager.PlayCue(cue);
                if (audioManager.IsMuted)
                    UpdateStatusText($"Triggered {cue} (MUTED)");
            }
            else
            {
                AudioManager.Play(cue);
                UpdateStatusText($"Triggered {cue} (No AudioManager instance)");
            }
        }

        public void OnMuteButtonClicked()
        {
            if (audioManager == null)
                audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindAnyObjectByType<AudioManager>();

            if (audioManager != null)
                audioManager.ToggleMute();

            RefreshMuteVisuals();
        }

        private void OnMuteChanged(bool isMuted)
        {
            RefreshMuteVisuals();
            UpdateStatusText(isMuted ? "Audio Muted" : "Audio Unmuted");
        }

        private void OnCuePlayed(SoundCue cue, AudioClip clip)
        {
            string clipName = clip != null ? clip.name : "None";
            UpdateStatusText($"Playing: {cue}  ({clipName})");
        }

        private void RefreshMuteVisuals()
        {
            bool muted = audioManager != null && audioManager.IsMuted;
            if (muteButtonLabel != null)
                muteButtonLabel.text = muted ? "MUTE: ON (Click to Unmute)" : "MUTE: OFF (Click to Mute)";
            if (muteButtonBackground != null)
                muteButtonBackground.color = muted ? MutedButtonColor : UnmutedButtonColor;
        }

        private void UpdateStatusText(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
