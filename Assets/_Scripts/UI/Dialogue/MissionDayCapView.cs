using ArtemisX.Simulation;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI.Dialogue
{
    /// <summary>Displays "Day X" alongside DayManager. The day cap is announced by Bauzer, not shown in the HUD.</summary>
    public class MissionDayCapView : MonoBehaviour
    {
        [SerializeField] private DayManager days;
        [SerializeField] private DialogueConfig config;
        [SerializeField] private UnityEngine.UI.Text label;

        private bool dirty;
        private int lastShownDay = -1;

        private int MissionLength => config != null ? Mathf.Max(1, config.missionLength) : 30;

        private void OnEnable()
        {
            if (days != null)
            {
                days.DayAdvanced += OnDayAdvanced;
                lastShownDay = days.CurrentDay;
                dirty = true;
            }
        }

        private void Start()
        {
            if (days != null)
            {
                lastShownDay = days.CurrentDay;
                dirty = true;
            }
        }

        private void OnDisable()
        {
            if (days != null)
                days.DayAdvanced -= OnDayAdvanced;
        }

        private void LateUpdate()
        {
            if (!dirty) return;
            dirty = false;
            ApplyText(lastShownDay > 0 ? lastShownDay : 1);
        }

        private void OnDayAdvanced(int day)
        {
            lastShownDay = day;
            dirty = true;
            ApplyText(day);
        }

        private void ApplyText(int day)
        {
            if (label != null)
                label.text = $"Day {day}";
        }
    }
}
