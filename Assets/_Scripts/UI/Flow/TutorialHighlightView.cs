using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>Pulsing frame drawn over a HUD element or a 3D object, plus a hint banner at the top of the screen.</summary>
    public class TutorialHighlightView : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private RectTransform frame;
        [SerializeField] private GameObject hintRoot;
        [SerializeField] private Text hintLabel;
        [Tooltip("Tutorial progress now lives in the Mission card; keep the top banner off.")]
        [SerializeField] private bool showHintBanner = false;
        [SerializeField] private Vector2 worldFrameSize = new Vector2(240f, 240f);
        [SerializeField] private float uiPadding = 24f;
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseAmount = 0.08f;

        private RectTransform uiTarget;
        private Transform worldTarget;
        private Camera worldCamera;

        /// <summary>Hides the frame and hint without forgetting the target (e.g. while the rover map is open).</summary>
        public bool Suppressed { get; set; }

        private void Awake() => Hide();

        public void ShowUi(RectTransform target, string hint)
        {
            uiTarget = target;
            worldTarget = null;
            Show(hint);
        }

        public void ShowWorld(Transform target, Camera cam, string hint)
        {
            worldTarget = target;
            worldCamera = cam;
            uiTarget = null;
            Show(hint);
        }

        public void Hide()
        {
            uiTarget = null;
            worldTarget = null;
            if (frame != null) frame.gameObject.SetActive(false);
            if (hintRoot != null) hintRoot.SetActive(false);
        }

        private void Show(string hint)
        {
            if (frame != null) frame.gameObject.SetActive(true);
            if (hintLabel != null) hintLabel.text = showHintBanner ? (hint ?? string.Empty) : string.Empty;
            if (hintRoot != null) hintRoot.SetActive(showHintBanner && !string.IsNullOrEmpty(hint));
            LateUpdate();
        }

        private void LateUpdate()
        {
            if (frame == null || canvasRect == null) return;
            bool hasTarget = uiTarget != null || worldTarget != null;
            if (hintRoot != null) hintRoot.SetActive(showHintBanner && hasTarget && !Suppressed && hintLabel != null && hintLabel.text.Length > 0);
            if (!hasTarget || Suppressed) { frame.gameObject.SetActive(false); return; }
            frame.gameObject.SetActive(true);
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            frame.localScale = new Vector3(pulse, pulse, 1f);

            if (uiTarget != null)
            {
                var corners = new Vector3[4];
                uiTarget.GetWorldCorners(corners); // Screen Space Overlay: world corners are screen pixels.
                Vector2 min = ToLocal(corners[0]);
                Vector2 max = ToLocal(corners[2]);
                frame.anchoredPosition = (min + max) * 0.5f;
                frame.sizeDelta = (max - min) + Vector2.one * uiPadding * 2f;
            }
            else if (worldTarget != null)
            {
                var cam = worldCamera != null ? worldCamera : Camera.main;
                if (cam == null) return;
                Vector3 screen = cam.WorldToScreenPoint(worldTarget.position);
                if (screen.z <= 0f) frame.gameObject.SetActive(false);
                frame.anchoredPosition = ToLocal(screen);
                frame.sizeDelta = worldFrameSize;
            }
        }

        private Vector2 ToLocal(Vector3 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
            return local;
        }
    }
}
