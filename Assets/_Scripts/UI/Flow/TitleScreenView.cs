using System;
using ArtemisX.Audio;
using ArtemisX.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Title screen in front of the game flow: slow camera orbit around the base, "ARTEMIS X", PLAY and a Settings gear.
    /// GameFlowController calls Show(onPlay); PLAY (or Enter/Space) restores the camera, hides the title and continues.
    /// </summary>
    public class TitleScreenView : MonoBehaviour
    {
        [SerializeField] private ScreensConfig config;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform titleBlock;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private Button playButton;
        [SerializeField] private Text playLabel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsModalView settings;
        [Tooltip("The camera rig the main camera follows (same one GameFlowController moves).")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private CameraPanController panController;

        private Action onPlay;
        private Vector3 homePosition;
        private Quaternion homeRotation;
        private bool panWasEnabled;
        private float yaw;
        private float elapsed;
        private Vector2 titleRestPos;

        public bool IsShowing => root != null && root.activeSelf;

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (titleBlock != null) titleRestPos = titleBlock.anchoredPosition;
            if (config != null)
            {
                if (titleLabel != null) titleLabel.text = config.titleName;
                if (subtitleLabel != null) subtitleLabel.text = config.titleSubtitle;
                if (playLabel != null) playLabel.text = config.titlePlayLabel;
            }
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(Play);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        }

        public void Show(Action onPlayPressed)
        {
            onPlay = onPlayPressed;
            elapsed = 0f;
            if (cameraRig != null)
            {
                homePosition = cameraRig.position;
                homeRotation = cameraRig.rotation;
                yaw = homeRotation.eulerAngles.y;
            }
            if (panController != null)
            {
                panWasEnabled = panController.enabled;
                panController.enabled = false;
            }
            if (settings != null)
            {
                settings.TitleMode = true;
                settings.SetReplayVisible(false);
            }
            if (root != null) root.SetActive(true);
            UpdateCamera(0f);
        }

        private void Update()
        {
            if (!IsShowing) return;
            float dt = Time.unscaledDeltaTime;
            elapsed += dt;
            UpdateCamera(dt);

            if (config != null)
            {
                if (titleBlock != null)
                    titleBlock.anchoredPosition = titleRestPos + Vector2.up * (config.titleFloatPixels * Mathf.Sin(elapsed * config.titleFloatHz * 2f * Mathf.PI));
                if (playButton != null)
                {
                    float k = 0.5f - 0.5f * Mathf.Cos(elapsed * config.titlePlayPulseHz * 2f * Mathf.PI);
                    float s = Mathf.Lerp(1f, config.titlePlayPulseScale, k);
                    playButton.transform.localScale = new Vector3(s, s, 1f);
                }
            }

            // Esc is handled by the Settings modal itself; Enter/Space only start the game while it is closed.
            if (settings != null && settings.IsOpen) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                Play();
        }

        private void UpdateCamera(float dt)
        {
            if (cameraRig == null || config == null) return;
            yaw += config.titleOrbitDegreesPerSecond * dt;
            var rot = Quaternion.Euler(config.titleOrbitPitch, yaw, 0f);
            cameraRig.SetPositionAndRotation(config.titleOrbitCenter - rot * Vector3.forward * config.titleOrbitDistance, rot);
        }

        private void OpenSettings()
        {
            AudioManager.Play(SoundCue.UiClick);
            if (settings != null) settings.Open();
        }

        public void Play()
        {
            if (!IsShowing) return;
            AudioManager.Play(SoundCue.UiClick);
            if (settings != null)
            {
                if (settings.IsOpen) settings.Close();
                settings.TitleMode = false;
                settings.SetReplayVisible(true);
            }
            if (cameraRig != null) cameraRig.SetPositionAndRotation(homePosition, homeRotation);
            if (panController != null) panController.enabled = panWasEnabled;
            if (playButton != null) playButton.transform.localScale = Vector3.one;
            if (root != null) root.SetActive(false);
            var callback = onPlay;
            onPlay = null;
            callback?.Invoke();
        }
    }
}
