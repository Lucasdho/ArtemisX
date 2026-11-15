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
        [SerializeField] private Row[] rows;
        [Tooltip("Optional. Rows light up on BuildingCompleted.")]
        [SerializeField] private BuildingManager buildings;

        private void Awake()
        {
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
            if (buildings == null || buildings.Config == null) return;
            for (int i = 0; i < rows.Length && i < buildings.Config.Count; i++)
            {
                if (rows[i].label != null) rows[i].label.text = "Star " + (i + 1) + ": " + buildings.Config.buildings[i].displayName;
                SetProgress(i, buildings.IsBuilt(i) ? 1f : 0f);
            }
        }

        private void OnBuildingCompleted(int index)
        {
            SetProgress(index, 1f);
            if (index >= 0 && index < rows.Length && rows[index].checkCircle != null)
            {
                var t = rows[index].checkCircle.transform;
                t.localScale = Vector3.one * 1.6f;
                DG.Tweening.ShortcutExtensions.DOScale(t, 1f, 0.4f);
            }
        }

        /// <summary>Progress 0 to 1 for goal <paramref name="index"/>; 1 marks it done.</summary>
        public void SetProgress(int index, float progress)
        {
            if (rows == null || index < 0 || index >= rows.Length) return;

            var row = rows[index];
            float p = Mathf.Clamp01(progress);
            bool done = p >= 1f;

            if (row.fillMask != null)
            {
                var anchor = row.fillMask.anchorMax;
                anchor.x = p;
                row.fillMask.anchorMax = anchor;
            }

            if (style == null) return;
            if (row.track != null) row.track.color = style.trackColor;
            if (row.fill != null) row.fill.color = done ? style.doneColor : style.progressColor;
            if (row.checkCircle != null) row.checkCircle.color = done ? style.doneColor : style.trackColor;
            if (row.label != null)
            {
                row.label.color = style.textColor;
                if (style.font != null) row.label.font = style.font;
            }
        }
    }
}
