using System.Collections.Generic;
using ArtemisX.Core;
using ArtemisX.Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ArtemisX.UI.EditorTools
{
    public static class ToyUISceneApplier
    {
        private const string ToyDir = "Assets/GameAssets/Free Icon Pack v3.1 (Basic)/Claude outputs/ToyUI/";
        private const string IconDir = "Assets/GameAssets/Free Icon Pack v3.1 (Basic)/";

        private static readonly Color Ink = new Color(0.016f, 0.016f, 0.208f, 1f);
        private static readonly Color Muted = new Color(0.337f, 0.396f, 0.498f, 1f);
        private static readonly Color Green = new Color(0.184f, 0.796f, 0.306f, 1f);
        private static readonly Color DividerColor = new Color(0.82f, 0.85f, 0.90f, 1f);

        private static Font luckiest;
        private static Font atkinson;
        private static UIStyle style;

        [MenuItem("Artemis X/Apply Remaining Toy UI & Build Flag")]
        public static void ApplyAll()
        {
            EnsureSpritesImported();

            luckiest = AssetDatabase.LoadAssetAtPath<Font>(ToyDir + "LuckiestGuy-Regular.ttf");
            atkinson = AssetDatabase.LoadAssetAtPath<Font>(ToyDir + "AtkinsonHyperlegible-Bold.ttf");
            style = AssetDatabase.LoadAssetAtPath<UIStyle>("Assets/_Config/UI/UIStyle.asset");

            var hudGo = GameObject.Find("------------HUD-----------------");
            if (hudGo == null) { Debug.LogError("HUD not found in active scene."); return; }
            var hudRoot = hudGo.transform;

            ApplyRoverMarkers(hudRoot);
            ApplyEventDialog(hudRoot);
            ApplyBuildNextFlag(hudRoot);
            ApplyToyUiPolish(hudRoot);

            EditorSceneManager.MarkSceneDirty(hudGo.scene);
            EditorSceneManager.SaveScene(hudGo.scene);
            Debug.Log("[ToyUI] Applied RoverMap markers, EventDialog choice cards, BuildNextFlag, and Toy UI polish.");
        }

        private static void EnsureSpritesImported()
        {
            string[] paths =
            {
                IconDir + "Currency/Crystal/256w/Crystal Blue Outline 256px.png",
                IconDir + "Main/Settings/256px/Settings 1 1st Outline 256px.png",
                IconDir + "Main/Lighting/256px/Lighting 1st Outline 256px.png",
                IconDir + "Nature/Apple/256px/Apple 1st Outline 256px.png",
                IconDir + "Item/Hammer/256px/Hammer 1st Outline 256px.png",
                IconDir + "Item/Shield/256px/Shield 1st Outline 256px.png",
                IconDir + "Nature/Thunderstorm/256w/Thunderstorm 1st Outline 256px.png",
                IconDir + "Main/Fire/256px/Fire 1st Outline 256px.png",
                IconDir + "Nature/Leaf/256px/Leaf 1st Outline 256px.png",
                IconDir + "UI/Skip/256w/Skip 1st Outline 256px.png",
                IconDir + "UI/Warning/256px/Warning 1st Outline 256px.png",
                IconDir + "Item/Lock/64px/Lock 1st Outline 64px.png",
                IconDir + "UI/Checkmark/64px/Checkmark 1st Outline 64px.png",
                IconDir + "UI/Warning/64px/Warning 1st Outline 64px.png",
                ToyDir + "ground-shadow.png",
                ToyDir + "marker-pill.png",
                ToyDir + "marker-pill-selected.png",
                ToyDir + "marker-point.png",
                ToyDir + "name-tag.png",
            };

            foreach (var p in paths)
            {
                var ti = AssetImporter.GetAtPath(p) as TextureImporter;
                if (ti == null) continue;
                bool dirty = false;
                if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; dirty = true; }
                if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect)
                {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    ti.SetTextureSettings(settings);
                    dirty = true;
                }
                if (dirty) ti.SaveAndReimport();
            }
        }

        // ---------- 1. Rover Map Toy Pill Flags ----------

        private static void ApplyRoverMarkers(Transform hudRoot)
        {
            var roverMap = hudRoot.Find("RoverMap");
            if (roverMap == null) return;
            var markersRoot = roverMap.Find("Markers");
            if (markersRoot == null) return;

            var pillNormal = LoadSprite(ToyDir + "marker-pill.png");
            var pillSelected = LoadSprite(ToyDir + "marker-pill-selected.png");
            var pointSprite = LoadSprite(ToyDir + "marker-point.png");
            var nameTagSprite = LoadSprite(ToyDir + "name-tag.png");
            var riskOn = LoadSprite(ToyDir + "risk-on.png");
            var riskOff = LoadSprite(ToyDir + "risk-off.png");
            var water256 = LoadSprite(IconDir + "Currency/Crystal/256w/Crystal Blue Outline 256px.png");
            var parts256 = LoadSprite(IconDir + "Main/Settings/256px/Settings 1 1st Outline 256px.png");

            foreach (Transform markerTr in markersRoot)
            {
                var view = markerTr.GetComponent<RoverSiteMarkerView>();
                if (view == null) continue;

                var rt = (RectTransform)markerTr;
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(196f, 100f);

                // Disable old background/shadow/old children without deleting referenced objects
                var rootImg = markerTr.GetComponent<Image>();
                if (rootImg != null) rootImg.enabled = false;
                var rootShadow = markerTr.GetComponent<Shadow>();
                if (rootShadow != null) rootShadow.enabled = false;

                foreach (var oldName in new[] { "Chips", "Risk", "Stem" })
                {
                    var oldCh = markerTr.Find(oldName);
                    if (oldCh != null) oldCh.gameObject.SetActive(false);
                }

                // Rebuild our Toy Pill Flag children idempotently
                foreach (var toyName in new[] { "MarkerPoint", "NameTag", "FlagPill" })
                {
                    var existing = markerTr.Find(toyName);
                    if (existing != null) Object.DestroyImmediate(existing.gameObject);
                }

                // MarkerPoint at (0, 0)
                var pointRt = NewUI("MarkerPoint", markerTr);
                Place(pointRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(16f, 16f));
                var pointImg = pointRt.gameObject.AddComponent<Image>();
                pointImg.sprite = pointSprite;
                pointImg.type = Image.Type.Simple;
                pointImg.preserveAspect = true;
                pointImg.raycastTarget = false;

                // NameTag above point
                var tagRt = NewUI("NameTag", markerTr);
                Place(tagRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 13f), new Vector2(168f, 28f));
                var tagImg = tagRt.gameObject.AddComponent<Image>();
                tagImg.sprite = nameTagSprite;
                tagImg.type = Image.Type.Sliced;
                tagImg.pixelsPerUnitMultiplier = 2f;
                tagImg.color = Color.white;
                tagImg.raycastTarget = true;

                // Move/restyle existing Name Text inside NameTag
                var nameTr = markerTr.Find("Name") ?? tagRt.Find("Name");
                Text nameText = nameTr != null ? nameTr.GetComponent<Text>() : null;
                if (nameText != null)
                {
                    nameTr.SetParent(tagRt, false);
                    var nrt = (RectTransform)nameTr;
                    nrt.anchorMin = Vector2.zero;
                    nrt.anchorMax = Vector2.one;
                    nrt.pivot = new Vector2(0.5f, 0.5f);
                    nrt.offsetMin = new Vector2(10f, 1f);
                    nrt.offsetMax = new Vector2(-10f, -3f);
                    nameText.font = luckiest;
                    nameText.fontSize = 13;
                    nameText.color = Ink;
                    nameText.alignment = TextAnchor.MiddleCenter;
                    nameText.resizeTextForBestFit = true;
                    nameText.resizeTextMinSize = 10;
                    nameText.resizeTextMaxSize = 13;
                    nameText.raycastTarget = false;
                }

                // FlagPill above NameTag
                var pillRt = NewUI("FlagPill", markerTr);
                Place(pillRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(14f, 38f), new Vector2(166f, 52f));
                var pillImg = pillRt.gameObject.AddComponent<Image>();
                pillImg.sprite = pillNormal;
                pillImg.type = Image.Type.Sliced;
                pillImg.pixelsPerUnitMultiplier = 2f;
                pillImg.color = Color.white;
                pillImg.raycastTarget = true;

                var btn = markerTr.GetComponent<Button>();
                if (btn != null)
                {
                    btn.transition = Selectable.Transition.None;
                    btn.targetGraphic = pillImg;
                }

                // RewardIcon breaking out left edge
                var icoRt = NewUI("RewardIcon", pillRt);
                Place(icoRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(6f, 0f), new Vector2(58f, 58f));
                var icoImg = icoRt.gameObject.AddComponent<Image>();
                icoImg.sprite = water256;
                icoImg.type = Image.Type.Simple;
                icoImg.preserveAspect = true;
                icoImg.raycastTarget = false;

                // RewardLabel (+32)
                var rewRt = NewUI("RewardLabel", pillRt);
                Place(rewRt, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(36f, -2f), new Vector2(54f, 0f));
                var rewTxt = rewRt.gameObject.AddComponent<Text>();
                rewTxt.font = luckiest;
                rewTxt.fontSize = 22;
                rewTxt.color = Green;
                rewTxt.alignment = TextAnchor.MiddleCenter;
                rewTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
                rewTxt.verticalOverflow = VerticalWrapMode.Overflow;
                rewTxt.raycastTarget = false;
                rewTxt.text = "+32";
                EnsureOutline(rewTxt.gameObject, 2f);

                // Divider
                var divRt = NewUI("Divider", pillRt);
                Place(divRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(94f, 1f), new Vector2(2f, 24f));
                var divImg = divRt.gameObject.AddComponent<Image>();
                divImg.color = DividerColor;
                divImg.raycastTarget = false;

                // 4 mini risk blocks (12x16)
                var blocksRt = NewUI("RiskBlocks", pillRt);
                Place(blocksRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(101f, 1f), new Vector2(57f, 16f));
                var hl = blocksRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 3f;
                hl.childAlignment = TextAnchor.MiddleLeft;
                hl.childControlWidth = false;
                hl.childControlHeight = false;
                var blockImages = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    var brt = NewUI("Block_" + i, blocksRt);
                    brt.sizeDelta = new Vector2(12f, 16f);
                    var bimg = brt.gameObject.AddComponent<Image>();
                    bimg.sprite = riskOff;
                    bimg.type = Image.Type.Sliced;
                    bimg.pixelsPerUnitMultiplier = 2f;
                    bimg.color = Color.white;
                    bimg.raycastTarget = false;
                    blockImages[i] = bimg;
                }

                // Wire RoverSiteMarkerView serialized fields
                var so = new SerializedObject(view);
                so.FindProperty("flagImage").objectReferenceValue = pillImg;
                so.FindProperty("pillNormalSprite").objectReferenceValue = pillNormal;
                so.FindProperty("pillSelectedSprite").objectReferenceValue = pillSelected;
                so.FindProperty("toyRewardIcon").objectReferenceValue = icoImg;
                so.FindProperty("toyRewardLabel").objectReferenceValue = rewTxt;
                so.FindProperty("riskOnSprite").objectReferenceValue = riskOn;
                so.FindProperty("riskOffSprite").objectReferenceValue = riskOff;
                so.FindProperty("waterIcon256").objectReferenceValue = water256;
                so.FindProperty("partsIcon256").objectReferenceValue = parts256;
                var blocksProp = so.FindProperty("toyRiskBlocks");
                blocksProp.arraySize = 4;
                for (int i = 0; i < 4; i++)
                    blocksProp.GetArrayElementAtIndex(i).objectReferenceValue = blockImages[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------- 2. Event Dialog (Choice Cards) on MorningCard / MorningSequenceController ----------

        private static void ApplyEventDialog(Transform hudRoot)
        {
            var morningTr = hudRoot.Find("MorningCard");
            if (morningTr == null) return;
            var modal = morningTr.GetComponent<ModalCardView>();
            var cardTr = morningTr.Find("Card");
            if (modal == null || cardTr == null) return;

            var bubbleSpr = LoadSprite(ToyDir + "bubble.png");
            var tailSpr = LoadSprite(ToyDir + "bubble-tail.png");
            var keplerSpr = LoadSprite(ToyDir + "kepler.png");
            var traySpr = LoadSprite(ToyDir + "recipe-tray.png");
            var stickerSpr = LoadSprite(ToyDir + "loss-sticker.png");
            var lock64 = LoadSprite(IconDir + "Item/Lock/64px/Lock 1st Outline 64px.png");
            var check64 = LoadSprite(IconDir + "UI/Checkmark/64px/Checkmark 1st Outline 64px.png");
            var warn64 = LoadSprite(IconDir + "UI/Warning/64px/Warning 1st Outline 64px.png");

            var primNorm = LoadSprite(ToyDir + "button-primary.png");
            var primPres = LoadSprite(ToyDir + "button-primary-pressed.png");
            var secNorm = LoadSprite(ToyDir + "button-secondary.png");
            var secPres = LoadSprite(ToyDir + "button-secondary-pressed.png");
            var disBtn = LoadSprite(ToyDir + "button-disabled.png");

            foreach (var oldName in new[] { "EventIcon", "KeplerBubbleRow", "ChoiceCardsRow", "FooterLockRow" })
            {
                var ex = cardTr.Find(oldName);
                if (ex != null) Object.DestroyImmediate(ex.gameObject);
            }

            // EventIcon breaking out top-left
            var evIconRt = NewUI("EventIcon", cardTr);
            Place(evIconRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(26f, -20f), new Vector2(72f, 72f));
            var evIconImg = evIconRt.gameObject.AddComponent<Image>();
            evIconImg.preserveAspect = true;
            evIconImg.raycastTarget = false;
            evIconRt.gameObject.SetActive(false);

            // KeplerBubbleRow (96px Kepler + bubble + bubble-tail)
            var keplerRowRt = NewUI("KeplerBubbleRow", cardTr);
            Place(keplerRowRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(-48f, 100f));

            var kpRt = NewUI("Portrait", keplerRowRt);
            Place(kpRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(96f, 96f));
            var kpImg = kpRt.gameObject.AddComponent<Image>();
            kpImg.sprite = keplerSpr;
            kpImg.preserveAspect = true;
            kpImg.raycastTarget = false;

            var bubRt = NewUI("Bubble", keplerRowRt);
            Place(bubRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bubRt.offsetMin = new Vector2(114f, 4f);
            bubRt.offsetMax = new Vector2(0f, -4f);
            var bubImg = bubRt.gameObject.AddComponent<Image>();
            bubImg.sprite = bubbleSpr;
            bubImg.type = Image.Type.Sliced;
            bubImg.pixelsPerUnitMultiplier = 2f;
            bubImg.color = Color.white;
            bubImg.raycastTarget = false;

            var tailRt = NewUI("BubbleTail", bubRt);
            Place(tailRt, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(6f, 22f), new Vector2(28f, 38f));
            var tailImg = tailRt.gameObject.AddComponent<Image>();
            tailImg.sprite = tailSpr;
            tailImg.preserveAspect = true;
            tailImg.raycastTarget = false;

            var bubTxtRt = NewUI("Text", bubRt);
            Place(bubTxtRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            bubTxtRt.offsetMin = new Vector2(20f, 12f);
            bubTxtRt.offsetMax = new Vector2(-18f, -10f);
            var bubTxt = bubTxtRt.gameObject.AddComponent<Text>();
            bubTxt.font = atkinson;
            bubTxt.fontSize = 18;
            bubTxt.color = Ink;
            bubTxt.alignment = TextAnchor.MiddleLeft;
            bubTxt.resizeTextForBestFit = true;
            bubTxt.resizeTextMinSize = 13;
            bubTxt.resizeTextMaxSize = 18;
            bubTxt.raycastTarget = false;
            keplerRowRt.gameObject.SetActive(false);

            // ChoiceCardsRow: two recipe-tray cards side by side
            var cardsRowRt = NewUI("ChoiceCardsRow", cardTr);
            Place(cardsRowRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cardsRowRt.offsetMin = new Vector2(24f, 52f);
            cardsRowRt.offsetMax = new Vector2(-24f, -184f);
            var hl = cardsRowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 16f;
            hl.childAlignment = TextAnchor.UpperCenter;
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight = true;

            var soModal = new SerializedObject(modal);
            var slotsProp = soModal.FindProperty("choiceCards");
            slotsProp.arraySize = 2;

            for (int i = 0; i < 2; i++)
            {
                var cRt = NewUI("ChoiceCard_" + i, cardsRowRt);
                var cBg = cRt.gameObject.AddComponent<Image>();
                cBg.sprite = traySpr;
                cBg.type = Image.Type.Sliced;
                cBg.pixelsPerUnitMultiplier = 2f;
                cBg.color = Color.white;
                cBg.raycastTarget = false;

                var vl = cRt.gameObject.AddComponent<VerticalLayoutGroup>();
                vl.padding = new RectOffset(14, 14, 14, 14);
                vl.spacing = 8f;
                vl.childAlignment = TextAnchor.UpperCenter;
                vl.childControlWidth = true;
                vl.childControlHeight = true;
                vl.childForceExpandWidth = true;
                vl.childForceExpandHeight = false;

                // 64px icon
                var icoHolder = NewUI("Icon", cRt);
                icoHolder.sizeDelta = new Vector2(64f, 64f);
                var icoLe = icoHolder.gameObject.AddComponent<LayoutElement>();
                icoLe.preferredWidth = 64f;
                icoLe.preferredHeight = 64f;
                icoLe.flexibleWidth = 0f;
                var cIcon = icoHolder.gameObject.AddComponent<Image>();
                cIcon.preserveAspect = true;
                cIcon.raycastTarget = false;

                // Choice name (Luckiest Guy 22, ink)
                var titleRt = NewUI("Name", cRt);
                titleRt.sizeDelta = new Vector2(0f, 30f);
                var titleLe = titleRt.gameObject.AddComponent<LayoutElement>();
                titleLe.preferredHeight = 30f;
                var cTitle = titleRt.gameObject.AddComponent<Text>();
                cTitle.font = luckiest;
                cTitle.fontSize = 22;
                cTitle.color = Ink;
                cTitle.alignment = TextAnchor.MiddleCenter;
                cTitle.resizeTextForBestFit = true;
                cTitle.resizeTextMinSize = 15;
                cTitle.resizeTextMaxSize = 22;
                cTitle.raycastTarget = false;

                // CostRow ("costs" + loss-sticker)
                var costRowRt = NewUI("CostRow", cRt);
                costRowRt.sizeDelta = new Vector2(0f, 36f);
                var costLe = costRowRt.gameObject.AddComponent<LayoutElement>();
                costLe.preferredHeight = 36f;
                var costHl = costRowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                costHl.spacing = 8f;
                costHl.childAlignment = TextAnchor.MiddleCenter;
                costHl.childControlWidth = false;
                costHl.childControlHeight = false;

                var costsCapRt = NewUI("Caption", costRowRt);
                costsCapRt.sizeDelta = new Vector2(46f, 24f);
                var costsCap = costsCapRt.gameObject.AddComponent<Text>();
                costsCap.font = atkinson;
                costsCap.fontSize = 14;
                costsCap.color = Muted;
                costsCap.alignment = TextAnchor.MiddleRight;
                costsCap.text = "costs";
                costsCap.raycastTarget = false;

                var stickerRt = NewUI("Sticker", costRowRt);
                stickerRt.sizeDelta = new Vector2(82f, 35f);
                var stImg = stickerRt.gameObject.AddComponent<Image>();
                stImg.sprite = stickerSpr;
                stImg.type = Image.Type.Sliced;
                stImg.pixelsPerUnitMultiplier = 2f;
                stImg.color = Color.white;
                stImg.raycastTarget = false;

                var stIcoRt = NewUI("Icon", stickerRt);
                Place(stIcoRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(26f, 26f));
                var stIco = stIcoRt.gameObject.AddComponent<Image>();
                stIco.preserveAspect = true;
                stIco.raycastTarget = false;

                var stAmtRt = NewUI("Amount", stickerRt);
                Place(stAmtRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                stAmtRt.offsetMin = new Vector2(34f, 0f);
                stAmtRt.offsetMax = new Vector2(-8f, -2f);
                var stAmt = stAmtRt.gameObject.AddComponent<Text>();
                stAmt.font = luckiest;
                stAmt.fontSize = 19;
                stAmt.color = Color.white;
                stAmt.alignment = TextAnchor.MiddleCenter;
                stAmt.text = "−5";
                stAmt.raycastTarget = false;
                EnsureOutline(stAmt.gameObject, 2f);

                // OutcomeRow (icon + 1 line outcome)
                var outRowRt = NewUI("OutcomeRow", cRt);
                outRowRt.sizeDelta = new Vector2(0f, 28f);
                var outLe = outRowRt.gameObject.AddComponent<LayoutElement>();
                outLe.preferredHeight = 28f;
                var outHl = outRowRt.gameObject.AddComponent<HorizontalLayoutGroup>();
                outHl.spacing = 6f;
                outHl.childAlignment = TextAnchor.MiddleCenter;
                outHl.childControlWidth = false;
                outHl.childControlHeight = false;

                var outIcoRt = NewUI("Icon", outRowRt);
                outIcoRt.sizeDelta = new Vector2(22f, 22f);
                var outIco = outIcoRt.gameObject.AddComponent<Image>();
                outIco.sprite = check64;
                outIco.preserveAspect = true;
                outIco.raycastTarget = false;

                var outTxtRt = NewUI("Text", outRowRt);
                outTxtRt.sizeDelta = new Vector2(200f, 24f);
                var outTxt = outTxtRt.gameObject.AddComponent<Text>();
                outTxt.font = atkinson;
                outTxt.fontSize = 15;
                outTxt.color = Ink;
                outTxt.alignment = TextAnchor.MiddleLeft;
                outTxt.resizeTextForBestFit = true;
                outTxt.resizeTextMinSize = 13;
                outTxt.resizeTextMaxSize = 15;
                outTxt.raycastTarget = false;

                // Spacer to push button to bottom of card
                var spacerRt = NewUI("Spacer", cRt);
                spacerRt.sizeDelta = Vector2.zero;
                var spacerLe = spacerRt.gameObject.AddComponent<LayoutElement>();
                spacerLe.flexibleHeight = 1f;
                spacerLe.minHeight = 0f;
                spacerLe.preferredHeight = 0f;

                // Button (58px tall)
                var btnRt = NewUI("ActionButton", cRt);
                btnRt.sizeDelta = new Vector2(0f, 58f);
                var btnLe = btnRt.gameObject.AddComponent<LayoutElement>();
                btnLe.preferredHeight = 58f;
                var btnImg = btnRt.gameObject.AddComponent<Image>();
                btnImg.sprite = i == 0 ? primNorm : secNorm;
                btnImg.type = Image.Type.Sliced;
                btnImg.pixelsPerUnitMultiplier = 2f;
                btnImg.color = Color.white;

                var btn = btnRt.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.SpriteSwap;
                btn.targetGraphic = btnImg;
                var st = btn.spriteState;
                st.highlightedSprite = i == 0 ? primNorm : secNorm;
                st.pressedSprite = i == 0 ? primPres : secPres;
                st.disabledSprite = disBtn;
                btn.spriteState = st;

                var btnLblRt = NewUI("Label", btnRt);
                Place(btnLblRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -2f), Vector2.zero);
                btnLblRt.offsetMin = new Vector2(12f, 2f);
                btnLblRt.offsetMax = new Vector2(-12f, -4f);
                var btnLbl = btnLblRt.gameObject.AddComponent<Text>();
                btnLbl.font = luckiest;
                btnLbl.fontSize = 22;
                btnLbl.color = i == 0 ? Color.white : Ink;
                btnLbl.alignment = TextAnchor.MiddleCenter;
                btnLbl.resizeTextForBestFit = true;
                btnLbl.resizeTextMinSize = 14;
                btnLbl.resizeTextMaxSize = 22;
                btnLbl.raycastTarget = false;
                var btnOut = EnsureOutline(btnLbl.gameObject, 2f);
                var btnShd = EnsureShadow(btnLbl.gameObject, new Vector2(0f, -3f));
                btnOut.enabled = i == 0;
                btnShd.enabled = i == 0;

                var slotEl = slotsProp.GetArrayElementAtIndex(i);
                slotEl.FindPropertyRelative("root").objectReferenceValue = cRt.gameObject;
                slotEl.FindPropertyRelative("icon").objectReferenceValue = cIcon;
                slotEl.FindPropertyRelative("title").objectReferenceValue = cTitle;
                slotEl.FindPropertyRelative("costRow").objectReferenceValue = costRowRt.gameObject;
                slotEl.FindPropertyRelative("costIcon").objectReferenceValue = stIco;
                slotEl.FindPropertyRelative("costAmount").objectReferenceValue = stAmt;
                slotEl.FindPropertyRelative("outcomeRow").objectReferenceValue = outRowRt.gameObject;
                slotEl.FindPropertyRelative("outcomeIcon").objectReferenceValue = outIco;
                slotEl.FindPropertyRelative("outcomeText").objectReferenceValue = outTxt;
                slotEl.FindPropertyRelative("button").objectReferenceValue = btn;
                slotEl.FindPropertyRelative("buttonImage").objectReferenceValue = btnImg;
                slotEl.FindPropertyRelative("buttonLabel").objectReferenceValue = btnLbl;
                slotEl.FindPropertyRelative("buttonOutline").objectReferenceValue = btnOut;
                slotEl.FindPropertyRelative("buttonShadow").objectReferenceValue = btnShd;
            }
            cardsRowRt.gameObject.SetActive(false);

            // FooterLockRow
            var footerRt = NewUI("FooterLockRow", cardTr);
            Place(footerRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(-48f, 24f));
            var fHl = footerRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            fHl.spacing = 6f;
            fHl.childAlignment = TextAnchor.MiddleCenter;
            fHl.childControlWidth = false;
            fHl.childControlHeight = false;

            var lockRt = NewUI("LockIcon", footerRt);
            lockRt.sizeDelta = new Vector2(18f, 18f);
            var lockImg = lockRt.gameObject.AddComponent<Image>();
            lockImg.sprite = lock64;
            lockImg.preserveAspect = true;
            lockImg.raycastTarget = false;

            var lockTxtRt = NewUI("Text", footerRt);
            lockTxtRt.sizeDelta = new Vector2(240f, 22f);
            var lockTxt = lockTxtRt.gameObject.AddComponent<Text>();
            lockTxt.font = atkinson;
            lockTxt.fontSize = 14;
            lockTxt.color = Muted;
            lockTxt.alignment = TextAnchor.MiddleLeft;
            lockTxt.text = "Answer to unlock Next day";
            lockTxt.raycastTarget = false;
            footerRt.gameObject.SetActive(false);

            var buttonsTr = cardTr.Find("Buttons");
            soModal.FindProperty("buttonsRow").objectReferenceValue = buttonsTr != null ? buttonsTr.gameObject : null;
            soModal.FindProperty("eventIconImage").objectReferenceValue = evIconImg;
            soModal.FindProperty("keplerBubbleRow").objectReferenceValue = keplerRowRt.gameObject;
            soModal.FindProperty("keplerBubblePortrait").objectReferenceValue = kpImg;
            soModal.FindProperty("keplerBubbleText").objectReferenceValue = bubTxt;
            soModal.FindProperty("choiceCardsRow").objectReferenceValue = cardsRowRt.gameObject;
            soModal.FindProperty("footerLockRow").objectReferenceValue = footerRt.gameObject;
            soModal.FindPropertyRelativeFallback(soModal, "footerLockText", lockTxt);
            soModal.FindProperty("primaryNormalSprite").objectReferenceValue = primNorm;
            soModal.FindProperty("primaryPressedSprite").objectReferenceValue = primPres;
            soModal.FindProperty("secondaryNormalSprite").objectReferenceValue = secNorm;
            soModal.FindProperty("secondaryPressedSprite").objectReferenceValue = secPres;
            soModal.FindProperty("disabledButtonSprite").objectReferenceValue = disBtn;
            soModal.ApplyModifiedPropertiesWithoutUndo();

            // Wire MorningSequenceController 256px / 64px icons
            var morningCtrl = hudRoot.GetComponent<MorningSequenceController>();
            if (morningCtrl != null)
            {
                var soCtrl = new SerializedObject(morningCtrl);
                soCtrl.FindProperty("waterIcon256").objectReferenceValue = LoadSprite(IconDir + "Currency/Crystal/256w/Crystal Blue Outline 256px.png");
                soCtrl.FindProperty("partsIcon256").objectReferenceValue = LoadSprite(IconDir + "Main/Settings/256px/Settings 1 1st Outline 256px.png");
                soCtrl.FindProperty("powerIcon256").objectReferenceValue = LoadSprite(IconDir + "Main/Lighting/256px/Lighting 1st Outline 256px.png");
                soCtrl.FindProperty("foodIcon256").objectReferenceValue = LoadSprite(IconDir + "Nature/Apple/256px/Apple 1st Outline 256px.png");
                soCtrl.FindProperty("hammerIcon256").objectReferenceValue = LoadSprite(IconDir + "Item/Hammer/256px/Hammer 1st Outline 256px.png");
                soCtrl.FindProperty("shieldIcon256").objectReferenceValue = LoadSprite(IconDir + "Item/Shield/256px/Shield 1st Outline 256px.png");
                soCtrl.FindProperty("stormIcon256").objectReferenceValue = LoadSprite(IconDir + "Nature/Thunderstorm/256w/Thunderstorm 1st Outline 256px.png");
                soCtrl.FindProperty("radiationIcon256").objectReferenceValue = LoadSprite(IconDir + "Main/Fire/256px/Fire 1st Outline 256px.png");
                soCtrl.FindProperty("leafIcon256").objectReferenceValue = LoadSprite(IconDir + "Nature/Leaf/256px/Leaf 1st Outline 256px.png");
                soCtrl.FindProperty("skipIcon256").objectReferenceValue = LoadSprite(IconDir + "UI/Skip/256w/Skip 1st Outline 256px.png");
                soCtrl.FindProperty("warningIcon256").objectReferenceValue = LoadSprite(IconDir + "UI/Warning/256px/Warning 1st Outline 256px.png");
                soCtrl.FindProperty("checkmarkIcon64").objectReferenceValue = check64;
                soCtrl.FindProperty("warningIcon64").objectReferenceValue = warn64;
                soCtrl.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ---------- 3. Build Here Next Flag + Ground Shadow ----------

        private static void ApplyBuildNextFlag(Transform hudRoot)
        {
            var oldFlag = hudRoot.Find("BuildNextFlag");
            if (oldFlag != null) Object.DestroyImmediate(oldFlag.gameObject);
            var oldShadow = GameObject.Find("BuildNextGroundShadow");
            if (oldShadow != null) Object.DestroyImmediate(oldShadow);

            var pillSpr = LoadSprite(ToyDir + "marker-pill.png");
            var stickerSpr = LoadSprite(ToyDir + "loss-sticker.png");
            var shadowSpr = LoadSprite(ToyDir + "ground-shadow.png");
            var settings256 = LoadSprite(IconDir + "Main/Settings/256px/Settings 1 1st Outline 256px.png");
            var lighting256 = LoadSprite(IconDir + "Main/Lighting/256px/Lighting 1st Outline 256px.png");
            var leaf256 = LoadSprite(IconDir + "Nature/Leaf/256px/Leaf 1st Outline 256px.png");
            var hammer256 = LoadSprite(IconDir + "Item/Hammer/256px/Hammer 1st Outline 256px.png");

            // World ground shadow
            var shadowGo = new GameObject("BuildNextGroundShadow");
            shadowGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            shadowGo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            var sr = shadowGo.AddComponent<SpriteRenderer>();
            sr.sprite = shadowSpr;
            sr.color = new Color(1f, 1f, 1f, 0.7f);
            sr.sortingOrder = 1;

            // Screen-space flag on HUD (placed before MorningCard/DialogueCanvas/ContextConfirmPopup)
            var rootRt = BuildStretch("BuildNextFlag", hudRoot);
            var morningTr = hudRoot.Find("MorningCard");
            if (morningTr != null) rootRt.SetSiblingIndex(morningTr.GetSiblingIndex());

            var visualRt = NewUI("Visual", rootRt);
            Place(visualRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(260f, 52f));

            var pillRt = NewUI("Pill", visualRt);
            Place(pillRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var pillImg = pillRt.gameObject.AddComponent<Image>();
            pillImg.sprite = pillSpr;
            pillImg.type = Image.Type.Sliced;
            pillImg.pixelsPerUnitMultiplier = 2f;
            pillImg.color = Color.white;
            pillImg.raycastTarget = true;

            var btn = visualRt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = pillImg;

            // 66px icon overlapping left end by 12px and sticking out ~10px above
            var icoRt = NewUI("BuildingIcon", visualRt);
            Place(icoRt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-12f, 10f), new Vector2(66f, 66f));
            var icoImg = icoRt.gameObject.AddComponent<Image>();
            icoImg.sprite = settings256;
            icoImg.preserveAspect = true;
            icoImg.raycastTarget = false;

            // Name (white Luckiest Guy 20 + Outline 2,-2 + Shadow 0,-3)
            var nameRt = NewUI("Name", visualRt);
            Place(nameRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            nameRt.offsetMin = new Vector2(56f, 0f);
            nameRt.offsetMax = new Vector2(-14f, -3f);
            var nameTxt = nameRt.gameObject.AddComponent<Text>();
            nameTxt.font = luckiest;
            nameTxt.fontSize = 20;
            nameTxt.color = Color.white;
            nameTxt.alignment = TextAnchor.MiddleCenter;
            nameTxt.resizeTextForBestFit = true;
            nameTxt.resizeTextMinSize = 13;
            nameTxt.resizeTextMaxSize = 20;
            nameTxt.raycastTarget = false;
            nameTxt.text = "Processing Plant";
            EnsureOutline(nameTxt.gameObject, 2f);
            EnsureShadow(nameTxt.gameObject, new Vector2(0f, -3f));

            // Costs on top-right edge (right -14px, top -22px -> growing to the left)
            var costsRt = NewUI("Costs", visualRt);
            Place(costsRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(-14f, -4f), new Vector2(180f, 34f));
            var chl = costsRt.gameObject.AddComponent<HorizontalLayoutGroup>();
            chl.spacing = 6f;
            chl.childAlignment = TextAnchor.MiddleRight;
            chl.childControlWidth = false;
            chl.childControlHeight = false;

            var view = rootRt.gameObject.AddComponent<BuildNextFlagView>();
            var so = new SerializedObject(view);
            var stickersProp = so.FindProperty("costStickers");
            stickersProp.arraySize = 2;

            for (int i = 0; i < 2; i++)
            {
                var stRt = NewUI("Cost_" + i, costsRt);
                stRt.sizeDelta = new Vector2(76f, 32f);
                var stImg = stRt.gameObject.AddComponent<Image>();
                stImg.sprite = stickerSpr;
                stImg.type = Image.Type.Sliced;
                stImg.pixelsPerUnitMultiplier = 2f;
                stImg.color = Color.white;
                stImg.raycastTarget = false;

                var cIcoRt = NewUI("Icon", stRt);
                Place(cIcoRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(7f, 0f), new Vector2(24f, 24f));
                var cIco = cIcoRt.gameObject.AddComponent<Image>();
                cIco.preserveAspect = true;
                cIco.raycastTarget = false;

                var cAmtRt = NewUI("Amount", stRt);
                Place(cAmtRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                cAmtRt.offsetMin = new Vector2(31f, 0f);
                cAmtRt.offsetMax = new Vector2(-7f, -2f);
                var cAmt = cAmtRt.gameObject.AddComponent<Text>();
                cAmt.font = luckiest;
                cAmt.fontSize = 17;
                cAmt.color = Color.white;
                cAmt.alignment = TextAnchor.MiddleCenter;
                cAmt.text = "−15";
                cAmt.raycastTarget = false;
                EnsureOutline(cAmt.gameObject, 2f);

                var el = stickersProp.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = stRt.gameObject;
                el.FindPropertyRelative("icon").objectReferenceValue = cIco;
                el.FindPropertyRelative("amount").objectReferenceValue = cAmt;
            }

            var buildings = Object.FindAnyObjectByType<BuildingManager>(FindObjectsInactive.Include);
            var resources = Object.FindAnyObjectByType<ResourceManager>(FindObjectsInactive.Include);
            var hud = hudRoot.GetComponent<GameplayHud>();
            var slots = Object.FindObjectsByType<GhostSlotInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            so.FindProperty("buildings").objectReferenceValue = buildings;
            so.FindProperty("resources").objectReferenceValue = resources;
            so.FindProperty("hud").objectReferenceValue = hud;
            so.FindProperty("style").objectReferenceValue = style;
            so.FindProperty("viewCamera").objectReferenceValue = Camera.main;
            var slotsProp = so.FindProperty("slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

            so.FindProperty("visualRoot").objectReferenceValue = visualRt.gameObject;
            so.FindProperty("pillRect").objectReferenceValue = visualRt;
            so.FindProperty("pillImage").objectReferenceValue = pillImg;
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("buildingIcon").objectReferenceValue = icoImg;
            so.FindProperty("nameLabel").objectReferenceValue = nameTxt;
            so.FindProperty("fallbackHammerIcon").objectReferenceValue = hammer256;
            so.FindProperty("groundShadow").objectReferenceValue = shadowGo;
            var bIcons = so.FindProperty("buildingIcons");
            bIcons.arraySize = 3;
            bIcons.GetArrayElementAtIndex(0).objectReferenceValue = settings256;
            bIcons.GetArrayElementAtIndex(1).objectReferenceValue = lighting256;
            bIcons.GetArrayElementAtIndex(2).objectReferenceValue = leaf256;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Disable old GhostRingPulse + LineRenderer on slot 0 immediately in EditMode
            foreach (var s in slots)
            {
                if (s != null && s.BuildingIndex == 0)
                {
                    var ring = s.transform.Find("GhostRing");
                    if (ring != null)
                    {
                        var pulse = ring.GetComponent<Visuals.GhostRingPulse>();
                        if (pulse != null) pulse.enabled = false;
                        var lr = ring.GetComponent<LineRenderer>();
                        if (lr != null) lr.enabled = false;
                    }
                }
            }
        }

        // ---------- 4. Toy UI Polish (Badges, Shadows) ----------

        private static void ApplyToyUiPolish(Transform hudRoot)
        {
            // SupplyAlertButton: replace UICircle Badge with button-danger, Frame ppu=2
            var supplyAlert = hudRoot.Find("SupplyAlertButton/Visual");
            if (supplyAlert != null)
            {
                var frameImg = supplyAlert.Find("Frame")?.GetComponent<Image>();
                if (frameImg != null) frameImg.pixelsPerUnitMultiplier = 2f;

                var badgeImg = supplyAlert.Find("Badge")?.GetComponent<Image>();
                if (badgeImg != null)
                {
                    badgeImg.sprite = LoadSprite(ToyDir + "button-danger.png");
                    badgeImg.type = Image.Type.Sliced;
                    badgeImg.pixelsPerUnitMultiplier = 2f;
                    badgeImg.color = Color.white;
                }
            }

            // ResourcePills: remove orphan duplicate AlertBadge, ensure ppu=2 and Shadow on Amount
            var pillsRoot = hudRoot.Find("ResourcePills");
            if (pillsRoot != null)
            {
                foreach (Transform pill in pillsRoot)
                {
                    var pv = pill.GetComponent<ResourcePillView>();
                    GameObject wiredBadge = null;
                    if (pv != null)
                    {
                        var ab = new SerializedObject(pv).FindProperty("alertBadge").objectReferenceValue as Component;
                        if (ab != null) wiredBadge = ab.gameObject;
                    }
                    var toDelete = new List<GameObject>();
                    for (int i = 0; i < pill.childCount; i++)
                    {
                        var ch = pill.GetChild(i);
                        if (ch.name == "AlertBadge" && ch.gameObject != wiredBadge)
                            toDelete.Add(ch.gameObject);
                    }
                    foreach (var d in toDelete) Object.DestroyImmediate(d);

                    if (wiredBadge != null)
                    {
                        var wImg = wiredBadge.GetComponent<Image>();
                        if (wImg != null) wImg.pixelsPerUnitMultiplier = 2f;
                    }

                    var amtTxt = pill.Find("Amount")?.GetComponent<Text>();
                    if (amtTxt != null) EnsureShadow(amtTxt.gameObject, new Vector2(0f, -4f));
                }
            }

            // Add Shadow (0, -3) on white-on-color Luckiest Guy texts
            string[] whiteOnColorPaths =
            {
                "NextDayButton/Label",
                "MissionCard/Title",
                "DialogueCanvas/DialogueRoot/BottomBox/SpeakerNameBanner/SpeakerNameLabel",
                "DialogueCanvas/DialogueRoot/BottomBox/ChoicesRoot/ChoiceButton_0/Label",
                "DialogueCanvas/DialogueRoot/BottomBox/ContinueNextBg/NextLabel",
                "MorningCard/Card/Banner/Text",
                "MorningCard/Card/Buttons/ButtonA/Label",
                "ContextConfirmPopup/Title",
                "ContextConfirmPopup/ConfirmButton/Label",
                "ContextConfirmPopup/CostRow/Cost_0/Amount",
                "ContextConfirmPopup/CostRow/Cost_1/Amount",
                "ContextConfirmPopup/CostRow/Cost_2/Amount",
                "RoverMap/Title/Text",
                "RoverMap/SiteCard/Card/Title",
                "RoverMap/SiteCard/Card/SendButton/Content/Label",
                "RoverMap/SiteCard/Card/SendButton/CostSticker/Amount",
            };
            foreach (var p in whiteOnColorPaths)
            {
                var t = hudRoot.Find(p);
                if (t != null) EnsureShadow(t.gameObject, new Vector2(0f, -3f));
            }

            var tut = GameObject.Find("Run4_TutorialCanvas");
            if (tut != null)
            {
                var ht = tut.transform.Find("HintBanner/Text");
                if (ht != null) EnsureShadow(ht.gameObject, new Vector2(0f, -3f));
            }

            var end = GameObject.Find("Run4_EndScreenCanvas");
            if (end != null)
            {
                foreach (var p in new[] { "Root/Card/Title", "Root/Card/BauzerName", "Root/Card/PlayAgainButton/Label" })
                {
                    var t = end.transform.Find(p);
                    if (t != null) EnsureShadow(t.gameObject, new Vector2(0f, -3f));
                }
            }
        }

        // ---------- Helpers ----------

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static RectTransform BuildStretch(string name, Transform parent)
        {
            var rt = NewUI(name, parent);
            Place(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rt;
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
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Outline EnsureOutline(GameObject go, float dist)
        {
            var o = go.GetComponent<Outline>();
            if (o == null) o = go.AddComponent<Outline>();
            o.effectColor = Ink;
            o.effectDistance = new Vector2(dist, -dist);
            return o;
        }

        private static Shadow EnsureShadow(GameObject go, Vector2 dist)
        {
            Shadow found = null;
            foreach (var s in go.GetComponents<Shadow>())
            {
                if (!(s is Outline)) { found = s; break; }
            }
            if (found == null) found = go.AddComponent<Shadow>();
            found.effectColor = Ink;
            found.effectDistance = dist;
            return found;
        }

        private static void FindPropertyRelativeFallback(this SerializedObject _, SerializedObject so, string propName, Object val)
        {
            var p = so.FindProperty(propName);
            if (p != null) p.objectReferenceValue = val;
        }
    }
}
