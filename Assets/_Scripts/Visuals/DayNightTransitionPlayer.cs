using System.Collections;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Plays Next Day as Daylight -> Storm/Radiation (event days) -> Night through AmbientMood / MoodLighting,
    /// then tells the simulation to resolve the night. Dawn switches back to Normal when the morning is ready.
    /// The only subscriber of DayResolutionController.DayResolvingStarted.
    /// </summary>
    public class DayNightTransitionPlayer : MonoBehaviour
    {
        [SerializeField] private DayResolutionController resolution;
        [SerializeField] private AmbientMood ambientMood;
        [SerializeField] private GameplayVisualsConfig config;

        private Coroutine running;

        private void OnEnable()
        {
            if (resolution == null) return;
            resolution.DayResolvingStarted += OnResolvingStarted;
            resolution.MorningReady += OnMorningReady;
        }

        private void OnDisable()
        {
            if (resolution == null) return;
            resolution.DayResolvingStarted -= OnResolvingStarted;
            resolution.MorningReady -= OnMorningReady;
        }

        private void OnResolvingStarted(WeatherEventType weather)
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Play(weather));
        }

        private IEnumerator Play(WeatherEventType weather)
        {
            if (weather != WeatherEventType.Calm)
            {
                SetMood(MoodState.Storm);
                yield return new WaitForSeconds(config != null ? config.stormSeconds : 1.8f);
                SetMood(MoodState.Night);
                yield return new WaitForSeconds(config != null ? config.nightSeconds : 1f);
            }
            else
            {
                SetMood(MoodState.Night);
                yield return new WaitForSeconds(config != null ? config.calmNightSeconds : 1.5f);
            }
            running = null;
            resolution.CompleteNightResolution();
        }

        private void OnMorningReady(DailyReportData _) => SetMood(MoodState.Normal);

        private void SetMood(MoodState mood)
        {
            if (ambientMood != null) ambientMood.SetMood(mood);
        }
    }
}
