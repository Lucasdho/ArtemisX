using System;
using ArtemisX.Story;
using UnityEngine;

namespace ArtemisX.UI
{
    /// <summary>Texts, timings and sounds for the gameplay HUD, morning cards, rover map and Bauzer tips.</summary>
    [CreateAssetMenu(fileName = "GameplayUIConfig", menuName = "Artemis X/UI/Gameplay UI Config")]
    public class GameplayUIConfig : ScriptableObject
    {
        [Serializable]
        public struct KeyedLine
        {
            public string key;
            public SpeakerMood mood;
            [TextArea(1, 2)] public string text;
        }

        [Header("Bauzer")]
        public Speaker bauzer;
        [Tooltip("Bauzer's comment under the Daily Report, by DailyReportData.bauzerSummaryKey.")]
        public KeyedLine[] reportLines =
        {
            new KeyedLine { key = "crew_restricted", mood = SpeakerMood.Worried, text = "Oxygen or Food hit zero. No building or rover trips until we restock!" },
            new KeyedLine { key = "solar_broken", mood = SpeakerMood.Worried, text = "Storm rocks hit the solar panels. Repair them with 5 Parts to get Power back." },
            new KeyedLine { key = "rover_broken", mood = SpeakerMood.Worried, text = "The rover came back broken. Repair it with 5 Parts before the next trip." },
            new KeyedLine { key = "supply_arrived", mood = SpeakerMood.Happy, text = "A supply capsule from Earth landed! Tap the pile on the landing pad." },
            new KeyedLine { key = "rover_haul", mood = SpeakerMood.Happy, text = "The rover is home with fresh cargo. Nice trip, {playerName}!" },
            new KeyedLine { key = "storm_safe", mood = SpeakerMood.Happy, text = "We made it through the space weather safely. Good call!" },
            new KeyedLine { key = "calm", mood = SpeakerMood.Neutral, text = "Quiet night on the Moon. Let's keep building toward self-sufficiency." },
        };

        [Tooltip("Replaces the Daily Report line on Day 25.")]
        public KeyedLine day25Line = new KeyedLine { key = "day25", mood = SpeakerMood.Worried, text = "Day 25, {playerName}! Only 5 days left. Let's finish those buildings!" };
        [Tooltip("Replaces the Daily Report line on the last mission day.")]
        public KeyedLine finalDayLine = new KeyedLine { key = "final_day", mood = SpeakerMood.Worried, text = "Last day of the mission! Give it your best shot, {playerName}." };
        [TextArea(1, 2)] public string demandLine = "Something needs a quick decision, {playerName}. Pick one!";
        [TextArea(1, 2)] public string stormLine = "A solar storm hits tonight. Retract the panels to keep them safe, or keep full Power and risk rocks?";
        [TextArea(1, 2)] public string radiationLine = "Solar radiation today! Shelter the crew and keep the rover docked, or spend Power on shields.";
        [TextArea(2, 4)] public string stormFallbackNote = "Solar storms shake Earth's and the Moon's space environment. Exposed equipment can get damaged.";
        [TextArea(2, 4)] public string radiationFallbackNote = "Fast particles from the Sun can harm astronauts outside shelter. Shields or walls keep them safe.";

        [Header("Tips (blocked actions)")]
        [TextArea(1, 2)] public string tipCrewRestricted = "Need Oxygen and Food first! Restock before building or sending the rover.";
        [TextArea(1, 2)] public string tipNotEnoughPower = "Not enough Power for that. Keep the solar panels out to recharge.";
        [TextArea(1, 2)] public string tipNotEnoughParts = "Not enough Parts. Mineral trips and supply capsules bring more.";
        [TextArea(1, 2)] public string tipRoverOnMission = "The rover is out on a trip. It comes back tomorrow morning.";
        [TextArea(1, 2)] public string tipRoverBroken = "The rover is broken. Repair it with 5 Parts first.";
        [TextArea(1, 2)] public string tipRoverLocked = "Radiation today! The rover stays docked to keep the crew safe.";
        [TextArea(1, 2)] public string tipBuildOrder = "Build in order: Water & O2 Plant, then Greenhouse, then Fuel Reserve.";
        [TextArea(1, 2)] public string tipGeneric = "That can't be done right now.";
        [TextArea(1, 3)] public string helpText = "Tap glowing objects to act. Send the rover for Water and Parts, then build the next module. Press Next Day when ready!";

        [Header("Card labels")]
        public string startDayLabel = "Start Day";
        public string shieldLabel = "Shield & Retract";
        public string keepPowerLabel = "Keep Full Power";
        public string shelterLabel = "Shelter Crew & Lock Rover";
        public string overclockLabelFormat = "Overclock Shields (-{0} Power)";

        [Header("Rover map")]
        [Tooltip("Seconds for the camera to fly to the top-down map and back.")]
        [Min(0.05f)] public float mapTransitionSeconds = 0.8f;
        [Tooltip("Pitch of the top-down map camera (90 = straight down).")]
        [Range(45f, 90f)] public float mapPitch = 80f;
        [Tooltip("Extra world units around the five sites when framing the map.")]
        [Min(0f)] public float mapMargin = 9f;
        [Tooltip("Marker height above the hot spot in world units.")]
        public float markerHeight = 1.5f;

        [Header("Feedback")]
        [Min(1)] public int flyIconCount = 5;
        [Min(0.05f)] public float flyIconSeconds = 0.7f;
        [Min(0f)] public float flyIconStagger = 0.06f;
        [Min(0.1f)] public float alertPulsePeriod = 0.8f;
        public AudioClip buildClip;
        public AudioClip collectClip;
        public AudioClip errorClip;
        public AudioClip confirmClip;

        public KeyedLine GetReportLine(string key)
        {
            if (reportLines != null)
            {
                foreach (var line in reportLines)
                    if (string.Equals(line.key, key, StringComparison.Ordinal)) return line;
            }
            return new KeyedLine { key = key, mood = SpeakerMood.Neutral, text = string.Empty };
        }
    }
}
