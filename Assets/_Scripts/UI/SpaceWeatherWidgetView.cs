using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>HUD Space Weather widget: Today and Tomorrow icons with Calm / Storm Warning labels.</summary>
    public class SpaceWeatherWidgetView : MonoBehaviour
    {
        [SerializeField] private SpaceWeatherManager weather;
        [SerializeField] private RawImage todayIcon;
        [SerializeField] private Text todayLabel;
        [SerializeField] private RawImage tomorrowIcon;
        [SerializeField] private Text tomorrowLabel;
        [Tooltip("Stand-in icons until the in-house Sun / Storm / Radiation icons exist.")]
        [SerializeField] private Texture calmIcon;
        [SerializeField] private Texture stormIcon;
        [SerializeField] private Texture radiationIcon;
        [SerializeField] private Color calmColor = new Color(1f, 0.78f, 0.2f);
        [SerializeField] private Color stormColor = Color.white;
        [SerializeField] private Color radiationColor = Color.white;

        private void OnEnable()
        {
            if (weather != null) weather.WeatherUpdated += OnWeatherUpdated;
        }

        private void OnDisable()
        {
            if (weather != null) weather.WeatherUpdated -= OnWeatherUpdated;
        }

        private void Start()
        {
            if (weather != null) OnWeatherUpdated(weather.TodayWeather, weather.TomorrowWeather);
        }

        private void OnWeatherUpdated(WeatherEventType today, WeatherEventType tomorrow)
        {
            Apply(todayIcon, todayLabel, today, true);
            Apply(tomorrowIcon, tomorrowLabel, tomorrow, false);
        }

        private void Apply(RawImage icon, Text label, WeatherEventType type, bool isToday)
        {
            if (icon != null)
            {
                icon.texture = type == WeatherEventType.SolarStorm ? stormIcon : type == WeatherEventType.SolarRadiation ? radiationIcon : calmIcon;
                icon.color = type == WeatherEventType.SolarStorm ? stormColor : type == WeatherEventType.SolarRadiation ? radiationColor : calmColor;
            }
            if (label == null) return;
            if (type == WeatherEventType.Calm) label.text = "Calm";
            else if (isToday) label.text = type == WeatherEventType.SolarStorm ? "Solar Storm" : "Radiation";
            else label.text = "Storm Warning";
        }
    }
}
