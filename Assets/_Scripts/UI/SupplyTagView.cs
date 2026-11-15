using ArtemisX.Simulation;
using ArtemisX.Story;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>Small tag under a pill that says when the next Earth supply ship arrives.</summary>
    public class SupplyTagView : MonoBehaviour
    {
        [SerializeField] private DayManager days;
        [SerializeField] private DialogueConfig config;
        [SerializeField] private Text label;

        private int Interval => config != null ? Mathf.Max(1, config.supplyInterval) : 5;

        private void OnEnable()
        {
            if (days == null) return;
            days.DayAdvanced += OnDayAdvanced;
            Refresh(days.CurrentDay);
        }

        private void OnDisable()
        {
            if (days != null) days.DayAdvanced -= OnDayAdvanced;
        }

        private void OnDayAdvanced(int day) => Refresh(day);

        private void Refresh(int day)
        {
            if (label == null) return;
            int remaining = Interval - day % Interval;
            label.text = day % Interval == 0 ? "Supply ship today" : "Next supply in " + remaining + "d";
        }
    }
}
