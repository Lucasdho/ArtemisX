using System.Collections.Generic;
using ArtemisX.Story;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// End screen shown after Bauzer's mission-end debrief (StoryDirector.LeaderboardHandoffRequested):
    /// stars, days, Bauzer's grade, the online run histogram (days 1..30 + Mission Extended) and Play Again.
    /// </summary>
    public class EndScreenView : MonoBehaviour
    {
        [SerializeField] private ScreensConfig config;
        [SerializeField] private GameplayUIConfig uiConfig;
        [SerializeField] private StoryDirector story;
        [SerializeField] private DialogueRunner runner;
        [SerializeField] private LeaderboardClient leaderboard;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform card;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text subtitleLabel;
        [SerializeField] private RawImage[] stars = new RawImage[3];
        [SerializeField] private Texture starOn;
        [SerializeField] private Texture starOff;
        [SerializeField] private Image bauzerPortrait;
        [SerializeField] private Text bauzerLine;
        [SerializeField] private Text histogramTitle;
        [SerializeField] private RectTransform barsContainer;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Text playAgainLabel;
        [SerializeField] private Font labelFont;
        [SerializeField] private int missionLength = 30;

        private readonly List<Image> bars = new List<Image>();
        private MissionRunRecord record;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(GameFlowController.PlayAgain);
        }

        private void OnEnable()
        {
            if (story != null) story.LeaderboardHandoffRequested += Show;
        }

        private void OnDisable()
        {
            if (story != null) story.LeaderboardHandoffRequested -= Show;
        }

        private void OnDestroy()
        {
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(GameFlowController.PlayAgain);
        }

        /// <summary>Shows the player's result right away, then fills the histogram when the leaderboard answers.</summary>
        public void Show(MissionRunRecord run)
        {
            record = run;
            bool win = run.endReason == MissionEndReason.EarlyWin;
            if (titleLabel != null) titleLabel.text = win ? config.earlyWinTitle : config.dayCapTitle;
            if (subtitleLabel != null)
                subtitleLabel.text = win ? string.Format(config.earlyWinSubtitle, run.daysTaken)
                                         : string.Format(config.dayCapSubtitle, run.daysTaken, run.stars);
            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].texture = i < run.stars ? starOn : starOff;
                stars[i].color = i < run.stars ? Color.white : new Color(1f, 1f, 1f, 0.20f);
            }

            var grade = PickGrade(run);
            var bauzer = uiConfig != null ? uiConfig.bauzer : null;
            if (bauzerLine != null) bauzerLine.text = runner != null ? runner.FormatText(grade.text) : grade.text;
            if (bauzerPortrait != null)
            {
                bauzerPortrait.sprite = bauzer != null ? bauzer.GetPortrait(grade.mood) : null;
                bauzerPortrait.enabled = bauzerPortrait.sprite != null;
            }

            if (histogramTitle != null) histogramTitle.text = config.histogramTitle;
            if (playAgainLabel != null) playAgainLabel.text = config.playAgainLabel;
            if (statusLabel != null) statusLabel.text = config.loadingText;
            DrawBars(null);

            if (root != null) root.SetActive(true);
            if (card != null)
            {
                card.localScale = Vector3.one * 0.85f;
                card.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(gameObject);
            }

            if (leaderboard != null) leaderboard.SubmitAndFetch(ToPayload(run), OnLeaderboard);
            else OnLeaderboard(false, null);
        }

        private ScreensConfig.Line PickGrade(MissionRunRecord run)
        {
            if (run.endReason == MissionEndReason.EarlyWin && run.stars >= 3 && run.daysTaken <= config.fastRunDay && !string.IsNullOrEmpty(config.fastRunLine.text))
                return config.fastRunLine;
            if (config.gradeLines == null || config.gradeLines.Length == 0) return default;
            return config.gradeLines[Mathf.Clamp(run.stars, 0, config.gradeLines.Length - 1)];
        }

        private static LeaderboardClient.RunPayload ToPayload(MissionRunRecord run)
        {
            int character = 0;
            if (!string.IsNullOrEmpty(run.character))
            {
                int underscore = run.character.LastIndexOf('_');
                int.TryParse(underscore >= 0 ? run.character.Substring(underscore + 1) : run.character, out character);
            }
            return new LeaderboardClient.RunPayload
            {
                nickname = run.nickname,
                character_index = Mathf.Clamp(character, 0, 3),
                days_taken = Mathf.Clamp(run.daysTaken, 1, 30),
                stars = Mathf.Clamp(run.stars, 0, 3),
                end_reason = run.endReason == MissionEndReason.EarlyWin ? "early_win" : "day_cap"
            };
        }

        private void OnLeaderboard(bool online, LeaderboardClient.HistogramRow[] rows)
        {
            if (!IsOpen) return;
            if (!online || rows == null)
            {
                if (statusLabel != null) statusLabel.text = config.offlineText;
                DrawBars(null);
                return;
            }

            var counts = new int[missionLength + 1]; // 0..29 = days 1..30, last = Mission Extended
            int total = 0;
            foreach (var row in rows)
            {
                if (row == null || row.runs <= 0) continue;
                int index = row.end_reason == "day_cap" ? missionLength : Mathf.Clamp(row.days_taken, 1, missionLength) - 1;
                counts[index] += row.runs;
                total += row.runs;
            }
            if (statusLabel != null) statusLabel.text = string.Format(config.onlineText, total);
            DrawBars(counts);
        }

        /// <summary>counts == null: only the player's own run (offline / loading).</summary>
        private void DrawBars(int[] counts)
        {
            if (barsContainer == null) return;
            EnsureBars();
            int own = record.endReason == MissionEndReason.EarlyWin ? Mathf.Clamp(record.daysTaken, 1, missionLength) - 1 : missionLength;
            int max = 1;
            if (counts != null) foreach (int c in counts) max = Mathf.Max(max, c);
            for (int i = 0; i < bars.Count; i++)
            {
                int value = counts != null ? counts[i] : (i == own ? 1 : 0);
                if (i == own) value = Mathf.Max(value, 1);
                float height = value > 0 ? Mathf.Max(0.04f, value / (float)max) : 0.015f;
                var rt = bars[i].rectTransform;
                rt.anchorMax = new Vector2(1f, height);
                bars[i].color = i == own ? config.ownBarColor : config.barColor;
            }
        }

        private void EnsureBars()
        {
            if (bars.Count == missionLength + 1) return;
            for (int i = 0; i <= missionLength; i++)
            {
                var slot = new GameObject("Bar_" + (i + 1), typeof(RectTransform));
                slot.transform.SetParent(barsContainer, false);
                var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
                area.SetParent(slot.transform, false);
                area.anchorMin = Vector2.zero;
                area.anchorMax = Vector2.one;
                area.offsetMin = new Vector2(2f, 26f);
                area.offsetMax = new Vector2(-2f, 0f);
                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(area, false);
                var fill = fillGo.GetComponent<Image>();
                fill.raycastTarget = false;
                var frt = fill.rectTransform;
                frt.anchorMin = new Vector2(0f, 0f);
                frt.anchorMax = new Vector2(1f, 0.02f);
                frt.offsetMin = Vector2.zero;
                frt.offsetMax = Vector2.zero;
                bars.Add(fill);

                bool extended = i == missionLength;
                int day = i + 1;
                if (extended || day == 1 || day % 5 == 0)
                {
                    var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                    labelGo.transform.SetParent(slot.transform, false);
                    var label = labelGo.GetComponent<Text>();
                    label.font = labelFont;
                    label.fontSize = 16;
                    label.alignment = TextAnchor.LowerCenter;
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.color = new Color(0.337f, 0.396f, 0.498f);
                    label.raycastTarget = false;
                    label.text = extended ? config.extendedBarLabel : day.ToString();
                    var lrt = label.rectTransform;
                    lrt.anchorMin = Vector2.zero;
                    lrt.anchorMax = new Vector2(1f, 0f);
                    lrt.sizeDelta = new Vector2(0f, 24f);
                    lrt.anchoredPosition = new Vector2(0f, 12f);
                }
            }
        }
    }
}
