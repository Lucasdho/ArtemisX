using System;
using ArtemisX.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>One floating site pin on the top-down rover map: name, -Power, +Water/+Parts and breakdown risk.</summary>
    public class RoverSiteMarkerView : MonoBehaviour
    {
        [SerializeField] private string siteId;
        [SerializeField] private Transform worldTarget;
        [SerializeField] private UIStyle style;
        [SerializeField] private Button button;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Image costIcon;
        [SerializeField] private Text costLabel;
        [SerializeField] private Image rewardIconA;
        [SerializeField] private Text rewardLabelA;
        [SerializeField] private Image rewardIconB;
        [SerializeField] private Text rewardLabelB;
        [SerializeField] private Text riskLabel;

        private Action<string> onClicked;

        public string SiteId => siteId;
        public Transform WorldTarget => worldTarget;
        public RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(Action<string> callback) => onClicked = callback;

        /// <summary>Fills the labels. Rewards: Water first, then Parts; empty slots hide.</summary>
        public void Show(string displayName, int powerCost, int water, int parts, int riskPercent, bool affordable)
        {
            if (nameLabel != null) nameLabel.text = displayName;
            SetChip(costIcon, costLabel, ResourceType.Power, -powerCost, true);
            bool hasWater = water > 0;
            SetChip(rewardIconA, rewardLabelA, hasWater ? ResourceType.Water : ResourceType.Parts, hasWater ? water : parts, hasWater || parts > 0);
            SetChip(rewardIconB, rewardLabelB, ResourceType.Parts, parts, hasWater && parts > 0);
            if (riskLabel != null) riskLabel.text = riskPercent + "% risk";
            // Stays clickable so Bauzer can explain a blocked trip; dimmed when not affordable.
            if (button != null && button.targetGraphic != null) button.targetGraphic.color = affordable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        private void SetChip(Image icon, Text label, ResourceType type, int amount, bool visible)
        {
            if (icon != null)
            {
                icon.transform.parent.gameObject.SetActive(visible);
                if (style != null && style.TryGetLook(type, out var look))
                {
                    icon.sprite = look.icon;
                    icon.color = look.icon != null ? Color.white : look.accent;
                }
            }
            if (label != null)
            {
                label.text = amount > 0 ? "+" + amount : amount < 0 ? "−" + Mathf.Abs(amount) : "0";
                if (style != null) label.color = style.DeltaColor(amount);
            }
        }

        private void HandleClick() => onClicked?.Invoke(siteId);
    }
}
