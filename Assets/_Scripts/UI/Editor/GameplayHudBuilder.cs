using System.Collections.Generic;
using System.Linq;
using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Story;
using ArtemisX.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.EditorTools
{
    /// <summary>
    /// Run 3B one-shot scene wiring for HandMadeBase.unity: adds the simulation managers, the gameplay HUD
    /// (weather widget, rover / help buttons, morning card, rover map, fly-icon layer), and the world interactables.
    /// Safe to run again: it rebuilds its own objects and re-wires references.
    /// </summary>
    public static class GameplayHudBuilder
    {
        private const string ConfigDir = "Assets/_Config/";
        private const string UiConfigPath = "Assets/_Config/Visuals/GameplayUIConfig.asset";
        private const string VisualsConfigPath = "Assets/_Config/Visuals/GameplayVisualsConfig.asset";
        private const string CirclePath = "Assets/_Prefabs/UI/Sprites/UICircle.png";

        private static UIStyle style;
        private static Sprite circle;
        private static Font font;

        [MenuItem("Artemis X/Run 3B/Build Gameplay HUD and Wiring")]
        public static void Build()
        {
            style = Load<UIStyle>("Assets/_Config/UI/UIStyle.asset");
            circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
            font = style != null ? style.font : null;

            var uiConfig = LoadOrCreate<GameplayUIConfig>(UiConfigPath);
            var visConfig = LoadOrCreate<GameplayVisualsConfig>(VisualsConfigPath);
            SetupUiConfig(uiConfig);

            var gameConfig = Load<GameConfig>(ConfigDir + "GameConfig.asset");
            var visuals = Load<VisualsConfig>(ConfigDir + "Visuals/VisualsConfig.asset");

            // ---------- Managers ----------
            var dayManager = Object.FindAnyObjectByType<DayManager>();
            var managers = dayManager.gameObject;
            var resources = managers.GetComponent<ResourceManager>();
            var runner = Object.FindAnyObjectByType<DialogueRunner>();
            var story = Object.FindAnyObjectByType<StoryDirector>();
            var nextDayFlow = Object.FindAnyObjectByType<NextDayFlow>();

            var weather = GetOrAdd<SpaceWeatherManager>(managers);
            var demands = GetOrAdd<UnexpectedDemandManager>(managers);
            var rover = GetOrAdd<RoverExpeditionManager>(managers);
            var solar = GetOrAdd<SolarFieldManager>(managers);
            var buildings = GetOrAdd<BuildingManager>(managers);
            var supply = GetOrAdd<SupplyManager>(managers);
            var resolution = GetOrAdd<DayResolutionController>(managers);
            var dayNight = GetOrAdd<DayNightTransitionPlayer>(managers);
            var router = GetOrAdd<WorldClickRouter>(managers);

            var weatherCfg = Load<SpaceWeatherConfig>(ConfigDir + "Simulation/SpaceWeatherConfig.asset");
            Wire(weather, ("config", weatherCfg), ("dayManager", dayManager), ("resources", resources), ("solar", solar));
            Wire(demands, ("config", Load<UnexpectedDemandConfig>(ConfigDir + "Simulation/UnexpectedDemandConfig.asset")),
                ("resources", resources), ("dayManager", dayManager), ("weather", weather));
            Wire(rover, ("config", Load<ExpeditionSiteConfig>(ConfigDir + "Simulation/ExpeditionSiteConfig.asset")),
                ("gameConfig", gameConfig), ("resources", resources), ("weather", weather));
            Wire(solar, ("config", gameConfig), ("resources", resources));
            Wire(buildings, ("config", Load<BuildingConfig>(ConfigDir + "Simulation/BuildingConfig.asset")), ("resources", resources));
            Wire(supply, ("config", gameConfig), ("resources", resources), ("dayManager", dayManager));
            Wire(resolution, ("config", gameConfig), ("dayManager", dayManager), ("resources", resources), ("weather", weather),
                ("rover", rover), ("solar", solar), ("buildings", buildings), ("supply", supply), ("demands", demands),
                ("storyDirector", story));
            if (nextDayFlow != null) Wire(nextDayFlow, ("dayResolution", resolution));
            if (story != null) SetBool(story, "queueDailyBriefings", false);

            var oldInteractor = managers.GetComponent<BuildingClickInteractor>();
            if (oldInteractor != null) Object.DestroyImmediate(oldInteractor);
            foreach (var cb in Object.FindObjectsByType<ClickableBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(cb);

            var ambient = Object.FindAnyObjectByType<AmbientMood>();
            Wire(dayNight, ("resolution", resolution), ("ambientMood", ambient), ("config", visConfig));

            // ---------- HUD ----------
            var hudRoot = Object.FindAnyObjectByType<MissionCardView>().transform.parent;
            var hudCanvas = hudRoot.GetComponent<Canvas>();
            foreach (var n in new[] { "WeatherWidget", "RoverButton", "HelpButton", "MorningCard", "RoverMap", "FlyLayer", "NightSkip" })
            {
                var old = hudRoot.Find(n);
                if (old != null) Object.DestroyImmediate(old.gameObject);
            }

            var hud = GetOrAdd<GameplayHud>(hudRoot.gameObject);
            var morning = GetOrAdd<MorningSequenceController>(hudRoot.gameObject);
            var map = GetOrAdd<RoverMapController>(hudRoot.gameObject);
            var fly = GetOrAdd<ResourceFlyFx>(hudRoot.gameObject);
            var sfx = GetOrAdd<AudioSource>(hudRoot.gameObject);
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            var popup = hudRoot.GetComponentInChildren<ContextConfirmPopupView>(true);
            BuildPopupCosts(popup);

            var widget = BuildWeatherWidget(hudRoot, weather);
            var roverButton = BuildRoverButton(hudRoot, rover, resources);
            var helpButton = BuildHelpButton(hudRoot);
            var card = BuildMorningCard(hudRoot, uiConfig, runner);
            var flyLayer = BuildStretch("FlyLayer", hudRoot);
            var skipRoot = BuildSkipOverlay(hudRoot, dayNight);
            BuildPillBadges(hudRoot);

            // Pills to fly to.
            var pills = hudRoot.GetComponentsInChildren<ResourcePillView>(true);
            var flySo = new SerializedObject(fly);
            flySo.FindProperty("config").objectReferenceValue = uiConfig;
            flySo.FindProperty("style").objectReferenceValue = style;
            flySo.FindProperty("layer").objectReferenceValue = flyLayer;
            var targets = flySo.FindProperty("targets");
            targets.arraySize = pills.Length;
            for (int i = 0; i < pills.Length; i++)
            {
                var pso = new SerializedObject(pills[i]);
                var el = targets.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("type").enumValueIndex = pso.FindProperty("type").enumValueIndex;
                el.FindPropertyRelative("pill").objectReferenceValue = pills[i].transform;
            }
            flySo.ApplyModifiedPropertiesWithoutUndo();

            var mission = Object.FindAnyObjectByType<MissionCardView>();
            Wire(mission, ("buildings", buildings));

            // ---------- World ----------
            var solarFarm = GameObject.Find("solarFarm");
            var solarInteract = GetOrAdd<SolarFieldInteractable>(solarFarm);
            Wire(solarInteract, ("hud", hud), ("solar", solar), ("gameConfig", gameConfig));
            BuildSolarVisuals(solarFarm, solar, visConfig, visuals);

            var plant = GameObject.Find("ProcessingPlant");
            var electro = GameObject.Find("Electrolyser_H2Generator");
            var terraforming = GameObject.Find("foodFarm");
            var ringMaterial = Object.FindAnyObjectByType<ResourceHotspot>().GetComponentInChildren<LineRenderer>().sharedMaterial;
            BuildSlot(plant, 0, hud, buildings, visConfig, visuals, ringMaterial);
            BuildSlot(electro, 1, hud, buildings, visConfig, visuals, ringMaterial);
            BuildSlot(terraforming, 2, hud, buildings, visConfig, visuals, ringMaterial);

            var roverGo = Object.FindAnyObjectByType<RoverDriver>(FindObjectsInactive.Include).gameObject;
            var oldDriver = roverGo.GetComponent<RoverDriver>();
            if (oldDriver != null) oldDriver.enabled = false;
            EnsureBoxCollider(roverGo);
            var roverInteract = GetOrAdd<RoverInteractable>(roverGo);
            Wire(roverInteract, ("hud", hud), ("rover", rover), ("resources", resources));
            var missionDriver = GetOrAdd<RoverMissionDriver>(roverGo);
            var routes = GameObject.Find("RoverRoutes").GetComponentsInChildren<WaypointPath>();
            var hotspots = Object.FindObjectsByType<ResourceHotspot>(FindObjectsSortMode.None);
            Wire(missionDriver, ("rover", rover), ("config", visConfig), ("wheelDust", roverGo.GetComponentInChildren<ParticleSystem>()));
            SetArray(missionDriver, "routes", routes);
            SetArray(missionDriver, "hotspots", hotspots);

            var pad = GameObject.Find("suplyLandingPad");
            var pileInteract = GetOrAdd<SupplyPileInteractable>(pad);
            Wire(pileInteract, ("hud", hud), ("supply", supply), ("morning", morning), ("visuals", visConfig),
                ("pile", pad.transform.Find("cargo_A_stacked").gameObject), ("capsule", GameObject.Find("dropship").transform));

            // ---------- Rover map ----------
            var camRig = Object.FindAnyObjectByType<CameraPanController>();
            var mapParts = BuildRoverMap(hudRoot, rover.GetComponent<RoverExpeditionManager>(), hotspots, out var markers, out var back, out var title, out var markerLayer);
            Wire(map, ("hud", hud), ("config", uiConfig), ("rover", rover), ("resources", resources), ("weather", weather),
                ("cameraRig", camRig.transform), ("panController", camRig), ("mapRoot", mapParts), ("markerLayer", markerLayer),
                ("backButton", back), ("titleLabel", title));
            SetArray(map, "markers", markers);

            // ---------- Hub ----------
            Wire(router, ("hud", hud), ("panController", camRig));
            Wire(morning, ("config", uiConfig), ("style", style), ("card", card), ("resolution", resolution), ("demands", demands),
                ("weather", weather), ("rover", rover), ("hud", hud), ("transition", dayNight));
            Wire(roverButton, ("roverInteractable", roverInteract));
            var nextDayButton = nextDayFlow != null ? new SerializedObject(nextDayFlow).FindProperty("nextDayButton").objectReferenceValue as Button : null;
            var groups = new List<CanvasGroup>();
            foreach (var go in new[] { nextDayButton != null ? nextDayButton.gameObject : null, roverButton.gameObject, helpButton.gameObject })
                if (go != null) groups.Add(GetOrAdd<CanvasGroup>(go));
            Wire(hud, ("config", uiConfig), ("viewCamera", Camera.main), ("runner", runner), ("resolution", resolution), ("popup", popup),
                ("morning", morning), ("roverMap", map), ("flyFx", fly), ("transition", dayNight), ("sfxSource", sfx), ("helpButton", helpButton));
            SetArray(hud, "bottomActions", groups.ToArray());

            // Morning card and fly layer on top, then dialogue above them.
            card.transform.SetAsLastSibling();
            flyLayer.SetAsLastSibling();
            skipRoot.SetAsLastSibling();
            var dialogueCanvas = hudRoot.Find("DialogueCanvas");
            if (dialogueCanvas != null) dialogueCanvas.SetAsLastSibling();
            popup.transform.SetAsLastSibling();

            EditorSceneManager.MarkSceneDirty(managers.scene);
            Debug.Log("[Run 3B] Gameplay HUD and wiring built.");
        }

        // ---------- Config ----------

        private static void SetupUiConfig(GameplayUIConfig cfg)
        {
            var so = new SerializedObject(cfg);
            if (so.FindProperty("bauzer").objectReferenceValue == null)
            {
                var guid = AssetDatabase.FindAssets("t:Speaker", new[] { "Assets/_Config/Story" })
                    .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Speaker>)
                    .FirstOrDefault(s => s != null && s.speakerId == "bauzer");
                so.FindProperty("bauzer").objectReferenceValue = guid;
            }
            const string audio = "Assets/GameAssets/kenney_interface-sounds/Audio/";
            SetIfNull(so, "buildClip", AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "confirmation_001.ogg"));
            SetIfNull(so, "collectClip", AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "drop_002.ogg"));
            SetIfNull(so, "errorClip", AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "error_002.ogg"));
            SetIfNull(so, "confirmClip", AssetDatabase.LoadAssetAtPath<AudioClip>(audio + "click_002.ogg"));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cfg);
        }

        // ---------- World pieces ----------

        private static void BuildSolarVisuals(GameObject farm, SolarFieldManager solar, GameplayVisualsConfig visConfig, VisualsConfig visuals)
        {
            var panels = farm.transform.Cast<Transform>().Where(t => t.name.StartsWith("solarpanel")).ToArray();
            var oldLamp = farm.transform.Find("StormAlertLamp");
            if (oldLamp != null) Object.DestroyImmediate(oldLamp.gameObject);

            Bounds b = panels.Select(p => p.GetComponent<Renderer>().bounds).Aggregate((a, c) => { a.Encapsulate(c); return a; });
            var lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lamp.name = "StormAlertLamp";
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.transform.SetParent(farm.transform, true);
            lamp.transform.position = new Vector3(b.center.x, b.max.y + 0.5f, b.center.z);
            lamp.transform.localScale = Vector3.one * 0.3f / Mathf.Max(0.0001f, farm.transform.lossyScale.x);
            var blink = lamp.AddComponent<BlinkingLight>();
            Wire(blink, ("config", visuals), ("lamp", lamp.GetComponent<Renderer>()));
            var bso = new SerializedObject(blink);
            bso.FindProperty("onColor").colorValue = new Color(1f, 0.15f, 0.1f);
            bso.FindProperty("offColor").colorValue = new Color(0.25f, 0.03f, 0.02f);
            bso.ApplyModifiedPropertiesWithoutUndo();

            var view = GetOrAdd<SolarDamageView>(farm);
            Wire(view, ("solar", solar), ("config", visConfig), ("alertLight", lamp));
            SetArray(view, "panels", panels);
            SetArray(view, "rockPrefabs", new[]
            {
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Environment/Rock_Chunk_Small.prefab"),
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Environment/Rock_Quat_1.prefab"),
            }.Where(p => p != null).ToArray());
        }

        private static void BuildSlot(GameObject slot, int index, GameplayHud hud, BuildingManager buildings,
            GameplayVisualsConfig visConfig, VisualsConfig visuals, Material ringMaterial)
        {
            var oldRing = slot.transform.Find("GhostRing");
            if (oldRing != null) Object.DestroyImmediate(oldRing.gameObject);

            var col = slot.GetComponent<BoxCollider>();
            Bounds b = col.bounds;
            float radius = Mathf.Max(b.extents.x, b.extents.z) * visConfig.ghostRingRadiusScale;

            var ring = new GameObject("GhostRing");
            ring.transform.SetParent(slot.transform, false);
            ring.transform.position = new Vector3(b.center.x, b.min.y + 0.25f, b.center.z);
            ring.transform.rotation = Quaternion.identity;
            var lr = ring.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.sharedMaterial = ringMaterial;
            lr.widthMultiplier = 0.1f;
            lr.startColor = lr.endColor = visConfig.ghostRingColor;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int segments = 64;
            lr.positionCount = segments;
            Vector3 s = slot.transform.lossyScale;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius / s.x, 0f, Mathf.Sin(a) * radius / s.z));
            }
            var pulse = ring.AddComponent<GhostRingPulse>();
            Wire(pulse, ("config", visuals), ("ring", lr));

            var interact = GetOrAdd<GhostSlotInteractable>(slot);
            Wire(interact, ("hud", hud), ("buildings", buildings), ("visuals", visConfig), ("ghostRing", ring));
            var so = new SerializedObject(interact);
            so.FindProperty("buildingIndex").intValue = index;
            so.FindProperty("buildingParts").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureBoxCollider(GameObject go)
        {
            var box = go.GetComponent<BoxCollider>();
            if (box == null) box = go.AddComponent<BoxCollider>();
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            box.center = go.transform.InverseTransformPoint(b.center);
            Vector3 ls = go.transform.lossyScale;
            box.size = new Vector3(b.size.x / ls.x, b.size.y / ls.y, b.size.z / ls.z) * 1.1f;
        }

        // ---------- HUD pieces ----------

        private static void BuildPopupCosts(ContextConfirmPopupView popup)
        {
            var root = (RectTransform)popup.transform;
            root.sizeDelta = new Vector2(380f, 240f);
            var titleText = root.Find("Title").GetComponent<Text>();
            titleText.resizeTextForBestFit = true; titleText.resizeTextMinSize = 12; titleText.resizeTextMaxSize = titleText.fontSize;
            var old = root.Find("CostRow");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var desc = (RectTransform)root.Find("Description");
            desc.offsetMin = new Vector2(22f, 84f);
            desc.offsetMax = new Vector2(-22f, -70f);
            desc.GetComponent<Text>().resizeTextForBestFit = true;
            desc.GetComponent<Text>().resizeTextMinSize = 12;
            desc.GetComponent<Text>().resizeTextMaxSize = desc.GetComponent<Text>().fontSize;

            var row = NewUI("CostRow", root);
            Place(row, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 74f), new Vector2(-36f, 34f));
            var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter;
            hl.spacing = 14f;
            hl.childControlWidth = hl.childControlHeight = false;
            var so = new SerializedObject(popup);
            var cells = so.FindProperty("costCells");
            cells.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                MakeChip(row, "Cost_" + i, out var chip, out var icon, out var amount);
                var el = cells.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = chip.gameObject;
                el.FindPropertyRelative("icon").objectReferenceValue = icon;
                el.FindPropertyRelative("amount").objectReferenceValue = amount;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Icon + number chip (parent of the icon is the chip, as views expect).</summary>
        private static void MakeChip(RectTransform parent, string name, out RectTransform chip, out Image icon, out Text amount)
        {
            chip = NewUI(name, parent);
            chip.sizeDelta = new Vector2(94f, 34f);
            var bg = chip.gameObject.AddComponent<Image>();
            bg.sprite = circle; bg.type = Image.Type.Sliced; bg.color = style.trackColor; bg.raycastTarget = false;
            var iconRt = NewUI("Icon", chip);
            Place(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(28f, 28f));
            icon = iconRt.gameObject.AddComponent<Image>();
            icon.preserveAspect = true; icon.raycastTarget = false;
            amount = MakeText(chip, "Amount", "0", 18, style.textColor, TextAnchor.MiddleRight, FontStyle.Bold);
            amount.resizeTextForBestFit = true; amount.resizeTextMinSize = 11; amount.resizeTextMaxSize = 18;
            Place((RectTransform)amount.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ((RectTransform)amount.transform).offsetMin = new Vector2(32f, 0f);
            ((RectTransform)amount.transform).offsetMax = new Vector2(-8f, 0f);
        }

        private static SpaceWeatherWidgetView BuildWeatherWidget(Transform hud, SpaceWeatherManager weather)
        {
            var root = NewUI("WeatherWidget", hud);
            Place(root, Vector2.one, Vector2.one, Vector2.one, new Vector2(-32f, -148f), new Vector2(250f, 100f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = circle; bg.type = Image.Type.Sliced; bg.color = style.panelColor;
            root.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -3f);
            var header = MakeText(root, "Header", "SPACE WEATHER", 12, style.mutedTextColor, TextAnchor.UpperCenter, FontStyle.Bold);
            Place((RectTransform)header.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(0f, 16f));

            RawImage MakeDay(string name, float x, string dayLabel, out Text label)
            {
                var col = NewUI(name, root);
                Place(col, new Vector2(x, 0f), new Vector2(x + 0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(0f, -24f));
                var d = MakeText(col, "Day", dayLabel, 12, style.mutedTextColor, TextAnchor.UpperCenter, FontStyle.Normal);
                Place((RectTransform)d.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 14f));
                var iconRt = NewUI("Icon", col);
                Place(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, -6f), new Vector2(30f, 30f));
                var icon = iconRt.gameObject.AddComponent<RawImage>();
                icon.raycastTarget = false;
                label = MakeText(col, "Label", "Calm", 15, style.textColor, TextAnchor.MiddleLeft, FontStyle.Bold);
                Place((RectTransform)label.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), Vector2.zero);
                ((RectTransform)label.transform).offsetMin = new Vector2(42f, -6f);
                ((RectTransform)label.transform).offsetMax = new Vector2(-2f, -14f);
                label.resizeTextForBestFit = true; label.resizeTextMinSize = 9; label.resizeTextMaxSize = 13;
                return icon;
            }

            var todayIcon = MakeDay("Today", 0f, "Today", out var todayLabel);
            var tomorrowIcon = MakeDay("Tomorrow", 0.5f, "Tomorrow", out var tomorrowLabel);
            var view = root.gameObject.AddComponent<SpaceWeatherWidgetView>();
            const string emotes = "Assets/GameAssets/kenney_emotes-pack/PNG/Vector/Style 1/";
            Wire(view, ("weather", weather), ("todayIcon", todayIcon), ("todayLabel", todayLabel), ("tomorrowIcon", tomorrowIcon),
                ("tomorrowLabel", tomorrowLabel),
                ("calmIcon", AssetDatabase.LoadAssetAtPath<Texture2D>(CirclePath)),
                ("stormIcon", AssetDatabase.LoadAssetAtPath<Texture2D>(emotes + "emote_swirl.png")),
                ("radiationIcon", AssetDatabase.LoadAssetAtPath<Texture2D>(emotes + "emote_alert.png")));
            return view;
        }

        private static RoverStatusButtonView BuildRoverButton(Transform hud, RoverExpeditionManager rover, ResourceManager resources)
        {
            var root = NewUI("RoverButton", hud);
            Place(root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-204f, 48f), new Vector2(210f, 84f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = circle; bg.type = Image.Type.Sliced; bg.color = style.panelColor;
            root.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -3f);
            var button = root.gameObject.AddComponent<Button>();
            var title = MakeText(root, "Title", "ROVER", 14, style.mutedTextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            Place((RectTransform)title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(12f, -12f), new Vector2(-24f, 18f));
            var dotRt = NewUI("Dot", root);
            Place(dotRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(18f, 20f), new Vector2(18f, 18f));
            var dot = dotRt.gameObject.AddComponent<Image>();
            dot.sprite = circle; dot.raycastTarget = false;
            var status = MakeText(root, "Status", "Ready", 24, style.textColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            Place((RectTransform)status.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(46f, 10f), new Vector2(-56f, 38f));
            var view = root.gameObject.AddComponent<RoverStatusButtonView>();
            Wire(view, ("rover", rover), ("resources", resources), ("style", style), ("button", button), ("statusLabel", status), ("statusDot", dot));
            return view;
        }

        private static Button BuildHelpButton(Transform hud)
        {
            var root = NewUI("HelpButton", hud);
            Place(root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-430f, 54f), new Vector2(72f, 72f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.sprite = circle; bg.color = style.panelColor;
            root.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -3f);
            var button = root.gameObject.AddComponent<Button>();
            var t = MakeText(root, "Label", "?", 38, style.progressColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)t.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static void BuildPillBadges(Transform hud)
        {
            foreach (var pill in hud.GetComponentsInChildren<ResourcePillView>(true))
            {
                var old = pill.transform.Find("AlertBadge");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var badge = NewUI("AlertBadge", pill.transform);
                Place(badge, Vector2.one, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(-8f, -6f), new Vector2(30f, 30f));
                var img = badge.gameObject.AddComponent<Image>();
                img.sprite = circle; img.color = new Color(0.88f, 0.16f, 0.12f); img.raycastTarget = false;
                var t = MakeText(badge, "Mark", "!", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                Place((RectTransform)t.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                Wire(pill, ("alertBadge", img));
                badge.gameObject.SetActive(false);
            }
        }

        private static ModalCardView BuildMorningCard(Transform hud, GameplayUIConfig cfg, DialogueRunner runner)
        {
            var root = BuildStretch("MorningCard", hud);
            var blocker = root.gameObject.AddComponent<Image>();
            blocker.color = new Color(0.05f, 0.07f, 0.12f, 0.45f);
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var card = NewUI("Card", root);
            Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(660f, 560f));
            var bg = card.gameObject.AddComponent<Image>();
            bg.sprite = circle; bg.type = Image.Type.Sliced; bg.color = style.panelColor;
            card.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -6f);

            var banner = NewUI("Banner", card);
            Place(banner, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -38f), new Vector2(480f, 38f));
            var bannerBg = banner.gameObject.AddComponent<Image>();
            bannerBg.sprite = circle; bannerBg.type = Image.Type.Sliced; bannerBg.color = style.progressColor;
            var bannerText = MakeText(banner, "Text", "DAILY REPORT", 17, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            bannerText.resizeTextForBestFit = true; bannerText.resizeTextMinSize = 10; bannerText.resizeTextMaxSize = 17;
            Place((RectTransform)bannerText.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var title = MakeText(card, "Title", "Good morning!", 32, style.textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)title.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(-48f, 44f));

            var body = MakeText(card, "Body", "", 19, style.mutedTextColor, TextAnchor.UpperCenter, FontStyle.Normal);
            Place((RectTransform)body.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(-64f, 124f));
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 13; body.resizeTextMaxSize = 19;

            var deltaRow = NewUI("Deltas", card);
            Place(deltaRow, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -248f), new Vector2(-48f, 40f));
            var hl = deltaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 12f; hl.childControlWidth = hl.childControlHeight = false;
            var deltaCells = new List<(GameObject, Image, Text)>();
            for (int i = 0; i < 5; i++)
            {
                MakeChip(deltaRow, "Delta_" + i, out var chip, out var icon, out var amount);
                chip.sizeDelta = new Vector2(104f, 40f);
                deltaCells.Add((chip.gameObject, icon, amount));
            }

            var bauzerRow = NewUI("Bauzer", card);
            Place(bauzerRow, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -306f), new Vector2(-48f, 128f));
            var rowBg = bauzerRow.gameObject.AddComponent<Image>();
            rowBg.sprite = circle; rowBg.type = Image.Type.Sliced; rowBg.color = new Color(0.93f, 0.96f, 1f);
            var portraitRt = NewUI("Portrait", bauzerRow);
            Place(portraitRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(100f, 100f));
            var portrait = portraitRt.gameObject.AddComponent<Image>();
            portrait.preserveAspect = true; portrait.raycastTarget = false;
            var name = MakeText(bauzerRow, "Name", "Bauzer", 16, new Color(0.24f, 0.55f, 0.86f), TextAnchor.UpperLeft, FontStyle.Bold);
            Place((RectTransform)name.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(60f, -10f), new Vector2(-144f, 20f));
            var line = MakeText(bauzerRow, "Line", "", 18, style.textColor, TextAnchor.UpperLeft, FontStyle.Normal);
            Place((RectTransform)line.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ((RectTransform)line.transform).offsetMin = new Vector2(124f, 10f);
            ((RectTransform)line.transform).offsetMax = new Vector2(-16f, -34f);
            line.resizeTextForBestFit = true; line.resizeTextMinSize = 12; line.resizeTextMaxSize = 18;

            var buttons = NewUI("Buttons", card);
            Place(buttons, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(-48f, 64f));
            var bl = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
            bl.childAlignment = TextAnchor.MiddleCenter; bl.spacing = 20f; bl.childControlWidth = bl.childControlHeight = false;
            var a = MakeButton(buttons, "ButtonA", "Start Day", style.progressColor, Color.white, new Vector2(280f, 64f), out var labelA);
            var b = MakeButton(buttons, "ButtonB", "", style.doneColor, Color.white, new Vector2(280f, 64f), out var labelB);

            var view = root.gameObject.AddComponent<ModalCardView>();
            Wire(view, ("style", style), ("config", cfg), ("runner", runner), ("root", root.gameObject), ("card", card), ("canvasGroup", group),
                ("bannerBackground", bannerBg), ("bannerText", bannerText), ("titleText", title), ("bodyText", body),
                ("deltaRow", deltaRow.gameObject), ("bauzerPortrait", portrait), ("bauzerName", name), ("bauzerText", line),
                ("buttonA", a), ("labelA", labelA), ("buttonB", b), ("labelB", labelB));
            var so = new SerializedObject(view);
            var cells = so.FindProperty("deltaCells");
            cells.arraySize = deltaCells.Count;
            for (int i = 0; i < deltaCells.Count; i++)
            {
                var el = cells.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = deltaCells[i].Item1;
                el.FindPropertyRelative("icon").objectReferenceValue = deltaCells[i].Item2;
                el.FindPropertyRelative("amount").objectReferenceValue = deltaCells[i].Item3;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return view;
        }

        private static RectTransform BuildSkipOverlay(Transform hud, DayNightTransitionPlayer player)
        {
            var holder = BuildStretch("NightSkip", hud);
            var root = BuildStretch("Root", holder);
            var tap = root.gameObject.AddComponent<Image>();
            tap.color = new Color(0f, 0f, 0f, 0f);
            var tapButton = root.gameObject.AddComponent<Button>();
            tapButton.transition = Selectable.Transition.None;
            var row = NewUI("SkipRow", root);
            Place(row, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-48f, 200f), new Vector2(160f, 56f));
            var skip = MakeButton(row, "SkipButton", "Skip  >>", new Color(1f, 1f, 1f, 0.85f), style.textColor, new Vector2(160f, 56f), out _);
            ((RectTransform)skip.transform).anchoredPosition = Vector2.zero;
            var hint = MakeText(root, "Hint", "Tap anywhere to skip", 16, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerCenter, FontStyle.Normal);
            Place((RectTransform)hint.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(0f, 24f));
            var view = holder.gameObject.AddComponent<NightTransitionSkipView>();
            Wire(view, ("transition", player), ("root", root.gameObject), ("tapAnywhere", tapButton), ("skipButton", skip));
            root.gameObject.SetActive(false);
            return holder;
        }

        private static GameObject BuildRoverMap(Transform hud, RoverExpeditionManager rover, ResourceHotspot[] hotspots,
            out RoverSiteMarkerView[] markers, out Button back, out Text title, out RectTransform markerLayer)
        {
            var root = BuildStretch("RoverMap", hud);
            markerLayer = BuildStretch("Markers", root);

            var titleRt = NewUI("Title", root);
            Place(titleRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(520f, 52f));
            var tbg = titleRt.gameObject.AddComponent<Image>();
            tbg.sprite = circle; tbg.type = Image.Type.Sliced; tbg.color = style.panelColor; tbg.raycastTarget = false;
            title = MakeText(titleRt, "Text", "Send the rover to a site", 24, style.textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)title.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var backRt = NewUI("BackRow", root);
            Place(backRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(240f, 64f));
            back = MakeButton(backRt, "BackButton", "Back", style.trackColor, style.textColor, new Vector2(240f, 64f), out _);

            var config = rover.Config;
            var list = new List<RoverSiteMarkerView>();
            foreach (var site in config.sites)
            {
                var spot = hotspots.FirstOrDefault(h => h.hotspotId == site.siteId);
                if (spot == null) continue;
                var m = NewUI("Marker_" + site.siteId, markerLayer);
                Place(m, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(310f, 112f));
                var mbg = m.gameObject.AddComponent<Image>();
                mbg.sprite = circle; mbg.type = Image.Type.Sliced; mbg.color = Color.white;
                m.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -4f);
                var button = m.gameObject.AddComponent<Button>();
                var nameText = MakeText(m, "Name", site.displayName, 19, style.textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
                Place((RectTransform)nameText.transform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-16f, 26f));
                var chips = NewUI("Chips", m);
                Place(chips, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), new Vector2(-12f, 34f));
                var hl = chips.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 4f; hl.childControlWidth = hl.childControlHeight = false;
                MakeChip(chips, "Cost", out _, out var costIcon, out var costLabel);
                MakeChip(chips, "RewardA", out _, out var rewardIconA, out var rewardLabelA);
                MakeChip(chips, "RewardB", out _, out var rewardIconB, out var rewardLabelB);
                var risk = MakeText(m, "Risk", "5% risk", 15, style.mutedTextColor, TextAnchor.MiddleCenter, FontStyle.Normal);
                Place((RectTransform)risk.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-16f, 22f));
                var stem = NewUI("Stem", m);
                Place(stem, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 2f), new Vector2(14f, 14f));
                var stemImg = stem.gameObject.AddComponent<Image>();
                stemImg.sprite = circle; stemImg.color = Color.white; stemImg.raycastTarget = false;

                var view = m.gameObject.AddComponent<RoverSiteMarkerView>();
                var so = new SerializedObject(view);
                so.FindProperty("siteId").stringValue = site.siteId;
                so.ApplyModifiedPropertiesWithoutUndo();
                Wire(view, ("worldTarget", spot.transform), ("style", style), ("button", button), ("nameLabel", nameText),
                    ("costIcon", costIcon), ("costLabel", costLabel), ("rewardIconA", rewardIconA), ("rewardLabelA", rewardLabelA),
                    ("rewardIconB", rewardIconB), ("rewardLabelB", rewardLabelB), ("riskLabel", risk));
                list.Add(view);
            }
            markers = list.ToArray();
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        // ---------- Helpers ----------

        private static RectTransform BuildStretch(string name, Transform parent)
        {
            var rt = NewUI(name, parent);
            Place(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rt;
        }

        private static Button MakeButton(RectTransform parent, string name, string label, Color color, Color textColor, Vector2 size, out Text text)
        {
            var rt = NewUI(name, parent);
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = circle; img.type = Image.Type.Sliced; img.color = color;
            rt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0f, -3f);
            var button = rt.gameObject.AddComponent<Button>();
            text = MakeText(rt, "Label", label, 21, textColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place((RectTransform)text.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ((RectTransform)text.transform).offsetMin = new Vector2(10f, 4f);
            ((RectTransform)text.transform).offsetMax = new Vector2(-10f, -4f);
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = 21;
            return button;
        }

        private static Text MakeText(Transform parent, string name, string value, int size, Color color, TextAnchor align, FontStyle fontStyle)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.text = value; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = fontStyle;
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogError("[Run 3B] Missing asset " + path);
            return a;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static void Wire(Object target, params (string prop, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (prop, value) in refs)
            {
                var p = so.FindProperty(prop);
                if (p == null) { Debug.LogError($"[Run 3B] {target.GetType().Name} has no field '{prop}'"); continue; }
                p.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(Object target, string prop, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string prop, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(prop).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetIfNull(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p.objectReferenceValue == null) p.objectReferenceValue = value;
        }
    }
}
