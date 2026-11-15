using System.Collections.Generic;
using System.Linq;
using ArtemisX.Core;
using ArtemisX.Simulation;
using ArtemisX.Story;
using ArtemisX.UI.StartScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.EditorTools
{
    /// <summary>
    /// Run 4: wires the Start Screen prefab, intro cutscene + Day 1 onboarding, end screen and leaderboard into the
    /// open gameplay scene. Re-runnable: it deletes and rebuilds only its own objects.
    /// </summary>
    public static class Run4ScreensBuilder
    {
        private const string FlowName = "Run4_Flow";
        private const string TutorialName = "Run4_TutorialCanvas";
        private const string EndName = "Run4_EndScreenCanvas";
        private const string StartPrefabPath = "Assets/_Prefabs/UI/StartScreenCanvas.prefab";
        private const string ScreensConfigPath = "Assets/_Config/UI/ScreensConfig.asset";
        private const string LeaderboardConfigPath = "Assets/_Config/Leaderboard/LeaderboardConfig.asset";
        private const string GameScenePath = "Assets/Scenes/HandMadeBase.unity";

        [MenuItem("Artemis X/Run 4/Build Screens and Wiring")]
        public static void Build()
        {
            var theme = Load<UIThemeConfig>("Assets/_Config/UI/UIThemeConfig.asset");
            var uiConfig = Load<GameplayUIConfig>("Assets/_Config/Visuals/GameplayUIConfig.asset");
            var profile = Load<PlayerRunProfile>("Assets/_Config/UI/PlayerRunProfile.asset");
            var screens = LoadOrCreate<ScreensConfig>(ScreensConfigPath);
            var lbConfig = LoadOrCreate<LeaderboardConfig>(LeaderboardConfigPath);

            var managers = Find("---------MANAGERS-------------");
            var hudRoot = Find("------------HUD-----------------");
            if (managers == null || hudRoot == null) { Debug.LogError("Run4ScreensBuilder: open HandMadeBase.unity first."); return; }

            foreach (var n in new[] { FlowName, TutorialName, EndName, "StartScreenCanvas" })
            {
                var old = Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }

            var runner = managers.GetComponent<DialogueRunner>();
            var story = managers.GetComponent<StoryDirector>();
            var hud = hudRoot.GetComponent<GameplayHud>();
            var cameraRig = Find("CM_BaseCamera");
            var lander = Find("lander_A") ?? Find("landingPad + lander");

            // --- Start screen (Run 3C prefab) ---
            var startPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StartPrefabPath);
            var startGo = (GameObject)PrefabUtility.InstantiatePrefab(startPrefab);
            startGo.name = "StartScreenCanvas";
            var startView = startGo.GetComponentInChildren<StartScreenView>(true);
            Set(startView, "dialogueRunner", runner);
            var overlay = new SerializedObject(startView).FindProperty("overlayRoot").objectReferenceValue as GameObject;
            AddAiCredit(overlay != null ? overlay.transform : startGo.transform, theme);

            // --- Tutorial / skip overlay ---
            var tutorial = NewCanvas(TutorialName, 40);
            var tutRect = (RectTransform)tutorial.transform;
            var frame = NewImage("HighlightFrame", tutRect, theme.panelBorderSprite, theme.amberHighlight);
            frame.raycastTarget = false;
            frame.rectTransform.anchorMin = frame.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            var hint = NewImage("HintBanner", tutRect, theme.panelBaseSprite, theme.panelBackdropColor);
            hint.raycastTarget = false;
            Anchor(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(900f, 76f));
            var hintText = NewText("Text", hint.rectTransform, theme.headerFont, 28, theme.amberHighlight, TextAnchor.MiddleCenter);
            Stretch(hintText.rectTransform, 16f);
            var highlight = tutorial.AddComponent<TutorialHighlightView>();
            Set(highlight, "canvasRect", tutRect);
            Set(highlight, "frame", frame.rectTransform);
            Set(highlight, "hintRoot", hint.gameObject);
            Set(highlight, "hintLabel", hintText);

            var skip = NewButton("SkipButton", tutRect, theme.buttonBlueNormal, theme, out Text skipText);
            Anchor((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(-150f, -60f), new Vector2(220f, 72f));
            skipText.text = screens.skipLabel;

            // --- End screen ---
            var endCanvas = NewCanvas(EndName, 70);
            var endRect = (RectTransform)endCanvas.transform;
            var endRoot = NewImage("Root", endRect, null, new Color(0.02f, 0.03f, 0.08f, 0.72f));
            Stretch(endRoot.rectTransform, 0f);
            var card = NewImage("Card", endRoot.rectTransform, theme.panelBaseSprite, theme.cardSurfaceColor);
            Anchor(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1280f, 940f));
            var border = NewImage("Border", card.rectTransform, theme.panelBorderSprite, theme.cyanAccentBorder);
            border.raycastTarget = false;
            Stretch(border.rectTransform, 0f);
            var title = NewText("Title", card.rectTransform, theme.headerFont, 56, theme.textPrimary, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1100f, 80f));
            var subtitle = NewText("Subtitle", card.rectTransform, theme.bodyFont, 30, theme.textSecondary, TextAnchor.MiddleCenter);
            Anchor(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(1150f, 50f));
            var starOn = Load<Texture2D>("Assets/GameAssets/kenney_ui-pack/PNG/Yellow/Default/star.png");
            var starOff = Load<Texture2D>("Assets/GameAssets/kenney_ui-pack/PNG/Grey/Default/star.png");
            var starImages = new RawImage[3];
            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("Star" + (i + 1), typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(card.rectTransform, false);
                starImages[i] = go.GetComponent<RawImage>();
                starImages[i].texture = starOn;
                starImages[i].raycastTarget = false;
                Anchor(starImages[i].rectTransform, new Vector2(0.5f, 1f), new Vector2((i - 1) * 130f, -235f), new Vector2(110f, 110f));
            }
            var portrait = NewImage("BauzerPortrait", card.rectTransform, null, Color.white);
            portrait.preserveAspect = true;
            Anchor(portrait.rectTransform, new Vector2(0.5f, 1f), new Vector2(-470f, -385f), new Vector2(130f, 130f));
            var bauzerName = NewText("BauzerName", card.rectTransform, theme.headerFont, 22, theme.cyanAccentBorder, TextAnchor.UpperLeft);
            Anchor(bauzerName.rectTransform, new Vector2(0.5f, 1f), new Vector2(90f, -335f), new Vector2(980f, 30f));
            bauzerName.text = "BAUZER";
            var bauzerLine = NewText("BauzerLine", card.rectTransform, theme.bodyFont, 28, theme.textPrimary, TextAnchor.UpperLeft);
            Anchor(bauzerLine.rectTransform, new Vector2(0.5f, 1f), new Vector2(90f, -400f), new Vector2(980f, 90f));
            var histTitle = NewText("HistogramTitle", card.rectTransform, theme.headerFont, 22, theme.textSecondary, TextAnchor.MiddleCenter);
            Anchor(histTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -485f), new Vector2(1150f, 34f));
            var histBg = NewImage("Histogram", card.rectTransform, theme.panelBaseSprite, theme.panelBackdropColor);
            histBg.raycastTarget = false;
            Anchor(histBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -625f), new Vector2(1150f, 230f));
            var bars = new GameObject("Bars", typeof(RectTransform), typeof(HorizontalLayoutGroup)).GetComponent<RectTransform>();
            bars.SetParent(histBg.rectTransform, false);
            Stretch(bars, 14f);
            var layout = bars.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            layout.spacing = 3f;
            var status = NewText("Status", card.rectTransform, theme.bodyFont, 24, theme.amberHighlight, TextAnchor.MiddleCenter);
            Anchor(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -765f), new Vector2(1150f, 36f));
            var playAgain = NewButton("PlayAgainButton", card.rectTransform, theme.buttonGreenNormal, theme, out Text playAgainText);
            Anchor((RectTransform)playAgain.transform, new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(360f, 86f));

            var endView = endCanvas.AddComponent<EndScreenView>();

            // --- Flow object ---
            var flow = new GameObject(FlowName);
            var leaderboard = flow.AddComponent<LeaderboardClient>();
            var onboarding = flow.AddComponent<Day1Onboarding>();
            var gameFlow = flow.AddComponent<GameFlowController>();

            Set(leaderboard, "config", lbConfig);

            Set(endView, "config", screens);
            Set(endView, "uiConfig", uiConfig);
            Set(endView, "story", story);
            Set(endView, "runner", runner);
            Set(endView, "leaderboard", leaderboard);
            Set(endView, "root", endRoot.gameObject);
            Set(endView, "card", card.rectTransform);
            Set(endView, "titleLabel", title);
            Set(endView, "subtitleLabel", subtitle);
            SetArray(endView, "stars", starImages);
            Set(endView, "starOn", starOn);
            Set(endView, "starOff", starOff);
            Set(endView, "bauzerPortrait", portrait);
            Set(endView, "bauzerLine", bauzerLine);
            Set(endView, "histogramTitle", histTitle);
            Set(endView, "barsContainer", bars);
            Set(endView, "statusLabel", status);
            Set(endView, "playAgainButton", playAgain);
            Set(endView, "playAgainLabel", playAgainText);
            Set(endView, "labelFont", theme.bodyFont);
            endRoot.gameObject.SetActive(false);

            Set(onboarding, "config", screens);
            Set(onboarding, "uiConfig", uiConfig);
            Set(onboarding, "hud", hud);
            Set(onboarding, "runner", runner);
            Set(onboarding, "nextDay", managers.GetComponent<NextDayFlow>());
            Set(onboarding, "days", managers.GetComponent<DayManager>());
            Set(onboarding, "resolution", managers.GetComponent<DayResolutionController>());
            Set(onboarding, "rover", managers.GetComponent<RoverExpeditionManager>());
            Set(onboarding, "morning", hudRoot.GetComponent<MorningSequenceController>());
            Set(onboarding, "transition", managers.GetComponent<Visuals.DayNightTransitionPlayer>());
            Set(onboarding, "highlight", highlight);
            Set(onboarding, "weatherWidget", Find("WeatherWidget")?.GetComponent<RectTransform>());
            Set(onboarding, "nextDayButton", Find("NextDayButton")?.GetComponent<RectTransform>());
            Set(onboarding, "solarField", Find("solarFarm")?.transform);
            Set(onboarding, "roverButton", Find("RoverButton")?.GetComponent<RectTransform>());
            Set(onboarding, "playerSpeaker", profile != null ? profile.playerSpeaker : null);

            Set(gameFlow, "config", screens);
            Set(gameFlow, "uiConfig", uiConfig);
            Set(gameFlow, "startScreen", startView);
            Set(gameFlow, "runner", runner);
            Set(gameFlow, "story", story);
            Set(gameFlow, "hud", hud);
            Set(gameFlow, "onboarding", onboarding);
            Set(gameFlow, "cameraRig", cameraRig != null ? cameraRig.transform : null);
            Set(gameFlow, "panController", cameraRig != null ? cameraRig.GetComponent<CameraPanController>() : null);
            Set(gameFlow, "introFocus", lander != null ? lander.transform : null);
            Set(gameFlow, "skipRoot", skip.gameObject);
            Set(gameFlow, "skipButton", skip);
            Set(gameFlow, "skipLabel", skipText);

            // --- Existing scene objects ---
            Set(story, "playDay1IntroOnStart", false);
            var topBar = new List<CanvasGroup>();
            foreach (var n in new[] { "DayPill", "ResourcePills", "MissionCard", "SupplyNotice", "WeatherWidget" })
            {
                var go = Find(n);
                if (go == null) continue;
                var group = go.GetComponent<CanvasGroup>();
                if (group == null) group = go.AddComponent<CanvasGroup>();
                topBar.Add(group);
            }
            SetArray(hud, "topBar", topBar.ToArray());

            var widget = Find("WeatherWidget")?.GetComponent<SpaceWeatherWidgetView>();
            if (widget != null)
            {
                Set(widget, "calmIcon", Load<Texture2D>("Assets/_UI/Icons/icon_weather_calm.png"));
                Set(widget, "stormIcon", Load<Texture2D>("Assets/_UI/Icons/icon_weather_storm.png"));
                Set(widget, "radiationIcon", Load<Texture2D>("Assets/_UI/Icons/icon_weather_radiation.png"));
                Set(widget, "calmColor", Color.white);
                Set(widget, "stormColor", Color.white);
                Set(widget, "radiationColor", Color.white);
            }

            // --- Build settings: gameplay scene first ---
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != GameScenePath).ToList();
            foreach (var s in scenes) s.enabled = false;
            scenes.Insert(0, new EditorBuildSettingsScene(GameScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();

            EditorSceneManager.MarkSceneDirty(managers.scene);
            EditorSceneManager.SaveScene(managers.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Run4] Screens built and wired.");
        }

        private static void AddAiCredit(Transform startRoot, UIThemeConfig theme)
        {
            var text = NewText("AiArtCredit", (RectTransform)startRoot, theme.bodyFont, 18, theme.textSecondary, TextAnchor.LowerRight);
            Anchor(text.rectTransform, new Vector2(1f, 0f), new Vector2(-470f, 26f), new Vector2(900f, 30f));
            text.text = "Portraits, logo and weather icons: AI-generated art";
            text.raycastTarget = false;
        }

        private static GameObject NewCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return go;
        }

        private static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            return image;
        }

        private static Text NewText(string name, RectTransform parent, Font font, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button NewButton(string name, RectTransform parent, Sprite sprite, UIThemeConfig theme, out Text label)
        {
            var image = NewImage(name, parent, sprite, Color.white);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = NewText("Label", image.rectTransform, theme.headerFont, theme.buttonFontSize + 6, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 8f);
            return button;
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static GameObject Find(string name)
        {
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            }
            return null;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            string folder = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void Set(Object target, string field, object value)
        {
            if (target == null) { Debug.LogError("Run4ScreensBuilder: missing target for " + field); return; }
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"Run4ScreensBuilder: {target.GetType().Name}.{field} not found"); return; }
            switch (value)
            {
                case Object o: prop.objectReferenceValue = o; break;
                case bool b: prop.boolValue = b; break;
                case Color c: prop.colorValue = c; break;
                case null: prop.objectReferenceValue = null; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
