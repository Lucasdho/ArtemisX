using ArtemisX.Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>Contextual confirmation pop-up anchored near the clicked building/slot in screen space.</summary>
    public class ContextConfirmPopupView : MonoBehaviour
    {
        [SerializeField] private UIStyle style;
        [SerializeField] private RectTransform panel;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Text confirmButtonText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 18f);
        [SerializeField] private float edgePadding = 20f;

        private ClickableBuilding currentTarget;
        private Camera viewCamera;
        private RectTransform canvasRect;

        public bool IsOpen => currentTarget != null;

        private void Awake()
        {
            canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(Close);
            ApplyStyle();
            HideImmediate();
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Close);
        }

        public void Open(ClickableBuilding target, Camera cam)
        {
            if (target == null) return;
            currentTarget = target;
            viewCamera = cam != null ? cam : Camera.main;

            if (titleText != null) titleText.text = target.Title;
            if (descriptionText != null) descriptionText.text = target.Description;
            if (confirmButtonText != null) confirmButtonText.text = target.ConfirmLabel;

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
            currentTarget = null;
            HideImmediate();
        }

        private void LateUpdate()
        {
            if (currentTarget != null) UpdatePosition();
        }

        private void OnConfirmClicked()
        {
            ClickableBuilding target = currentTarget;
            Close();
            if (target != null) target.Confirm();
        }

        private void UpdatePosition()
        {
            if (panel == null || canvasRect == null || currentTarget == null) return;
            if (viewCamera == null) viewCamera = Camera.main;
            if (viewCamera == null) return;

            Vector3 screenPos = viewCamera.WorldToScreenPoint(currentTarget.AnchorWorldPosition);
            if (screenPos.z < 0f) return;

            Canvas parentCanvas = canvasRect.GetComponent<Canvas>();
            Camera uiCam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCam, out Vector2 localPos))
            {
                localPos += screenOffset;
                Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
                Vector2 halfPanel = panel.rect.size * 0.5f;
                float minX = -halfCanvas.x + halfPanel.x + edgePadding;
                float maxX = halfCanvas.x - halfPanel.x - edgePadding;
                float minY = -halfCanvas.y + halfPanel.y + edgePadding;
                float maxY = halfCanvas.y - halfPanel.y - edgePadding;
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
        }

        private void HideImmediate()
        {
            if (panel != null) panel.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();
            gameObject.SetActive(false);
        }
    }
}
