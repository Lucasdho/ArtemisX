using System;
using System.Collections;
using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEngine;

namespace ArtemisX.Visuals
{
    /// <summary>
    /// Plays Next Day as Daylight -> Storm/Radiation (event days) -> Night -> Dawn through AmbientMood / MoodLighting
    /// (about 10 s, tunable in GameplayVisualsConfig). The night is resolved at nightfall; dawn plays before the morning
    /// cards. Skip() jumps to the end and still resolves the night exactly once.
    /// The only subscriber of DayResolutionController.DayResolvingStarted.
    /// </summary>
    public class DayNightTransitionPlayer : MonoBehaviour
    {
        [SerializeField] private DayResolutionController resolution;
        [SerializeField] private AmbientMood ambientMood;
        [SerializeField] private GameplayVisualsConfig config;
        [Tooltip("Optional. Found in the scene if left empty. Zooms out to the whole base when the transition starts.")]
        [SerializeField] private CameraPanController panController;

        /// <summary>Raised when the transition starts and when it ends (played out or skipped).</summary>
        public event Action TransitionStarted;
        public event Action TransitionFinished;

        public bool IsPlaying { get; private set; }

        private Coroutine running;
        private bool nightResolved;

        private void OnEnable()
        {
            if (resolution != null) resolution.DayResolvingStarted += OnResolvingStarted;
        }

        private void OnDisable()
        {
            if (resolution != null) resolution.DayResolvingStarted -= OnResolvingStarted;
        }

        private void OnResolvingStarted(WeatherEventType weather)
        {
            if (running != null) StopCoroutine(running);
            IsPlaying = true;
            nightResolved = false;
            TransitionStarted?.Invoke();
            if (panController == null) panController = FindAnyObjectByType<CameraPanController>();
            if (panController != null && panController.isActiveAndEnabled) panController.ZoomOutToBase();
            running = StartCoroutine(Play(weather));
        }

        private IEnumerator Play(WeatherEventType weather)
        {
            bool eventDay = weather != WeatherEventType.Calm;
            Vector4 split = config != null ? (eventDay ? config.eventDaySplit : config.calmDaySplit) : new Vector4(1f, eventDay ? 3f : 0f, 4f, 2f);
            float sum = Mathf.Max(0.0001f, split.x + split.y + split.z + split.w);
            float total = config != null ? config.transitionSeconds : 10f;

            yield return new WaitForSeconds(total * split.x / sum); // Daylight
            if (eventDay)
            {
                SetMood(MoodState.Storm);
                yield return new WaitForSeconds(total * split.y / sum);
            }
            SetMood(MoodState.Night);
            yield return new WaitForSeconds(total * split.z / sum);

            ResolveNightOnce();
            if (resolution == null || !resolution.IsMissionOver)
            {
                SetMood(MoodState.Normal); // Dawn
                yield return new WaitForSeconds(total * split.w / sum);
            }
            Finish();
        }

        /// <summary>Jumps to the end of the transition. Resolves the night if it hasn't been resolved yet.</summary>
        public void Skip()
        {
            if (!IsPlaying) return;
            if (running != null) StopCoroutine(running);
            ResolveNightOnce();
            if (resolution == null || !resolution.IsMissionOver) SetMood(MoodState.Normal);
            Finish();
        }

        private void ResolveNightOnce()
        {
            if (nightResolved) return;
            nightResolved = true;
            if (resolution != null) resolution.CompleteNightResolution();
        }

        private void Finish()
        {
            running = null;
            IsPlaying = false;
            TransitionFinished?.Invoke();
        }

        private void SetMood(MoodState mood)
        {
            if (ambientMood != null) ambientMood.SetMood(mood);
        }
    }
}
