using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArtemisX.Simulation.Editor
{
    /// <summary>
    /// Bakes SpaceWeatherConfig.days from the NASA DONKI dataset stored in the project: mission Day 1 is
    /// missionStartDate, and each following mission day is the next real day. GST at or above stormMinKp
    /// becomes a Solar Storm; an observed SEP event (not a model forecast) becomes Solar Radiation.
    /// </summary>
    public static class DonkiScheduleBaker
    {
        public const string ConfigPath = "Assets/_Config/Simulation/SpaceWeatherConfig.asset";
        public const string DatasetPath = "Assets/_Data/NASA/DONKI/donki_2024_events.json";

        [Serializable]
        public class Dataset
        {
            public string source;
            public string sourceUrl;
            public string retrievedUtc;
            public string range;
            public Storm[] storms;
            public Particle[] particles;
        }

        [Serializable]
        public class Storm
        {
            public string id;
            public string startTime;
            public float maxKp;
            public string link;
        }

        [Serializable]
        public class Particle
        {
            public string id;
            public string eventTime;
            public string instruments;
            public string linkedFlares;
            public string link;
        }

        [MenuItem("Artemis X/Data/Bake DONKI Schedule")]
        public static void BakeMenu()
        {
            var config = AssetDatabase.LoadAssetAtPath<SpaceWeatherConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError("[DONKI] SpaceWeatherConfig not found at " + ConfigPath);
                return;
            }
            Bake(config);
        }

        /// <summary>Fills config.days from its DONKI dataset and saves the asset.</summary>
        public static void Bake(SpaceWeatherConfig config)
        {
            if (config.donkiDataset == null)
                config.donkiDataset = AssetDatabase.LoadAssetAtPath<TextAsset>(DatasetPath);
            if (config.donkiDataset == null)
            {
                Debug.LogError("[DONKI] No dataset assigned and none found at " + DatasetPath);
                return;
            }

            var dataset = Parse(config.donkiDataset.text);
            Undo.RecordObject(config, "Bake DONKI Schedule");
            config.days = BuildSchedule(dataset, config.missionStartDate, config.scheduleLength, config.stormMinKp);
            if (!string.IsNullOrEmpty(dataset.sourceUrl)) config.sourceUrl = dataset.sourceUrl;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            var events = config.days.Select((d, i) => (d, day: i + 1)).Where(x => x.d.weather != WeatherEventType.Calm)
                .Select(x => $"Day {x.day}: {x.d.weather} ({x.d.donkiDate} {x.d.donkiEventType})");
            Debug.Log($"[DONKI] Baked {config.days.Length} days from {config.missionStartDate}. " + string.Join(", ", events));
        }

        public static Dataset Parse(string json) => JsonUtility.FromJson<Dataset>(json);

        public static SpaceWeatherConfig.WeatherDay[] BuildSchedule(Dataset dataset, string startDate, int length, float stormMinKp)
        {
            var start = DateTime.ParseExact(startDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var storms = (dataset.storms ?? new Storm[0]).GroupBy(s => DateOf(s.startTime)).ToDictionary(g => g.Key, g => g.Max(s => s.maxKp));
            var particles = (dataset.particles ?? new Particle[0])
                .Where(p => !string.IsNullOrEmpty(p.instruments) && !p.instruments.StartsWith("MODEL", StringComparison.OrdinalIgnoreCase))
                .GroupBy(p => DateOf(p.eventTime)).ToDictionary(g => g.Key, g => g.ToList());

            var days = new SpaceWeatherConfig.WeatherDay[length];
            for (int i = 0; i < length; i++)
            {
                string date = start.AddDays(i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (storms.TryGetValue(date, out float kp) && kp >= stormMinKp)
                {
                    days[i] = new SpaceWeatherConfig.WeatherDay
                    {
                        weather = WeatherEventType.SolarStorm,
                        donkiDate = date,
                        donkiEventType = "GST",
                        note = $"A blast of solar wind hit Earth and the Moon: Kp {kp:0.##}, a {GScale(kp)} geomagnetic storm."
                    };
                }
                else if (particles.TryGetValue(date, out List<Particle> seen))
                {
                    string spacecraft = string.Join(" and ", seen.Select(p => p.instruments.Split(':')[0].Trim()).Distinct());
                    string flare = seen.Select(p => p.linkedFlares).FirstOrDefault(f => !string.IsNullOrEmpty(f));
                    days[i] = new SpaceWeatherConfig.WeatherDay
                    {
                        weather = WeatherEventType.SolarRadiation,
                        donkiDate = date,
                        donkiEventType = "SEP",
                        note = string.IsNullOrEmpty(flare)
                            ? $"Fast solar particles were measured by {spacecraft}."
                            : $"A class {flare.Split(',')[0].Trim()} solar flare sent fast particles into space, measured by {spacecraft}."
                    };
                }
                else
                {
                    days[i] = new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.Calm, note = "Quiet space weather." };
                }
            }
            return days;
        }

        private static string DateOf(string isoTime) => string.IsNullOrEmpty(isoTime) || isoTime.Length < 10 ? "" : isoTime.Substring(0, 10);

        private static string GScale(float kp)
        {
            if (kp >= 9f) return "G5 (extreme)";
            if (kp >= 8f) return "G4 (severe)";
            if (kp >= 7f) return "G3 (strong)";
            if (kp >= 6f) return "G2 (moderate)";
            return "G1 (minor)";
        }
    }
}
