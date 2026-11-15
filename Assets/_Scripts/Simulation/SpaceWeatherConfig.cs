using System;
using UnityEngine;

namespace ArtemisX.Simulation
{
    /// <summary>Pre-baked 30-day space weather schedule. Event days carry the real NASA DONKI event they come from.</summary>
    [CreateAssetMenu(fileName = "SpaceWeatherConfig", menuName = "Artemis X/Simulation/Space Weather Config")]
    public class SpaceWeatherConfig : ScriptableObject
    {
        [Serializable]
        public struct WeatherDay
        {
            public WeatherEventType weather;
            [Tooltip("Real DONKI event date (UTC), empty on calm days.")]
            public string donkiDate;
            [Tooltip("DONKI event type: GST (geomagnetic storm) or SEP (solar energetic particles).")]
            public string donkiEventType;
            [Tooltip("Short factual note for the DONKI Alert card and references.md.")]
            public string note;
        }

        [Tooltip("Where the events come from (for references.md).")]
        public string sourceName = "NASA DONKI (Space Weather Database Of Notifications, Knowledge, Information), CCMC";
        public string sourceUrl = "https://kauai.ccmc.gsfc.nasa.gov/DONKI/";

        [Header("NASA DONKI Dataset")]
        [Tooltip("Real DONKI records stored in the project (Assets/_Data/NASA/DONKI). The schedule below is baked from it.")]
        public TextAsset donkiDataset;
        [Tooltip("Real date (UTC, yyyy-MM-dd) that becomes mission Day 1. Each mission day is the next real day.")]
        public string missionStartDate = "2024-08-01";
        [Tooltip("Number of mission days baked into the schedule.")]
        [Min(1)] public int scheduleLength = 30;
        [Tooltip("A DONKI geomagnetic storm (GST) becomes a Solar Storm day when its max Kp reaches this (6 = NOAA G2).")]
        [Min(0)] public float stormMinKp = 6f;

        [Header("Space Weather Choices")]
        [Tooltip("Power paid at once for 'Overclock Shields' on a Solar Radiation day.")]
        [Min(0)] public int overclockShieldsPowerCost = 15;

        [Tooltip("One entry per mission day; index 0 is Day 1.")]
        public WeatherDay[] days = new WeatherDay[0];

        /// <summary>Weather for a 1-based mission day. Days outside the schedule are Calm.</summary>
        public WeatherEventType GetWeather(int day)
        {
            int i = day - 1;
            if (days == null || i < 0 || i >= days.Length) return WeatherEventType.Calm;
            return days[i].weather;
        }

        /// <summary>Schedule entry for a 1-based mission day, or a Calm entry.</summary>
        public WeatherDay GetDay(int day)
        {
            int i = day - 1;
            if (days == null || i < 0 || i >= days.Length) return new WeatherDay { weather = WeatherEventType.Calm };
            return days[i];
        }
    }
}
