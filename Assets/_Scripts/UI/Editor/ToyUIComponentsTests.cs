using System.Collections.Generic;
using ArtemisX.Core;
using ArtemisX.Simulation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
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
                if (created[i] is GameObject go)
                {
                    foreach (var c in go.GetComponentsInChildren<Camera>(true))
                        c.targetTexture = null;
                }
                else if (created[i] is Camera c)
                {
                    c.targetTexture = null;
                }
            }
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
            var titleGo = Track(new GameObject("Title", typeof(Text)));
            var bodyGo = Track(new GameObject("Body", typeof(Text)));
            var rewardAGo = Track(new GameObject("RewardA", typeof(Text)));
            var rewardBTextGo = Track(new GameObject("RewardB", typeof(Text)));
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
            so.FindProperty("headerTitle").objectReferenceValue = titleGo.GetComponent<Text>();
            so.FindProperty("bodyLabel").objectReferenceValue = bodyGo.GetComponent<Text>();
            so.FindProperty("rewardTextA").objectReferenceValue = rewardAGo.GetComponent<Text>();
            so.FindProperty("rewardTextB").objectReferenceValue = rewardBTextGo.GetComponent<Text>();
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

            // Ready state: Crossroads Basin (25% calm risk, two rewards)
            card.Show(siteCfg.sites[0], rover, res, weather, null);
            Assert.IsTrue(stickerGo.activeSelf);
            Assert.AreEqual("Crossroads Basin", titleGo.GetComponent<Text>().text);
            Assert.AreEqual("Mid-range. Balanced haul and risk.", bodyGo.GetComponent<Text>().text);
            Assert.AreEqual("+16", rewardAGo.GetComponent<Text>().text);
            Assert.AreEqual("+8", rewardBTextGo.GetComponent<Text>().text);
            Assert.AreEqual("−14", costAmtGo.GetComponent<Text>().text);
            Assert.AreEqual("Send Luna", sendLabelGo.GetComponent<Text>().text);
            Assert.AreEqual(primarySpr, sendBtnGo.GetComponent<Image>().sprite);
            Assert.IsTrue(roverIcoGo.activeSelf);
            Assert.AreEqual(1, CountLit(riskImages, riskOnSpr), "25% calm risk lights 1 of 4 blocks.");
            Assert.IsTrue(rewardBGo.activeSelf);

            // Risk is visual only: 0%, 25%, 50% -> 0, 1, 2 lit blocks.
            card.Show(siteCfg.sites[1], rover, res, weather, null);
            Assert.AreEqual("Frostline Hollow", titleGo.GetComponent<Text>().text);
            Assert.AreEqual("Close by. Cheap and safe trip.", bodyGo.GetComponent<Text>().text);
            Assert.AreEqual("+18", rewardAGo.GetComponent<Text>().text);
            Assert.IsFalse(rewardBGo.activeSelf);
            Assert.AreEqual(0, CountLit(riskImages, riskOnSpr), "0% risk lights no blocks.");

            card.Show(siteCfg.sites[2], rover, res, weather, null);
            Assert.AreEqual("Cryo Abyss", titleGo.GetComponent<Text>().text);
            Assert.AreEqual("Far away. Big haul, risky trip.", bodyGo.GetComponent<Text>().text);
            Assert.AreEqual("+32", rewardAGo.GetComponent<Text>().text);
            Assert.AreEqual(2, CountLit(riskImages, riskOnSpr), "50% risk lights 2 of 4 blocks.");

            // Crew restricted -> Rover locked
            res.Add(ResourceType.Oxygen, -100);
            card.Show(siteCfg.sites[0], rover, res, weather, null);
            Assert.AreEqual("Rover locked", sendLabelGo.GetComponent<Text>().text);
            Assert.AreEqual(disabledSpr, sendBtnGo.GetComponent<Image>().sprite);
            Assert.IsFalse(roverIcoGo.activeSelf);
            res.Add(ResourceType.Oxygen, 100);

            // Dispatch rover so Status becomes OnMission (busy)
            Assert.IsTrue(rover.TryDispatchRover("balanced"));
            card.Show(siteCfg.sites[0], rover, res, weather, null);

            Assert.AreEqual("Luna is out", sendLabelGo.GetComponent<Text>().text);
            Assert.AreEqual(disabledSpr, sendBtnGo.GetComponent<Image>().sprite);
            Assert.IsFalse(roverIcoGo.activeSelf);
            Assert.IsTrue(stickerGo.activeSelf, "CostSticker must stay visible even when rover is busy.");
        }

        [Test]
        public void RoverSiteCardView_StormRiskLightsOneBlock_AndBrokenShowsBrokenState()
        {
            var go = Track(new GameObject("SiteCardStormTest"));
            var res = go.AddComponent<ResourceManager>();
            var weather = go.AddComponent<SpaceWeatherManager>();
            var rover = go.AddComponent<RoverExpeditionManager>();
            var days = go.AddComponent<DayManager>();
            var solar = go.AddComponent<SolarFieldManager>();

            var gameCfg = Track(ScriptableObject.CreateInstance<GameConfig>());
            var siteCfg = Track(ScriptableObject.CreateInstance<ExpeditionSiteConfig>());
            siteCfg.roverName = "Luna";
            siteCfg.stormBreakdownChance = 0.3f;
            siteCfg.sites = new[]
            {
                new ExpeditionSiteConfig.Site { siteId = "safe", displayName = "Frostline Hollow", distance = SiteDistance.Near, powerCost = 10, waterReward = 18, calmBreakdownChance = 0f },
                new ExpeditionSiteConfig.Site { siteId = "far", displayName = "Titan's Vein", distance = SiteDistance.Far, powerCost = 18, partsReward = 16, calmBreakdownChance = 0.5f }
            };
            var weatherCfg = Track(ScriptableObject.CreateInstance<SpaceWeatherConfig>());
            weatherCfg.days = new[] { new SpaceWeatherConfig.WeatherDay { weather = WeatherEventType.SolarStorm } };

            res.Initialize(gameCfg);
            solar.Initialize(gameCfg, res);
            weather.Initialize(weatherCfg, days, res, solar);
            rover.Initialize(siteCfg, gameCfg, res, weather);
            rover.RandomValue = () => 0f; // guarantee breakdown when resolving storm mission

            var card = go.AddComponent<RoverSiteCardView>();
            var root = Track(new GameObject("Root"));
            var sendBtnGo = Track(new GameObject("SendBtn", typeof(Image), typeof(Button)));
            var sendLabelGo = Track(new GameObject("SendLabel", typeof(Text)));
            var roverIcoGo = Track(new GameObject("RoverIcon", typeof(Image)));
            var riskOnSpr = Track(MakeSprite("riskOnStorm"));
            var riskOffSpr = Track(MakeSprite("riskOffStorm"));
            var riskImages = new Image[4];
            for (int i = 0; i < riskImages.Length; i++)
                riskImages[i] = Track(new GameObject("Block" + i, typeof(Image))).GetComponent<Image>();
            var primarySpr = Track(MakeSprite("primaryStorm"));
            var disabledSpr = Track(MakeSprite("disabledStorm"));

            var so = new SerializedObject(card);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("sendButton").objectReferenceValue = sendBtnGo.GetComponent<Button>();
            so.FindProperty("sendButtonImage").objectReferenceValue = sendBtnGo.GetComponent<Image>();
            so.FindProperty("sendButtonLabel").objectReferenceValue = sendLabelGo.GetComponent<Text>();
            so.FindProperty("sendRoverIcon").objectReferenceValue = roverIcoGo.GetComponent<Image>();
            so.FindProperty("sendPrimarySprite").objectReferenceValue = primarySpr;
            so.FindProperty("sendDisabledSprite").objectReferenceValue = disabledSpr;
            so.FindProperty("riskOnSprite").objectReferenceValue = riskOnSpr;
            so.FindProperty("riskOffSprite").objectReferenceValue = riskOffSpr;
            var blocksProp = so.FindProperty("riskBlocks");
            blocksProp.arraySize = riskImages.Length;
            for (int i = 0; i < riskImages.Length; i++)
                blocksProp.GetArrayElementAtIndex(i).objectReferenceValue = riskImages[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // During SolarStorm, even 0% calm site shows 30% storm risk -> 1 lit block.
            card.Show(siteCfg.sites[0], rover, res, weather, null);
            Assert.AreEqual(1, CountLit(riskImages, riskOnSpr), "Solar storm (0.3) must light 1 of 4 blocks.");

            // Dispatch and break rover in storm -> Broken state label & disabled sprite
            Assert.IsTrue(rover.TryDispatchRover("safe"));
            rover.NotifyStorm();
            rover.ResolveReturn();
            Assert.AreEqual(RoverStatus.Broken, rover.Status);

            card.Show(siteCfg.sites[1], rover, res, weather, null);
            Assert.AreEqual("Luna is broken", sendLabelGo.GetComponent<Text>().text);
            Assert.AreEqual(disabledSpr, sendBtnGo.GetComponent<Image>().sprite);
            Assert.IsFalse(roverIcoGo.activeSelf);
        }

        [Test]
        public void RoverSiteMarkerView_RendersCreativeNameAndRiskPercent()
        {
            var go = Track(new GameObject("MarkerTest", typeof(RectTransform)));
            var marker = go.AddComponent<RoverSiteMarkerView>();
            var nameTxt = Track(new GameObject("Name", typeof(Text))).GetComponent<Text>();
            var riskTxt = Track(new GameObject("Risk", typeof(Text))).GetComponent<Text>();

            var so = new SerializedObject(marker);
            so.FindProperty("nameLabel").objectReferenceValue = nameTxt;
            so.FindProperty("riskLabel").objectReferenceValue = riskTxt;
            so.ApplyModifiedPropertiesWithoutUndo();

            marker.Show("Titan's Vein", 18, 0, 16, 50, true);
            Assert.AreEqual("Titan's Vein", nameTxt.text);
            Assert.AreEqual("50% risk", riskTxt.text);

            marker.Show("Frostline Hollow", 10, 18, 0, 0, true);
            Assert.AreEqual("Frostline Hollow", nameTxt.text);
            Assert.AreEqual("0% risk", riskTxt.text);
        }

        [Test]
        public void SupplyAlertButtonView_VisibilityAndBadge_TracksPendingSupplyAndOverlays()
        {
            var simGo = Track(new GameObject("SupplySim"));
            var cfg = Track(ScriptableObject.CreateInstance<GameConfig>());
            cfg.supplyInterval = 5;
            cfg.supplyFood = 30;
            cfg.supplyParts = 15;
            var res = simGo.AddComponent<ResourceManager>();
            res.Initialize(cfg);
            var days = simGo.AddComponent<DayManager>();
            var supply = simGo.AddComponent<SupplyManager>();
            var soSup = new SerializedObject(supply);
            soSup.FindProperty("config").objectReferenceValue = cfg;
            soSup.FindProperty("resources").objectReferenceValue = res;
            soSup.FindProperty("dayManager").objectReferenceValue = days;
            soSup.ApplyModifiedPropertiesWithoutUndo();

            var hudGo = Track(new GameObject("Hud"));
            var hud = hudGo.AddComponent<GameplayHud>();

            var btnGo = Track(new GameObject("SupplyAlertBtn", typeof(RectTransform), typeof(CanvasGroup), typeof(Button), typeof(SupplyAlertButtonView)));
            var cg = btnGo.GetComponent<CanvasGroup>();
            var btn = btnGo.GetComponent<Button>();
            var view = btnGo.GetComponent<SupplyAlertButtonView>();

            var visualGo = Track(new GameObject("Visual", typeof(RectTransform)));
            visualGo.transform.SetParent(btnGo.transform, false);
            var badgeGo = Track(new GameObject("Badge", typeof(RectTransform)));
            badgeGo.transform.SetParent(visualGo.transform, false);
            var badgeTxtGo = Track(new GameObject("Label", typeof(RectTransform), typeof(Text)));
            badgeTxtGo.transform.SetParent(badgeGo.transform, false);
            var badgeTxt = badgeTxtGo.GetComponent<Text>();

            var soView = new SerializedObject(view);
            soView.FindProperty("button").objectReferenceValue = btn;
            soView.FindProperty("canvasGroup").objectReferenceValue = cg;
            soView.FindProperty("visualRoot").objectReferenceValue = visualGo.GetComponent<RectTransform>();
            soView.FindProperty("jumpTarget").objectReferenceValue = visualGo.GetComponent<RectTransform>();
            soView.FindProperty("badgeRoot").objectReferenceValue = badgeGo;
            soView.FindProperty("badgeLabel").objectReferenceValue = badgeTxt;
            soView.ApplyModifiedPropertiesWithoutUndo();

            view.Bind(supply, null, null, hud);
            Assert.IsFalse(view.IsShowing);
            Assert.AreEqual(0f, cg.alpha, 0.001f);
            Assert.IsFalse(visualGo.activeSelf);

            supply.DropStarterSupply(1);
            view.Refresh();
            Assert.IsTrue(view.IsShowing);
            Assert.AreEqual(1f, cg.alpha, 0.001f);
            Assert.IsTrue(visualGo.activeSelf);
            Assert.IsTrue(badgeGo.activeSelf);
            Assert.AreEqual("!", badgeTxt.text);

            supply.OnDayStarted(5);
            view.Refresh();
            Assert.AreEqual("2", badgeTxt.text);

            hud.Cinematic = true;
            view.Refresh();
            Assert.IsFalse(view.IsShowing);
            Assert.AreEqual(0f, cg.alpha, 0.001f);

            hud.Cinematic = false;
            supply.CollectSupply();
            view.Refresh();
            Assert.IsFalse(view.IsShowing);
            Assert.AreEqual(0f, cg.alpha, 0.001f);
        }

        [Test]
        public void SupplyAlertButtonView_ClickPansWhenOffScreenOrBlockedByHud_CollectsWhenClearlyVisible()
        {
            var simGo = Track(new GameObject("SupplySim"));
            var cfg = Track(ScriptableObject.CreateInstance<GameConfig>());
            cfg.supplyInterval = 5;
            cfg.supplyFood = 30;
            cfg.supplyParts = 15;
            var res = simGo.AddComponent<ResourceManager>();
            res.Initialize(cfg);
            var days = simGo.AddComponent<DayManager>();
            var supply = simGo.AddComponent<SupplyManager>();
            var soSup = new SerializedObject(supply);
            soSup.FindProperty("config").objectReferenceValue = cfg;
            soSup.FindProperty("resources").objectReferenceValue = res;
            soSup.FindProperty("dayManager").objectReferenceValue = days;
            soSup.ApplyModifiedPropertiesWithoutUndo();

            var padGo = Track(new GameObject("LandingPad"));
            var pad = padGo.AddComponent<SupplyPileInteractable>();
            var soPad = new SerializedObject(pad);
            soPad.FindProperty("supply").objectReferenceValue = supply;
            soPad.ApplyModifiedPropertiesWithoutUndo();

            var camGo = Track(new GameObject("TestCam", typeof(Camera)));
            var cam = camGo.GetComponent<Camera>();
            var rt = Track(new RenderTexture(1920, 1080, 16));
            cam.targetTexture = rt;
            cam.transform.position = new Vector3(0f, 10f, -15f);
            cam.transform.LookAt(Vector3.zero);
            cam.fieldOfView = 50f;
            var panCfg = Track(ScriptableObject.CreateInstance<CameraConfig>());
            var pan = camGo.AddComponent<CameraPanController>();
            var soPan = new SerializedObject(pan);
            soPan.FindProperty("config").objectReferenceValue = panCfg;
            soPan.FindProperty("viewCamera").objectReferenceValue = cam;
            soPan.ApplyModifiedPropertiesWithoutUndo();

            var canvasGo = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            var occGo = Track(new GameObject("OccluderPanel", typeof(RectTransform), typeof(CanvasGroup)));
            occGo.transform.SetParent(canvasGo.transform, false);
            var occRt = occGo.GetComponent<RectTransform>();
            occRt.anchorMin = new Vector2(0.5f, 0.5f);
            occRt.anchorMax = new Vector2(0.5f, 0.5f);
            occRt.pivot = new Vector2(0.5f, 0.5f);
            occRt.anchoredPosition = Vector2.zero;
            occRt.sizeDelta = new Vector2(500f, 500f);
            var occCg = occGo.GetComponent<CanvasGroup>();
            occGo.SetActive(false);
            Canvas.ForceUpdateCanvases();

            var btnGo = Track(new GameObject("SupplyAlertBtn", typeof(RectTransform), typeof(CanvasGroup), typeof(Button), typeof(SupplyAlertButtonView)));
            btnGo.transform.SetParent(canvasGo.transform, false);
            var view = btnGo.GetComponent<SupplyAlertButtonView>();
            view.Bind(supply, pad, pan, null, new[] { occRt }, cam);

            var actions = new List<bool>();
            view.ActionTriggered += collected => actions.Add(collected);
            supply.DropStarterSupply(1);
            view.Refresh();

            // 1. Off-screen left -> pan, do not collect.
            padGo.transform.position = new Vector3(-60f, 0f, 0f);
            Assert.IsFalse(view.IsTargetClearlyVisible());
            view.OnClicked();
            CollectionAssert.AreEqual(new[] { false }, actions);
            Assert.IsTrue(pan.IsFocusing);
            Assert.IsTrue(supply.HasPendingSupply);

            // 2. Behind camera -> pan, do not collect.
            padGo.transform.position = new Vector3(0f, 10f, -30f);
            Assert.IsFalse(view.IsTargetClearlyVisible());
            view.OnClicked();
            CollectionAssert.AreEqual(new[] { false, false }, actions);
            Assert.IsTrue(supply.HasPendingSupply);

            // 3. Center-screen BUT blocked by active HUD occluder -> pan, do not collect.
            padGo.transform.position = Vector3.zero;
            occGo.SetActive(true);
            occCg.alpha = 1f;
            Canvas.ForceUpdateCanvases();
            Assert.IsFalse(view.IsTargetClearlyVisible());
            view.OnClicked();
            CollectionAssert.AreEqual(new[] { false, false, false }, actions);
            Assert.IsTrue(supply.HasPendingSupply);

            // 4. Occluder hidden via CanvasGroup alpha == 0 -> clearly visible -> collects!
            occCg.alpha = 0f;
            Assert.IsTrue(view.IsTargetClearlyVisible());
            int foodBefore = res.Get(ResourceType.Food);
            int partsBefore = res.Get(ResourceType.Parts);
            view.OnClicked();
            CollectionAssert.AreEqual(new[] { false, false, false, true }, actions);
            Assert.IsFalse(supply.HasPendingSupply);
            Assert.AreEqual(foodBefore + 30, res.Get(ResourceType.Food));
            Assert.AreEqual(partsBefore + 15, res.Get(ResourceType.Parts));
        }

        [Test]
        public void MissionCardView_ShowTutorialWith4Tasks_AndShowMissionsRestoresHeight()
        {
            var cardGo = Track(new GameObject("MissionCard", typeof(RectTransform)));
            var cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(516f, 278f);

            var titleGo = Track(new GameObject("Title", typeof(RectTransform), typeof(Text)));
            titleGo.transform.SetParent(cardGo.transform, false);

            var card = cardGo.AddComponent<MissionCardView>();
            var rows = new MissionCardView.Row[4];
            var rowObjects = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                var rowGo = Track(new GameObject($"Mission_{i + 1}", typeof(RectTransform)));
                rowGo.transform.SetParent(cardGo.transform, false);
                var labelGo = Track(new GameObject("Label", typeof(RectTransform), typeof(Text)));
                labelGo.transform.SetParent(rowGo.transform, false);
                var fillMaskGo = Track(new GameObject("FillMask", typeof(RectTransform)));
                fillMaskGo.transform.SetParent(rowGo.transform, false);
                rowObjects[i] = rowGo;
                rows[i] = new MissionCardView.Row
                {
                    label = labelGo.GetComponent<Text>(),
                    fillMask = fillMaskGo.GetComponent<RectTransform>()
                };
            }
            rowObjects[3].SetActive(false);

            var so = new SerializedObject(card);
            so.FindProperty("headerLabel").objectReferenceValue = titleGo.GetComponent<Text>();
            var rowsProp = so.FindProperty("rows");
            rowsProp.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                var rp = rowsProp.GetArrayElementAtIndex(i);
                rp.FindPropertyRelative("label").objectReferenceValue = rows[i].label;
                rp.FindPropertyRelative("fillMask").objectReferenceValue = rows[i].fillMask;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            card.ShowTutorial(new[] { "Solar", "Rover", "Cargo", "Next Day" });
            Assert.IsTrue(rowObjects[3].activeSelf);
            Assert.AreEqual("Cargo", rows[2].label.text);
            Assert.AreEqual("Next Day", rows[3].label.text);
            Assert.AreEqual(344f, cardRt.sizeDelta.y, 0.1f);

            card.CompleteTutorialTask(2);
            Assert.AreEqual(1f, rows[2].fillMask.anchorMax.x, 0.01f);

            card.ShowMissions();
            Assert.IsFalse(rowObjects[3].activeSelf);
            Assert.AreEqual(278f, cardRt.sizeDelta.y, 0.1f);
        }

        [Test]
        public void RoverSiteMarkerView_ToyPillFlag_HoverAndSelectSwapSpriteAndScale()
        {
            var go = Track(new GameObject("ToyMarkerTest", typeof(RectTransform), typeof(Button)));
            var marker = go.AddComponent<RoverSiteMarkerView>();
            var pillImg = Track(new GameObject("FlagPill", typeof(Image))).GetComponent<Image>();
            var rewardTxt = Track(new GameObject("Reward", typeof(Text))).GetComponent<Text>();
            var nameTxt = Track(new GameObject("Name", typeof(Text))).GetComponent<Text>();
            var riskTxt = Track(new GameObject("Risk", typeof(Text))).GetComponent<Text>();
            var normalSpr = Track(MakeSprite("marker-pill"));
            var selectedSpr = Track(MakeSprite("marker-pill-selected"));
            var riskOn = Track(MakeSprite("risk-on"));
            var riskOff = Track(MakeSprite("risk-off"));
            var blocks = new Image[4];
            for (int i = 0; i < 4; i++) blocks[i] = Track(new GameObject("B" + i, typeof(Image))).GetComponent<Image>();

            var so = new SerializedObject(marker);
            so.FindProperty("button").objectReferenceValue = go.GetComponent<Button>();
            so.FindProperty("nameLabel").objectReferenceValue = nameTxt;
            so.FindProperty("riskLabel").objectReferenceValue = riskTxt;
            so.FindProperty("flagImage").objectReferenceValue = pillImg;
            so.FindProperty("pillNormalSprite").objectReferenceValue = normalSpr;
            so.FindProperty("pillSelectedSprite").objectReferenceValue = selectedSpr;
            so.FindProperty("toyRewardLabel").objectReferenceValue = rewardTxt;
            so.FindProperty("riskOnSprite").objectReferenceValue = riskOn;
            so.FindProperty("riskOffSprite").objectReferenceValue = riskOff;
            var bp = so.FindProperty("toyRiskBlocks");
            bp.arraySize = 4;
            for (int i = 0; i < 4; i++) bp.GetArrayElementAtIndex(i).objectReferenceValue = blocks[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            marker.Show("Cryo Abyss", 18, 32, 0, 50, true);
            Assert.AreEqual("+32", rewardTxt.text);
            Assert.AreEqual(2, CountLit(blocks, riskOn));

            marker.OnPointerEnter(new PointerEventData(EventSystem.current));
            Assert.AreEqual(selectedSpr, pillImg.sprite);
            Assert.AreEqual(1.08f, go.transform.localScale.x, 0.001f);

            marker.OnPointerExit(new PointerEventData(EventSystem.current));
            Assert.AreEqual(normalSpr, pillImg.sprite);
            Assert.AreEqual(1f, go.transform.localScale.x, 0.001f);

            marker.SetSelected(true);
            Assert.AreEqual(selectedSpr, pillImg.sprite);
            Assert.AreEqual(1.08f, go.transform.localScale.x, 0.001f);
        }

        [Test]
        public void ModalCardView_EventChoiceMode_RendersChoicesAndOmitsEmptyOutcome()
        {
            var root = Track(new GameObject("ModalRoot", typeof(RectTransform)));
            var card = Track(new GameObject("Card", typeof(RectTransform)));
            card.transform.SetParent(root.transform, false);
            var modal = root.AddComponent<ModalCardView>();
            var bannerTxt = Track(new GameObject("Banner", typeof(Text))).GetComponent<Text>();
            var keplerRow = Track(new GameObject("KeplerRow"));
            var keplerTxt = Track(new GameObject("KeplerTxt", typeof(Text))).GetComponent<Text>();
            var cardsRow = Track(new GameObject("CardsRow"));
            var footerRow = Track(new GameObject("FooterRow"));
            var footerTxt = Track(new GameObject("FooterTxt", typeof(Text))).GetComponent<Text>();

            var so = new SerializedObject(modal);
            so.FindProperty("root").objectReferenceValue = root;
            so.FindProperty("card").objectReferenceValue = card.GetComponent<RectTransform>();
            so.FindProperty("bannerText").objectReferenceValue = bannerTxt;
            so.FindProperty("keplerBubbleRow").objectReferenceValue = keplerRow;
            so.FindProperty("keplerBubbleText").objectReferenceValue = keplerTxt;
            so.FindProperty("choiceCardsRow").objectReferenceValue = cardsRow;
            so.FindProperty("footerLockRow").objectReferenceValue = footerRow;
            so.FindProperty("footerLockText").objectReferenceValue = footerTxt;

            var slotsProp = so.FindProperty("choiceCards");
            slotsProp.arraySize = 2;
            var outcomeRows = new GameObject[2];
            var costRows = new GameObject[2];
            var costAmounts = new Text[2];
            var outcomeTexts = new Text[2];
            var btnLabels = new Text[2];
            for (int i = 0; i < 2; i++)
            {
                var cGo = Track(new GameObject("C" + i));
                var tTxt = Track(new GameObject("Title" + i, typeof(Text))).GetComponent<Text>();
                costRows[i] = Track(new GameObject("CostRow" + i));
                costAmounts[i] = Track(new GameObject("CostAmt" + i, typeof(Text))).GetComponent<Text>();
                outcomeRows[i] = Track(new GameObject("OutRow" + i));
                outcomeTexts[i] = Track(new GameObject("OutTxt" + i, typeof(Text))).GetComponent<Text>();
                var btn = Track(new GameObject("Btn" + i, typeof(Image), typeof(Button))).GetComponent<Button>();
                btnLabels[i] = Track(new GameObject("BtnLbl" + i, typeof(Text))).GetComponent<Text>();

                var el = slotsProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = cGo;
                el.FindPropertyRelative("title").objectReferenceValue = tTxt;
                el.FindPropertyRelative("costRow").objectReferenceValue = costRows[i];
                el.FindPropertyRelative("costAmount").objectReferenceValue = costAmounts[i];
                el.FindPropertyRelative("outcomeRow").objectReferenceValue = outcomeRows[i];
                el.FindPropertyRelative("outcomeText").objectReferenceValue = outcomeTexts[i];
                el.FindPropertyRelative("button").objectReferenceValue = btn;
                el.FindPropertyRelative("buttonImage").objectReferenceValue = btn.GetComponent<Image>();
                el.FindPropertyRelative("buttonLabel").objectReferenceValue = btnLabels[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            modal.Show(new ModalCardContent
            {
                banner = "Water leak!",
                bauzerLine = "Filter is leaking!",
                footerHint = "Answer to unlock Next day",
                choices = new[]
                {
                    new ModalEventChoice { title = "Fix Filter", costType = ResourceType.Parts, costAmount = 5, outcomeText = "Stops the leak", outcomeRisky = false, buttonLabel = "Fix it", enabled = true },
                    new ModalEventChoice { title = "Skip", costAmount = 0, outcomeText = null, buttonLabel = "Skip", enabled = true }
                }
            });

            Assert.AreEqual("Water leak!", bannerTxt.text);
            Assert.IsTrue(costRows[0].activeSelf);
            Assert.AreEqual("−5", costAmounts[0].text);
            Assert.IsFalse(costRows[1].activeSelf);
            Assert.IsTrue(outcomeRows[0].activeSelf);
            Assert.IsFalse(outcomeRows[1].activeSelf);
            Assert.AreEqual("Fix it", btnLabels[0].text);
            Assert.AreEqual("Answer to unlock Next day", footerTxt.text);
        }

        [Test]
        public void BuildNextFlagView_FollowsNextSlotTintsWhenUnaffordableAndHidesWhenAllBuilt()
        {
            var simGo = Track(new GameObject("BuildFlagSim"));
            var gameCfg = Track(ScriptableObject.CreateInstance<GameConfig>());
            var res = simGo.AddComponent<ResourceManager>();
            res.Initialize(gameCfg);
            res.Add(ResourceType.Parts, -15); // 5 Parts: cannot afford 15 Parts for Processing Plant

            var buildCfg = Track(ScriptableObject.CreateInstance<BuildingConfig>());
            var bldg = simGo.AddComponent<BuildingManager>();
            bldg.Initialize(buildCfg, res);

            var slot0Go = Track(new GameObject("Slot0"));
            var ring0 = Track(new GameObject("GhostRing"));
            ring0.transform.SetParent(slot0Go.transform, false);
            var lr0 = ring0.AddComponent<LineRenderer>();
            var slot0 = slot0Go.AddComponent<GhostSlotInteractable>();
            var s0So = new SerializedObject(slot0);
            s0So.FindProperty("buildings").objectReferenceValue = bldg;
            s0So.FindProperty("buildingIndex").intValue = 0;
            s0So.FindProperty("ghostRing").objectReferenceValue = ring0;
            s0So.ApplyModifiedPropertiesWithoutUndo();

            var slot1Go = Track(new GameObject("Slot1"));
            var slot1 = slot1Go.AddComponent<GhostSlotInteractable>();
            var s1So = new SerializedObject(slot1);
            s1So.FindProperty("buildings").objectReferenceValue = bldg;
            s1So.FindProperty("buildingIndex").intValue = 1;
            s1So.ApplyModifiedPropertiesWithoutUndo();

            var slot2Go = Track(new GameObject("Slot2"));
            var slot2 = slot2Go.AddComponent<GhostSlotInteractable>();
            var s2So = new SerializedObject(slot2);
            s2So.FindProperty("buildings").objectReferenceValue = bldg;
            s2So.FindProperty("buildingIndex").intValue = 2;
            s2So.ApplyModifiedPropertiesWithoutUndo();

            var canvasGo = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            var flagGo = Track(new GameObject("Flag", typeof(RectTransform)));
            flagGo.transform.SetParent(canvasGo.transform, false);
            var visualGo = Track(new GameObject("Visual", typeof(RectTransform)));
            visualGo.transform.SetParent(flagGo.transform, false);
            var pillImg = Track(new GameObject("Pill", typeof(Image))).GetComponent<Image>();
            var nameTxt = Track(new GameObject("Name", typeof(Text))).GetComponent<Text>();
            var c0Root = Track(new GameObject("C0"));
            var c0Amt = Track(new GameObject("C0Amt", typeof(Text))).GetComponent<Text>();
            var c1Root = Track(new GameObject("C1"));
            var c1Amt = Track(new GameObject("C1Amt", typeof(Text))).GetComponent<Text>();

            var flag = flagGo.AddComponent<BuildNextFlagView>();
            var fSo = new SerializedObject(flag);
            fSo.FindProperty("buildings").objectReferenceValue = bldg;
            fSo.FindProperty("resources").objectReferenceValue = res;
            var slotsProp = fSo.FindProperty("slots");
            slotsProp.arraySize = 3;
            slotsProp.GetArrayElementAtIndex(0).objectReferenceValue = slot0;
            slotsProp.GetArrayElementAtIndex(1).objectReferenceValue = slot1;
            slotsProp.GetArrayElementAtIndex(2).objectReferenceValue = slot2;
            fSo.FindProperty("visualRoot").objectReferenceValue = visualGo;
            fSo.FindProperty("pillImage").objectReferenceValue = pillImg;
            fSo.FindProperty("nameLabel").objectReferenceValue = nameTxt;
            var stProp = fSo.FindProperty("costStickers");
            stProp.arraySize = 2;
            stProp.GetArrayElementAtIndex(0).FindPropertyRelative("root").objectReferenceValue = c0Root;
            stProp.GetArrayElementAtIndex(0).FindPropertyRelative("amount").objectReferenceValue = c0Amt;
            stProp.GetArrayElementAtIndex(1).FindPropertyRelative("root").objectReferenceValue = c1Root;
            stProp.GetArrayElementAtIndex(1).FindPropertyRelative("amount").objectReferenceValue = c1Amt;
            fSo.ApplyModifiedPropertiesWithoutUndo();

            flag.RefreshTarget();
            Assert.AreEqual("Processing Plant", nameTxt.text);
            Assert.AreEqual("−15", c0Amt.text);
            Assert.AreEqual("−20", c1Amt.text);
            Assert.IsFalse(lr0.enabled, "Old ring LineRenderer must be disabled on flagged slot.");
            Assert.AreEqual(0.690f, pillImg.color.r, 0.01f, "Unaffordable pill should be #B0B0B0.");

            // Afford & build slot 0 -> moves to slot 1
            res.Add(ResourceType.Parts, 100);
            res.Add(ResourceType.Power, 100);
            Assert.IsTrue(bldg.TryBuildNext());
            flag.RefreshTarget();
            Assert.AreEqual("Electrolyser", nameTxt.text);
            Assert.AreEqual("−25", c0Amt.text);
            Assert.AreEqual(1f, pillImg.color.r, 0.01f);

            // Build slot 1 & slot 2 -> hides flag
            res.Add(ResourceType.Parts, 50);
            res.Add(ResourceType.Power, 50);
            Assert.IsTrue(bldg.TryBuildNext());
            res.Add(ResourceType.Parts, 50);
            res.Add(ResourceType.Power, 50);
            Assert.IsTrue(bldg.TryBuildNext());
            flag.RefreshTarget();
            Assert.IsFalse(visualGo.activeSelf);
        }
    }
}
