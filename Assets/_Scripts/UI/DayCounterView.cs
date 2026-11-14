using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>Shows the mission day. Listens to DayManager.DayAdvanced.</summary>
    public class DayCounterView : MonoBehaviour
    {
        [SerializeField] private DayManager days;
        [SerializeField] private Text label;

        private void OnEnable()
        {
            if (days != null) days.DayAdvanced += OnDayAdvanced;
        }

        private void OnDisable()
        {
            if (days != null) days.DayAdvanced -= OnDayAdvanced;
        }

        private void OnDayAdvanced(int day)
        {
            if (label != null) label.text = "Day " + day;
        }
    }
}
