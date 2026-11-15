using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Pulses the red zero-resource alert badge on a ResourcePill when active.
    /// Ready for 03b to toggle via gameObject.SetActive(true) or SetAlertActive(true) when Oxygen or Food hits 0.
    /// </summary>
    public class UIAlertPulseView : MonoBehaviour
    {
        [SerializeField] private UIThemeConfig theme;
        [SerializeField] private RectTransform badgeTransform;
        [SerializeField] private Graphic badgeGraphic;

        private void Awake()
        {
            if (badgeTransform == null)
                badgeTransform = transform as RectTransform;
            if (badgeGraphic == null)
                badgeGraphic = GetComponent<Graphic>();
        }

        private void OnEnable()
        {
            if (badgeTransform != null)
                badgeTransform.localScale = Vector3.one;
        }

        private void Update()
        {
            float speed = theme != null ? theme.alertPulseSpeed : 4.5f;
            float minScale = theme != null ? theme.alertPulseMinScale : 0.92f;
            float maxScale = theme != null ? theme.alertPulseMaxScale : 1.14f;

            float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            if (badgeTransform != null)
            {
                float s = Mathf.Lerp(minScale, maxScale, wave);
                badgeTransform.localScale = new Vector3(s, s, 1f);
            }

            if (badgeGraphic != null)
            {
                Color c = badgeGraphic.color;
                c.a = Mathf.Lerp(0.75f, 1f, wave);
                badgeGraphic.color = c;
            }
        }

        /// <summary>Shows or hides the pulsing alert badge.</summary>
        public void SetAlertActive(bool active)
        {
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }
    }
}
