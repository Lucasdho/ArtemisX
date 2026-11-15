using System;
using System.Text;
using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>
    /// Dawn cards in strict order: Daily Report -> Unexpected Demand (if rolled) -> Space Weather Choice (DONKI event day).
    /// The Daily Report carries Bauzer's morning line (StoryDirector's daily briefing is turned off in this scene).
    /// </summary>
    public class MorningSequenceController : MonoBehaviour
    {
        [SerializeField] private GameplayUIConfig config;
        [SerializeField] private UIStyle style;
        [SerializeField] private ModalCardView card;
        [SerializeField] private DayResolutionController resolution;
        [SerializeField] private UnexpectedDemandManager demands;
        [SerializeField] private SpaceWeatherManager weather;
        [SerializeField] private RoverExpeditionManager rover;
        [SerializeField] private GameplayHud hud;
        [Tooltip("Optional. The cards wait until the day-to-night-to-dawn transition has finished.")]
        [SerializeField] private Visuals.DayNightTransitionPlayer transition;

        private DailyReportData pendingReport;

        /// <summary>Raised after the last morning card closes.</summary>
        public event Action SequenceFinished;

        public bool IsShowing { get; private set; }

        private static readonly Color ReportBanner = new Color(0.29f, 0.56f, 0.85f);
        private static readonly Color StormBanner = new Color(0.85f, 0.42f, 0.18f);
        private static readonly Color RadiationBanner = new Color(0.62f, 0.32f, 0.82f);
        private static readonly Color DemandBanner = new Color(0.86f, 0.62f, 0.12f);

        private void OnEnable()
        {
            if (resolution != null) resolution.MorningReady += OnMorningReady;
            if (transition != null) transition.TransitionFinished += OnTransitionFinished;
        }

        private void OnDisable()
        {
            if (resolution != null) resolution.MorningReady -= OnMorningReady;
            if (transition != null) transition.TransitionFinished -= OnTransitionFinished;
        }

        private void OnMorningReady(DailyReportData report)
        {
            IsShowing = true;
            if (hud != null && hud.Popup != null) hud.Popup.Close();
            if (transition != null && transition.IsPlaying)
            {
                pendingReport = report; // Dawn is still playing: show the cards when it ends.
                return;
            }
            ShowReport(report);
        }

        private void OnTransitionFinished()
        {
            if (pendingReport == null) return;
            var report = pendingReport;
            pendingReport = null;
            ShowReport(report);
        }

        private void ShowReport(DailyReportData report)
        {
            var line = config != null ? config.GetReportLine(report.bauzerSummaryKey) : default;
            if (config != null && resolution != null)
            {
                if (report.newDay >= resolution.MissionLength && !string.IsNullOrEmpty(config.finalDayLine.text)) line = config.finalDayLine;
                else if (report.newDay == 25 && !string.IsNullOrEmpty(config.day25Line.text)) line = config.day25Line;
            }
            card.Show(new ModalCardContent
            {
                banner = "DAILY REPORT",
                bannerColor = ReportBanner,
                title = "Good morning! Day " + report.newDay,
                body = DescribeNight(report),
                deltas = report.resourceDeltas,
                bauzerLine = line.text,
                bauzerMood = line.mood,
                labelA = config != null ? config.startDayLabel : "Start Day",
                enabledA = true,
                actionA = () => AfterReport(report),
            });
        }

        private string DescribeNight(DailyReportData r)
        {
            var sb = new StringBuilder();
            sb.Append(r.nightWeather == WeatherEventType.SolarStorm ? "Last night: Solar Storm."
                : r.nightWeather == WeatherEventType.SolarRadiation ? "Last night: Solar Radiation."
                : "Last night: calm.");
            string roverName = rover != null ? rover.RoverName : "The rover";
            if (r.rover.returned)
            {
                sb.Append('\n');
                if (r.rover.isBroken) sb.Append(roverName).Append(" came back broken").Append(r.rover.stormHit ? " by the storm." : ".");
                else
                {
                    sb.Append(roverName).Append(" brought back");
                    if (r.rover.water > 0) sb.Append(" +").Append(r.rover.water).Append(" Water");
                    if (r.rover.water > 0 && r.rover.parts > 0) sb.Append(" and");
                    if (r.rover.parts > 0) sb.Append(" +").Append(r.rover.parts).Append(" Parts");
                    sb.Append('.');
                }
            }
            if (r.solarBroken) sb.Append("\nRocks hit the solar panels!");
            if (r.solarRedeployed) sb.Append("\nSolar panels deployed again for the new day.");
            if (r.supplyArrived) sb.Append("\nSupply capsule: +").Append(r.supplyFood).Append(" Food, +").Append(r.supplyParts).Append(" Parts waiting on the pad.");
            if (r.crewRestricted) sb.Append("\nCrew restricted: Oxygen or Food is at 0.");
            return sb.ToString();
        }

        private void AfterReport(DailyReportData report)
        {
            if (report.demandRolled && demands != null && demands.HasActiveDemand) ShowDemand();
            else AfterDemand();
        }

        private void ShowDemand()
        {
            var d = demands.ActiveDemand;
            string a = d.choices != null && d.choices.Length > 0 ? d.choices[0].label : "OK";
            string b = d.choices != null && d.choices.Length > 1 ? d.choices[1].label : null;
            card.Show(new ModalCardContent
            {
                banner = "UNEXPECTED: " + (d.pool ?? string.Empty).ToUpperInvariant(),
                bannerColor = DemandBanner,
                title = d.title,
                body = "Pick one now. Next Day stays locked until you answer.",
                bauzerLine = config != null ? config.demandLine : null,
                bauzerMood = SpeakerMood.Neutral,
                labelA = a,
                enabledA = demands.CanChoose(0) == ActionBlockReason.None,
                actionA = () => ChooseDemand(0),
                labelB = b,
                enabledB = demands.CanChoose(1) == ActionBlockReason.None,
                actionB = () => ChooseDemand(1),
            });
        }

        private void ChooseDemand(int index)
        {
            if (!demands.ResolveDemandChoice(index)) return;
            if (hud != null) hud.PlaySfx(config != null ? config.confirmClip : null);
            AfterDemand();
        }

        private void AfterDemand()
        {
            if (weather != null && weather.NeedsChoiceToday) ShowWeather();
            else Finish();
        }

        private void ShowWeather()
        {
            bool storm = weather.TodayWeather == WeatherEventType.SolarStorm;
            var entry = weather.TodayEntry;
            string note = !string.IsNullOrEmpty(entry.note) ? entry.note
                : config != null ? (storm ? config.stormFallbackNote : config.radiationFallbackNote) : string.Empty;
            string source = string.IsNullOrEmpty(entry.donkiDate) ? string.Empty
                : $"\nReal event: {entry.donkiDate} ({(entry.donkiEventType == "SEP" ? "Solar Energetic Particles" : entry.donkiEventType == "GST" ? "Geomagnetic Storm" : entry.donkiEventType)})";

            WeatherChoice a = storm ? WeatherChoice.ShieldAndRetract : WeatherChoice.ShelterCrewAndLockRover;
            WeatherChoice b = storm ? WeatherChoice.KeepFullPower : WeatherChoice.OverclockShields;
            string labelA = config == null ? a.ToString() : storm ? config.shieldLabel : config.shelterLabel;
            string labelB = config == null ? b.ToString() : storm ? config.keepPowerLabel : string.Format(config.overclockLabelFormat, weather.OverclockCost);

            card.Show(new ModalCardContent
            {
                banner = "NASA DONKI ALERT",
                bannerColor = storm ? StormBanner : RadiationBanner,
                title = storm ? "Solar Storm today" : "Solar Radiation today",
                body = note + source,
                bauzerLine = config != null ? (storm ? config.stormLine : config.radiationLine) : null,
                bauzerMood = SpeakerMood.Worried,
                labelA = labelA,
                enabledA = weather.CanApplyWeatherChoice(a) == ActionBlockReason.None,
                actionA = () => ChooseWeather(a),
                labelB = labelB,
                enabledB = weather.CanApplyWeatherChoice(b) == ActionBlockReason.None,
                actionB = () => ChooseWeather(b),
            });
        }

        private void ChooseWeather(WeatherChoice choice)
        {
            if (!weather.ApplyWeatherChoice(choice)) return;
            if (hud != null) hud.PlaySfx(config != null ? config.confirmClip : null);
            Finish();
        }

        private void Finish()
        {
            card.Hide();
            IsShowing = false;
            SequenceFinished?.Invoke();
        }

        /// <summary>Shows the Space Weather Choice card now if today needs one (used on Day 1 or after loading).</summary>
        public void ShowWeatherIfNeeded()
        {
            if (IsShowing || weather == null || !weather.NeedsChoiceToday) return;
            IsShowing = true;
            ShowWeather();
        }
    }
}
