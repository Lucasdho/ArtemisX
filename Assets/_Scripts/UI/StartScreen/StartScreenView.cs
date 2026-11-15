using System;
using ArtemisX.Story;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.StartScreen
{
    /// <summary>
    /// Controller for StartScreenCanvas.prefab: manages 4 astronaut selector cards,
    /// 12-char nickname InputField with client-side profanity/PII filtering, and Mission Launch handoff.
    /// </summary>
    public class StartScreenView : MonoBehaviour
    {
        [SerializeField] private PlayerRunProfile profile;
        [SerializeField] private UIThemeConfig theme;
        [SerializeField] private DialogueRunner dialogueRunner;
        [SerializeField] private GameObject overlayRoot;
        [SerializeField] private AstronautCardView[] astronautCards;
        [SerializeField] private InputField nicknameInputField;
        [SerializeField] private Text validationHintLabel;
        [SerializeField] private Button launchMissionButton;
        [SerializeField] private Text selectedSummaryLabel;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip cardSelectClip;
        [SerializeField] private AudioClip launchConfirmClip;

        /// <summary>Raised when the player clicks LAUNCH MISSION: (selectedCharacterIndex, sanitizedNickname).</summary>
        public event Action<int, string> OnMissionLaunchRequested;

        private int currentCharacterIndex;

        public PlayerRunProfile Profile => profile;
        public int SelectedCharacterIndex => currentCharacterIndex;
        public string CurrentNickname => nicknameInputField != null ? nicknameInputField.text : string.Empty;
        public bool IsOpen => overlayRoot != null ? overlayRoot.activeSelf : gameObject.activeSelf;

        private void Awake()
        {
            if (nicknameInputField != null)
            {
                nicknameInputField.characterLimit = PlayerRunProfile.MaxNicknameLength;
                nicknameInputField.onValueChanged.AddListener(HandleNicknameChanged);
                nicknameInputField.onEndEdit.AddListener(HandleNicknameEndEdit);
            }

            if (launchMissionButton != null)
                launchMissionButton.onClick.AddListener(LaunchMission);

            InitializeFromProfile();
        }

        private void OnDestroy()
        {
            if (nicknameInputField != null)
            {
                nicknameInputField.onValueChanged.RemoveListener(HandleNicknameChanged);
                nicknameInputField.onEndEdit.RemoveListener(HandleNicknameEndEdit);
            }

            if (launchMissionButton != null)
                launchMissionButton.onClick.RemoveListener(LaunchMission);
        }

        private void InitializeFromProfile()
        {
            if (astronautCards != null)
            {
                for (int i = 0; i < astronautCards.Length; i++)
                {
                    if (astronautCards[i] == null) continue;
                    var entry = profile != null
                        ? profile.GetAstronaut(i)
                        : new PlayerRunProfile.AstronautEntry
                        {
                            index = i,
                            characterId = "astronaut_" + i,
                            callsign = "ASTRO " + (i + 1),
                            roleTitle = "Junior Astronaut",
                            accentColor = new Color(0.220f, 0.741f, 0.973f, 1f)
                        };
                    astronautCards[i].Bind(entry, SelectAstronaut);
                }
            }

            int startIndex = profile != null ? profile.selectedCharacter : 0;
            SelectAstronaut(startIndex, playSound: false);

            string initialName = profile != null && !string.IsNullOrWhiteSpace(profile.playerNickname)
                ? profile.playerNickname
                : "Commander";

            if (nicknameInputField != null)
                nicknameInputField.SetTextWithoutNotify(initialName);

            UpdateValidationHint(initialName, applySanitizedToField: false);
        }

        /// <summary>Selects astronaut 0..3 and updates card highlight states.</summary>
        public void SelectAstronaut(int index) => SelectAstronaut(index, playSound: true);

        private void SelectAstronaut(int index, bool playSound)
        {
            int count = astronautCards != null && astronautCards.Length > 0 ? astronautCards.Length : PlayerRunProfile.RosterCount;
            currentCharacterIndex = Mathf.Clamp(index, 0, count - 1);

            if (astronautCards != null)
            {
                for (int i = 0; i < astronautCards.Length; i++)
                {
                    if (astronautCards[i] != null)
                        astronautCards[i].SetSelected(i == currentCharacterIndex);
                }
            }

            if (playSound && audioSource != null && cardSelectClip != null)
                audioSource.PlayOneShot(cardSelectClip);

            RefreshSummaryLabel();
        }

        /// <summary>Sets the nickname input text programmatically (for tests or presets) and validates it.</summary>
        public void SetNicknameInput(string rawNickname)
        {
            if (nicknameInputField != null)
                nicknameInputField.text = rawNickname ?? string.Empty;
            else
                UpdateValidationHint(rawNickname, applySanitizedToField: false);
        }

        private void HandleNicknameChanged(string newText)
        {
            if (newText != null && newText.Length > PlayerRunProfile.MaxNicknameLength && nicknameInputField != null)
            {
                string clamped = newText.Substring(0, PlayerRunProfile.MaxNicknameLength);
                nicknameInputField.SetTextWithoutNotify(clamped);
                newText = clamped;
            }

            UpdateValidationHint(newText, applySanitizedToField: false);
            RefreshSummaryLabel();
        }

        private void HandleNicknameEndEdit(string finalText)
        {
            UpdateValidationHint(finalText, applySanitizedToField: true);
            RefreshSummaryLabel();
        }

        private bool UpdateValidationHint(string candidate, out string sanitized)
        {
            bool clean = true;
            string hint = "Callsign ready for launch (max 12 chars).";
            sanitized = "Commander";

            if (profile != null)
            {
                clean = profile.ValidateAndSanitizeNickname(candidate, out sanitized, out hint);
            }
            else
            {
                sanitized = NicknameFilter.Sanitize(candidate, "Commander", PlayerRunProfile.MaxNicknameLength);
                clean = !string.IsNullOrWhiteSpace(candidate) && string.Equals(candidate.Trim(), sanitized, StringComparison.Ordinal);
                if (!clean)
                    hint = $"Adjusted callsign to \"{sanitized}\".";
            }

            if (validationHintLabel != null)
            {
                validationHintLabel.text = hint;
                validationHintLabel.color = clean
                    ? new Color(0.337f, 0.396f, 0.498f, 1f)
                    : new Color(1f, 0.353f, 0.122f, 1f);
            }

            return clean;
        }

        private void UpdateValidationHint(string candidate, bool applySanitizedToField)
        {
            bool clean = UpdateValidationHint(candidate, out string sanitized);
            if (!clean && applySanitizedToField && nicknameInputField != null && !string.IsNullOrWhiteSpace(candidate))
            {
                nicknameInputField.SetTextWithoutNotify(sanitized);
            }
        }

        private void RefreshSummaryLabel()
        {
            if (selectedSummaryLabel == null) return;
            var astro = profile != null ? profile.GetAstronaut(currentCharacterIndex) : default;
            string callsign = !string.IsNullOrEmpty(astro.callsign) ? astro.callsign : ("ASTRO " + (currentCharacterIndex + 1));
            string role = !string.IsNullOrEmpty(astro.roleTitle) ? astro.roleTitle : "Junior Astronaut";
            selectedSummaryLabel.text = $"CREW: {callsign} ({role})";
        }

        /// <summary>
        /// Validates and saves selectedCharacter + playerNickname into PlayerRunProfile,
        /// updates DialogueRunner if wired, fires OnMissionLaunchRequested, and closes the Start overlay.
        /// </summary>
        public void LaunchMission()
        {
            string rawName = nicknameInputField != null ? nicknameInputField.text : (profile != null ? profile.playerNickname : "Commander");
            string sanitizedName;
            string hint;

            if (profile != null)
            {
                sanitizedName = profile.ApplySelection(currentCharacterIndex, rawName, out hint);
            }
            else
            {
                sanitizedName = NicknameFilter.Sanitize(rawName, "Commander", PlayerRunProfile.MaxNicknameLength);
                hint = $"Launched as {sanitizedName}.";
            }

            if (nicknameInputField != null)
                nicknameInputField.SetTextWithoutNotify(sanitizedName);

            if (validationHintLabel != null)
                validationHintLabel.text = hint;

            if (dialogueRunner != null)
            {
                if (profile != null)
                    dialogueRunner.SetPlayerProfile(profile);
                else
                    dialogueRunner.SetPlayerProfile(sanitizedName, currentCharacterIndex);
            }

            if (audioSource != null && launchConfirmClip != null)
                audioSource.PlayOneShot(launchConfirmClip);

            OnMissionLaunchRequested?.Invoke(currentCharacterIndex, sanitizedName);

            if (overlayRoot != null)
                overlayRoot.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        /// <summary>Re-opens the Start Screen overlay.</summary>
        public void Show()
        {
            if (overlayRoot != null)
                overlayRoot.SetActive(true);
            gameObject.SetActive(true);
            InitializeFromProfile();
        }
    }
}
