using System;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Ordered dialogue lines with optional player choices presented after the last line.</summary>
    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "Artemis X/Story/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        [Serializable]
        public struct DialogueChoice
        {
            public string choiceId;
            public string label;
        }

        [Serializable]
        public struct DialogueLine
        {
            public Speaker speaker;
            [TextArea(2, 4)] public string text;
        }

        public string sequenceId;
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
        [Tooltip("Optional 2-3 choices displayed after the final line finishes typing.")]
        public DialogueChoice[] choices = Array.Empty<DialogueChoice>();

        public bool HasChoices => choices != null && choices.Length > 0;
    }
}
