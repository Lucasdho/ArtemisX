using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Runs queued dialogue sequences with a skippable typewriter and optional choices.</summary>
    public class DialogueRunner : MonoBehaviour
    {
        private struct QueuedEntry
        {
            public DialogueSequence sequence;
            public Action onFinished;
        }

        [SerializeField] private DialogueConfig config;

        public event Action<DialogueSequence> DialogueStarted;
        public event Action<DialogueSequence, int, DialogueSequence.DialogueLine> LineShown;
        public event Action<DialogueSequence> DialogueFinished;
        public event Action<string, string> ChoiceMade;
        public event Action<string> VisibleTextChanged;
        public event Action<DialogueSequence.DialogueChoice[]> ChoicesPresented;
        public event Action StepFeedbackTriggered;

        public bool IsOpen { get; private set; }
        public bool IsTyping { get; private set; }
        public bool IsWaitingForChoice { get; private set; }
        public DialogueSequence CurrentSequence => currentSequence;
        public int CurrentLineIndex => currentLineIndex;
        public string CurrentVisibleText { get; private set; } = string.Empty;
        public string CurrentFullText { get; private set; } = string.Empty;

        private readonly Queue<QueuedEntry> queue = new Queue<QueuedEntry>();
        private DialogueSequence currentSequence;
        private Action currentOnFinished;
        private int currentLineIndex = -1;
        private float revealedFloat;
        private int revealedChars;
        private float autoAdvanceTimer;
        private float lastInputTime = -10f;

        private float CharactersPerSecond => config != null ? Mathf.Max(1f, config.charactersPerSecond) : 45f;
        private float AutoAdvanceDelay => config != null ? Mathf.Max(0f, config.autoAdvanceDelay) : 0f;
        private float InputCooldown => config != null ? Mathf.Max(0f, config.inputCooldown) : 0.05f;

        private void Update()
        {
            if (!IsOpen || currentSequence == null) return;

            if (IsTyping)
            {
                revealedFloat += Time.unscaledDeltaTime * CharactersPerSecond;
                int nextChars = Mathf.Min(CurrentFullText.Length, Mathf.FloorToInt(revealedFloat));
                if (nextChars != revealedChars)
                {
                    revealedChars = nextChars;
                    CurrentVisibleText = CurrentFullText.Substring(0, revealedChars);
                    VisibleTextChanged?.Invoke(CurrentVisibleText);
                }

                if (revealedChars >= CurrentFullText.Length)
                    FinishTypingCurrentLine();

                return;
            }

            if (!IsWaitingForChoice && AutoAdvanceDelay > 0f)
            {
                autoAdvanceTimer += Time.unscaledDeltaTime;
                if (autoAdvanceTimer >= AutoAdvanceDelay)
                    AdvanceToNextLineOrFinish();
            }
        }

        /// <summary>Queues a dialogue sequence to play immediately or after active/queued sequences.</summary>
        public void Enqueue(DialogueSequence sequence, Action onFinished = null)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            queue.Enqueue(new QueuedEntry { sequence = sequence, onFinished = onFinished });
            if (!IsOpen)
                StartNextFromQueue();
        }

        /// <summary>Completes the typewriter on the first tap, or advances to the next line/end on the next.</summary>
        public void AdvanceOrSkip()
        {
            if (!IsOpen || currentSequence == null) return;
            if (Time.unscaledTime - lastInputTime < InputCooldown) return;
            lastInputTime = Time.unscaledTime;

            if (IsTyping)
            {
                FinishTypingCurrentLine();
                StepFeedbackTriggered?.Invoke();
                return;
            }

            if (IsWaitingForChoice) return;

            StepFeedbackTriggered?.Invoke();
            AdvanceToNextLineOrFinish();
        }

        /// <summary>Records the selected choice, fires ChoiceMade, and closes the current sequence.</summary>
        public void SelectChoice(string choiceId)
        {
            if (!IsOpen || !IsWaitingForChoice || currentSequence == null) return;
            lastInputTime = Time.unscaledTime;

            string sequenceId = string.IsNullOrEmpty(currentSequence.sequenceId)
                ? currentSequence.name
                : currentSequence.sequenceId;

            Debug.Log($"[Dialogue] ChoiceMade: {sequenceId} -> {choiceId}", this);
            StepFeedbackTriggered?.Invoke();
            ChoiceMade?.Invoke(sequenceId, choiceId);
            CompleteCurrentSequence();
        }

        private void StartNextFromQueue()
        {
            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                if (next.sequence == null || next.sequence.lines == null || next.sequence.lines.Length == 0)
                {
                    next.onFinished?.Invoke();
                    continue;
                }

                currentSequence = next.sequence;
                currentOnFinished = next.onFinished;
                IsOpen = true;
                DialogueStarted?.Invoke(currentSequence);
                ShowLine(0);
                return;
            }
        }

        private void ShowLine(int index)
        {
            currentLineIndex = index;
            var line = currentSequence.lines[index];
            CurrentFullText = line.text ?? string.Empty;
            CurrentVisibleText = string.Empty;
            revealedFloat = 0f;
            revealedChars = 0;
            autoAdvanceTimer = 0f;
            IsWaitingForChoice = false;
            IsTyping = CurrentFullText.Length > 0;

            LineShown?.Invoke(currentSequence, index, line);
            VisibleTextChanged?.Invoke(CurrentVisibleText);

            if (!IsTyping)
                FinishTypingCurrentLine();
        }

        private void FinishTypingCurrentLine()
        {
            IsTyping = false;
            revealedChars = CurrentFullText.Length;
            CurrentVisibleText = CurrentFullText;
            VisibleTextChanged?.Invoke(CurrentVisibleText);

            bool isLastLine = currentSequence != null && currentLineIndex >= currentSequence.lines.Length - 1;
            if (isLastLine && currentSequence.HasChoices)
            {
                IsWaitingForChoice = true;
                ChoicesPresented?.Invoke(currentSequence.choices);
            }
        }

        private void AdvanceToNextLineOrFinish()
        {
            int nextIndex = currentLineIndex + 1;
            if (currentSequence != null && currentSequence.lines != null && nextIndex < currentSequence.lines.Length)
                ShowLine(nextIndex);
            else
                CompleteCurrentSequence();
        }

        private void CompleteCurrentSequence()
        {
            var finishedSequence = currentSequence;
            var callback = currentOnFinished;

            currentSequence = null;
            currentOnFinished = null;
            currentLineIndex = -1;
            IsTyping = false;
            IsWaitingForChoice = false;
            IsOpen = false;
            CurrentFullText = string.Empty;
            CurrentVisibleText = string.Empty;

            DialogueFinished?.Invoke(finishedSequence);
            callback?.Invoke();

            if (!IsOpen && queue.Count > 0)
                StartNextFromQueue();
        }
    }
}
