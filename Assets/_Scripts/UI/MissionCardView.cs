using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Card listing the three self-sufficiency goals. Goal tracking does not exist in the sim yet,
    /// so rows stay at zero until something calls SetProgress.
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

        private void Awake()
        {
            for (int i = 0; i < rows.Length; i++) SetProgress(i, 0f);
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
