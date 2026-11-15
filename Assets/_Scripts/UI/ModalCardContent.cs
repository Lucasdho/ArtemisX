using System;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>What a ModalCardView shows: banner, title, body, optional resource deltas, Bauzer line and up to two buttons.</summary>
    public struct ModalCardContent
    {
        public string banner;
        public Color bannerColor;
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
    }
}
