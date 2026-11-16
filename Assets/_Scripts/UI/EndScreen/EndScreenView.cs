using ArtemisX.Simulation;
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
#pragma warning disable 0414
        // Legacy: the histogram moved to LeaderboardPanelView; kept so the scene's references stay intact.
        [SerializeField] private Font labelFont;
        [SerializeField] private int missionLength = 30;
#pragma warning restore 0414

        [Header("Toy UI")]
        [SerializeField] private BuildingManager buildings;
        [SerializeField] private RectTransform ribbon;
        [SerializeField] private RectTransform rays;
        [SerializeField] private Color starOffTint = new Color(0x3A / 255f, 0x3A / 255f, 0x55 / 255f, 0.6f);
        [SerializeField] private Sprite portraitOverride;
        [SerializeField] private Text daysValue;
        [SerializeField] private Text daysCaption;
        [SerializeField] private GameObject builtTile;
        [SerializeField] private Text builtValue;
        [SerializeField] private Text builtCaption;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Text leaderboardLabel;
        [SerializeField] private LeaderboardPanelView leaderboardPanel;

        private MissionRunRecord record;

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
            if (playAgainButton != null) playAgainButton.onClick.AddListener(GameFlowController.PlayAgain);
            if (leaderboardButton != null) leaderboardButton.onClick.AddListener(OpenLeaderboard);
        }

        private void Update()
        {
            if (rays != null && IsOpen && config != null)
                rays.Rotate(0f, 0f, -360f / config.raysTurnSeconds * Time.unscaledDeltaTime);
        }

        private void OpenLeaderboard()
        {
            if (leaderboardPanel != null) leaderboardPanel.Open();
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
            if (leaderboardButton != null) leaderboardButton.onClick.RemoveListener(OpenLeaderboard);
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
                stars[i].color = i < run.stars ? Color.white : starOffTint;
            }

            var grade = PickGrade(run);
            var bauzer = uiConfig != null ? uiConfig.bauzer : null;
            if (bauzerLine != null) bauzerLine.text = runner != null ? runner.FormatText(grade.text) : grade.text;
            if (bauzerPortrait != null)
            {
                bauzerPortrait.sprite = portraitOverride != null ? portraitOverride
                                      : bauzer != null ? bauzer.GetPortrait(grade.mood) : null;
                bauzerPortrait.enabled = bauzerPortrait.sprite != null;
            }

            if (histogramTitle != null) histogramTitle.text = config.histogramTitle;
            HideLegacy();
            if (playAgainLabel != null) playAgainLabel.text = config.playAgainLabel;
            if (statusLabel != null) statusLabel.text = config.loadingText;
            if (daysValue != null) daysValue.text = run.daysTaken.ToString();
            if (daysCaption != null) daysCaption.text = config.daysCaption;
            if (builtTile != null) builtTile.SetActive(buildings != null);
            if (builtValue != null && buildings != null) builtValue.text = buildings.BuiltCount.ToString();
            if (builtCaption != null) builtCaption.text = config.builtCaption;
            if (leaderboardLabel != null) leaderboardLabel.text = config.leaderboardButtonLabel;
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetLoading(run);
                leaderboardPanel.Close();
            }

            if (root != null) root.SetActive(true);
            PlayIntro(run.stars);

            if (leaderboard != null) leaderboard.SubmitAndFetch(ToPayload(run), OnLeaderboard);
            else OnLeaderboard(false, null);
        }

        /// <summary>The old subtitle, histogram and status line stay in the scene but are hidden on this screen.</summary>
        private void HideLegacy()
        {
            if (subtitleLabel != null) subtitleLabel.gameObject.SetActive(false);
            if (histogramTitle != null) histogramTitle.gameObject.SetActive(false);
            if (barsContainer != null && barsContainer.parent != null) barsContainer.parent.gameObject.SetActive(false);
            if (statusLabel != null) statusLabel.gameObject.SetActive(false);
        }

        private void PlayIntro(int earned)
        {
            var seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);
            if (card != null)
            {
                card.DOKill();
                card.localScale = Vector3.one * config.cardScaleFrom;
                seq.Insert(0f, card.DOScale(1f, config.cardScaleDuration).SetEase(Ease.OutBack));
            }

            float t = config.ribbonDropDelay;
            if (ribbon != null)
            {
                ribbon.DOKill();
                Vector2 rest = ribbon.anchoredPosition;
                var group = ribbon.GetComponent<CanvasGroup>();
                ribbon.anchoredPosition = rest + Vector2.up * config.ribbonDropDistance;
                if (group != null) group.alpha = 0f;
                seq.Insert(t, ribbon.DOAnchorPos(rest, config.ribbonDropDuration).SetEase(Ease.OutBack));
                if (group != null) seq.Insert(t, group.DOFade(1f, config.ribbonDropDuration * 0.5f));
                t += config.ribbonDropDuration;
            }

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                var star = stars[i].rectTransform;
                star.DOKill();
                star.localScale = Vector3.zero;
                float at = t + i * config.starPopDuration;
                seq.InsertCallback(at, () => star.localScale = Vector3.one * config.starPopScale);
                seq.Insert(at, star.DOScale(1f, config.starPopDuration).SetEase(Ease.OutQuad));
            }
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
            if (statusLabel != null) statusLabel.text = online && rows != null ? string.Empty : config.offlineText;
            if (leaderboardPanel != null) leaderboardPanel.SetResult(record, online, rows);
        }
    }
}
