using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// One floating resource pill: icon bubble, current amount and the passive net change per day.
    /// Listens to ResourceManager events and takes its colors from UIStyle.
    /// </summary>
    public class ResourcePillView : MonoBehaviour
    {
        [SerializeField] private ResourceType type;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private UIStyle style;
        [SerializeField] private Graphic pillBackground;
        [SerializeField] private Graphic iconBubble;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text iconLetter;
        [SerializeField] private Text amountLabel;
        [SerializeField] private Text deltaLabel;
        [Tooltip("Optional red badge that pulses while this resource is at 0 (Oxygen and Food restrict the crew).")]
        [SerializeField] private Graphic alertBadge;
        [SerializeField] private float alertPulsePeriod = 0.8f;

        private bool alertActive;

        private void Awake()
        {
            ApplyStyle();
            if (alertBadge != null) alertBadge.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!alertActive || alertBadge == null) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime / Mathf.Max(0.1f, alertPulsePeriod) * Mathf.PI * 2f);
            alertBadge.transform.localScale = Vector3.one * (0.9f + 0.25f * wave);
            var c = alertBadge.color;
            c.a = 0.6f + 0.4f * wave;
            alertBadge.color = c;
        }

        private void OnEnable()
        {
            if (resources == null) return;
            resources.ResourceChanged += OnResourceChanged;
            resources.NetRateChanged += OnNetRateChanged;
        }

        private void OnDisable()
        {
            if (resources == null) return;
            resources.ResourceChanged -= OnResourceChanged;
            resources.NetRateChanged -= OnNetRateChanged;
        }

        private void ApplyStyle()
        {
            if (style == null) return;

            if (style.TryGetLook(type, out var look))
            {
                bool hasIcon = look.icon != null;
                if (pillBackground != null) pillBackground.color = look.tint;
                if (iconBubble != null) iconBubble.color = hasIcon ? style.panelColor : look.accent;
                if (iconImage != null)
                {
                    iconImage.sprite = look.icon;
                    iconImage.enabled = hasIcon;
                }
                if (iconLetter != null)
                {
                    iconLetter.text = look.letter;
                    iconLetter.enabled = !hasIcon;
                }
            }

            foreach (var label in new[] { iconLetter, amountLabel, deltaLabel })
            {
                if (label != null && style.font != null) label.font = style.font;
            }

            if (amountLabel != null) amountLabel.color = style.textColor;
            if (iconLetter != null) iconLetter.color = Color.white;
        }

        private void OnResourceChanged(ResourceType changed, int current, int max)
        {
            if (changed != type) return;
            if (amountLabel != null) amountLabel.text = current.ToString();
            bool alert = current <= 0 && (type == ResourceType.Oxygen || type == ResourceType.Food);
            if (alert == alertActive) return;
            alertActive = alert;
            if (alertBadge != null)
            {
                alertBadge.gameObject.SetActive(alert);
                alertBadge.transform.localScale = Vector3.one;
            }
        }

        private void OnNetRateChanged(ResourceType changed, int perDay)
        {
            if (changed != type || deltaLabel == null) return;
            deltaLabel.text = perDay > 0 ? "+" + perDay : perDay < 0 ? "−" + Mathf.Abs(perDay) : "0";
            if (style != null) deltaLabel.color = style.DeltaColor(perDay);
        }
    }
}
