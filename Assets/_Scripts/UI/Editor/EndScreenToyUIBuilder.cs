using ArtemisX.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.EditorTools
{
    /// <summary>
    /// Re-skins Run4_EndScreenCanvas to the "G1 · Ribbon + report" Toy UI layout and builds the
    /// "L2 · How everyone did" leaderboard panel. Idempotent: finds existing objects by name and only disables old ones.
    /// </summary>
    public static class EndScreenToyUIBuilder
    {
        private const string ToyDir = "Assets/GameAssets/Free Icon Pack v3.1 (Basic)/Claude outputs/ToyUI/";
        private const string IconDir = "Assets/GameAssets/Free Icon Pack v3.1 (Basic)/";
        private const string StarBig = IconDir + "Main/Star/256px/Golden Star 1st Outline 256px.png";
        private const string CalendarIcon = IconDir + "Item/Calendar/64px/Calendar 1st Outline 64px.png";
        private const string HammerIcon = IconDir + "Item/Hammer/64px/Hammer 1st Outline 64px.png";
        private const string CloseIcon = IconDir + "UI/Close Button/64px/Close Button 1st Outline 64px.png";

        private static readonly Color Ink = Hex(0x04, 0x04, 0x35);
        private static readonly Color Muted = Hex(0x56, 0x65, 0x7F);
        private static readonly Color TrayTint = Hex(0xEE, 0xF1, 0xF6);

        private static Font luckiest;
        private static Font atkinson;

        [MenuItem("Artemis X/Build End Screen Toy UI")]
        public static void Build()
        {
            var canvasGo = FindInScene("Run4_EndScreenCanvas");
            if (canvasGo == null) { Debug.LogError("Run4_EndScreenCanvas not found."); return; }
            var view = canvasGo.GetComponent<EndScreenView>();
            if (view == null) { Debug.LogError("EndScreenView not found on Run4_EndScreenCanvas."); return; }

            foreach (var p in new[] { StarBig, CalendarIcon, HammerIcon, CloseIcon, ToyDir + "kepler.png", ToyDir + "rays.png",
                                      ToyDir + "ribbon-tail-left.png", ToyDir + "ribbon-tail-right.png" })
                EnsureSprite(p);

            luckiest = AssetDatabase.LoadAssetAtPath<Font>(ToyDir + "LuckiestGuy-Regular.ttf");
            atkinson = AssetDatabase.LoadAssetAtPath<Font>(ToyDir + "AtkinsonHyperlegible-Bold.ttf");

            var root = canvasGo.transform.Find("Root");
            var card = (RectTransform)root.Find("Card");
            BuildEndScreen(view, root, card);
            var panelView = BuildLeaderboard(canvasGo, card);

            var so = new SerializedObject(view);
            so.FindProperty("leaderboardPanel").objectReferenceValue = panelView;
            so.ApplyModifiedPropertiesWithoutUndo();

            var config = (ScreensConfig)new SerializedObject(view).FindProperty("config").objectReferenceValue;
            if (config != null)
            {
                var cso = new SerializedObject(config);
                cso.FindProperty("earlyWinTitle").stringValue = "Mission complete!";
                cso.FindProperty("dayCapTitle").stringValue = "Mission extended";
                cso.FindProperty("playAgainLabel").stringValue = "Play again";
                cso.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }

            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            EditorSceneManager.SaveScene(canvasGo.scene);
            Debug.Log("End screen Toy UI built.");
        }

        // ---------- End screen ----------

        private static void BuildEndScreen(EndScreenView view, Transform root, RectTransform card)
        {
            var scrim = root.GetComponent<Image>();
            if (scrim != null) scrim.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);

            Sliced(card.GetComponent<Image>(), "panel-plain.png");
            Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(540f, 430f));
            SetActive(card, "Border", false);
            SetActive(card, "Subtitle", false);
            SetActive(card, "HistogramTitle", false);
            SetActive(card, "Histogram", false);
            SetActive(card, "Status", false);
            SetActive(card, "BauzerName", false);
            SetActive(card, "BauzerNamePlate", false);

            // Ribbon on the card's top edge.
            var ribbon = Child(card, "Ribbon");
            Place(ribbon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 100f));
            if (ribbon.GetComponent<CanvasGroup>() == null) ribbon.gameObject.AddComponent<CanvasGroup>();
            ribbon.GetComponent<CanvasGroup>().blocksRaycasts = false;

            var rays = Child(ribbon, "Rays");
            Simple(rays, "rays.png", new Vector2(0f, 64f), new Vector2(230f, 230f));
            var tailL = Child(ribbon, "TailLeft");
            Simple(tailL, "ribbon-tail-left.png", new Vector2(-210f - 34f + 38f, 50f - 24f - 33f), new Vector2(76f, 66f));
            var tailR = Child(ribbon, "TailRight");
            Simple(tailR, "ribbon-tail-right.png", new Vector2(210f + 34f - 38f, 50f - 24f - 33f), new Vector2(76f, 66f));
            var plate = Child(ribbon, "Plate");
            Stretch(plate);
            Sliced(Img(plate), "ribbon-plate.png");

            var title = (RectTransform)card.Find("Title");
            if (title == null) title = (RectTransform)ribbon.Find("Title");
            title.SetParent(ribbon, false);
            Place(title, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -3f), new Vector2(-40f, 0f));
            StyleWhite(title.GetComponent<Text>(), 36, TextAnchor.MiddleCenter);

            var starTex = AssetDatabase.LoadAssetAtPath<Texture2D>(StarBig);
            var starNames = new[] { "Star1", "Star2", "Star3" };
            for (int i = 0; i < 3; i++)
            {
                var star = (RectTransform)(card.Find(starNames[i]) ?? ribbon.Find(starNames[i]));
                star.SetParent(ribbon, false);
                bool mid = i == 1;
                Place(star, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                      new Vector2((i - 1) * 70f, mid ? 64f : 50f), Vector2.one * (mid ? 76f : 56f));
                var raw = star.GetComponent<RawImage>();
                raw.texture = starTex;
                raw.raycastTarget = false;
            }
            rays.SetSiblingIndex(0);
            tailL.SetSiblingIndex(1);
            tailR.SetSiblingIndex(2);
            plate.SetSiblingIndex(3);
            title.SetSiblingIndex(4);
            for (int i = 0; i < 3; i++) ribbon.Find(starNames[i]).SetSiblingIndex(5 + i);

            // Kepler row.
            var kepler = AssetDatabase.LoadAssetAtPath<Sprite>(ToyDir + "kepler.png");
            var portrait = (RectTransform)card.Find("BauzerPortrait");
            Place(portrait, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-204f, -114f), new Vector2(82f, 82f));
            var portraitImg = portrait.GetComponent<Image>();
            portraitImg.sprite = kepler;
            portraitImg.type = Image.Type.Simple;
            portraitImg.preserveAspect = true;
            portraitImg.color = Color.white;

            var bubble = Child(card, "KeplerBubble");
            Place(bubble, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f, -114f), new Vector2(400f, 96f));
            Sliced(Img(bubble), "bubble.png");
            var line = (RectTransform)card.Find("BauzerLine") ?? (RectTransform)bubble.Find("BauzerLine");
            line.SetParent(bubble, false);
            Place(line, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            line.offsetMin = new Vector2(26f, 18f);
            line.offsetMax = new Vector2(-20f, -12f);
            var lineText = line.GetComponent<Text>();
            StyleInk(lineText, 17, TextAnchor.MiddleLeft);
            lineText.resizeTextForBestFit = true;
            lineText.resizeTextMinSize = 12;
            lineText.resizeTextMaxSize = 17;
            portrait.SetSiblingIndex(bubble.GetSiblingIndex());

            // Stat tiles.
            var stats = Child(card, "Stats");
            Place(stats, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -224f), new Vector2(500f, 84f));
            var statsLayout = Ensure<HorizontalLayoutGroup>(stats);
            statsLayout.spacing = 16f;
            statsLayout.childAlignment = TextAnchor.MiddleCenter;
            statsLayout.childControlWidth = statsLayout.childControlHeight = false;
            statsLayout.childForceExpandWidth = statsLayout.childForceExpandHeight = false;
            StatTile(stats, "DaysTile", CalendarIcon, out var daysValue, out var daysCaption);
            var builtTile = StatTile(stats, "BuiltTile", HammerIcon, out var builtValue, out var builtCaption);

            // Buttons.
            var buttons = Child(card, "Buttons");
            Place(buttons, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -340f), new Vector2(520f, 71f));
            var buttonsLayout = Ensure<HorizontalLayoutGroup>(buttons);
            buttonsLayout.spacing = 14f;
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlWidth = buttonsLayout.childControlHeight = false;
            buttonsLayout.childForceExpandWidth = buttonsLayout.childForceExpandHeight = false;

            var playAgain = (RectTransform)(card.Find("PlayAgainButton") ?? buttons.Find("PlayAgainButton"));
            playAgain.SetParent(buttons, false);
            ToyButton(playAgain, "button-primary", CalendarIcon, new Vector2(230f, 71f));
            var leaderboard = Child(buttons, "LeaderboardButton");
            ToyButton(leaderboard, "button-secondary", StarBig, new Vector2(250f, 71f));
            var leaderboardLabel = leaderboard.Find("Label").GetComponent<Text>();
            leaderboardLabel.text = "Leaderboard";
            playAgain.SetSiblingIndex(0);
            leaderboard.SetSiblingIndex(1);

            var so = new SerializedObject(view);
            so.FindProperty("starOn").objectReferenceValue = starTex;
            so.FindProperty("starOff").objectReferenceValue = starTex;
            so.FindProperty("ribbon").objectReferenceValue = ribbon;
            so.FindProperty("rays").objectReferenceValue = rays;
            so.FindProperty("portraitOverride").objectReferenceValue = kepler;
            so.FindProperty("daysValue").objectReferenceValue = daysValue;
            so.FindProperty("daysCaption").objectReferenceValue = daysCaption;
            so.FindProperty("builtTile").objectReferenceValue = builtTile.gameObject;
            so.FindProperty("builtValue").objectReferenceValue = builtValue;
            so.FindProperty("builtCaption").objectReferenceValue = builtCaption;
            so.FindProperty("leaderboardButton").objectReferenceValue = leaderboard.GetComponent<Button>();
            so.FindProperty("leaderboardLabel").objectReferenceValue = leaderboardLabel;
            so.FindProperty("playAgainLabel").objectReferenceValue = playAgain.Find("Label").GetComponent<Text>();
            so.FindProperty("buildings").objectReferenceValue = Object.FindFirstObjectByType<BuildingManager>(FindObjectsInactive.Include);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform StatTile(Transform parent, string name, string iconPath, out Text value, out Text caption)
        {
            var tile = Child(parent, name);
            tile.sizeDelta = new Vector2(150f, 84f);
            Sliced(Img(tile), "recipe-tray.png");

            var icon = Child(tile, "Icon");
            Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(38f, 0f), new Vector2(40f, 40f));
            var iconImg = Img(icon);
            iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var v = Child(tile, "Value");
            Place(v, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, 8f), new Vector2(-76f, 34f));
            value = Txt(v);
            StyleWhite(value, 26, TextAnchor.MiddleLeft);
            value.text = "0";

            var c = Child(tile, "Caption");
            Place(c, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(66f, -18f), new Vector2(-76f, 18f));
            caption = Txt(c);
            caption.font = atkinson;
            caption.fontSize = 13;
            caption.color = Muted;
            caption.alignment = TextAnchor.MiddleLeft;
            caption.raycastTarget = false;
            caption.text = name == "DaysTile" ? "days" : "built";
            return tile;
        }

        // ---------- Leaderboard panel ----------

        private static LeaderboardPanelView BuildLeaderboard(GameObject canvasGo, RectTransform card)
        {
            var view = Ensure<LeaderboardPanelView>(canvasGo.transform);
            var endView = canvasGo.GetComponent<EndScreenView>();

            var panel = Child(canvasGo.transform, "LeaderboardPanel");
            Stretch(panel);
            panel.SetAsLastSibling();

            var frame = Child(panel, "Frame");
            Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(600f, 520f));
            Sliced(Img(frame), "panel.png");

            var title = Child(frame, "Title");
            Place(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(-200f, 56f));
            var titleText = Txt(title);
            StyleWhite(titleText, 28, TextAnchor.MiddleCenter);
            titleText.text = "All missions";

            var star = Child(frame, "HeaderStar");
            Place(star, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(22f, -14f), new Vector2(88f, 88f));
            var starImg = Img(star);
            starImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(StarBig);
            starImg.preserveAspect = true;
            starImg.raycastTarget = false;

            var close = Child(frame, "CloseButton");
            Place(close, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(44f, 44f));
            var closeImg = Img(close);
            closeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloseIcon);
            closeImg.preserveAspect = true;
            var closeBtn = Ensure<Button>(close);
            closeBtn.targetGraphic = closeImg;

            // Kepler row.
            var kepler = Child(frame, "Kepler");
            Place(kepler, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-236f, -112f), new Vector2(64f, 64f));
            var keplerImg = Img(kepler);
            keplerImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToyDir + "kepler.png");
            keplerImg.preserveAspect = true;
            keplerImg.raycastTarget = false;

            var bubble = Child(frame, "Bubble");
            Place(bubble, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(38f, -112f), new Vector2(468f, 80f));
            Sliced(Img(bubble), "bubble.png");
            var line = Child(bubble, "Line");
            Stretch(line);
            line.offsetMin = new Vector2(26f, 18f);
            line.offsetMax = new Vector2(-20f, -10f);
            var lineText = Txt(line);
            StyleInk(lineText, 16, TextAnchor.MiddleLeft);
            lineText.supportRichText = true;
            lineText.resizeTextForBestFit = true;
            lineText.resizeTextMinSize = 12;
            lineText.resizeTextMaxSize = 16;

            // Chart.
            var chart = Child(frame, "Chart");
            Place(chart, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(556f, 180f));
            var chartImg = Img(chart);
            Sliced(chartImg, "recipe-tray.png");
            chartImg.color = TrayTint;
            var inner = Child(chart, "Inner");
            Stretch(inner);
            inner.offsetMin = new Vector2(12f, 12f);
            inner.offsetMax = new Vector2(-12f, -12f);

            var labels = Child(inner, "Labels");
            Place(labels, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 22f));
            var axis = Child(inner, "Axis");
            Place(axis, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(0f, 3f));
            var axisImg = Img(axis);
            axisImg.color = Ink;
            axisImg.raycastTarget = false;
            var bars = Child(inner, "Bars");
            Place(bars, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 25f), new Vector2(0f, 130f));
            var you = Child(bars, "You");
            Place(you, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 19f), new Vector2(34f, 34f));
            var youImg = Img(you);
            youImg.sprite = keplerImg.sprite;
            youImg.preserveAspect = true;
            youImg.raycastTarget = false;
            axis.SetAsLastSibling();

            // Buttons.
            var buttons = Child(frame, "Buttons");
            Place(buttons, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 74f), new Vector2(520f, 71f));
            var layout = Ensure<HorizontalLayoutGroup>(buttons);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var playAgain = Child(buttons, "PlayAgainButton");
            ToyButton(playAgain, "button-primary", CalendarIcon, new Vector2(230f, 71f));
            playAgain.Find("Label").GetComponent<Text>().text = "Play again";
            var back = Child(buttons, "BackButton");
            ToyButton(back, "button-secondary", null, new Vector2(170f, 71f));
            back.Find("Label").GetComponent<Text>().text = "Back";

            panel.gameObject.SetActive(false);

            var so = new SerializedObject(view);
            so.FindProperty("config").objectReferenceValue = new SerializedObject(endView).FindProperty("config").objectReferenceValue;
            so.FindProperty("root").objectReferenceValue = panel.gameObject;
            so.FindProperty("endScreenCard").objectReferenceValue = card.gameObject;
            so.FindProperty("titleLabel").objectReferenceValue = titleText;
            so.FindProperty("keplerLine").objectReferenceValue = lineText;
            so.FindProperty("chartArea").objectReferenceValue = chart.gameObject;
            so.FindProperty("barsContainer").objectReferenceValue = bars;
            so.FindProperty("labelsContainer").objectReferenceValue = labels;
            so.FindProperty("youMarker").objectReferenceValue = you;
            so.FindProperty("barSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(ToyDir + "bar.png");
            so.FindProperty("labelFont").objectReferenceValue = atkinson;
            so.FindProperty("closeButton").objectReferenceValue = closeBtn;
            so.FindProperty("backButton").objectReferenceValue = back.GetComponent<Button>();
            so.FindProperty("backLabel").objectReferenceValue = back.Find("Label").GetComponent<Text>();
            so.FindProperty("playAgainButton").objectReferenceValue = playAgain.GetComponent<Button>();
            so.FindProperty("playAgainLabel").objectReferenceValue = playAgain.Find("Label").GetComponent<Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ---------- Pieces ----------

        private static void ToyButton(RectTransform rt, string sprite, string iconPath, Vector2 size)
        {
            rt.sizeDelta = size;
            var img = Img(rt);
            Sliced(img, sprite + ".png");
            img.raycastTarget = true;
            var btn = Ensure<Button>(rt);
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            var state = btn.spriteState;
            state.pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToyDir + sprite + "-pressed.png");
            btn.spriteState = state;

            float left = 16f;
            if (iconPath != null)
            {
                var icon = Child(rt, "Icon");
                Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(40f, 4f), new Vector2(36f, 36f));
                var iconImg = Img(icon);
                iconImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
                iconImg.preserveAspect = true;
                iconImg.raycastTarget = false;
                left = 62f;
            }

            var label = Child(rt, "Label");
            Stretch(label);
            label.offsetMin = new Vector2(left, 7f);
            label.offsetMax = new Vector2(-16f, -3f);
            var text = Txt(label);
            StyleWhite(text, 24, TextAnchor.MiddleCenter);
            text.raycastTarget = false;
        }

        private static void StyleWhite(Text text, int size, TextAnchor align)
        {
            text.font = luckiest;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = Color.white;
            text.alignment = align;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            Outline outline = null;
            Shadow shadow = null;
            foreach (var s in text.GetComponents<Shadow>())
            {
                if (s is Outline o) outline = o;
                else shadow = s;
            }
            if (outline == null) outline = text.gameObject.AddComponent<Outline>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            outline.effectColor = Ink;
            outline.effectDistance = new Vector2(2f, -2f);
            shadow.effectColor = Ink;
            shadow.effectDistance = new Vector2(0f, -3f);
        }

        private static void StyleInk(Text text, int size, TextAnchor align)
        {
            text.font = atkinson;
            text.fontSize = size;
            text.fontStyle = FontStyle.Normal;
            text.color = Ink;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            foreach (var s in text.GetComponents<Shadow>()) s.enabled = false;
        }

        // ---------- Helpers ----------

        private static GameObject FindInScene(string name)
        {
            foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == name) return go;
            return GameObject.Find(name);
        }

        private static void EnsureSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var t = parent.Find(name);
            if (t != null) return (RectTransform)t;
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static T Ensure<T>(Component c) where T : Component
        {
            var existing = c.GetComponent<T>();
            return existing != null ? existing : c.gameObject.AddComponent<T>();
        }

        private static Image Img(Component c) => Ensure<Image>(c);
        private static Text Txt(Component c) => Ensure<Text>(c);

        private static void Sliced(Image img, string file)
        {
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToyDir + file);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 2f;
            img.fillCenter = true;
            img.color = Color.white;
        }

        private static void Simple(RectTransform rt, string file, Vector2 pos, Vector2 size)
        {
            Place(rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var img = Img(rt);
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ToyDir + file);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private static void Stretch(RectTransform rt) =>
            Place(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        private static void Place(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void SetActive(Transform parent, string name, bool active)
        {
            var t = parent.Find(name);
            if (t != null) t.gameObject.SetActive(active);
        }

        private static Color Hex(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}
