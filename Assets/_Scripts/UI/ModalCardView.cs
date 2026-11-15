using System;
using ArtemisX.Core;
using ArtemisX.Story;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Centered modal card used by the morning sequence (Daily Report, Unexpected Demand, Space Weather Choice).
    /// A full-screen blocker behind it stops clicks reaching the base.
    /// </summary>
    public class ModalCardView : MonoBehaviour
    {
        [Serializable]
        public class DeltaCell
        {
            public GameObject root;
            public Image icon;
            public Text amount;
        }

        [SerializeField] private UIStyle style;
        [SerializeField] private GameplayUIConfig config;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform card;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image bannerBackground;
        [SerializeField] private Text bannerText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private GameObject deltaRow;
        [SerializeField] private DeltaCell[] deltaCells = Array.Empty<DeltaCell>();
        [SerializeField] private Image bauzerPortrait;
        [SerializeField] private Text bauzerName;
        [SerializeField] private Text bauzerText;
        [SerializeField] private Button buttonA;
        [SerializeField] private Text labelA;
        [SerializeField] private Button buttonB;
        [SerializeField] private Text labelB;

        private Action actionA;
        private Action actionB;
        private bool showing;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (buttonA != null) buttonA.onClick.AddListener(OnA);
            if (buttonB != null) buttonB.onClick.AddListener(OnB);
            ApplyFonts();
            // Awake runs on the first Show when this object starts inactive; don't hide it then.
            if (!showing && root != null) root.SetActive(false);
        }

        private void OnDestroy()
        {
            if (buttonA != null) buttonA.onClick.RemoveListener(OnA);
            if (buttonB != null) buttonB.onClick.RemoveListener(OnB);
        }

        public void Show(ModalCardContent content)
        {
            showing = true;
            if (bannerText != null) bannerText.text = content.banner ?? string.Empty;
            if (bannerBackground != null)
            {
                bannerBackground.gameObject.SetActive(!string.IsNullOrEmpty(content.banner));
                if (bannerBackground.enabled)
                    bannerBackground.color = content.bannerColor.a > 0f ? content.bannerColor : (style != null ? style.progressColor : Color.gray);
            }
            if (titleText != null) titleText.text = content.title ?? string.Empty;
            if (bodyText != null)
            {
                bodyText.text = content.body ?? string.Empty;
                bodyText.gameObject.SetActive(!string.IsNullOrEmpty(content.body));
            }

            ShowDeltas(content.deltas);
            ShowBauzer(content.bauzerLine, content.bauzerMood);

            actionA = content.actionA;
            actionB = content.actionB;
            if (labelA != null) labelA.text = content.labelA ?? string.Empty;
            if (buttonA != null)
            {
                buttonA.gameObject.SetActive(!string.IsNullOrEmpty(content.labelA));
                buttonA.interactable = content.enabledA;
            }
            if (labelB != null) labelB.text = content.labelB ?? string.Empty;
            if (buttonB != null)
            {
                buttonB.gameObject.SetActive(!string.IsNullOrEmpty(content.labelB));
                buttonB.interactable = content.enabledB;
            }

            if (root != null) root.SetActive(true);
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * 0.85f;
                card.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.18f).SetUpdate(true).SetLink(gameObject);
            }
        }

        public void Hide()
        {
            showing = false;
            actionA = null;
            actionB = null;
            if (card != null) card.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();
            if (root != null) root.SetActive(false);
        }

        private void OnA()
        {
            var a = actionA;
            a?.Invoke();
        }

        private void OnB()
        {
            var b = actionB;
            b?.Invoke();
        }

        private void ShowDeltas(int[] deltas)
        {
            if (deltaRow != null) deltaRow.SetActive(deltas != null);
            if (deltas == null || deltaCells == null) return;

            for (int i = 0; i < deltaCells.Length; i++)
            {
                var cell = deltaCells[i];
                if (cell == null) continue;
                var type = (ResourceType)i;
                if (type == ResourceType.Oxygen)
                {
                    if (cell.root != null) cell.root.SetActive(false);
                    continue;
                }
                if (cell.root != null) cell.root.SetActive(true);
                int value = i < deltas.Length ? deltas[i] : 0;
                if (cell.icon != null && style != null && style.TryGetLook(type, out var look))
                {
                    cell.icon.sprite = look.icon;
                    cell.icon.color = look.icon != null ? Color.white : look.accent;
                }
                if (cell.amount != null)
                {
                    cell.amount.text = value > 0 ? "▲+" + value : value < 0 ? "▼−" + Mathf.Abs(value) : "0";
                    if (style != null) cell.amount.color = style.DeltaColor(value);
                }
            }
        }

        private void ShowBauzer(string line, SpeakerMood mood)
        {
            bool show = !string.IsNullOrEmpty(line);
            Speaker speaker = config != null ? config.bauzer : null;
            if (bauzerText != null)
            {
                bauzerText.transform.parent.gameObject.SetActive(show);
                bauzerText.text = show ? (runner != null ? runner.FormatText(line) : line) : string.Empty;
            }
            if (bauzerName != null) bauzerName.text = speaker != null ? speaker.displayName : "Kepler";
            if (bauzerPortrait != null)
            {
                bauzerPortrait.sprite = speaker != null ? speaker.GetPortrait(mood) : null;
                bauzerPortrait.enabled = bauzerPortrait.sprite != null;
            }
        }

        private void ApplyFonts()
        {
        }
    }
}
