using System.Collections.Generic;
using ArtemisX.Core;
using ArtemisX.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.Tests
{
    public class ToyUIComponentsTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
            created.Clear();
        }

        private T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        private static Sprite MakeSprite(string name)
        {
            var tex = new Texture2D(4, 4);
            var sp = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            sp.name = name;
            return sp;
        }

        private static int CountLit(Image[] blocks, Sprite onSprite)
        {
            int n = 0;
            foreach (var b in blocks) if (b.sprite == onSprite) n++;
            return n;
        }

        [Test]
        public void DayCalendarView_RendersTodayCalmStormSupplyAndUnknown()
        {
            var go = Track(new GameObject("CalendarTest"));
            var days = go.AddComponent<DayManager>();
            var res = go.AddComponent<ResourceManager>();
            var weather = go.AddComponent<SpaceWeatherManager>();
            var supply = go.AddComponent<SupplyManager>();
            var solar = go.AddComponent<SolarFieldManager>();

            var gameCfg = Track(ScriptableObject.CreateInstance<GameConfig>());
            gameCfg.startDay = 1;
            gameCfg.missionLength = 30;
            gameCfg.supplyInterval = 5;

            var weatherCfg = Track(ScriptableObject.CreateInstance<SpaceWeatherConfig>());
            // Only 4 days of forecast: Day 1 Calm, Day 2 Storm, Day 3 Calm, Day 4 Calm; Day 5 has no forecast, but IS a supply day.
            weatherCfg.days = new[]
            {
                new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.Calm },
                new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.SolarStorm },
                new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.Calm },
                new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.Calm }
            };

            res.Initialize(gameCfg);
            solar.Initialize(gameCfg, res);
            weather.Initialize(weatherCfg, days, res, solar);
            supply.Initialize(gameCfg, res, days);

            var cal = go.AddComponent<DayCalendarView>();
            var so = new SerializedObject(cal);
            so.FindProperty("days").objectReferenceValue = days;
            so.FindProperty("weather").objectReferenceValue = weather;
            so.FindProperty("supply").objectReferenceValue = supply;

            var todaySpr = Track(MakeSprite("day-today"));
            var calmSpr = Track(MakeSprite("day-tile"));
            var stormSpr = Track(MakeSprite("day-tile-storm"));
            var calmIco = Track(MakeSprite("cloud"));
            var stormIco = Track(MakeSprite("thunder"));
            var rocketIco = Track(MakeSprite("rocket"));
            var warnIco = Track(MakeSprite("warning"));

            so.FindProperty("todaySprite").objectReferenceValue = todaySpr;
            so.FindProperty("futureCalmSprite").objectReferenceValue = calmSpr;
            so.FindProperty("futureStormSprite").objectReferenceValue = stormSpr;
            so.FindProperty("calmIcon").objectReferenceValue = calmIco;
            so.FindProperty("stormIcon").objectReferenceValue = stormIco;
            so.FindProperty("supplyRocketIcon").objectReferenceValue = rocketIco;
            so.FindProperty("warningIcon").objectReferenceValue = warnIco;

            var tilesProp = so.FindProperty("tiles");
            tilesProp.arraySize = 5;
            var bgs = new Image[5];
            var nums = new Text[5];
            var icons = new Image[5];
            var warns = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var tileGo = Track(new GameObject("Day_" + i, typeof(RectTransform), typeof(Image)));
                tileGo.transform.SetParent(go.transform, false);
                var numGo = Track(new GameObject("Number", typeof(RectTransform), typeof(Text)));
                numGo.transform.SetParent(tileGo.transform, false);
                var icoGo = Track(new GameObject("Icon", typeof(RectTransform), typeof(Image)));
                icoGo.transform.SetParent(tileGo.transform, false);
                var warnGo = Track(new GameObject("Warning", typeof(RectTransform), typeof(Image)));
                warnGo.transform.SetParent(tileGo.transform, false);

                bgs[i] = tileGo.GetComponent<Image>();
                nums[i] = numGo.GetComponent<Text>();
                icons[i] = icoGo.GetComponent<Image>();
                warns[i] = warnGo.GetComponent<Image>();

                var el = tilesProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = tileGo;
                el.FindPropertyRelative("background").objectReferenceValue = bgs[i];
                el.FindPropertyRelative("dayNumber").objectReferenceValue = nums[i];
                el.FindPropertyRelative("icon").objectReferenceValue = icons[i];
                el.FindPropertyRelative("warningBadge").objectReferenceValue = warns[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            cal.Refresh();

            // Day 1 (Today): calm
            Assert.AreEqual("1", nums[0].text);
            Assert.AreEqual(todaySpr, bgs[0].sprite);
            Assert.AreEqual(calmIco, icons[0].sprite);
            Assert.IsFalse(warns[0].gameObject.activeSelf);

            // Day 2: storm
            Assert.AreEqual("2", nums[1].text);
            Assert.AreEqual(stormSpr, bgs[1].sprite);
            Assert.AreEqual(stormIco, icons[1].sprite);
            Assert.IsTrue(warns[1].gameObject.activeSelf);

            // Day 5: forecast unknown (ForecastLength = 4), still shows Supply Rocket up to missionDays (30)!
            Assert.AreEqual("5", nums[4].text);
            Assert.AreEqual(calmSpr, bgs[4].sprite);
            Assert.AreEqual(rocketIco, icons[4].sprite);
            Assert.IsTrue(icons[4].enabled);
            Assert.IsFalse(warns[4].gameObject.activeSelf);

            // Advance to Day 2 (Storm Today!)
            days.AdvanceDay();
            weather.SetDay(2);
            cal.Refresh();

            Assert.AreEqual("2", nums[0].text);
            Assert.AreEqual(todaySpr, bgs[0].sprite);
            Assert.AreEqual(stormIco, icons[0].sprite);
            Assert.IsTrue(warns[0].gameObject.activeSelf);

            // Day 6 (index 4 on Day 2): unknown & not supply -> icon disabled
            Assert.AreEqual("6", nums[4].text);
            Assert.IsFalse(icons[4].enabled);
            Assert.IsNull(icons[4].sprite);
        }

        [Test]
        public void RoverSiteCardView_KeepsCostStickerVisibleWhenRoverIsBusy()
        {
            var go = Track(new GameObject("SiteCardTest"));
            var res = go.AddComponent<ResourceManager>();
            var weather = go.AddComponent<SpaceWeatherManager>();
            var rover = go.AddComponent<RoverExpeditionManager>();
            var days = go.AddComponent<DayManager>();
            var solar = go.AddComponent<SolarFieldManager>();

            var gameCfg = Track(ScriptableObject.CreateInstance<GameConfig>());
            var siteCfg = Track(ScriptableObject.CreateInstance<ExpeditionSiteConfig>());
            siteCfg.roverName = "Luna";
            siteCfg.sites = new[]
            {
                new ExpeditionSiteConfig.Site
                {
                    siteId = "balanced",
                    displayName = "Crossroads Basin",
                    distance = SiteDistance.Medium,
                    powerCost = 14,
                    waterReward = 16,
                    partsReward = 8,
                    calmBreakdownChance = 0.25f
                },
                new ExpeditionSiteConfig.Site { siteId = "safe", displayName = "Frostline Hollow", distance = SiteDistance.Near, powerCost = 10, waterReward = 18, calmBreakdownChance = 0f },
                new ExpeditionSiteConfig.Site { siteId = "far", displayName = "Cryo Abyss", distance = SiteDistance.Far, powerCost = 18, waterReward = 32, calmBreakdownChance = 0.5f }
            };
            var weatherCfg = Track(ScriptableObject.CreateInstance<SpaceWeatherConfig>());
            weatherCfg.days = new[] { new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.Calm } };

            res.Initialize(gameCfg);
            solar.Initialize(gameCfg, res);
            weather.Initialize(weatherCfg, days, res, solar);
            rover.Initialize(siteCfg, gameCfg, res, weather);

            var card = go.AddComponent<RoverSiteCardView>();
            var root = Track(new GameObject("Root"));
            var sendBtnGo = Track(new GameObject("SendBtn", typeof(Image), typeof(Button)));
            var sendLabelGo = Track(new GameObject("SendLabel", typeof(Text)));
            var roverIcoGo = Track(new GameObject("RoverIcon", typeof(Image)));
            var stickerGo = Track(new GameObject("CostSticker"));
            var costAmtGo = Track(new GameObject("CostAmount", typeof(Text)));
            var riskOnSpr = Track(MakeSprite("riskOn"));
            var riskOffSpr = Track(MakeSprite("riskOff"));
            var riskImages = new Image[4];
            for (int i = 0; i < riskImages.Length; i++)
                riskImages[i] = Track(new GameObject("Block" + i, typeof(Image))).GetComponent<Image>();
            var rewardBGo = Track(new GameObject("SlotB"));
            var primarySpr = Track(MakeSprite("primary"));
            var disabledSpr = Track(MakeSprite("disabled"));

            var so = new SerializedObject(card);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("sendButton").objectReferenceValue = sendBtnGo.GetComponent<Button>();
            so.FindProperty("sendButtonImage").objectReferenceValue = sendBtnGo.GetComponent<Image>();
            so.FindProperty("sendButtonLabel").objectReferenceValue = sendLabelGo.GetComponent<Text>();
            so.FindProperty("sendRoverIcon").objectReferenceValue = roverIcoGo.GetComponent<Image>();
            so.FindProperty("sendPrimarySprite").objectReferenceValue = primarySpr;
            so.FindProperty("sendDisabledSprite").objectReferenceValue = disabledSpr;
            so.FindProperty("costSticker").objectReferenceValue = stickerGo;
            so.FindProperty("costAmount").objectReferenceValue = costAmtGo.GetComponent<Text>();
            so.FindProperty("riskOnSprite").objectReferenceValue = riskOnSpr;
            so.FindProperty("riskOffSprite").objectReferenceValue = riskOffSpr;
            var blocksProp = so.FindProperty("riskBlocks");
            blocksProp.arraySize = riskImages.Length;
            for (int i = 0; i < riskImages.Length; i++)
                blocksProp.GetArrayElementAtIndex(i).objectReferenceValue = riskImages[i];
            so.FindProperty("rewardSlotB").objectReferenceValue = rewardBGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Ready state
            card.Show(siteCfg.sites[0], rover, res, weather, null);
            Assert.IsTrue(stickerGo.activeSelf);
            Assert.AreEqual("−14", costAmtGo.GetComponent<Text>().text);
            Assert.AreEqual("Send Luna", sendLabelGo.GetComponent<Text>().text);
            Assert.AreEqual(primarySpr, sendBtnGo.GetComponent<Image>().sprite);
            Assert.AreEqual(1, CountLit(riskImages, riskOnSpr), "25% calm risk lights 1 of 4 blocks.");
            Assert.IsTrue(rewardBGo.activeSelf);

            // Risk is visual only: 0%, 25%, 50% -> 0, 1, 2 lit blocks.
            card.Show(siteCfg.sites[1], rover, res, weather, null);
            Assert.AreEqual(0, CountLit(riskImages, riskOnSpr), "0% risk lights no blocks.");
            card.Show(siteCfg.sites[2], rover, res, weather, null);
            Assert.AreEqual(2, CountLit(riskImages, riskOnSpr), "50% risk lights 2 of 4 blocks.");
            card.Show(siteCfg.sites[0], rover, res, weather, null);

            // Dispatch rover so Status becomes OnMission (busy)
            Assert.IsTrue(rover.TryDispatchRover("balanced"));
            card.Show(siteCfg.sites[0], rover, res, weather, null);

            Assert.AreEqual("Luna is out", sendLabelGo.GetComponent<Text>().text);
            Assert.AreEqual(disabledSpr, sendBtnGo.GetComponent<Image>().sprite);
            Assert.IsTrue(stickerGo.activeSelf, "CostSticker must stay visible even when rover is busy.");
        }
    }
}
