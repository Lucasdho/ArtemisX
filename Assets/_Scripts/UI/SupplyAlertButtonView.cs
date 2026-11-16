using System;
using ArtemisX.Core;
using ArtemisX.Simulation;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Jumping HUD icon (Golden Box + alert badge) shown whenever Earth supply cargo is waiting on the landing pad.
    /// Pans the camera to the pad if it is off-screen or blocked by HUD elements; collects cargo directly when clearly visible.
    /// </summary>
    public class SupplyAlertButtonView : MonoBehaviour
    {
        [SerializeField] private SupplyManager supply;
        [SerializeField] private SupplyPileInteractable supplyPad;
        [SerializeField] private CameraPanController panController;
        [SerializeField] private GameplayHud hud;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private RectTransform jumpTarget;
        [SerializeField] private GameObject badgeRoot;
        [SerializeField] private Text badgeLabel;
        [SerializeField] private RectTransform[] hudOccluders = Array.Empty<RectTransform>();
        [SerializeField] private Vector2 viewportMargin = new Vector2(0.08f, 0.10f);
        [SerializeField] private float jumpHeight = 14f;
        [SerializeField] private float jumpDuration = 0.28f;
        [SerializeField] private float pauseSeconds = 0.35f;

        private Sequence jumpTween;
        private Vector2 jumpRestPos;
        private bool restCaptured;
        private bool clickWired;
        private bool isShowing;
        private SupplyManager subscribedSupply;

        public RectTransform Rect => transform as RectTransform;
        public Button Button => button;
        public bool IsShowing => isShowing;

        public event Action<bool> ActionTriggered;

        private void Awake()
        {
            EnsureClickWired();
            CaptureRestPosition();
        }

        private void OnEnable()
        {
            EnsureClickWired();
            SubscribeSupply();
            Refresh();
        }

        private void OnDisable()
        {
            UnsubscribeSupply();
            StopJumpAnimation();
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>Programmatic setup for runtime or EditMode tests.</summary>
        public void Bind(
            SupplyManager supplyManager,
            SupplyPileInteractable pad,
            CameraPanController pan,
            GameplayHud gameplayHud,
            RectTransform[] occluders = null,
            Camera cam = null)
        {
            UnsubscribeSupply();
            supply = supplyManager;
            supplyPad = pad;
            panController = pan;
            hud = gameplayHud;
            if (occluders != null) hudOccluders = occluders;
            if (cam != null) worldCamera = cam;
            if (button == null) button = GetComponent<Button>();
            if (visualRoot == null) visualRoot = transform as RectTransform;
            EnsureClickWired();
            CaptureRestPosition();
            if (isActiveAndEnabled) SubscribeSupply();
            Refresh();
        }

        public bool ShouldBeVisible()
        {
            if (supply == null || !supply.HasPendingSupply) return false;
            if (hud != null && hud.IsOverlayCoveringHud) return false;
            return true;
        }

        public bool IsClickBlocked()
        {
            return hud != null && hud.IsWorldInputBlocked;
        }

        public Camera ResolveCamera()
        {
            if (worldCamera != null) return worldCamera;
            if (hud != null && hud.ViewCamera != null) return hud.ViewCamera;
            return Camera.main;
        }

        public Vector3 ResolveTargetWorldPosition()
        {
            if (supplyPad != null) return supplyPad.AnchorWorldPosition;
            return Vector3.zero;
        }

        public bool IsTargetClearlyVisible()
        {
            if (supplyPad == null) return false;
            return IsWorldPointClearlyVisible(ResolveTargetWorldPosition(), ResolveCamera(), hudOccluders, viewportMargin);
        }

        /// <summary>
        /// Returns true iff <paramref name="worldPos"/> projects inside the padded screen viewport
        /// in front of <paramref name="cam"/> and does not overlap any visible HUD occluder.
        /// </summary>
        public static bool IsWorldPointClearlyVisible(
            Vector3 worldPos,
            Camera cam,
            RectTransform[] occluders,
            Vector2 margin)
        {
            if (cam == null || cam.pixelWidth <= 0 || cam.pixelHeight <= 0) return false;

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f) return false;

            float nx = screenPos.x / cam.pixelWidth;
            float ny = screenPos.y / cam.pixelHeight;
            float padX = Mathf.Clamp(margin.x, 0f, 0.45f);
            float padY = Mathf.Clamp(margin.y, 0f, 0.45f);
            if (nx < padX || nx > 1f - padX || ny < padY || ny > 1f - padY)
                return false;

            if (occluders == null) return true;

            Vector2 screenPoint = new Vector2(screenPos.x, screenPos.y);
            for (int i = 0; i < occluders.Length; i++)
            {
                RectTransform occluder = occluders[i];
                if (!IsOccluderActive(occluder)) continue;

                Canvas canvas = occluder.GetComponentInParent<Canvas>();
                Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
                Camera uiCam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? rootCanvas.worldCamera
                    : null;

                if (RectTransformUtility.RectangleContainsScreenPoint(occluder, screenPoint, uiCam))
                    return false;
            }

            return true;
        }

        public void Refresh()
        {
            bool show = ShouldBeVisible();
            isShowing = show;
            bool blocked = IsClickBlocked();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = show ? 1f : 0f;
                canvasGroup.blocksRaycasts = show;
                canvasGroup.interactable = show && !blocked;
            }

            if (visualRoot != null && visualRoot != Rect)
            {
                if (visualRoot.gameObject.activeSelf != show)
                    visualRoot.gameObject.SetActive(show);
            }

            if (button != null)
                button.interactable = show && !blocked;

            if (badgeRoot != null)
            {
                bool showBadge = show;
                if (badgeRoot.activeSelf != showBadge) badgeRoot.SetActive(showBadge);
                if (showBadge && badgeLabel != null && supply != null)
                    badgeLabel.text = supply.PendingDrops > 1 ? supply.PendingDrops.ToString() : "!";
            }

            if (show)
                StartJumpAnimation();
            else
                StopJumpAnimation();
        }

        public void OnClicked()
        {
            if (!ShouldBeVisible() || IsClickBlocked()) return;

            if (IsTargetClearlyVisible() && supplyPad != null)
            {
                bool collected = supplyPad.TryCollectSupply();
                if (collected)
                {
                    Refresh();
                    ActionTriggered?.Invoke(true);
                    return;
                }
            }

            if (hud != null && hud.Popup != null)
                hud.Popup.Close();

            if (panController != null && supplyPad != null)
            {
                panController.FocusOn(ResolveTargetWorldPosition());
                ActionTriggered?.Invoke(false);
            }
        }

        private void EnsureClickWired()
        {
            if (clickWired) return;
            if (button == null) button = GetComponent<Button>();
            if (button == null) return;
            button.onClick.AddListener(OnClicked);
            clickWired = true;
        }

        private void SubscribeSupply()
        {
            if (supply == null || subscribedSupply == supply) return;
            subscribedSupply = supply;
            subscribedSupply.SupplyDropped += OnSupplyChanged;
            subscribedSupply.SupplyCollected += OnSupplyCollected;
        }

        private void UnsubscribeSupply()
        {
            if (subscribedSupply == null) return;
            subscribedSupply.SupplyDropped -= OnSupplyChanged;
            subscribedSupply.SupplyCollected -= OnSupplyCollected;
            subscribedSupply = null;
        }

        private void OnSupplyChanged(int _, int __, int ___) => Refresh();
        private void OnSupplyCollected(int _, int __) => Refresh();

        private void CaptureRestPosition()
        {
            if (restCaptured || jumpTarget == null) return;
            jumpRestPos = jumpTarget.anchoredPosition;
            restCaptured = true;
        }

        private void StartJumpAnimation()
        {
            if (jumpTarget == null || jumpTween != null && jumpTween.IsActive()) return;
            if (!Application.isPlaying) return;

            CaptureRestPosition();
            jumpTarget.DOKill();
            jumpTarget.anchoredPosition = jumpRestPos;
            jumpTarget.localScale = Vector3.one;

            float upTime = Mathf.Max(0.08f, jumpDuration);
            float downTime = upTime * 0.9f;

            jumpTween = DOTween.Sequence().SetLink(gameObject).SetLoops(-1);
            jumpTween.Append(jumpTarget.DOAnchorPosY(jumpRestPos.y + jumpHeight, upTime).SetEase(Ease.OutQuad));
            jumpTween.Join(jumpTarget.DOScale(new Vector3(0.95f, 1.06f, 1f), upTime * 0.6f).SetEase(Ease.OutQuad));
            jumpTween.Append(jumpTarget.DOAnchorPosY(jumpRestPos.y, downTime).SetEase(Ease.InQuad));
            jumpTween.Join(jumpTarget.DOScale(new Vector3(1.08f, 0.92f, 1f), downTime).SetEase(Ease.InQuad));
            jumpTween.Append(jumpTarget.DOScale(Vector3.one, 0.12f).SetEase(Ease.OutBack));
            if (pauseSeconds > 0f) jumpTween.AppendInterval(pauseSeconds);
        }

        private void StopJumpAnimation()
        {
            if (jumpTween != null)
            {
                jumpTween.Kill();
                jumpTween = null;
            }
            if (jumpTarget != null)
            {
                jumpTarget.DOKill();
                if (restCaptured) jumpTarget.anchoredPosition = jumpRestPos;
                jumpTarget.localScale = Vector3.one;
            }
        }

        private static bool IsOccluderActive(RectTransform rt)
        {
            if (rt == null || !rt.gameObject.activeInHierarchy) return false;
            Transform cur = rt;
            while (cur != null)
            {
                var cg = cur.GetComponent<CanvasGroup>();
                if (cg != null && cg.alpha <= 0.01f) return false;
                cur = cur.parent;
            }
            return true;
        }
    }
}
