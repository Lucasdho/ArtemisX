using System;
using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// 5-day Mini Calendar strip in the top-right HUD corner:
    /// Today highlighted in orange (84x96), followed by the next 4 days (62x78) with weather,
    /// storm warning badges, and earth supply rocket markers.
    /// </summary>
    public class DayCalendarView : MonoBehaviour
    {
        [Serializable]
        public class DayTile
        {
            public GameObject root;
            public Image background;
            public Text dayNumber;
            public Image icon;
            public Image warningBadge;
        }

        [Header("Managers")]
        [SerializeField] private DayManager days;
        [SerializeField] private SpaceWeatherManager weather;
        [SerializeField] private SupplyManager supply;
        [SerializeField] private DayResolutionController resolution;

        [Header("Tiles (0 = Today, 1..4 = Next 4 Days)")]
        [SerializeField] private DayTile[] tiles = new DayTile[5];

        [Header("Sprites")]
        [SerializeField] private Sprite todaySprite;
        [SerializeField] private Sprite futureCalmSprite;
        [SerializeField] private Sprite futureStormSprite;
        [SerializeField] private Sprite calmIcon;
        [SerializeField] private Sprite stormIcon;
        [SerializeField] private Sprite supplyRocketIcon;
        [SerializeField] private Sprite warningIcon;

        private static readonly Color FutureNumberColor = new Color(0.337f, 0.396f, 0.498f, 1f);

        private void OnEnable()
        {
            if (days != null) days.DayAdvanced += OnDayAdvanced;
            if (weather != null) weather.WeatherUpdated += OnWeatherUpdated;
            Refresh();
        }

        private void OnDisable()
        {
            if (days != null) days.DayAdvanced -= OnDayAdvanced;
            if (weather != null) weather.WeatherUpdated -= OnWeatherUpdated;
        }

        private void Start() => Refresh();

        private void OnDayAdvanced(int _) => Refresh();

        private void OnWeatherUpdated(WeatherEventType _, WeatherEventType __) => Refresh();

        public void Refresh()
        {
            int currentDay = days != null ? Mathf.Max(1, days.CurrentDay) : (weather != null ? Mathf.Max(1, weather.Day) : 1);
            int supplyInterval = supply != null ? supply.Interval : 5;
            int missionDays = resolution != null ? resolution.MissionLength : 30;

            if (tiles == null) return;

            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null) continue;

                int day = currentDay + i;
                bool isToday = i == 0;

                if (tile.dayNumber != null)
                {
                    tile.dayNumber.text = day.ToString();
                    tile.dayNumber.color = isToday ? Color.white : FutureNumberColor;
                }

                WeatherEventType w = WeatherEventType.Calm;
                bool hasForecast = weather != null && weather.TryGetWeather(day, out w);
                bool isEvent = hasForecast && w != WeatherEventType.Calm;
                bool isSupply = day <= missionDays && SupplyManager.IsSupplyDay(day, supplyInterval);

                if (tile.background != null)
                {
                    if (isToday)
                        tile.background.sprite = todaySprite;
                    else
                        tile.background.sprite = isEvent && futureStormSprite != null ? futureStormSprite : futureCalmSprite;
                    tile.background.color = Color.white;
                }

                if (tile.warningBadge != null)
                {
                    if (warningIcon != null) tile.warningBadge.sprite = warningIcon;
                    tile.warningBadge.enabled = isEvent;
                    tile.warningBadge.gameObject.SetActive(isEvent);
                }

                if (tile.icon != null)
                {
                    if (isSupply && supplyRocketIcon != null)
                    {
                        tile.icon.sprite = supplyRocketIcon;
                        tile.icon.enabled = true;
                        tile.icon.color = Color.white;
                    }
                    else if (hasForecast)
                    {
                        Sprite weatherSprite = isEvent ? stormIcon : calmIcon;
                        tile.icon.sprite = weatherSprite;
                        tile.icon.enabled = weatherSprite != null;
                        tile.icon.color = Color.white;
                    }
                    else
                    {
                        tile.icon.sprite = null;
                        tile.icon.enabled = false;
                    }
                }
            }
        }
    }
}
