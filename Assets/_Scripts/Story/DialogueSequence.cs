using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Story
{
    /// <summary>Ordered dialogue lines with optional resource-affecting choices presented on the last line.</summary>
    [CreateAssetMenu(fileName = "DialogueSequence", menuName = "Artemis X/Story/Dialogue Sequence")]
    public class DialogueSequence : ScriptableObject
    {
        [Serializable]
        public struct ResourceDelta
        {
            public ResourceType resource;
            public int delta;
        }

        [Serializable]
        public struct DialogueChoice
        {
            public string choiceId;
            public string label;
            public ResourceDelta[] resourceDeltas;
            public bool fixesBrokenComponent;
            [Tooltip("One-sentence trade-off note used by evening debrief templates.")]
            public string outcomeSummary;
        }

        [Serializable]
        public struct DialogueLine
        {
            public Speaker speaker;
            public SpeakerMood mood;
            [TextArea(2, 4)] public string text;
        }

        public string sequenceId;
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
        [Tooltip("Optional 2 choices displayed after the final line finishes typing.")]
        public DialogueChoice[] choices = Array.Empty<DialogueChoice>();

        public bool HasChoices => choices != null && choices.Length > 0;
    }
}
