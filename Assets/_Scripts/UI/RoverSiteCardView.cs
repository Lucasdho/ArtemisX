using System;
using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Approved Toy UI SiteCard popup shown when the player selects a rover site on the RoverMap.
    /// Displays the site name, one-line summary, reward tray + risk tray (4 blocks, purely visual:
    /// 0 lit = safe, 1 lit = 25%, 2 lit = 50%) and a full-width Send button with cost loss-sticker.
    /// </summary>
    public class RoverSiteCardView : MonoBehaviour
    {
        [SerializeField] private UIStyle style;
        [SerializeField] private GameObject root;
        [SerializeField] private Button backdropCloseButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text headerTitle;
        [SerializeField] private Text bodyLabel;

        [Header("Brings Back Tray")]
        [SerializeField] private Image rewardIconA;
        [SerializeField] private Text rewardTextA;
        [SerializeField] private GameObject rewardSlotB;
        [SerializeField] private Image rewardIconB;
        [SerializeField] private Text rewardTextB;
        [SerializeField] private Sprite waterIcon256;
        [SerializeField] private Sprite partsIcon256;

        [Header("Risk Tray")]
        [SerializeField] private Image[] riskBlocks = new Image[4];
        [SerializeField] private Sprite riskOnSprite;
        [SerializeField] private Sprite riskOffSprite;

        [Header("Send Button & Cost Sticker")]
        [SerializeField] private Button sendButton;
        [SerializeField] private Image sendButtonImage;
        [SerializeField] private Text sendButtonLabel;
        [SerializeField] private Image sendRoverIcon;
        [SerializeField] private Sprite sendPrimarySprite;
        [SerializeField] private Sprite sendPrimaryPressedSprite;
        [SerializeField] private Sprite sendDisabledSprite;
        [SerializeField] private GameObject costSticker;
        [SerializeField] private Image costIcon;
        [SerializeField] private Text costAmount;

        private Action onSendClicked;

        public event Action Closed;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (backdropCloseButton != null) backdropCloseButton.onClick.AddListener(Hide);
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (sendButton != null) sendButton.onClick.AddListener(HandleSend);
            if (root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (backdropCloseButton != null) backdropCloseButton.onClick.RemoveListener(Hide);
            if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
            if (sendButton != null) sendButton.onClick.RemoveListener(HandleSend);
        }

        public void Show(ExpeditionSiteConfig.Site site, RoverExpeditionManager rover, ResourceManager resources,
            SpaceWeatherManager weather, Action onConfirmSend)
        {
            onSendClicked = onConfirmSend;

            if (headerTitle != null) headerTitle.text = site.displayName;
            if (bodyLabel != null) bodyLabel.text = DescribeSite(site);

            bool hasWater = site.waterReward > 0;
            bool hasParts = site.partsReward > 0;
            SetRewardSlot(rewardIconA, rewardTextA, hasWater ? ResourceType.Water : ResourceType.Parts,
                hasWater ? site.waterReward : site.partsReward, hasWater || hasParts);
            if (rewardSlotB != null) rewardSlotB.SetActive(hasWater && hasParts);
            if (hasWater && hasParts)
                SetRewardSlot(rewardIconB, rewardTextB, ResourceType.Parts, site.partsReward, true);

            bool storm = weather != null && weather.TodayWeather == WeatherEventType.SolarStorm;
            float chance = storm && rover != null && rover.Config != null ? rover.Config.stormBreakdownChance : site.calmBreakdownChance;
            ApplyRisk(chance);

            string roverName = rover != null ? rover.RoverName : "Luna";
            var status = rover != null ? rover.Status : RoverStatus.Ready;
            bool busy = status == RoverStatus.OnMission;
            bool broken = status == RoverStatus.Broken;
            bool locked = status == RoverStatus.LockedByRadiation || (resources != null && resources.IsCrewRestricted);

            if (sendButtonLabel != null)
            {
                if (busy) sendButtonLabel.text = roverName + " is out";
                else if (broken) sendButtonLabel.text = roverName + " is broken";
                else if (locked) sendButtonLabel.text = "Rover locked";
                else sendButtonLabel.text = "Send " + roverName;
            }

            bool ready = !busy && !broken && !locked;
            if (sendButtonImage != null)
                sendButtonImage.sprite = ready ? sendPrimarySprite : sendDisabledSprite;

            if (sendButton != null)
            {
                var st = sendButton.spriteState;
                st.highlightedSprite = ready ? sendPrimarySprite : sendDisabledSprite;
                st.pressedSprite = ready ? sendPrimaryPressedSprite : sendDisabledSprite;
                st.disabledSprite = sendDisabledSprite;
                sendButton.spriteState = st;
            }

            if (sendRoverIcon != null)
                sendRoverIcon.gameObject.SetActive(ready);

            if (costSticker != null)
            {
                costSticker.SetActive(site.powerCost > 0);
                if (costIcon != null && style != null && style.TryGetLook(ResourceType.Power, out var powerLook))
                {
                    costIcon.sprite = powerLook.icon;
                    costIcon.color = Color.white;
                }
                if (costAmount != null)
                    costAmount.text = "−" + site.powerCost;
            }

            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            bool wasOpen = IsOpen;
            onSendClicked = null;
            if (root != null) root.SetActive(false);
            if (wasOpen) Closed?.Invoke();
        }

        private void HandleSend() => onSendClicked?.Invoke();

        private void SetRewardSlot(Image icon, Text label, ResourceType type, int amount, bool visible)
        {
            if (icon != null)
            {
                icon.enabled = visible;
                if (visible)
                {
                    Sprite large = type == ResourceType.Water ? waterIcon256
                                 : type == ResourceType.Parts ? partsIcon256
                                 : null;
                    if (large != null)
                    {
                        icon.sprite = large;
                        icon.color = Color.white;
                    }
                    else if (style != null && style.TryGetLook(type, out var look))
                    {
                        icon.sprite = look.icon;
                        icon.color = Color.white;
                    }
                }
            }
            if (label != null)
            {
                label.enabled = visible;
                label.text = "+" + amount;
                if (style != null) label.color = style.positiveColor;
            }
        }

        private void ApplyRisk(float chance)
        {
            int activeBlocks = chance <= 0f ? 0 : Mathf.Clamp(Mathf.RoundToInt(chance * 4f), 1, 4);
            if (riskBlocks != null)
            {
                for (int i = 0; i < riskBlocks.Length; i++)
                {
                    if (riskBlocks[i] == null) continue;
                    bool on = i < activeBlocks;
                    riskBlocks[i].sprite = on ? riskOnSprite : riskOffSprite;
                    riskBlocks[i].color = Color.white;
                }
            }
        }

        private static string DescribeSite(ExpeditionSiteConfig.Site site)
        {
            switch (site.distance)
            {
                case SiteDistance.Near: return "Close by. Cheap and safe trip.";
                case SiteDistance.Far: return "Far away. Big haul, risky trip.";
                default: return "Mid-range. Balanced haul and risk.";
            }
        }
    }
}
