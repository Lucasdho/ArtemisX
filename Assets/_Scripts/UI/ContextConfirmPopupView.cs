using System;
using ArtemisX.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>Contextual confirmation pop-up anchored near the clicked object in screen space, with optional resource cost chips.</summary>
    public class ContextConfirmPopupView : MonoBehaviour
    {
        [Serializable]
        public class CostCell
        {
            public GameObject root;
            public Image icon;
            public Text amount;
        }

        /// <summary>One resource cost (or gain) chip on the card.</summary>
        public struct Cost
        {
            public ResourceType type;
            public int amount;
            public Cost(ResourceType type, int amount) { this.type = type; this.amount = amount; }
        }

        [SerializeField] private UIStyle style;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmButtonText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private CostCell[] costCells = Array.Empty<CostCell>();
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 18f);
        [SerializeField] private float edgePadding = 20f;

        private Func<Vector3> anchor;
        private Action onConfirm;
        private Camera viewCamera;
        private RectTransform canvasRect;

        public bool IsOpen => anchor != null;

        private void Awake()
        {
            canvasRect = GetComponentInParent<Canvas>()?.rootCanvas.GetComponent<RectTransform>();
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            ApplyStyle();
            // Awake runs on the first Open when the object starts inactive; don't hide it then.
            if (!IsOpen) HideImmediate();
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
        }

        /// <summary>Legacy entry point for static ClickableBuilding objects.</summary>
        public void Open(ClickableBuilding target, Camera cam)
        {
            if (target == null) return;
            Open(() => target.AnchorWorldPosition, target.Title, target.Description, target.ConfirmLabel, true, target.Confirm, null, cam);
        }

        /// <summary>Opens the card next to a moving world anchor. Confirm runs <paramref name="confirm"/> then closes.</summary>
        public void Open(Func<Vector3> worldAnchor, string title, string description, string confirmLabel, bool canConfirm,
            Action confirm, Cost[] costs, Camera cam)
        {
            if (worldAnchor == null) return;
            anchor = worldAnchor;
            onConfirm = confirm;
            viewCamera = cam != null ? cam : Camera.main;

            if (titleText != null) titleText.text = title;
            if (descriptionText != null) descriptionText.text = description;
            if (confirmButtonText != null) confirmButtonText.text = confirmLabel;
            if (confirmButton != null)
            {
                confirmButton.gameObject.SetActive(confirm != null);
                confirmButton.interactable = canConfirm;
            }
            ApplyCosts(costs);

            gameObject.SetActive(true);
            UpdatePosition();

            if (panel != null)
            {
                panel.DOKill();
                panel.localScale = Vector3.one * 0.9f;
                panel.DOScale(1f, 0.16f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.14f).SetUpdate(true).SetLink(gameObject);
            }
        }

        public void Close()
        {
            anchor = null;
            onConfirm = null;
            HideImmediate();
        }

        private void LateUpdate()
        {
            if (anchor != null) UpdatePosition();
        }

        private void OnConfirmClicked()
        {
            Action action = onConfirm;
            Close();
            action?.Invoke();
        }

        private void ApplyCosts(Cost[] costs)
        {
            if (costCells == null) return;
            int count = costs != null ? costs.Length : 0;
            for (int i = 0; i < costCells.Length; i++)
            {
                var cell = costCells[i];
                if (cell == null || cell.root == null) continue;
                bool show = i < count;
                cell.root.SetActive(show);
                if (!show) continue;

                var cost = costs[i];
                if (cell.icon != null && style != null && style.TryGetLook(cost.type, out var look))
                {
                    cell.icon.sprite = look.icon;
                    cell.icon.color = look.icon != null ? Color.white : look.accent;
                }
                if (cell.amount != null)
                {
                    cell.amount.text = cost.amount > 0 ? "+" + cost.amount : cost.amount < 0 ? "−" + Mathf.Abs(cost.amount) : "0";
                    if (style != null) cell.amount.color = style.DeltaColor(cost.amount);
                }
            }
        }

        private void UpdatePosition()
        {
            if (panel == null || canvasRect == null || anchor == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            Vector3 screenPos = viewCamera.WorldToScreenPoint(anchor());
            if (screenPos.z < 0f) return;

            Canvas parentCanvas = canvasRect.GetComponent<Canvas>();
            Camera uiCam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 localPos))
            {
                localPos += screenOffset;
                Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
                Vector2 size = panel.rect.size;
                float minX = -halfCanvas.x + size.x * panel.pivot.x + edgePadding;
                float maxX = halfCanvas.x - size.x * (1f - panel.pivot.x) - edgePadding;
                float minY = -halfCanvas.y + size.y * panel.pivot.y + edgePadding;
                float maxY = halfCanvas.y - size.y * (1f - panel.pivot.y) - edgePadding;
                localPos.x = Mathf.Clamp(localPos.x, minX, maxX);
                localPos.y = Mathf.Clamp(localPos.y, minY, maxY);
                panel.anchoredPosition = localPos;
            }
        }

        private void ApplyStyle()
        {
            if (style == null) return;
            if (panel != null && panel.TryGetComponent(out Image bg)) bg.color = style.panelColor;
            if (titleText != null)
            {
                titleText.color = style.textColor;
                if (style.font != null) titleText.font = style.font;
            }
            if (descriptionText != null)
            {
                descriptionText.color = style.mutedTextColor;
                if (style.font != null) descriptionText.font = style.font;
            }
            if (confirmButton != null && confirmButton.TryGetComponent(out Image confirmBg))
                confirmBg.color = style.progressColor;
            if (confirmButtonText != null && style.font != null)
                confirmButtonText.font = style.font;
            if (cancelButton != null && cancelButton.TryGetComponent(out Image cancelBg))
                cancelBg.color = style.trackColor;
            if (cancelButton != null)
            {
                Text cancelText = cancelButton.GetComponentInChildren<Text>();
                if (cancelText != null)
                {
                    cancelText.color = style.textColor;
                    if (style.font != null) cancelText.font = style.font;
                }
            }
            if (costCells == null) return;
            foreach (var cell in costCells)
                if (cell != null && cell.amount != null && style.font != null) cell.amount.font = style.font;
        }

        private void HideImmediate()
        {
            if (panel != null) panel.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();
            gameObject.SetActive(false);
        }
    }
}
