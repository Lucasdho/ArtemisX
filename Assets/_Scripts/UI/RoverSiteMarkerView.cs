using System;
using ArtemisX.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>One floating site pin on the top-down rover map: Toy UI pill flag with reward, risk blocks, name tag and pointer.</summary>
    public class RoverSiteMarkerView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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

        [Header("Toy Pill Flag")]
        [SerializeField] private Image flagImage;
        [SerializeField] private Sprite pillNormalSprite;
        [SerializeField] private Sprite pillSelectedSprite;
        [SerializeField] private Image toyRewardIcon;
        [SerializeField] private Text toyRewardLabel;
        [SerializeField] private Image[] toyRiskBlocks = new Image[4];
        [SerializeField] private Sprite riskOnSprite;
        [SerializeField] private Sprite riskOffSprite;
        [SerializeField] private Sprite waterIcon256;
        [SerializeField] private Sprite partsIcon256;

        private Action<string> onClicked;
        private bool hovered;
        private bool selected;

        public string SiteId => siteId;
        public Transform WorldTarget => worldTarget;
        public RectTransform Rect => (RectTransform)transform;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            hovered = false;
            selected = false;
            UpdateVisualState();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(Action<string> callback) => onClicked = callback;

        public void SetSelected(bool value)
        {
            selected = value;
            UpdateVisualState();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            UpdateVisualState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            UpdateVisualState();
        }

        /// <summary>Fills the labels, reward icon, and mini risk blocks. Rewards: Water first, then Parts.</summary>
        public void Show(string displayName, int powerCost, int water, int parts, int riskPercent, bool affordable)
        {
            if (nameLabel != null) nameLabel.text = displayName;
            SetChip(costIcon, costLabel, ResourceType.Power, -powerCost, true);
            bool hasWater = water > 0;
            SetChip(rewardIconA, rewardLabelA, hasWater ? ResourceType.Water : ResourceType.Parts, hasWater ? water : parts, hasWater || parts > 0);
            SetChip(rewardIconB, rewardLabelB, ResourceType.Parts, parts, hasWater && parts > 0);
            if (riskLabel != null) riskLabel.text = riskPercent + "% risk";

            // Toy Pill Flag visuals
            int mainReward = hasWater ? water : parts;
            if (toyRewardIcon != null)
            {
                Sprite large = hasWater ? waterIcon256 : partsIcon256;
                if (large != null)
                {
                    toyRewardIcon.sprite = large;
                    toyRewardIcon.color = Color.white;
                }
                else if (style != null && style.TryGetLook(hasWater ? ResourceType.Water : ResourceType.Parts, out var look))
                {
                    toyRewardIcon.sprite = look.icon;
                    toyRewardIcon.color = Color.white;
                }
            }
            if (toyRewardLabel != null)
            {
                toyRewardLabel.text = "+" + mainReward;
                if (style != null) toyRewardLabel.color = style.positiveColor;
            }
            if (toyRiskBlocks != null)
            {
                int activeBlocks = riskPercent <= 0 ? 0 : Mathf.Clamp(Mathf.RoundToInt(riskPercent / 100f * 4f), 1, 4);
                for (int i = 0; i < toyRiskBlocks.Length; i++)
                {
                    if (toyRiskBlocks[i] == null) continue;
                    bool on = i < activeBlocks;
                    if (riskOnSprite != null && riskOffSprite != null)
                        toyRiskBlocks[i].sprite = on ? riskOnSprite : riskOffSprite;
                    toyRiskBlocks[i].color = Color.white;
                }
            }

            // Stays clickable so Kepler can explain a blocked trip; dimmed when not affordable.
            if (button != null && button.targetGraphic != null)
                button.targetGraphic.color = affordable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
        }

        private void UpdateVisualState()
        {
            bool active = hovered || selected;
            if (flagImage != null && pillNormalSprite != null && pillSelectedSprite != null)
                flagImage.sprite = active ? pillSelectedSprite : pillNormalSprite;
            transform.localScale = active ? Vector3.one * 1.08f : Vector3.one;
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
                label.text = amount > 0 ? "▲+" + amount : amount < 0 ? "▼−" + Mathf.Abs(amount) : "0";
                if (style != null) label.color = style.DeltaColor(amount);
            }
        }

        private void HandleClick() => onClicked?.Invoke(siteId);
    }
}
