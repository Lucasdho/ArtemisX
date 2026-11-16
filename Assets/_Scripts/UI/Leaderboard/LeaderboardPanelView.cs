using System.Collections.Generic;
using ArtemisX.Story;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// "All missions" panel opened from the end screen: Kepler's comparison line and a per-day bar chart built
    /// from the run_histogram rows EndScreenView already fetched. Purely visual; it never calls the network.
    /// </summary>
    public class LeaderboardPanelView : MonoBehaviour
    {
        [SerializeField] private ScreensConfig config;
        [SerializeField] private GameObject root;
        [SerializeField] private GameObject endScreenCard;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text keplerLine;
        [SerializeField] private GameObject chartArea;
        [SerializeField] private RectTransform barsContainer;
        [SerializeField] private RectTransform labelsContainer;
        [SerializeField] private RectTransform youMarker;
        [SerializeField] private Sprite barSprite;
        [SerializeField] private Font labelFont;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Text backLabel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Text playAgainLabel;
        [SerializeField] private int missionLength = 30;
        [SerializeField] private Color barColor = new Color32(0x6B, 0xB5, 0xD6, 0xFF);
        [SerializeField] private Color ownBarColor = new Color32(0xFF, 0x5A, 0x1F, 0xFF);
        [SerializeField] private Color emptyBarColor = new Color32(0xC9, 0xD3, 0xE3, 0xFF);
        [SerializeField] private Color labelColor = new Color32(0x56, 0x65, 0x7F, 0xFF);
        [SerializeField] private string highlightHex = "#16803C";
        [SerializeField] private int labelFontSize = 13;

        private enum State { Loading, Offline, Online }

        private readonly List<RectTransform> bars = new List<RectTransform>();
        private readonly List<float> heights = new List<float>();
        private MissionRunRecord record;
        private LeaderboardClient.HistogramRow[] rows;
        private State state = State.Loading;
        private int ownBar = -1;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backButton != null) backButton.onClick.AddListener(Close);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(GameFlowController.PlayAgain);
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (backButton != null) backButton.onClick.RemoveListener(Close);
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(GameFlowController.PlayAgain);
        }

        /// <summary>Called by EndScreenView when the run is shown, before the leaderboard answers.</summary>
        public void SetLoading(MissionRunRecord run)
        {
            record = run;
            rows = null;
            state = State.Loading;
            if (IsOpen) Refresh(true);
        }

        /// <summary>Called by EndScreenView with the histogram it fetched (online == false: offline).</summary>
        public void SetResult(MissionRunRecord run, bool online, LeaderboardClient.HistogramRow[] histogram)
        {
            record = run;
            rows = histogram;
            state = online && histogram != null ? State.Online : State.Offline;
            if (IsOpen) Refresh(true);
        }

        public void Open()
        {
            if (endScreenCard != null) endScreenCard.SetActive(false);
            if (root != null) root.SetActive(true);
            Refresh(true);
        }

        public void Close()
        {
            if (root != null) root.SetActive(false);
            if (endScreenCard != null) endScreenCard.SetActive(true);
        }

        private void Refresh(bool animate)
        {
            if (config == null) return;
            if (titleLabel != null) titleLabel.text = config.leaderboardTitle;
            if (backLabel != null) backLabel.text = config.leaderboardBackLabel;
            if (playAgainLabel != null) playAgainLabel.text = config.playAgainLabel;

            bool extended = record.endReason != MissionEndReason.EarlyWin;
            int day = Mathf.Clamp(record.daysTaken, 1, missionLength);
            var counts = new int[missionLength + 1]; // 0..29 = days 1..30, last = extended
            int total = 0;
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (row == null || row.runs <= 0) continue;
                    int index = row.end_reason == "day_cap" ? missionLength : Mathf.Clamp(row.days_taken, 1, missionLength) - 1;
                    counts[index] += row.runs;
                    total += row.runs;
                }
            }

            if (keplerLine != null) keplerLine.text = BuildLine(extended, day, counts, total);
            bool showChart = state == State.Online;
            if (chartArea != null) chartArea.SetActive(showChart);
            if (showChart) BuildChart(counts, extended ? missionLength : day - 1, animate);
        }

        private string BuildLine(bool extended, int day, int[] counts, int total)
        {
            if (state == State.Loading) return config.loadingText;
            if (state == State.Offline) return config.leaderboardOfflineLine;
            if (total < config.leaderboardMinRunsForStats) return config.leaderboardFirstCrewsLine;
            int dayCap = counts[missionLength];
            if (extended) return string.Format(config.leaderboardExtendedLine, Highlight(InTen(dayCap, total)));
            int slower = dayCap;
            for (int i = day; i < missionLength; i++) slower += counts[i]; // index i = day i+1 > day
            return string.Format(config.leaderboardFasterLine, day, Highlight(InTen(slower, total)));
        }

        private static int InTen(int part, int total) => Mathf.Clamp(Mathf.RoundToInt(10f * part / Mathf.Max(1, total)), 1, 9);

        private string Highlight(int k) => "<color=" + highlightHex + ">" + k + "</color>";

        private void BuildChart(int[] counts, int ownIndex, bool animate)
        {
            if (barsContainer == null) return;
            ClearChart();

            int firstDay = Mathf.Min(config.leaderboardFirstDay, missionLength);
            for (int i = 0; i < missionLength; i++)
                if (counts[i] > 0 || i == ownIndex) { firstDay = Mathf.Min(firstDay, i + 1); break; }

            int dayBars = missionLength - firstDay + 1;
            float gap = config.leaderboardBarGap;
            float width = config.leaderboardBarWidth;
            float extWidth = config.leaderboardExtendedWidth;
            float extGap = config.leaderboardExtendedGap;
            float available = barsContainer.rect.width;
            float needed = dayBars * (width + gap) - gap + extGap + extWidth;
            if (available > 0f && needed > available)
            {
                width = Mathf.Max(4f, (available - extGap - extWidth - gap * (dayBars - 1)) / dayBars);
                needed = dayBars * (width + gap) - gap + extGap + extWidth;
            }

            int max = 1;
            foreach (int c in counts) max = Mathf.Max(max, c);

            float x = -needed * 0.5f;
            for (int i = firstDay - 1; i <= missionLength; i++)
            {
                bool isExt = i == missionLength;
                if (isExt) x += extGap - gap;
                float w = isExt ? extWidth : width;
                int value = counts[i];
                bool own = i == ownIndex;
                if (own) value = Mathf.Max(value, 1);
                float h = value > 0 ? Mathf.Max(config.leaderboardBarMinHeight, config.leaderboardBarMaxHeight * value / max)
                                    : config.leaderboardBarMinHeight;
                Color color = own ? ownBarColor : isExt || value == 0 ? emptyBarColor : barColor;
                var bar = NewBar("Bar_" + (isExt ? "Extended" : (i + 1).ToString()), x + w * 0.5f, w, color);
                bars.Add(bar);
                heights.Add(h);
                if (own) ownBar = bars.Count - 1;

                int dayNumber = i + 1;
                // The last day label ends at its bar and "extended" starts at its bar so the two never overlap.
                if (isExt) AddLabel(config.leaderboardExtendedLabel, x, 0f);
                else if (dayNumber == missionLength) AddLabel(string.Format(config.leaderboardDayLabel, dayNumber), x + w, 1f);
                else if (dayNumber % 10 == 0) AddLabel(string.Format(config.leaderboardDayLabel, dayNumber), x + w * 0.5f, 0.5f);
                x += w + gap;
            }

            for (int i = 0; i < bars.Count; i++)
            {
                var bar = bars[i];
                float target = heights[i];
                bar.DOKill();
                if (!animate)
                {
                    bar.sizeDelta = new Vector2(bar.sizeDelta.x, target);
                    continue;
                }
                bar.sizeDelta = new Vector2(bar.sizeDelta.x, 0f);
                bar.DOSizeDelta(new Vector2(bar.sizeDelta.x, target), config.leaderboardGrowDuration)
                   .SetDelay(i * config.leaderboardGrowStagger).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(bar.gameObject);
            }

            if (youMarker != null)
            {
                youMarker.gameObject.SetActive(ownBar >= 0);
                if (ownBar >= 0)
                {
                    var bar = bars[ownBar];
                    youMarker.SetAsLastSibling();
                    float baseY = youMarker.rect.height * 0.5f + 2f;
                    youMarker.anchoredPosition = new Vector2(bar.anchoredPosition.x, baseY + (animate ? 0f : heights[ownBar]));
                    youMarker.DOKill();
                    if (animate)
                        youMarker.DOAnchorPosY(baseY + heights[ownBar], config.leaderboardGrowDuration)
                                 .SetDelay(ownBar * config.leaderboardGrowStagger).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(youMarker.gameObject);
                }
            }
        }

        private RectTransform NewBar(string name, float centerX, float width, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.layer = barsContainer.gameObject.layer;
            go.transform.SetParent(barsContainer, false);
            var image = go.GetComponent<Image>();
            image.sprite = barSprite;
            image.type = barSprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.pixelsPerUnitMultiplier = 2f;
            image.color = color;
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(centerX, 0f);
            rt.sizeDelta = new Vector2(width, 0f);
            return rt;
        }

        private void AddLabel(string text, float x, float pivotX)
        {
            var parent = labelsContainer != null ? labelsContainer : barsContainer;
            var go = new GameObject("Label_" + text, typeof(RectTransform), typeof(Text));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = labelFont;
            label.fontSize = labelFontSize;
            label.color = labelColor;
            label.alignment = pivotX <= 0f ? TextAnchor.MiddleLeft : pivotX >= 1f ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.raycastTarget = false;
            label.text = text;
            var rt = label.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(pivotX, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(80f, 20f);
        }

        private void ClearChart()
        {
            foreach (var bar in bars) if (bar != null) { bar.DOKill(); Destroy(bar.gameObject); }
            bars.Clear();
            heights.Clear();
            ownBar = -1;
            if (labelsContainer != null)
                for (int i = labelsContainer.childCount - 1; i >= 0; i--) Destroy(labelsContainer.GetChild(i).gameObject);
        }
    }
}
