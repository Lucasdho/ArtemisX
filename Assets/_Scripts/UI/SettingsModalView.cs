using ArtemisX.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// HUD gear button + Settings modal: sound on/off (saved in PlayerPrefs) and Replay tutorial.
    /// Pauses the game (timeScale 0) while open; closes with X, backdrop click or Esc.
    /// </summary>
    public class SettingsModalView : MonoBehaviour
    {
        public const string SoundPrefKey = "settings_sound_on";

        [Header("Gear button")]
        [SerializeField] private Button openButton;
        [SerializeField] private CanvasGroup openButtonGroup;

        [Header("Modal")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform card;
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button closeButton;

        [Header("Sound row")]
        [SerializeField] private Button soundToggle;
        [SerializeField] private Image soundToggleImage;
        [SerializeField] private Text soundToggleLabel;
        [SerializeField] private Outline soundLabelOutline;
        [SerializeField] private Shadow soundLabelShadow;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOnIcon;
        [SerializeField] private Sprite soundOffIcon;
        [SerializeField] private Sprite toggleOnSprite;
        [SerializeField] private Sprite toggleOnPressedSprite;
        [SerializeField] private Sprite toggleOffSprite;
        [SerializeField] private Sprite toggleOffPressedSprite;
        [SerializeField] private Color inkColor = new Color(0.016f, 0.016f, 0.208f, 1f);

        [Header("Replay tutorial")]
        [SerializeField] private Button replayButton;
        [SerializeField] private Day1Onboarding onboarding;

        [Header("Visibility")]
        [SerializeField] private GameplayHud hud;
        [SerializeField] private EndScreenView endScreen;
        [SerializeField] private float popSeconds = 0.12f;
        [SerializeField] private float popStartScale = 0.9f;

        private float previousTimeScale = 1f;
        private float popElapsed;

        public bool IsOpen => root != null && root.activeSelf;
        public static bool SoundOn => PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;

        private void Awake()
        {
            AudioListener.volume = SoundOn ? 1f : 0f;
            if (openButton != null) openButton.onClick.AddListener(Open);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (soundToggle != null) soundToggle.onClick.AddListener(ToggleSound);
            if (replayButton != null) replayButton.onClick.AddListener(ReplayTutorial);
            if (root != null) root.SetActive(false);
        }

        private void Start()
        {
            // AudioManager.Awake resets AudioListener.volume from its config, so apply the saved choice after it.
            ApplySound(SoundOn);
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (backdropButton != null) backdropButton.onClick.RemoveListener(Close);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (soundToggle != null) soundToggle.onClick.RemoveListener(ToggleSound);
            if (replayButton != null) replayButton.onClick.RemoveListener(ReplayTutorial);
            if (IsOpen) Time.timeScale = previousTimeScale;
        }

        private void Update()
        {
            bool gameplay = (hud == null || !hud.Cinematic) && (endScreen == null || !endScreen.IsOpen);
            if (openButtonGroup != null)
            {
                openButtonGroup.alpha = gameplay ? 1f : 0f;
                openButtonGroup.blocksRaycasts = gameplay;
                openButtonGroup.interactable = gameplay;
            }
            if (IsOpen && !gameplay) Close();
            if (!IsOpen) return;

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (card != null && popElapsed < popSeconds)
            {
                popElapsed += Time.unscaledDeltaTime;
                float k = popSeconds > 0f ? Mathf.Clamp01(popElapsed / popSeconds) : 1f;
                float s = Mathf.Lerp(popStartScale, 1f, 1f - (1f - k) * (1f - k));
                card.localScale = new Vector3(s, s, 1f);
            }
        }

        public void Open()
        {
            if (IsOpen || root == null) return;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            RefreshSound();
            popElapsed = 0f;
            if (card != null) card.localScale = new Vector3(popStartScale, popStartScale, 1f);
            root.SetActive(true);
            AudioManager.Play(SoundCue.UiOpen);
        }

        public void Close()
        {
            if (!IsOpen) return;
            root.SetActive(false);
            if (card != null) card.localScale = Vector3.one;
            Time.timeScale = previousTimeScale;
            AudioManager.Play(SoundCue.UiClose);
        }

        public void ToggleSound()
        {
            bool on = !SoundOn;
            PlayerPrefs.SetInt(SoundPrefKey, on ? 1 : 0);
            PlayerPrefs.Save();
            ApplySound(on);
            if (on) AudioManager.Play(SoundCue.ToggleSwitch);
        }

        public void ReplayTutorial()
        {
            Close();
            if (onboarding != null) onboarding.RestartTutorial();
        }

        private void ApplySound(bool on)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetMuted(!on);
            AudioListener.volume = on ? 1f : 0f;
            RefreshSound();
        }

        private void RefreshSound()
        {
            bool on = SoundOn;
            if (soundToggleImage != null) soundToggleImage.sprite = on ? toggleOnSprite : toggleOffSprite;
            if (soundToggle != null)
            {
                var state = soundToggle.spriteState;
                state.pressedSprite = on ? toggleOnPressedSprite : toggleOffPressedSprite;
                soundToggle.spriteState = state;
            }
            if (soundToggleLabel != null)
            {
                soundToggleLabel.text = on ? "ON" : "OFF";
                soundToggleLabel.color = on ? Color.white : inkColor;
            }
            if (soundLabelOutline != null) soundLabelOutline.enabled = on;
            if (soundLabelShadow != null) soundLabelShadow.enabled = on;
            if (soundIcon != null && soundOnIcon != null && soundOffIcon != null) soundIcon.sprite = on ? soundOnIcon : soundOffIcon;
        }
    }
}
