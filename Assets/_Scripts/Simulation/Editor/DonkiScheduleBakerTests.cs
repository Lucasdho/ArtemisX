using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ArtemisX.Simulation.Editor
{
    public class DonkiScheduleBakerTests
    {
        [Test]
        public void ConfigSchedule_MatchesTheStoredDonkiDataset()
        {
            var config = AssetDatabase.LoadAssetAtPath<SpaceWeatherConfig>(DonkiScheduleBaker.ConfigPath);
            var dataset = config.donkiDataset != null ? config.donkiDataset : AssetDatabase.LoadAssetAtPath<TextAsset>(DonkiScheduleBaker.DatasetPath);
            Assert.IsNotNull(dataset, "DONKI dataset missing from the project");

            var baked = DonkiScheduleBaker.BuildSchedule(DonkiScheduleBaker.Parse(dataset.text),
                config.missionStartDate, config.scheduleLength, config.stormMinKp);

            Assert.AreEqual(baked.Length, config.days.Length, "Re-run Artemis X > Data > Bake DONKI Schedule");
            for (int i = 0; i < baked.Length; i++)
            {
                Assert.AreEqual(baked[i].weather, config.days[i].weather, $"Day {i + 1} weather differs from the dataset");
                Assert.AreEqual(baked[i].donkiDate ?? "", config.days[i].donkiDate ?? "", $"Day {i + 1} DONKI date differs");
            }
        }

        [Test]
        public void Bake_From2024August_GivesStormsAndRadiationFromRealRecords()
        {
            var dataset = AssetDatabase.LoadAssetAtPath<TextAsset>(DonkiScheduleBaker.DatasetPath);
            var days = DonkiScheduleBaker.BuildSchedule(DonkiScheduleBaker.Parse(dataset.text), "2024-08-01", 30, 6f);
            var events = days.Select((d, i) => (d.weather, day: i + 1)).Where(x => x.weather != WeatherEventType.Calm).ToList();

            CollectionAssert.AreEqual(new[] { 4, 5, 12, 17 }, events.Select(e => e.day).ToArray());
            Assert.AreEqual(WeatherEventType.SolarStorm, days[3].weather);     // 2024-08-04 GST Kp 6.67
            Assert.AreEqual(WeatherEventType.SolarRadiation, days[4].weather); // 2024-08-05 SEP after X1.7 flare
            Assert.AreEqual(WeatherEventType.SolarStorm, days[11].weather);    // 2024-08-12 GST Kp 8
        }
    }
}
