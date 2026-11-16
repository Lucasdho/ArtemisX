using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Card listing the three self-sufficiency goals (one per linear building). With a BuildingManager assigned the
    /// labels come from BuildingConfig and each row lights up as soon as its building is completed.
    /// </summary>
    public class MissionCardView : MonoBehaviour
    {
        [System.Serializable]
        public class Row
        {
            public Text label;
            public Graphic checkCircle;
            public Graphic track;
            public RectTransform fillMask;
            public Graphic fill;
        }

        [SerializeField] private UIStyle style;
        [SerializeField] private Text headerLabel;
        [SerializeField] private Row[] rows;
        [Tooltip("Optional. Rows light up on BuildingCompleted.")]
        [SerializeField] private BuildingManager buildings;
        [Tooltip("Shown in place of the circle when a mission is completed.")]
        [SerializeField] private Sprite checkmarkSprite;

        private CameraPanController panController;
        private Sprite[] defaultCheckSprites;
        private string defaultHeaderText;
        private bool tutorialMode;
        private float baseCardHeight = -1f;

        private void Awake()
        {
            EnsureBaseHeight();
            if (headerLabel == null)
            {
                var title = transform.Find("Title");
                if (title != null) headerLabel = title.GetComponent<Text>();
            }
            if (headerLabel != null) defaultHeaderText = headerLabel.text;

            CacheDefaultCheckSprites();
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++) SetProgress(i, 0f);
        }

        private void OnEnable()
        {
            if (buildings != null) buildings.BuildingCompleted += OnBuildingCompleted;
        }

        private void OnDisable()
        {
            if (buildings != null) buildings.BuildingCompleted -= OnBuildingCompleted;
        }

        private void Start()
        {
            if (tutorialMode) return;
            if (buildings == null || buildings.Config == null) return;
            for (int i = 0; i < rows.Length && i < buildings.Config.Count; i++)
                SetProgress(i, buildings.IsBuilt(i) ? 1f : 0f);
            RefreshLabels();
        }

        private void OnBuildingCompleted(int index)
        {
            if (tutorialMode) return;
            void Reveal()
            {
                if (tutorialMode) return;
                SetProgress(index, 1f);
                RefreshLabels();
                AnimateCheckPop(index);
            }

            if (panController == null) panController = FindAnyObjectByType<CameraPanController>();
            if (panController != null && panController.isActiveAndEnabled) panController.ZoomOutToBase(Reveal);
            else Reveal();
        }

        private void RefreshLabels()
        {
            if (tutorialMode) return;
            if (buildings == null || buildings.Config == null || rows == null) return;
            for (int i = 0; i < rows.Length && i < buildings.Config.Count; i++)
            {
                var label = rows[i].label;
                if (label == null) continue;

                var b = buildings.Config.buildings[i];
                bool built = buildings.IsBuilt(i);
                bool isNext = !built && buildings.NextIndex == i;

                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 13;
                label.resizeTextMaxSize = 24;
                label.text = isNext
                    ? $"NEXT: {b.displayName}"
                    : $"Star {i + 1}: {b.displayName}";
                label.fontStyle = isNext ? FontStyle.Bold : FontStyle.Normal;
                if (style != null)
                    label.color = (!built && !isNext) ? style.mutedTextColor : style.textColor;
            }
        }

        /// <summary>Replaces the mission rows with tutorial tasks (all unchecked).</summary>
        public void ShowTutorial(string[] labels, string header = "Tutorial")
        {
            tutorialMode = true;
            if (headerLabel != null && !string.IsNullOrEmpty(header)) headerLabel.text = header;
            if (rows == null || labels == null) return;
            int visibleCount = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                bool used = i < labels.Length;
                SetRowVisible(i, used || i < 3);
                if (used || i < 3) visibleCount++;
                if (rows[i].label != null)
                {
                    rows[i].label.resizeTextForBestFit = true;
                    rows[i].label.resizeTextMinSize = 13;
                    rows[i].label.resizeTextMaxSize = 24;
                    rows[i].label.fontStyle = FontStyle.Normal;
                    if (used) rows[i].label.text = labels[i];
                    if (style != null) rows[i].label.color = style.textColor;
                }
                SetProgress(i, 0f);
            }
            UpdateCardHeight(visibleCount);
        }

        /// <summary>Checks off one tutorial task with a small pop animation.</summary>
        public void CompleteTutorialTask(int index)
        {
            if (rows == null || index < 0 || index >= rows.Length) return;
            SetProgress(index, 1f);
            AnimateCheckPop(index);
        }

        /// <summary>Tutorial finished: restores the real missions.</summary>
        public void ShowMissions()
        {
            tutorialMode = false;
            if (headerLabel != null && !string.IsNullOrEmpty(defaultHeaderText)) headerLabel.text = defaultHeaderText;
            if (rows == null) return;
            int missionCount = buildings != null && buildings.Config != null ? buildings.Config.Count : 3;
            for (int i = 0; i < rows.Length; i++)
            {
                SetRowVisible(i, i < missionCount);
                SetProgress(i, 0f);
            }
            if (buildings != null && buildings.Config != null)
                for (int i = 0; i < rows.Length && i < buildings.Config.Count; i++)
                    SetProgress(i, buildings.IsBuilt(i) ? 1f : 0f);
            UpdateCardHeight(missionCount);
            RefreshLabels();
        }

        private void SetRowVisible(int index, bool visible)
        {
            if (rows == null || index < 0 || index >= rows.Length) return;
            var row = rows[index];
            Transform rowRoot = row.label != null ? row.label.transform.parent : (row.track != null ? row.track.transform.parent : null);
            if (rowRoot != null && rowRoot != transform)
                rowRoot.gameObject.SetActive(visible);
        }

        private void EnsureBaseHeight()
        {
            if (baseCardHeight < 0f && transform is RectTransform rt)
                baseCardHeight = rt.sizeDelta.y;
        }

        private void UpdateCardHeight(int visibleRows)
        {
            EnsureBaseHeight();
            if (!(transform is RectTransform rt) || baseCardHeight <= 0f) return;
            int extra = Mathf.Max(0, visibleRows - 3);
            var size = rt.sizeDelta;
            size.y = baseCardHeight + extra * 66f;
            rt.sizeDelta = size;
        }

        private void AnimateCheckPop(int index)
        {
            if (rows == null || index < 0 || index >= rows.Length || rows[index].checkCircle == null) return;
            var t = rows[index].checkCircle.transform;
            t.localScale = Vector3.one * 1.6f;
            DG.Tweening.ShortcutExtensions.DOScale(t, 1f, 0.4f);
        }

        /// <summary>Progress 0 to 1 for goal <paramref name="index"/>; 1 marks it done.</summary>
        public void SetProgress(int index, float progress)
        {
            if (rows == null || index < 0 || index >= rows.Length) return;
            CacheDefaultCheckSprites();

            var row = rows[index];
            float p = Mathf.Clamp01(progress);
            bool done = p >= 1f;

            if (row.fillMask != null)
            {
                var anchor = row.fillMask.anchorMax;
                anchor.x = p;
                row.fillMask.anchorMax = anchor;
            }

            bool showCheckmark = done && checkmarkSprite != null;
            if (row.checkCircle != null)
            {
                if (row.checkCircle is UnityEngine.UI.Image checkImage)
                {
                    Sprite fallback = defaultCheckSprites != null && index < defaultCheckSprites.Length
                        ? defaultCheckSprites[index]
                        : null;
                    checkImage.sprite = showCheckmark ? checkmarkSprite : (fallback != null ? fallback : checkImage.sprite);
                    checkImage.preserveAspect = showCheckmark;
                }

                if (showCheckmark) row.checkCircle.color = Color.white;
                else if (style != null) row.checkCircle.color = done ? style.doneColor : style.trackColor;
            }

            if (style == null) return;
            if (row.track != null) row.track.color = style.trackColor;
            if (row.fill != null) row.fill.color = style.progressColor;
            if (row.label != null)
            {
                row.label.color = style.textColor;
                if (style.font != null) row.label.font = style.font;
            }
        }

        private void CacheDefaultCheckSprites()
        {
            if (rows == null || defaultCheckSprites != null) return;
            defaultCheckSprites = new Sprite[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].checkCircle is UnityEngine.UI.Image img) defaultCheckSprites[i] = img.sprite;
            }
        }
    }
}
