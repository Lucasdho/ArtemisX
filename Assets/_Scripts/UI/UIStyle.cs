using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Colors, font and per-resource look for the HUD. One asset lives in Assets/_Config/UI/.</summary>
    [CreateAssetMenu(fileName = "UIStyle", menuName = "Artemis X/UI Style")]
    public class UIStyle : ScriptableObject
    {
        [Serializable]
        public struct ResourceLook
        {
            public ResourceType type;
            [Tooltip("Pill background.")] public Color tint;
            [Tooltip("Icon bubble.")] public Color accent;
            [Tooltip("Resource icon shown in the bubble.")] public Sprite icon;
            [Tooltip("Fallback letter when no icon is assigned.")] public string letter;
        }

        [Header("Text")]
        public Font font;
        public Color textColor = new Color(0.17f, 0.18f, 0.21f);
        public Color mutedTextColor = new Color(0.48f, 0.52f, 0.57f);

        [Header("Delta")]
        public Color positiveColor = new Color(0.18f, 0.55f, 0.24f);
        public Color negativeColor = new Color(0.75f, 0.22f, 0.17f);
        public Color neutralColor = new Color(0.48f, 0.52f, 0.57f);

        [Header("Panels")]
        public Color panelColor = Color.white;
        public Color trackColor = new Color(0.87f, 0.88f, 0.90f);
        public Color progressColor = new Color(0.29f, 0.56f, 0.85f);
        public Color doneColor = new Color(0.35f, 0.66f, 0.31f);

        [Header("Resources")]
        public ResourceLook[] resources =
        {
            new ResourceLook { type = ResourceType.Power,  tint = Hex(0xFFF1C2), accent = Hex(0xE0B000), letter = "P"  },
            new ResourceLook { type = ResourceType.Water,  tint = Hex(0xCFE6FF), accent = Hex(0x4A90D9), letter = "W"  },
            new ResourceLook { type = ResourceType.Oxygen, tint = Hex(0xD2F3F5), accent = Hex(0x2FB5C0), letter = "O"  },
            new ResourceLook { type = ResourceType.Food,   tint = Hex(0xD6F2D2), accent = Hex(0x58A84F), letter = "F"  },
            new ResourceLook { type = ResourceType.Parts,  tint = Hex(0xFFDDC2), accent = Hex(0xD9803A), letter = "Pt" },
        };

        public bool TryGetLook(ResourceType type, out ResourceLook look)
        {
            foreach (var r in resources)
            {
                if (r.type != type) continue;
                look = r;
                return true;
            }

            look = default;
            return false;
        }

        public Color DeltaColor(int delta) => delta > 0 ? positiveColor : delta < 0 ? negativeColor : neutralColor;

        private static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f);
    }
}
