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

        [Header("Title screen")]
        public string titleName = "ARTEMIS X";
        public string titleSubtitle = "Junior Astronaut Mission Trainer";
        public string titlePlayLabel = "PLAY";
        [Tooltip("World point the title camera orbits (base center).")]
        public Vector3 titleOrbitCenter = Vector3.zero;
        [Min(1f)] public float titleOrbitDistance = 40f;
        [Range(5f, 85f)] public float titleOrbitPitch = 35f;
        public float titleOrbitDegreesPerSecond = 4f;
        [Min(0f)] public float titleFloatPixels = 6f;
        [Min(0f)] public float titleFloatHz = 0.5f;
        [Min(1f)] public float titlePlayPulseScale = 1.04f;
        [Min(0f)] public float titlePlayPulseHz = 1f;

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
            new Line { mood = SpeakerMood.Happy, text = "First, here is our training checklist! Complete these 4 steps to get the base running, then your main missions unlock." },
        };
        public string goalHint = "GOAL (1/6)  3 Modules = 3 Stars";
        public Line[] buildNextLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Build here next! The glowing ring marks the Processing Plant." },
            new Line { mood = SpeakerMood.Neutral, text = "Moon rocks and polar ice lock up metals and water. Refining them here gives us Water and Parts without Earth rockets!" },
        };
        public string buildNextHint = "BUILD NEXT (2/6)  Processing Plant (15 Parts, 20 Power)";
        public Line[] step1Lines =
        {
            new Line { mood = SpeakerMood.Neutral, text = "How to get Power: watch space weather! Real NASA DONKI data warns us of storms." },
            new Line { mood = SpeakerMood.Neutral, text = "Solar panels power our base and machines. On storm days, retract them to stay safe—tap the solar field to see." },
        };
        public string step1WeatherHint = "RESOURCES (3/6)  Space weather & Power";
        public string step1Hint = "RESOURCES (3/6)  Tap the solar panels";
        public Line[] step2Lines =
        {
            new Line { mood = SpeakerMood.Happy, text = "How to get Parts & Water: tap the Rover button and send Luna to Ice or Mineral sites!" },
            new Line { mood = SpeakerMood.Neutral, text = "That's how you gather what the Processing Plant needs. Far sites carry more, at higher Power and breakdown risk." },
        };
        public string step2Hint = "RESOURCES (4/6)  Tap the Rover button";
        public Line[] supplyLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Important tip, {playerName}! Every 5 days, Earth launches a supply capsule to our left landing pad packed with Food and Parts." },
            new Line { mood = SpeakerMood.Neutral, text = "A starter drop just landed! Whenever new cargo is waiting, tap the jumping crate icon to pan straight to it—then tap the crate or icon again to collect!" },
        };
        public string supplyAlertHint = "SUPPLY CARGO (5/6)  Tap the jumping cargo icon";
        public string supplyCollectHint = "SUPPLY CARGO (5/6)  Tap the cargo crate or icon to open it";
        public Line[] step3Lines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Press Next Day to finish your shift. Overnight, Luna returns with cargo and solar panels recharge!" },
        };
        public string step3Hint = "NEXT SHIFT (6/6)  Press Next Day";
        public Line[] doneLines =
        {
            new Line { mood = SpeakerMood.Happy, text = "Training done, {playerName}! Your 3 main missions are now active—earn 3 Stars by building the Processing Plant, Electrolyser, and Terraforming Dome!" },
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

        [Header("End screen (Toy UI)")]
        public string daysCaption = "days";
        public string builtCaption = "built";
        public string leaderboardButtonLabel = "Leaderboard";
        [Min(0.01f)] public float cardScaleFrom = 0.92f;
        [Min(0.01f)] public float cardScaleDuration = 0.18f;
        [Min(0f)] public float ribbonDropDelay = 0.12f;
        [Min(0.01f)] public float ribbonDropDuration = 0.2f;
        public float ribbonDropDistance = 60f;
        [Min(1f)] public float starPopScale = 1.2f;
        [Min(0.01f)] public float starPopDuration = 0.15f;
        [Min(0.1f)] public float raysTurnSeconds = 8f;

        [Header("Leaderboard panel")]
        public string leaderboardTitle = "All missions";
        [Tooltip("{0} = player's day, {1} = k (in 10)")]
        public string leaderboardFasterLine = "You finished on day {0}. That's faster than {1} in 10 players!";
        [Tooltip("{0} = k (in 10)")]
        public string leaderboardExtendedLine = "Your mission was extended. {0} in 10 players needed more time too!";
        public string leaderboardFirstCrewsLine = "You're one of the first crews on the Moon!";
        public string leaderboardOfflineLine = "Leaderboard offline. Your run is saved for next time.";
        public string leaderboardExtendedLabel = "extended";
        public string leaderboardDayLabel = "day {0}";
        public string leaderboardBackLabel = "Back";
        [Min(1)] public int leaderboardMinRunsForStats = 5;
        [Min(1)] public int leaderboardFirstDay = 10;
        [Min(1f)] public float leaderboardBarMaxHeight = 120f;
        [Min(0f)] public float leaderboardBarMinHeight = 2f;
        [Min(1f)] public float leaderboardBarWidth = 15f;
        [Min(0f)] public float leaderboardBarGap = 2f;
        [Min(0f)] public float leaderboardExtendedGap = 8f;
        [Min(1f)] public float leaderboardExtendedWidth = 30f;
        [Min(0.01f)] public float leaderboardGrowDuration = 0.4f;
        [Min(0f)] public float leaderboardGrowStagger = 0.02f;
    }
}
