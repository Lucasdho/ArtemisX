using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>
    /// Centralized Space Expansion visual theme (palette, typography, 9-sliced frames, button states, and weather icons).
    /// Asset lives at Assets/_Config/UI/UIThemeConfig.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "UIThemeConfig", menuName = "Artemis X/UI/UI Theme Config")]
    public class UIThemeConfig : ScriptableObject
    {
        [Header("Cohesive Space Palette")]
        [Tooltip("Deep slate-navy semi-transparent panel backdrop (#0F172AE6).")]
        public Color panelBackdropColor = new Color(0.059f, 0.090f, 0.165f, 0.902f);
        [Tooltip("Elevated card/inner surface color (#1E293BF2).")]
        public Color cardSurfaceColor = new Color(0.118f, 0.161f, 0.231f, 0.950f);
        [Tooltip("Subtle cyan accent border (#38BDF8).")]
        public Color cyanAccentBorder = new Color(0.220f, 0.741f, 0.973f, 1.000f);
        [Tooltip("Warm amber highlight (#F59E0B).")]
        public Color amberHighlight = new Color(0.961f, 0.620f, 0.043f, 1.000f);
        [Tooltip("Confirm / positive green (#22C55E).")]
        public Color confirmGreen = new Color(0.133f, 0.773f, 0.369f, 1.000f);
        [Tooltip("Alert / cancel coral red (#EF4444).")]
        public Color alertRed = new Color(0.937f, 0.267f, 0.267f, 1.000f);
        [Tooltip("Primary crisp white text (#F8FAFC).")]
        public Color textPrimary = new Color(0.973f, 0.980f, 0.988f, 1.000f);
        [Tooltip("Secondary cool slate text (#94A3B8).")]
        public Color textSecondary = new Color(0.580f, 0.639f, 0.722f, 1.000f);

        [Header("Typography")]
        public Font headerFont;
        public Font bodyFont;
        [Min(10)] public int headerFontSize = 24;
        [Min(10)] public int bodyFontSize = 18;
        [Min(10)] public int buttonFontSize = 20;
        [Min(10)] public int pillAmountFontSize = 26;
        [Min(10)] public int pillDeltaFontSize = 18;

        [Header("9-Sliced Panels & Frames")]
        public Sprite panelBaseSprite;
        public Sprite panelBorderSprite;
        public Sprite pillBorderSprite;
        public Sprite portraitFrameSprite;
        public Sprite cardHeaderBlueSprite;
        public Sprite cardHeaderYellowSprite;
        public Sprite cardHeaderRedSprite;

        [Header("9-Sliced Pills")]
        public Sprite pillBlueSprite;
        public Sprite pillGreenSprite;
        public Sprite pillRedSprite;
        public Sprite pillYellowSprite;
        public Sprite pillGreySprite;

        [Header("Button Sprites (Space Expansion)")]
        [Tooltip("Green = Confirm / Start / Send.")]
        public Sprite buttonGreenNormal;
        public Sprite buttonGreenHover;
        [Tooltip("Blue = Info / Secondary.")]
        public Sprite buttonBlueNormal;
        public Sprite buttonBlueHover;
        [Tooltip("Red = Cancel / Alert.")]
        public Sprite buttonRedNormal;
        public Sprite buttonRedHover;
        [Tooltip("Yellow = Next Day / Star highlight.")]
        public Sprite buttonYellowNormal;
        public Sprite buttonYellowHover;
        [Tooltip("Grey = Disabled.")]
        public Sprite buttonDisabled;

        [Header("Space Weather Icons")]
        public Sprite weatherCalmIcon;
        public Sprite weatherStormIcon;
        public Sprite weatherRadiationIcon;

        [Header("Alert Pulse Tuning")]
        [Min(0.5f)] public float alertPulseSpeed = 4.5f;
        [Range(0.8f, 1.2f)] public float alertPulseMinScale = 0.92f;
        [Range(1.0f, 1.5f)] public float alertPulseMaxScale = 1.14f;

        /// <summary>Returns the weather sprite matching a NASA DONKI WeatherEventType.</summary>
        public Sprite GetWeatherIcon(WeatherEventType weather)
        {
            switch (weather)
            {
                case WeatherEventType.SolarStorm:
                    return weatherStormIcon != null ? weatherStormIcon : weatherCalmIcon;
                case WeatherEventType.SolarRadiation:
                    return weatherRadiationIcon != null ? weatherRadiationIcon : weatherStormIcon;
                default:
                    return weatherCalmIcon;
            }
        }

        /// <summary>Returns the short English HUD label for a WeatherEventType.</summary>
        public string GetWeatherShortLabel(WeatherEventType weather)
        {
            switch (weather)
            {
                case WeatherEventType.SolarStorm:
                    return "STORM WARNING";
                case WeatherEventType.SolarRadiation:
                    return "RADIATION";
                default:
                    return "CALM";
            }
        }

        /// <summary>Returns the accent color for a WeatherEventType.</summary>
        public Color GetWeatherAccentColor(WeatherEventType weather)
        {
            switch (weather)
            {
                case WeatherEventType.SolarStorm:
                    return alertRed;
                case WeatherEventType.SolarRadiation:
                    return amberHighlight;
                default:
                    return cyanAccentBorder;
            }
        }
    }
}
