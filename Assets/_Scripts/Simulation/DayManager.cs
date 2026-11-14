using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Tracks the mission day. No simulation rules yet: advancing only increments the counter.</summary>
    public class DayManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        /// <summary>Raised with the new day number, and once on Start with the first day.</summary>
        public event Action<int> DayAdvanced;

        public int CurrentDay { get; private set; } = 1;

        private void Awake()
        {
            if (config != null) CurrentDay = config.startDay;
            else Debug.LogError("DayManager has no GameConfig assigned.", this);
        }

        private void Start() => DayAdvanced?.Invoke(CurrentDay);

        public void AdvanceDay()
        {
            CurrentDay++;
            DayAdvanced?.Invoke(CurrentDay);
        }
    }
}
