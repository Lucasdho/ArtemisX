using System;
using ArtemisX.Core;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Reads the pre-baked DONKI schedule, reports Today / Tomorrow, and applies the Space Weather Choice.</summary>
    public class SpaceWeatherManager : MonoBehaviour
    {
        [SerializeField] private SpaceWeatherConfig config;
        [SerializeField] private DayManager dayManager;
        [SerializeField] private ResourceManager resources;
        [SerializeField] private SolarFieldManager solar;

        /// <summary>Raised whenever the day changes: (today, tomorrow).</summary>
        public event Action<WeatherEventType, WeatherEventType> WeatherUpdated;

        /// <summary>Raised in the morning of a DONKI event day (it hits during that night): (day, event type).</summary>
        public event Action<int, WeatherEventType> StormIncoming;

        /// <summary>Raised when the rover's radiation lock for today turns on or off.</summary>
        public event Action<bool> RoverLockChanged;

        /// <summary>Raised after a Space Weather Choice is applied.</summary>
        public event Action<WeatherChoice> WeatherChoiceApplied;

        public SpaceWeatherConfig Config => config;
        public int Day { get; private set; } = 1;
        public WeatherEventType TodayWeather => config != null ? config.GetWeather(Day) : WeatherEventType.Calm;
        public WeatherEventType TomorrowWeather => config != null ? config.GetWeather(Day + 1) : WeatherEventType.Calm;
        public bool IsEventToday => TodayWeather != WeatherEventType.Calm;
        public bool IsSolarStormToday => TodayWeather == WeatherEventType.SolarStorm;
        public SpaceWeatherConfig.WeatherDay TodayEntry => config != null ? config.GetDay(Day) : default;

        /// <summary>The choice made today, or null. The choice card is shown only on event days.</summary>
        public WeatherChoice? TodayChoice { get; private set; }
        public bool NeedsChoiceToday => IsEventToday && !TodayChoice.HasValue;

        /// <summary>On a Solar Radiation day the rover stays docked unless the player chose Overclock Shields.</summary>
        public bool IsRoverLockedToday =>
            TodayWeather == WeatherEventType.SolarRadiation && TodayChoice != WeatherChoice.OverclockShields;

        public int OverclockCost => config != null ? config.overclockShieldsPowerCost : 15;

        private bool subscribed;
        private bool lastLock;

        /// <summary>Sets references and the current day without events. Tests call this directly.</summary>
        public void Initialize(SpaceWeatherConfig weatherConfig, DayManager days, ResourceManager resourceManager, SolarFieldManager solarField)
        {
            Unsubscribe();
            config = weatherConfig;
            dayManager = days;
            resources = resourceManager;
            solar = solarField;
            if (dayManager != null) Day = dayManager.CurrentDay;
            TodayChoice = null;
            lastLock = IsRoverLockedToday;
            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        private void Start()
        {
            if (config == null) Debug.LogError("SpaceWeatherManager has no SpaceWeatherConfig assigned.", this);
            if (dayManager != null) SetDay(dayManager.CurrentDay);
        }

        /// <summary>Moves the forecast to a day, clears yesterday's choice and raises the weather events.</summary>
        public void SetDay(int day)
        {
            Day = day;
            TodayChoice = null;
            WeatherUpdated?.Invoke(TodayWeather, TomorrowWeather);
            if (IsEventToday) StormIncoming?.Invoke(day, TodayWeather);
            UpdateLock();
        }

        /// <summary>Why this choice can't be applied now, or None.</summary>
        public ActionBlockReason CanApplyWeatherChoice(WeatherChoice choice)
        {
            if (TodayChoice.HasValue) return ActionBlockReason.AlreadyChosen;
            bool stormChoice = choice == WeatherChoice.ShieldAndRetract || choice == WeatherChoice.KeepFullPower;
            if (stormChoice && TodayWeather != WeatherEventType.SolarStorm) return ActionBlockReason.WrongWeather;
            if (!stormChoice && TodayWeather != WeatherEventType.SolarRadiation) return ActionBlockReason.WrongWeather;
            if (choice == WeatherChoice.OverclockShields && (resources == null || !resources.Has(ResourceType.Power, OverclockCost)))
                return ActionBlockReason.NotEnoughPower;
            return ActionBlockReason.None;
        }

        /// <summary>Applies the Space Weather Choice for today. Once per day.</summary>
        public bool ApplyWeatherChoice(WeatherChoice choice)
        {
            if (CanApplyWeatherChoice(choice) != ActionBlockReason.None) return false;

            switch (choice)
            {
                case WeatherChoice.ShieldAndRetract:
                    if (solar != null && solar.State == SolarFieldState.Active) solar.ToggleRetract();
                    break;
                case WeatherChoice.KeepFullPower:
                    if (solar != null && solar.State == SolarFieldState.Retracted) solar.ToggleRetract();
                    break;
                case WeatherChoice.OverclockShields:
                    resources.Add(ResourceType.Power, -OverclockCost);
                    break;
            }

            TodayChoice = choice;
            WeatherChoiceApplied?.Invoke(choice);
            UpdateLock();
            return true;
        }

        private void UpdateLock()
        {
            bool locked = IsRoverLockedToday;
            if (locked == lastLock) return;
            lastLock = locked;
            RoverLockChanged?.Invoke(locked);
        }

        private void OnDayAdvanced(int day) => SetDay(day);

        private void Subscribe()
        {
            if (subscribed || dayManager == null) return;
            dayManager.DayAdvanced += OnDayAdvanced;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || dayManager == null) return;
            dayManager.DayAdvanced -= OnDayAdvanced;
            subscribed = false;
        }
    }
}
