using ArtemisX.Story;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ArtemisX.UI.Dialogue
{
    /// <summary>Renders speaker portrait/mood, formatted speaker name, typewriter text, and choice buttons for DialogueRunner.</summary>
    public class DialogueBoxView : MonoBehaviour
    {
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private GameObject dialogueRoot;
        [SerializeField] private UnityEngine.UI.Image portraitImage;
        [SerializeField] private UnityEngine.UI.Graphic portraitFrame;
        [SerializeField] private UnityEngine.UI.Graphic speakerNameBanner;
        [SerializeField] private UnityEngine.UI.Text speakerNameLabel;
        [SerializeField] private UnityEngine.UI.Text bodyTextLabel;
        [SerializeField] private GameObject continuePrompt;
        [SerializeField] private GameObject choicesRoot;
        [SerializeField] private DialogueChoiceButtonView[] choiceButtons;
        [SerializeField] private UnityEngine.UI.Button[] advanceClickTargets;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip advanceClickSound;

        private static readonly Color DefaultAccentColor = new Color(0.24f, 0.72f, 0.96f, 1f);

        private void Awake()
        {
            if (advanceClickTargets == null) return;

            for (int i = 0; i < advanceClickTargets.Length; i++)
            {
                if (advanceClickTargets[i] != null)
                    advanceClickTargets[i].onClick.AddListener(OnAdvanceClicked);
            }
        }

        private void OnDestroy()
        {
            if (advanceClickTargets == null) return;

            for (int i = 0; i < advanceClickTargets.Length; i++)
            {
                if (advanceClickTargets[i] != null)
                    advanceClickTargets[i].onClick.RemoveListener(OnAdvanceClicked);
            }
        }

        private void OnEnable()
        {
            Subscribe();
            SyncWithRunner();
        }

        private void Start() => SyncWithRunner();

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (runner == null || !runner.IsOpen) return;

            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                runner.AdvanceOrSkip();
        }

        private void Subscribe()
        {
            if (runner == null) return;

            runner.DialogueStarted += OnDialogueStarted;
            runner.LineShown += OnLineShown;
            runner.VisibleTextChanged += OnVisibleTextChanged;
            runner.ChoicesPresented += OnChoicesPresented;
            runner.DialogueFinished += OnDialogueFinished;
            runner.StepFeedbackTriggered += OnStepFeedbackTriggered;
        }

        private void Unsubscribe()
        {
            if (runner == null) return;

            runner.DialogueStarted -= OnDialogueStarted;
            runner.LineShown -= OnLineShown;
            runner.VisibleTextChanged -= OnVisibleTextChanged;
            runner.ChoicesPresented -= OnChoicesPresented;
            runner.DialogueFinished -= OnDialogueFinished;
            runner.StepFeedbackTriggered -= OnStepFeedbackTriggered;
        }

        private void SyncWithRunner()
        {
            if (runner == null || !runner.IsOpen || runner.CurrentSequence == null)
            {
                HideChoices();
                if (continuePrompt != null) continuePrompt.SetActive(false);
                if (dialogueRoot != null) dialogueRoot.SetActive(false);
                return;
            }

            if (dialogueRoot != null) dialogueRoot.SetActive(true);
            ClearSelection();

            var seq = runner.CurrentSequence;
            int idx = runner.CurrentLineIndex;
            if (seq.lines != null && idx >= 0 && idx < seq.lines.Length)
                ApplyLineMetadata(seq.lines[idx]);

            OnVisibleTextChanged(runner.CurrentVisibleText);
            if (runner.IsWaitingForChoice && seq.HasChoices)
                OnChoicesPresented(seq.choices);
            else
                HideChoices();
        }

        private void OnAdvanceClicked()
        {
            if (runner != null && runner.IsOpen)
                runner.AdvanceOrSkip();
        }

        private void OnDialogueStarted(DialogueSequence _)
        {
            if (dialogueRoot != null) dialogueRoot.SetActive(true);
            ClearSelection();
        }

        private void OnLineShown(DialogueSequence _, int __, DialogueSequence.DialogueLine line)
        {
            HideChoices();
            ApplyLineMetadata(line);
        }

        private void ApplyLineMetadata(DialogueSequence.DialogueLine line)
        {
            var speaker = line.speaker;
            string playerName = runner != null ? runner.PlayerName : null;

            if (speakerNameLabel != null)
                speakerNameLabel.text = speaker != null ? speaker.GetDisplayName(playerName) : string.Empty;

            if (portraitImage != null)
            {
                portraitImage.sprite = speaker != null ? speaker.GetPortrait(line.mood) : null;
                portraitImage.enabled = portraitImage.sprite != null;
            }

            Color accent = speaker != null ? speaker.accentColor : DefaultAccentColor;
            if (portraitFrame != null) portraitFrame.color = accent;
            if (speakerNameBanner != null) speakerNameBanner.color = accent;
        }

        private void OnVisibleTextChanged(string visibleText)
        {
            if (bodyTextLabel != null)
                bodyTextLabel.text = visibleText ?? string.Empty;

            if (continuePrompt != null)
            {
                bool readyToAdvance = runner != null && runner.IsOpen && !runner.IsTyping && !runner.IsWaitingForChoice;
                continuePrompt.SetActive(readyToAdvance);
            }
        }

        private void OnChoicesPresented(DialogueSequence.DialogueChoice[] choices)
        {
            ClearSelection();
            if (continuePrompt != null) continuePrompt.SetActive(false);

            if (choicesRoot != null) choicesRoot.SetActive(true);

            int choiceCount = choices != null ? choices.Length : 0;
            if (choiceButtons == null) return;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                var btn = choiceButtons[i];
                if (btn == null) continue;

                if (i < choiceCount)
                {
                    string formattedLabel = runner != null ? runner.FormatText(choices[i].label) : choices[i].label;
                    btn.Bind(choices[i].choiceId, formattedLabel, HandleChoiceSelected);
                }
                else
                {
                    btn.Hide();
                }
            }
        }

        private void HandleChoiceSelected(string choiceId)
        {
            if (runner != null)
                runner.SelectChoice(choiceId);
        }

        private void OnDialogueFinished(DialogueSequence _)
        {
            HideChoices();
            if (continuePrompt != null) continuePrompt.SetActive(false);
            if (dialogueRoot != null) dialogueRoot.SetActive(false);
        }

        private void OnStepFeedbackTriggered()
        {
            if (ArtemisX.Audio.AudioManager.Instance != null) return; // AudioManager plays UiClick for this step
            if (audioSource != null && advanceClickSound != null)
                audioSource.PlayOneShot(advanceClickSound);
        }

        private void HideChoices()
        {
            if (choicesRoot != null) choicesRoot.SetActive(false);
            if (choiceButtons == null) return;

            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                    choiceButtons[i].Hide();
            }
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
