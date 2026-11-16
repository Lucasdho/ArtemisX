using System;
using ArtemisX.Core;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>One choice card shown in the Event Dialog mode of <see cref="ModalCardView"/>.</summary>
    public struct ModalEventChoice
    {
        public Sprite icon;
        public string title;
        public ResourceType costType;
        public int costAmount;
        public Sprite outcomeIcon;
        public string outcomeText;
        public bool outcomeRisky;
        public string buttonLabel;
        public bool enabled;
        public Action action;
    }

    /// <summary>What a ModalCardView shows: banner, title, body, optional resource deltas, Kepler line and up to two buttons or event choice cards.</summary>
    public struct ModalCardContent
    {
        public string banner;
        public Color bannerColor;
        public Sprite eventIcon;
        public string title;
        public string body;
        /// <summary>One value per ResourceType, or null to hide the delta row.</summary>
        public int[] deltas;
        public string bauzerLine;
        public SpeakerMood bauzerMood;
        public string labelA;
        public bool enabledA;
        public Action actionA;
        /// <summary>Leave empty for a single-button card.</summary>
        public string labelB;
        public bool enabledB;
        public Action actionB;
        /// <summary>When non-empty, renders the approved Event Dialog choice cards layout instead of standard report body/buttons.</summary>
        public ModalEventChoice[] choices;
        public string footerHint;
    }
}
