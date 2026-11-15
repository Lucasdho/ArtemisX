using System;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Copy and timings for the intro cutscene, the Day 1 onboarding and the end screen.</summary>
    [CreateAssetMenu(fileName = "ScreensConfig", menuName = "Artemis X/UI/Screens Config")]
    public class ScreensConfig : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            [Tooltip("Spoken by the chosen astronaut instead of Bauzer.")]
            public bool player;
            public SpeakerMood mood;
            [TextArea(1, 2)] public string text;
        }

        [Header("Intro cutscene (camera orbit around the lander)")]
        public Vector3 introFocusOffset = new Vector3(0f, 1f, 0f);
        [Min(1f)] public float introStartDistance = 6f;
        [Min(1f)] public float introEndDistance = 22f;
        public float introStartYawOffset = -90f;
        public float introEndYawOffset = 0f;
        [Range(0f, 80f)] public float introStartPitch = 32f;
        [Range(0f, 80f)] public float introEndPitch = 38f;
        [Min(0.1f)] public float introOrbitSeconds = 12f;
        [Min(0.1f)] public float introReturnSeconds = 2.5f;
        public string skipLabel = "SKIP >>";

        [Header("Day 1 onboarding")]
        public Line[] step1Lines =
        {
            new Line { mood = SpeakerMood.Neutral, text = "Step 1: space weather! This panel shows today and tomorrow, using real NASA DONKI data." },
            new Line { mood = SpeakerMood.Neutral, text = "On storm days, retract the solar panels to protect them. Tap the solar panels to see how." },
        };
        public string step1WeatherHint = "STEP 1/3  Space weather";
        public string step1Hint = "STEP 1/3  Tap the solar panels";
        public Line[] step2Lines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Step 2: exploring! Tap the Rover button, then pick a site on the map to send it out." },
            new Line { mood = SpeakerMood.Neutral, text = "Far sites bring more, but cost more Power and break down more often. Your call!" },
        };
        public string step2Hint = "STEP 2/3  Tap the Rover button";
        public Line[] step3Lines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Step 3: press Next Day to end the shift. Watch the Moon go from day to night!" },
        };
        public string step3Hint = "STEP 3/3  Press Next Day";
        public Line[] doneLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Training done, {playerName}! Build the Water & O2 Plant first. The base is yours!" },
        };
        [TextArea(1, 2)] public string replayLine = "Welcome back, {playerName}! Day 1 again. Can you beat your last run?";

        [Header("End screen")]
        public string earlyWinTitle = "MISSION COMPLETE";
        public string dayCapTitle = "MISSION EXTENDED";
        [Tooltip("{0} = days taken")] public string earlyWinSubtitle = "Base self-sufficient in {0} days!";
        [Tooltip("{0} = days, {1} = buildings built")] public string dayCapSubtitle = "Day {0}: {1} of 3 buildings built. Mission extended!";
        [Tooltip("Bauzer's grade by stars (index 0..3).")]
        public Line[] gradeLines =
        {
            new Line { mood = SpeakerMood.Worried, text = "No buildings yet, but every astronaut learns by trying. Let's train again!" },
            new Line { mood = SpeakerMood.Neutral, text = "One star! Fresh water and air for the crew. Next time, aim for the Greenhouse too." },
            new Line { mood = SpeakerMood.Happy, text = "Two stars! Water, air and food. Just the Fuel Reserve left to go home on our own!" },
            new Line { mood = SpeakerMood.Happy, text = "Three stars! The base runs on its own. You're a real lunar engineer, {playerName}!" },
        };
        [Min(1)] public int fastRunDay = 12;
        public Line fastRunLine = new Line { mood = SpeakerMood.Happy, text = "Three stars in record time! Mission Control wants your autograph, {playerName}!" };
        public string histogramTitle = "JUNIOR ASTRONAUTS WORLDWIDE: DAYS TO FINISH";
        public string loadingText = "Connecting to the leaderboard...";
        public string offlineText = "Leaderboard offline. Showing your run only.";
        [Tooltip("{0} = total runs")] public string onlineText = "{0} runs so far. Your bar is highlighted!";
        public string extendedBarLabel = "EXT";
        public string playAgainLabel = "PLAY AGAIN";
        public Color barColor = new Color(0.22f, 0.74f, 0.97f, 0.55f);
        public Color ownBarColor = new Color(0.96f, 0.62f, 0.04f, 1f);
    }
}
