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

        [Header("Event Dialog Icons (256px / 64px)")]
        [SerializeField] private Sprite waterIcon256;
        [SerializeField] private Sprite partsIcon256;
        [SerializeField] private Sprite powerIcon256;
        [SerializeField] private Sprite foodIcon256;
        [SerializeField] private Sprite hammerIcon256;
        [SerializeField] private Sprite shieldIcon256;
        [SerializeField] private Sprite stormIcon256;
        [SerializeField] private Sprite radiationIcon256;
        [SerializeField] private Sprite leafIcon256;
        [SerializeField] private Sprite skipIcon256;
        [SerializeField] private Sprite warningIcon256;
        [SerializeField] private Sprite checkmarkIcon64;
        [SerializeField] private Sprite warningIcon64;

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
            bool tracksOxygen = resolution == null || resolution.Resources == null || resolution.Resources.RestrictOnZeroOxygen;
            if (r.crewRestricted) sb.Append(tracksOxygen ? "\nCrew restricted: Oxygen or Food is at 0." : "\nCrew restricted: Food is at 0.");
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
            string header = d.demandId == "water_leak" ? "Water leak!"
                : d.demandId == "clogged_filter" ? "Clogged filter!"
                : d.demandId == "science_food" ? "Plant study!"
                : d.demandId == "science_parts" ? "Rock study!"
                : (d.title ?? "Unexpected event");

            Sprite eventIcon = d.demandId == "water_leak" ? waterIcon256
                : d.demandId == "science_food" ? leafIcon256
                : d.demandId == "science_parts" ? partsIcon256
                : warningIcon256;

            string keplerLine = d.demandId == "water_leak" ? "The water filter is clogged and leaking. What should we do?"
                : d.demandId == "clogged_filter" ? "Our air filter is jammed up. Should we fix it now?"
                : d.demandId == "science_food" ? "Earth asked for a quick lunar plant test. Spend Power for Food?"
                : d.demandId == "science_parts" ? "Earth asked for a moon rock study. Spend Power for Parts?"
                : (config != null ? config.demandLine : null);

            var c0 = BuildDemandChoice(d, 0);
            var c1 = BuildDemandChoice(d, 1);

            card.Show(new ModalCardContent
            {
                banner = header,
                bannerColor = DemandBanner,
                eventIcon = eventIcon,
                bauzerLine = keplerLine,
                bauzerMood = d.demandId == "water_leak" || d.demandId == "clogged_filter" ? SpeakerMood.Worried : SpeakerMood.Neutral,
                choices = d.choices != null && d.choices.Length > 1 ? new[] { c0, c1 } : new[] { c0 },
                footerHint = "Answer to unlock Next day"
            });
        }

        private ModalEventChoice BuildDemandChoice(UnexpectedDemandConfig.Demand d, int idx)
        {
            if (d.choices == null || idx >= d.choices.Length) return default;
            var ch = d.choices[idx];
            ResourceType costType = ResourceType.Parts;
            int costAmt = 0;
            if (ch.costs != null && ch.costs.Length > 0)
            {
                costType = ch.costs[0].resource;
                costAmt = ch.costs[0].amount;
            }

            string name = ch.label ?? string.Empty;
            int paren = name.IndexOf('(');
            if (paren > 0) name = name.Substring(0, paren).Trim();

            string btn = idx == 0
                ? (d.demandId == "water_leak" || d.demandId == "clogged_filter" ? "Fix it" : "Run test")
                : (d.demandId == "water_leak" || d.demandId == "clogged_filter" ? "Patch it" : "Skip");

            Sprite choiceIcon = idx == 0
                ? (d.demandId == "science_food" ? leafIcon256 : d.demandId == "science_parts" ? partsIcon256 : hammerIcon256)
                : (d.demandId == "water_leak" ? waterIcon256 : d.demandId == "clogged_filter" ? warningIcon256 : skipIcon256);

            string outcome = null;
            bool risky = false;
            Sprite outcomeIco = checkmarkIcon64;
            if (ch.effects != null && ch.effects.Length > 0)
            {
                var ef = ch.effects[0];
                if (ef.amount < 0)
                {
                    risky = true;
                    outcomeIco = warningIcon64;
                    outcome = "−" + Mathf.Abs(ef.amount) + " " + ef.resource;
                }
                else if (ef.amount > 0)
                {
                    outcome = "+" + ef.amount + " " + ef.resource;
                }
            }
            else if (idx == 0 && (d.demandId == "water_leak" || d.demandId == "clogged_filter"))
            {
                outcome = "Stops the leak";
            }

            return new ModalEventChoice
            {
                icon = choiceIcon,
                title = name,
                costType = costType,
                costAmount = costAmt,
                outcomeIcon = outcomeIco,
                outcomeText = outcome,
                outcomeRisky = risky,
                buttonLabel = btn,
                enabled = demands.CanChoose(idx) == ActionBlockReason.None,
                action = () => ChooseDemand(idx)
            };
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
            string source = string.IsNullOrEmpty(entry.donkiDate) ? string.Empty : $" ({entry.donkiDate})";

            WeatherChoice a = storm ? WeatherChoice.ShieldAndRetract : WeatherChoice.ShelterCrewAndLockRover;
            WeatherChoice b = storm ? WeatherChoice.KeepFullPower : WeatherChoice.OverclockShields;

            var choices = new[]
            {
                new ModalEventChoice
                {
                    icon = shieldIcon256,
                    title = storm ? "Shield & Retract" : "Shelter Crew",
                    costType = ResourceType.Power,
                    costAmount = 0,
                    outcomeIcon = checkmarkIcon64,
                    outcomeText = storm ? "Panels stay safe" : "Crew safe, rover docked",
                    outcomeRisky = false,
                    buttonLabel = storm ? "Retract" : "Shelter",
                    enabled = weather.CanApplyWeatherChoice(a) == ActionBlockReason.None,
                    action = () => ChooseWeather(a)
                },
                new ModalEventChoice
                {
                    icon = powerIcon256,
                    title = storm ? "Full Power" : "Overclock",
                    costType = ResourceType.Power,
                    costAmount = storm ? 0 : weather.OverclockCost,
                    outcomeIcon = storm ? warningIcon64 : checkmarkIcon64,
                    outcomeText = storm ? "Rocks may hit panels" : "Rover stays active",
                    outcomeRisky = storm,
                    buttonLabel = storm ? "Keep on" : "Overclock",
                    enabled = weather.CanApplyWeatherChoice(b) == ActionBlockReason.None,
                    action = () => ChooseWeather(b)
                }
            };

            string keplerLine = config != null
                ? (storm ? config.stormLine : config.radiationLine) + source
                : (storm ? "Solar storm tonight!" : "Solar radiation today!");

            card.Show(new ModalCardContent
            {
                banner = storm ? "Solar storm!" : "Solar radiation!",
                bannerColor = storm ? StormBanner : RadiationBanner,
                eventIcon = storm ? stormIcon256 : radiationIcon256,
                bauzerLine = keplerLine,
                bauzerMood = SpeakerMood.Worried,
                choices = choices,
                footerHint = "Answer to unlock Next day"
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
