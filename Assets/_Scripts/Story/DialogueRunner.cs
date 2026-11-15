using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Ordering bucket within a day so queued beats never play out of narrative order.</summary>
    public enum DialogueQueueSlot
    {
        Intro = 0,
        MorningBriefing = 10,
        EventCard = 20,
        SupplyArrival = 30,
        EveningDebrief = 40,
        MissionEnd = 50
    }

    /// <summary>Runs priority-queued dialogue sequences with template substitution and skippable typewriter.</summary>
    public class DialogueRunner : MonoBehaviour
    {
        private struct QueuedEntry
        {
            public DialogueSequence sequence;
            public Action onFinished;
            public DialogueQueueSlot slot;
            public int order;
        }

        [SerializeField] private DialogueConfig config;

        public event Action<DialogueSequence> DialogueStarted;
        public event Action<DialogueSequence, int, DialogueSequence.DialogueLine> LineShown;
        public event Action<DialogueSequence> DialogueFinished;
        public event Action<string, string> ChoiceMade;
        public event Action<string> VisibleTextChanged;
        public event Action<DialogueSequence.DialogueChoice[]> ChoicesPresented;
        public event Action StepFeedbackTriggered;

        public DialogueConfig Config => config;
        public bool IsOpen { get; private set; }
        public bool IsTyping { get; private set; }
        public bool IsWaitingForChoice { get; private set; }
        public DialogueSequence CurrentSequence => currentSequence;
        public int CurrentLineIndex => currentLineIndex;
        public string CurrentVisibleText { get; private set; } = string.Empty;
        public string CurrentFullText { get; private set; } = string.Empty;
        public string PlayerName => ResolvePlayerName();
        public string PlayerCharacter => ResolvePlayerCharacter();

        private readonly List<QueuedEntry> pendingQueue = new List<QueuedEntry>();
        private readonly Dictionary<string, string> templateValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private string playerNameOverride;
        private string playerCharacterOverride;
        private DialogueSequence currentSequence;
        private Action currentOnFinished;
        private int currentLineIndex = -1;
        private int nextInsertOrder;
        private float revealedFloat;
        private int revealedChars;
        private float autoAdvanceTimer;
        private float lastInputTime = -10f;

        private float CharactersPerSecond => config != null ? Mathf.Max(1f, config.charactersPerSecond) : 45f;
        private float AutoAdvanceDelay => config != null ? Mathf.Max(0f, config.autoAdvanceDelay) : 0f;
        private float InputCooldown => config != null ? Mathf.Max(0f, config.inputCooldown) : 0.05f;

        private void Awake()
        {
            SetPlayerName(ResolvePlayerName());
            SetPlayerCharacter(ResolvePlayerCharacter());
        }

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

        /// <summary>Updates the sanitized player nickname substituted into {playerName} and player lines.</summary>
        public void SetPlayerName(string playerName)
        {
            int maxLen = config != null ? config.maxNicknameLength : NicknameFilter.MaxLength;
            string fallback = config != null ? config.defaultPlayerName : "Commander";
            playerNameOverride = NicknameFilter.Sanitize(playerName, fallback, maxLen);
            templateValues["playerName"] = ResolvePlayerName();
        }

        /// <summary>Updates the chosen astronaut identifier used in leaderboard handoff records.</summary>
        public void SetPlayerCharacter(string characterId)
        {
            playerCharacterOverride = string.IsNullOrWhiteSpace(characterId) ? null : characterId.Trim();
        }

        /// <summary>Stores a runtime token substituted into dialogue lines and choice labels.</summary>
        public void SetTemplateValue(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            templateValues[key] = value ?? string.Empty;
        }

        /// <summary>Replaces {token} placeholders in a line or choice label with current runtime values.</summary>
        public string FormatText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText) || rawText.IndexOf('{') < 0)
                return rawText ?? string.Empty;

            if (!templateValues.ContainsKey("playerName"))
                templateValues["playerName"] = ResolvePlayerName();

            var sb = new StringBuilder(rawText.Length + 16);
            int pos = 0;
            while (pos < rawText.Length)
            {
                int open = rawText.IndexOf('{', pos);
                if (open < 0)
                {
                    sb.Append(rawText, pos, rawText.Length - pos);
                    break;
                }

                int close = rawText.IndexOf('}', open + 1);
                if (close < 0)
                {
                    sb.Append(rawText, pos, rawText.Length - pos);
                    break;
                }

                sb.Append(rawText, pos, open - pos);
                string token = rawText.Substring(open + 1, close - open - 1);
                if (templateValues.TryGetValue(token, out string replacement))
                    sb.Append(replacement);
                else
                    sb.Append(rawText, open, close - open + 1);

                pos = close + 1;
            }

            return sb.ToString();
        }

        /// <summary>Queues a sequence without interrupting active dialogue, ordered by narrative slot.</summary>
        public void Enqueue(DialogueSequence sequence, Action onFinished = null, DialogueQueueSlot slot = DialogueQueueSlot.MorningBriefing)
        {
            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            var entry = new QueuedEntry
            {
                sequence = sequence,
                onFinished = onFinished,
                slot = slot,
                order = nextInsertOrder++
            };

            InsertOrdered(entry);
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

        private void InsertOrdered(QueuedEntry entry)
        {
            int insertIndex = pendingQueue.Count;
            for (int i = 0; i < pendingQueue.Count; i++)
            {
                if ((int)entry.slot < (int)pendingQueue[i].slot)
                {
                    insertIndex = i;
                    break;
                }
            }

            pendingQueue.Insert(insertIndex, entry);
        }

        private string ResolvePlayerName()
        {
            int maxLen = config != null ? config.maxNicknameLength : NicknameFilter.MaxLength;
            string fallback = NicknameFilter.Sanitize(
                config != null ? config.defaultPlayerName : "Commander",
                "Commander",
                maxLen);

            if (!string.IsNullOrWhiteSpace(playerNameOverride))
                return NicknameFilter.Sanitize(playerNameOverride, fallback, maxLen);

            return fallback;
        }

        private string ResolvePlayerCharacter()
        {
            if (!string.IsNullOrWhiteSpace(playerCharacterOverride))
                return playerCharacterOverride;

            if (config != null && !string.IsNullOrWhiteSpace(config.defaultCharacterId))
                return config.defaultCharacterId;

            return "astronaut_1";
        }

        private void StartNextFromQueue()
        {
            while (pendingQueue.Count > 0)
            {
                var next = pendingQueue[0];
                pendingQueue.RemoveAt(0);

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
            CurrentFullText = FormatText(line.text);
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

            if (!IsOpen && pendingQueue.Count > 0)
                StartNextFromQueue();
        }
    }
}
