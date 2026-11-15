using ArtemisX.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI
{
    /// <summary>
    /// Compact HUD badge showing Today and Tomorrow NASA DONKI space weather icons and short status label.
    /// Subscribes to SpaceWeatherManager.WeatherUpdated when assigned, or can be updated via SetWeather.
    /// </summary>
    public class WeatherBadgeView : MonoBehaviour
    {
        [SerializeField] private SpaceWeatherManager weatherManager;
        [SerializeField] private UIThemeConfig theme;
        [SerializeField] private Image pillBackground;
        [SerializeField] private Image pillBorder;
        [SerializeField] private Image todayIconImage;
        [SerializeField] private Image tomorrowIconImage;
        [SerializeField] private Text todayStatusLabel;
        [SerializeField] private Text tomorrowHintLabel;

        private void OnEnable()
        {
            if (weatherManager != null)
            {
                weatherManager.WeatherUpdated += OnWeatherUpdated;
                SetWeather(weatherManager.TodayWeather, weatherManager.TomorrowWeather);
            }
        }

        private void OnDisable()
        {
            if (weatherManager != null)
                weatherManager.WeatherUpdated -= OnWeatherUpdated;
        }

        private void OnWeatherUpdated(WeatherEventType today, WeatherEventType tomorrow)
            => SetWeather(today, tomorrow);

        /// <summary>Updates the Today and Tomorrow weather icons, status copy, and border tint.</summary>
        public void SetWeather(WeatherEventType today, WeatherEventType tomorrow)
        {
            if (theme != null)
            {
                if (todayIconImage != null)
                {
                    Sprite sp = theme.GetWeatherIcon(today);
                    if (sp != null) todayIconImage.sprite = sp;
                }

                if (tomorrowIconImage != null)
                {
                    Sprite sp = theme.GetWeatherIcon(tomorrow);
                    if (sp != null) tomorrowIconImage.sprite = sp;
                }

                if (todayStatusLabel != null)
                {
                    todayStatusLabel.text = theme.GetWeatherShortLabel(today);
                    todayStatusLabel.color = today == WeatherEventType.Calm
                        ? theme.textPrimary
                        : theme.GetWeatherAccentColor(today);
                }

                if (tomorrowHintLabel != null)
                {
                    tomorrowHintLabel.text = "NEXT: " + theme.GetWeatherShortLabel(tomorrow);
                    tomorrowHintLabel.color = tomorrow == WeatherEventType.Calm
                        ? theme.textSecondary
                        : theme.GetWeatherAccentColor(tomorrow);
                }

                if (pillBorder != null)
                    pillBorder.color = theme.GetWeatherAccentColor(today);
            }
            else
            {
                if (todayStatusLabel != null)
                    todayStatusLabel.text = today == WeatherEventType.SolarStorm
                        ? "STORM WARNING"
                        : today == WeatherEventType.SolarRadiation ? "RADIATION" : "CALM";
            }
        }
    }
}
