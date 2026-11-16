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
    /// Supports both the standard Daily Report layout and the Toy UI Event Choice Cards layout.
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

        [Serializable]
        public class ChoiceCardSlot
        {
            public GameObject root;
            public Image icon;
            public Text title;
            public GameObject costRow;
            public Image costIcon;
            public Text costAmount;
            public GameObject outcomeRow;
            public Image outcomeIcon;
            public Text outcomeText;
            public Button button;
            public Image buttonImage;
            public Text buttonLabel;
            public Outline buttonOutline;
            public Shadow buttonShadow;
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
        [SerializeField] private GameObject buttonsRow;
        [SerializeField] private Button buttonA;
        [SerializeField] private Text labelA;
        [SerializeField] private Button buttonB;
        [SerializeField] private Text labelB;

        [Header("Toy UI Event Dialog")]
        [SerializeField] private Image eventIconImage;
        [SerializeField] private GameObject keplerBubbleRow;
        [SerializeField] private Image keplerBubblePortrait;
        [SerializeField] private Text keplerBubbleText;
        [SerializeField] private GameObject choiceCardsRow;
        [SerializeField] private ChoiceCardSlot[] choiceCards = new ChoiceCardSlot[2];
        [SerializeField] private GameObject footerLockRow;
        [SerializeField] private Text footerLockText;
        [SerializeField] private Sprite primaryNormalSprite;
        [SerializeField] private Sprite primaryPressedSprite;
        [SerializeField] private Sprite secondaryNormalSprite;
        [SerializeField] private Sprite secondaryPressedSprite;
        [SerializeField] private Sprite disabledButtonSprite;

        private Action actionA;
        private Action actionB;
        private readonly Action[] choiceActions = new Action[2];
        private bool showing;

        private static readonly Color InkColor = new Color(0.016f, 0.016f, 0.208f, 1f);
        private static readonly Color RiskyOutcomeColor = new Color(0.753f, 0.227f, 0.047f, 1f);

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (buttonA != null) buttonA.onClick.AddListener(OnA);
            if (buttonB != null) buttonB.onClick.AddListener(OnB);
            if (choiceCards != null)
            {
                for (int i = 0; i < choiceCards.Length; i++)
                {
                    int idx = i;
                    if (choiceCards[i] != null && choiceCards[i].button != null)
                        choiceCards[i].button.onClick.AddListener(() => OnChoiceClicked(idx));
                }
            }
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

            bool eventMode = content.choices != null && content.choices.Length > 0 && choiceCardsRow != null;

            if (eventIconImage != null)
            {
                bool showIcon = eventMode && content.eventIcon != null;
                eventIconImage.gameObject.SetActive(showIcon);
                if (showIcon)
                {
                    eventIconImage.sprite = content.eventIcon;
                    eventIconImage.color = Color.white;
                }
            }

            if (eventMode)
                ShowEventDialog(content);
            else
                ShowReportDialog(content);

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

        private void ShowReportDialog(ModalCardContent content)
        {
            if (card != null) card.sizeDelta = new Vector2(660f, 560f);
            if (keplerBubbleRow != null) keplerBubbleRow.SetActive(false);
            if (choiceCardsRow != null) choiceCardsRow.SetActive(false);
            if (footerLockRow != null) footerLockRow.SetActive(false);

            if (titleText != null)
            {
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(content.title));
                titleText.text = content.title ?? string.Empty;
            }
            if (bodyText != null)
            {
                bodyText.text = content.body ?? string.Empty;
                bodyText.gameObject.SetActive(!string.IsNullOrEmpty(content.body));
            }

            ShowDeltas(content.deltas);
            ShowBauzer(content.bauzerLine, content.bauzerMood);

            actionA = content.actionA;
            actionB = content.actionB;
            if (buttonsRow != null) buttonsRow.SetActive(true);
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
        }

        private void ShowEventDialog(ModalCardContent content)
        {
            if (titleText != null) titleText.gameObject.SetActive(false);
            if (bodyText != null) bodyText.gameObject.SetActive(false);
            if (deltaRow != null) deltaRow.SetActive(false);
            if (bauzerText != null) bauzerText.transform.parent.gameObject.SetActive(false);
            if (buttonsRow != null) buttonsRow.SetActive(false);
            actionA = null;
            actionB = null;

            bool anyOutcome = false;
            if (content.choices != null)
            {
                foreach (var c in content.choices)
                    if (!string.IsNullOrEmpty(c.outcomeText)) { anyOutcome = true; break; }
            }
            if (card != null) card.sizeDelta = new Vector2(600f, anyOutcome ? 540f : 500f);

            if (keplerBubbleRow != null)
            {
                bool showKepler = !string.IsNullOrEmpty(content.bauzerLine);
                keplerBubbleRow.SetActive(showKepler);
                if (showKepler)
                {
                    Speaker speaker = config != null ? config.bauzer : null;
                    if (keplerBubblePortrait != null)
                    {
                        keplerBubblePortrait.sprite = speaker != null ? speaker.GetPortrait(content.bauzerMood) : null;
                        keplerBubblePortrait.enabled = keplerBubblePortrait.sprite != null;
                    }
                    if (keplerBubbleText != null)
                        keplerBubbleText.text = runner != null ? runner.FormatText(content.bauzerLine) : content.bauzerLine;
                }
            }

            if (choiceCardsRow != null) choiceCardsRow.SetActive(true);
            for (int i = 0; i < choiceCards.Length; i++)
            {
                var slot = choiceCards[i];
                if (slot == null || slot.root == null) continue;
                bool has = content.choices != null && i < content.choices.Length;
                slot.root.SetActive(has);
                choiceActions[i] = has ? content.choices[i].action : null;
                if (!has) continue;

                var c = content.choices[i];
                if (slot.icon != null)
                {
                    slot.icon.enabled = c.icon != null;
                    slot.icon.sprite = c.icon;
                    slot.icon.color = Color.white;
                }
                if (slot.title != null) slot.title.text = c.title ?? string.Empty;

                if (slot.costRow != null)
                {
                    bool hasCost = c.costAmount > 0;
                    slot.costRow.SetActive(hasCost);
                    if (hasCost)
                    {
                        if (slot.costIcon != null && style != null && style.TryGetLook(c.costType, out var look))
                        {
                            slot.costIcon.sprite = look.icon;
                            slot.costIcon.color = Color.white;
                        }
                        if (slot.costAmount != null)
                            slot.costAmount.text = "−" + c.costAmount;
                    }
                }

                if (slot.outcomeRow != null)
                {
                    bool hasOutcome = !string.IsNullOrEmpty(c.outcomeText);
                    slot.outcomeRow.SetActive(hasOutcome);
                    if (hasOutcome)
                    {
                        if (slot.outcomeIcon != null)
                        {
                            slot.outcomeIcon.gameObject.SetActive(c.outcomeIcon != null);
                            slot.outcomeIcon.sprite = c.outcomeIcon;
                            slot.outcomeIcon.color = Color.white;
                        }
                        if (slot.outcomeText != null)
                        {
                            slot.outcomeText.text = c.outcomeText;
                            slot.outcomeText.color = c.outcomeRisky ? RiskyOutcomeColor : InkColor;
                        }
                    }
                }

                bool primary = i == 0;
                if (slot.button != null)
                {
                    slot.button.interactable = c.enabled;
                    var st = slot.button.spriteState;
                    st.highlightedSprite = primary ? primaryNormalSprite : secondaryNormalSprite;
                    st.pressedSprite = primary ? primaryPressedSprite : secondaryPressedSprite;
                    st.disabledSprite = disabledButtonSprite;
                    slot.button.spriteState = st;
                }
                if (slot.buttonImage != null)
                {
                    slot.buttonImage.sprite = c.enabled
                        ? (primary ? primaryNormalSprite : secondaryNormalSprite)
                        : (disabledButtonSprite != null ? disabledButtonSprite : (primary ? primaryNormalSprite : secondaryNormalSprite));
                    slot.buttonImage.color = Color.white;
                }
                if (slot.buttonLabel != null)
                {
                    slot.buttonLabel.text = c.buttonLabel ?? string.Empty;
                    bool whiteText = primary || !c.enabled;
                    slot.buttonLabel.color = whiteText ? Color.white : InkColor;
                    if (slot.buttonOutline != null) slot.buttonOutline.enabled = whiteText;
                    if (slot.buttonShadow != null) slot.buttonShadow.enabled = whiteText;
                }
            }

            if (footerLockRow != null)
            {
                bool showFooter = !string.IsNullOrEmpty(content.footerHint);
                footerLockRow.SetActive(showFooter);
                if (showFooter && footerLockText != null)
                {
                    footerLockText.text = content.footerHint;
                    footerLockText.alignment = TextAnchor.MiddleLeft;
                    footerLockText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    var footerLe = footerLockText.GetComponent<LayoutElement>();
                    if (footerLe == null) footerLe = footerLockText.gameObject.AddComponent<LayoutElement>();
                    footerLe.preferredWidth = footerLockText.preferredWidth;
                }
            }

            ClampChoiceCardsToRow();
        }

        /// <summary>
        /// Trays must never grow past the cards row (they would cover the footer and spill out of the card):
        /// drop their minimum height to 0 and let them flex to the row height, then rebuild now.
        /// </summary>
        private void ClampChoiceCardsToRow()
        {
            if (choiceCards == null) return;
            foreach (var slot in choiceCards)
            {
                if (slot == null || slot.root == null) continue;
                var le = slot.root.GetComponent<LayoutElement>();
                if (le == null) le = slot.root.AddComponent<LayoutElement>();
                le.minHeight = 0f;
                le.preferredHeight = -1f;
                le.flexibleHeight = 1f;
                le.flexibleWidth = 1f;
                le.preferredWidth = 0f;
            }
            if (choiceCardsRow != null && choiceCardsRow.transform is RectTransform rowRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowRect);
        }

        public void Hide()
        {
            showing = false;
            actionA = null;
            actionB = null;
            for (int i = 0; i < choiceActions.Length; i++) choiceActions[i] = null;
            if (card != null) card.DOKill();
            if (canvasGroup != null) canvasGroup.DOKill();
            if (root != null) root.SetActive(false);
        }

        private void OnA() => actionA?.Invoke();
        private void OnB() => actionB?.Invoke();

        private void OnChoiceClicked(int idx)
        {
            if (idx < 0 || idx >= choiceActions.Length) return;
            choiceActions[idx]?.Invoke();
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
    }
}
