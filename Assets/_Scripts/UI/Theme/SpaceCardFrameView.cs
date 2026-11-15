using System;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Reusable Space Expansion card/modal frame for Daily Report, Unexpected Demand,
    /// Space Weather Choice, and 3D object confirmation pop-ups.
    /// </summary>
    public class SpaceCardFrameView : MonoBehaviour
    {
        public enum CardAccentVariant
        {
            InfoBlue = 0,
            WarningYellow = 1,
            AlertRed = 2
        }

        [SerializeField] private UIThemeConfig theme;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image panelBorder;
        [SerializeField] private Image headerBar;
        [SerializeField] private Text headerTitleLabel;
        [SerializeField] private GameObject iconSlotRoot;
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform bodyContainer;
        [SerializeField] private Text bodyTextLabel;
        [SerializeField] private Text footerCommentLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmButtonLabel;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Text cancelButtonLabel;

        private Action confirmAction;
        private Action cancelAction;

        public RectTransform CardRoot => cardRoot;
        public RectTransform BodyContainer => bodyContainer;
        public Button ConfirmButton => confirmButton;
        public Button CancelButton => cancelButton;
        public Text HeaderTitleLabel => headerTitleLabel;
        public Text BodyTextLabel => bodyTextLabel;
        public Text FooterCommentLabel => footerCommentLabel;
        public Image IconImage => iconImage;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
        }

        /// <summary>Configures the card's title, body copy, optional icon, footer note, and Confirm/Cancel actions cleanly.</summary>
        public void Configure(
            string title,
            string body,
            Sprite icon = null,
            string confirmText = "CONFIRM",
            Action onConfirm = null,
            string cancelText = null,
            Action onCancel = null,
            string footerComment = null,
            CardAccentVariant variant = CardAccentVariant.InfoBlue)
        {
            confirmAction = onConfirm;
            cancelAction = onCancel;

            if (headerTitleLabel != null)
                headerTitleLabel.text = title ?? string.Empty;

            if (bodyTextLabel != null)
                bodyTextLabel.text = body ?? string.Empty;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            if (iconSlotRoot != null)
                iconSlotRoot.SetActive(icon != null);

            if (footerCommentLabel != null)
            {
                bool hasFooter = !string.IsNullOrWhiteSpace(footerComment);
                footerCommentLabel.text = hasFooter ? footerComment : string.Empty;
                footerCommentLabel.gameObject.SetActive(hasFooter);
            }

            if (confirmButton != null)
            {
                bool showConfirm = !string.IsNullOrWhiteSpace(confirmText) || onConfirm != null;
                confirmButton.gameObject.SetActive(showConfirm);
                if (confirmButtonLabel != null && !string.IsNullOrWhiteSpace(confirmText))
                    confirmButtonLabel.text = confirmText;
            }

            if (cancelButton != null)
            {
                bool showCancel = !string.IsNullOrWhiteSpace(cancelText) || onCancel != null;
                cancelButton.gameObject.SetActive(showCancel);
                if (cancelButtonLabel != null && !string.IsNullOrWhiteSpace(cancelText))
                    cancelButtonLabel.text = cancelText;
            }

            ApplyAccentVariant(variant);
        }

        /// <summary>Switches the card border and header bar accent between InfoBlue, WarningYellow, and AlertRed.</summary>
        public void ApplyAccentVariant(CardAccentVariant variant)
        {
            if (theme == null) return;

            Color accent = theme.cyanAccentBorder;
            Sprite headerSprite = theme.pillBlueSprite;
            switch (variant)
            {
                case CardAccentVariant.WarningYellow:
                    accent = theme.amberHighlight;
                    if (theme.pillYellowSprite != null) headerSprite = theme.pillYellowSprite;
                    break;
                case CardAccentVariant.AlertRed:
                    accent = theme.alertRed;
                    if (theme.pillRedSprite != null) headerSprite = theme.pillRedSprite;
                    break;
            }

            if (panelBorder != null)
                panelBorder.color = accent;
            if (headerBar != null && headerSprite != null)
                headerBar.sprite = headerSprite;
        }

        public void Show() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void HandleConfirmClicked() => confirmAction?.Invoke();

        private void HandleCancelClicked() => cancelAction?.Invoke();
    }
}
