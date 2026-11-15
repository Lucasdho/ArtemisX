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
        [Min(0f)] public float landerStartHeight = 14f;
        [Min(0.1f)] public float landerDescentSeconds = 4.5f;
        public string skipLabel = "SKIP >>";

        [Header("Day 1 onboarding")]
        public Line[] goalLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "First, our mission goal! See these 3 Missions? Each module you build earns 1 Star toward a self-sufficient base." },
            new Line { mood = SpeakerMood.Neutral, text = "Build them in order: 1) Processing Plant, 2) Electrolyser, 3) Terraforming Dome." },
        };
        public string goalHint = "GOAL (1/5)  3 Modules = 3 Stars";
        public Line[] buildNextLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Build here next! The glowing ring marks the Processing Plant." },
            new Line { mood = SpeakerMood.Neutral, text = "Moon rocks and polar ice lock up metals and water. Refining them here gives us Water and Parts without Earth rockets!" },
        };
        public string buildNextHint = "BUILD NEXT (2/5)  Processing Plant (15 Parts, 20 Power)";
        public Line[] step1Lines =
        {
            new Line { mood = SpeakerMood.Neutral, text = "How to get Power: watch space weather! Real NASA DONKI data warns us of storms." },
            new Line { mood = SpeakerMood.Neutral, text = "Solar panels power our base and machines. On storm days, retract them to stay safe—tap the solar field to see." },
        };
        public string step1WeatherHint = "RESOURCES (3/5)  Space weather & Power";
        public string step1Hint = "RESOURCES (3/5)  Tap the solar panels";
        public Line[] step2Lines =
        {
            new Line { mood = SpeakerMood.Happy, text = "How to get Parts & Water: tap the Rover button and send Luna to Ice or Mineral sites!" },
            new Line { mood = SpeakerMood.Neutral, text = "That's how you gather what the Processing Plant needs. Far sites carry more, at higher Power and breakdown risk." },
        };
        public string step2Hint = "RESOURCES (4/5)  Tap the Rover button";
        public Line[] step3Lines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Press Next Day to finish your shift. Overnight, Luna returns with cargo and solar panels recharge!" },
        };
        public string step3Hint = "NEXT SHIFT (5/5)  Press Next Day";
        public Line[] doneLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Training done, {playerName}! Goal: 3 Stars. Tap the glowing Processing Plant ring when you have 15 Parts and 20 Power!" },
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
            new Line { mood = SpeakerMood.Worried, text = "No buildings yet, but every astronaut learns by trying. Start with the Processing Plant next time!" },
            new Line { mood = SpeakerMood.Neutral, text = "One star! Processing Plant online—turning moon rocks and ice into Parts and Water. Aim for the Electrolyser next!" },
            new Line { mood = SpeakerMood.Happy, text = "Two stars! Processing Plant and Electrolyser are making Water, Parts, O2, and H2 fuel. Just the Terraforming Dome left!" },
            new Line { mood = SpeakerMood.Happy, text = "Three stars! Processing, Electrolysis, and Terraforming—the base lives on its own, {playerName}!" },
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
