using System.IO;
using ArtemisX.UI.StartScreen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ArtemisX.UI.EditorTools
{
    /// <summary>
    /// Builds Assets/Scenes/CharacterSelect.unity with cool 3D lighting, 4 idle Quaternius astronauts,
    /// runtime face-photo camera, and the minimal ToyUI box (avatar + prefilled funny name + NEXT button),
    /// then updates HandMadeBase.unity and Build Settings.
    /// </summary>
    public static class CharacterSelectSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/CharacterSelect.unity";
        private const string GameplayScenePath = "Assets/Scenes/HandMadeBase.unity";
        private const string ToyUiRoot = "Assets/GameAssets/Free Icon Pack v3.1 (Basic)/Claude outputs/ToyUI/";
        private const string ProfilePath = "Assets/_Config/UI/PlayerRunProfile.asset";
        private const string ControllerPath = "Assets/_Animations/AstronautWalker.controller";
        private const string GroundMatPath = "Assets/_Materials/M_LunarGround_HandMadeBase.mat";
        private const string CharacterMatPath = "Assets/_Materials/M_QuaterniusAtlas.mat";
        private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";
        private const string PadMatFolder = "Assets/_Materials/CharacterSelect";

        private static readonly string[] AstronautFbxPaths =
        {
            "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Characters/FBX/Astronaut_FinnTheFrog.fbx",
            "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Characters/FBX/Astronaut_RaeTheRedPanda.fbx",
            "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Characters/FBX/Astronaut_FernandoTheFlamingo.fbx",
            "Assets/GameAssets/Ultimate_Space_Kit_Quaternius/Characters/FBX/Astronaut_BarbaraTheBee.fbx"
        };

        private static readonly Color[] AccentColors =
        {
            new Color(0.133f, 0.773f, 0.369f, 1f), // Frog - green
            new Color(0.961f, 0.620f, 0.043f, 1f), // Red Panda - amber
            new Color(0.925f, 0.286f, 0.600f, 1f), // Flamingo - pink
            new Color(0.220f, 0.741f, 0.973f, 1f)  // Bee - cyan
        };

        private static readonly Color InkColor = new Color(0.015686f, 0.015686f, 0.207843f, 1f);
        private static readonly Color PlaceholderColor = new Color(0.337f, 0.396f, 0.498f, 1f);

        [MenuItem("Artemis X/Build Character Select Scene")]
        public static void BuildAll()
        {
            BuildCharacterSelectScene();
            WireGameplayScene();
            UpdateBuildSettings();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterSelectSceneBuilder] Built CharacterSelect.unity, updated HandMadeBase.unity, and configured Build Settings.");
        }

        public static void BuildCharacterSelectScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Cool Lunar Lighting & Fog ---
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.055f, 0.085f, 0.19f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.016f, 0.027f, 0.078f, 1f);
            RenderSettings.fogDensity = 0.020f;

            // --- Main Camera ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0.92f, -7.6f);
            camGo.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.014f, 0.024f, 0.070f, 1f);
            cam.fieldOfView = 27f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 80f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<PhysicsRaycaster>();
            var urpCam = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;

            // --- Global Post-Processing Volume ---
            var volProfile = Load<VolumeProfile>(VolumeProfilePath);
            if (volProfile != null)
            {
                var volGo = new GameObject("Global Volume");
                var vol = volGo.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.sharedProfile = volProfile;
            }

            // --- 3-Point Cool Studio + Moonlight Rig ---
            CreateDirectionalLight("KeyLight_Moon", new Vector3(38f, -35f, 0f), new Color(0.75f, 0.86f, 1.0f, 1f), 0.55f, LightShadows.Soft);
            CreateDirectionalLight("RimLight_Cyan", new Vector3(20f, 155f, 0f), new Color(0.22f, 0.78f, 1.0f, 1f), 2.4f, LightShadows.None);
            CreateDirectionalLight("FillLight_Warm", new Vector3(8f, 25f, 0f), new Color(1.0f, 0.82f, 0.65f, 1f), 0.28f, LightShadows.None);

            // --- Lunar Stage Environment ---
            BuildEnvironment();

            // --- Face Photo Camera & Dedicated Face Light (off except during Render()) ---
            var photoCamGo = new GameObject("FacePhotoCamera");
            var photoCam = photoCamGo.AddComponent<Camera>();
            photoCam.enabled = false;
            photoCam.clearFlags = CameraClearFlags.SolidColor;
            photoCam.backgroundColor = new Color(0.045f, 0.085f, 0.21f, 1f);
            photoCam.fieldOfView = 26f;
            photoCam.nearClipPlane = 0.05f;
            photoCam.farClipPlane = 10f;
            var photoUrp = photoCamGo.AddComponent<UniversalAdditionalCameraData>();
            photoUrp.renderPostProcessing = false;

            var photoLightGo = new GameObject("FacePhotoLight");
            var photoLight = photoLightGo.AddComponent<Light>();
            photoLight.type = LightType.Directional;
            photoLight.color = new Color(0.95f, 0.97f, 1.0f, 1f);
            photoLight.intensity = 0.95f;
            photoLight.shadows = LightShadows.None;
            photoLight.enabled = false;

            // --- 4 Nameless / Roleless 3D Astronauts ---
            var animController = Load<RuntimeAnimatorController>(ControllerPath);
            var selectableAstronauts = new SelectableAstronaut[4];
            float[] xPositions = { -2.25f, -0.75f, 0.75f, 2.25f };

            for (int i = 0; i < 4; i++)
            {
                selectableAstronauts[i] = BuildAstronautSlot(i, xPositions[i], animController);
            }

            // --- EventSystem (New Input System) ---
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            // --- Minimal ToyUI Canvas ---
            BuildToyUi(selectableAstronauts, cam, photoCam, photoLight);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void BuildEnvironment()
        {
            var envRoot = new GameObject("Environment");

            // Lunar ground disc/plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "LunarGround";
            ground.transform.SetParent(envRoot.transform, false);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            var groundMat = Load<Material>(GroundMatPath);
            if (groundMat != null)
                ground.GetComponent<Renderer>().sharedMaterial = groundMat;

            // Distant background moonbase dome with warm beacon & solar panels (centered in horizon gap)
            PlaceProp("Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/dome.fbx",
                envRoot.transform, new Vector3(0.0f, 0f, 14.5f), Quaternion.Euler(0f, -20f, 0f), 0.58f);
            var domeGlowGo = new GameObject("DomeBeaconLight");
            domeGlowGo.transform.SetParent(envRoot.transform, false);
            domeGlowGo.transform.position = new Vector3(0.0f, 0.9f, 14.2f);
            var domeGlow = domeGlowGo.AddComponent<Light>();
            domeGlow.type = LightType.Point;
            domeGlow.color = new Color(1.0f, 0.65f, 0.28f, 1f);
            domeGlow.intensity = 3.2f;
            domeGlow.range = 6f;

            PlaceProp("Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/solarpanel.fbx",
                envRoot.transform, new Vector3(0.95f, 0f, 14.8f), Quaternion.Euler(0f, -28f, 0f), 0.5f);
            PlaceProp("Assets/GameAssets/KayKit_Space_Base_Bits_1.0_SOURCE/Assets/fbx(unity)/solarpanel.fbx",
                envRoot.transform, new Vector3(-0.95f, 0f, 15.0f), Quaternion.Euler(0f, 22f, 0f), 0.5f);

            // Framed lunar rocks & ridge silhouettes (clear of astronaut silhouettes)
            PlaceProp("Assets/_Prefabs/Environment/Rock_Quat_Large_1.prefab", envRoot.transform, new Vector3(-4.8f, 0f, 4.2f), Quaternion.Euler(0f, 20f, 0f), 0.65f);
            PlaceProp("Assets/_Prefabs/Environment/Rock_Quat_Large_2.prefab", envRoot.transform, new Vector3(4.9f, 0f, 4.0f), Quaternion.Euler(0f, -45f, 0f), 0.68f);
            PlaceProp("Assets/_Prefabs/Environment/Rock_Quat_Large_3.prefab", envRoot.transform, new Vector3(2.9f, -0.15f, 13.5f), Quaternion.Euler(0f, 12f, 0f), 0.9f);
            PlaceProp("Assets/_Prefabs/Environment/Rock_Quat_1.prefab", envRoot.transform, new Vector3(-3.35f, 0f, -1.2f), Quaternion.Euler(0f, 15f, 0f), 0.42f);
            PlaceProp("Assets/_Prefabs/Environment/Rock_Quat_3.prefab", envRoot.transform, new Vector3(3.4f, 0f, -1.1f), Quaternion.Euler(0f, -65f, 0f), 0.4f);
            PlaceProp("Assets/_Prefabs/Environment/Rock_KayKit_A.prefab", envRoot.transform, new Vector3(-3.2f, 0f, 9.5f), Quaternion.Euler(0f, 40f, 0f), 0.48f);
            PlaceProp("Assets/_Prefabs/Environment/Rock_KayKit_B.prefab", envRoot.transform, new Vector3(3.3f, 0f, 9.8f), Quaternion.Euler(0f, -20f, 0f), 0.48f);

            // Subtle distant starfield particles
            var starsGo = new GameObject("Starfield");
            starsGo.transform.SetParent(envRoot.transform, false);
            starsGo.transform.position = new Vector3(0f, 4.8f, 16f);
            var ps = starsGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 10f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
            main.maxParticles = 220;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.88f, 1f, 0.85f), new Color(1f, 0.96f, 0.85f, 0.95f));
            var emission = ps.emission;
            emission.rateOverTime = 22f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 9f, 2f);
            var psr = starsGo.GetComponent<ParticleSystemRenderer>();
            var starMat = AssetDatabase.LoadAssetAtPath<Material>(PadMatFolder + "/M_StarParticle.mat");
            if (starMat == null)
            {
                Shader pUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                if (pUnlit != null)
                {
                    EnsureFolder(PadMatFolder);
                    starMat = new Material(pUnlit) { color = new Color(1.5f, 1.6f, 1.9f, 1f) };
                    Texture2D dotTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
                    if (dotTex != null) starMat.mainTexture = dotTex;
                    AssetDatabase.CreateAsset(starMat, PadMatFolder + "/M_StarParticle.mat");
                }
            }
            if (starMat != null) psr.sharedMaterial = starMat;
        }

        private static SelectableAstronaut BuildAstronautSlot(int index, float xPos, RuntimeAnimatorController animController)
        {
            var slot = new GameObject($"Astronaut_{index}");
            slot.transform.position = new Vector3(xPos, 0f, 0f);
            slot.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Face -Z toward Main Camera

            // Collider for click selection
            var col = slot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.62f, 0f);
            col.radius = 0.36f;
            col.height = 1.35f;

            // 3D FBX model (with Quaternius Atlas material + Pistol hidden)
            var fbx = Load<GameObject>(AstronautFbxPaths[index]);
            var atlasMat = Load<Material>(CharacterMatPath);
            Animator animator = null;
            if (fbx != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                model.transform.SetParent(slot.transform, false);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * 0.42f;

                foreach (var t in model.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Pistol")
                        t.gameObject.SetActive(false);
                }

                if (atlasMat != null)
                {
                    foreach (var r in model.GetComponentsInChildren<Renderer>(true))
                    {
                        var mats = r.sharedMaterials;
                        for (int m = 0; m < mats.Length; m++) mats[m] = atlasMat;
                        r.sharedMaterials = mats;
                    }
                }

                animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.runtimeAnimatorController = animController;
                animator.applyRootMotion = false;
            }

            // Sleek dark lunar pad with subtle accent glow
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = "PadRing";
            pad.transform.SetParent(slot.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            pad.transform.localScale = new Vector3(0.72f, 0.008f, 0.72f);
            Object.DestroyImmediate(pad.GetComponent<Collider>());
            var padMat = GetOrCreatePadMaterial(index, AccentColors[index]);
            var padRenderer = pad.GetComponent<Renderer>();
            if (padMat != null) padRenderer.sharedMaterial = padMat;

            // Dedicated overhead Spotlight (note: slot is rotated 180 deg, so local Z=-0.35 is behind, +0.35 is in front!)
            var spotGo = new GameObject("SpotLight");
            spotGo.transform.SetParent(slot.transform, false);
            spotGo.transform.localPosition = new Vector3(0f, 3.6f, 0.45f);
            spotGo.transform.localRotation = Quaternion.Euler(83f, 180f, 0f);
            var spot = spotGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = Color.Lerp(Color.white, AccentColors[index], 0.3f);
            spot.intensity = 2.5f;
            spot.range = 9f;
            spot.spotAngle = 32f;
            spot.innerSpotAngle = 14f;
            spot.shadows = LightShadows.Soft;

            var selectable = slot.AddComponent<SelectableAstronaut>();
            var so = new SerializedObject(selectable);
            so.FindProperty("characterIndex").intValue = index;
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("spotLight").objectReferenceValue = spot;
            so.FindProperty("padRingRenderer").objectReferenceValue = padRenderer;
            so.FindProperty("accentColor").colorValue = AccentColors[index];
            so.ApplyModifiedPropertiesWithoutUndo();

            return selectable;
        }

        private static void BuildToyUi(SelectableAstronaut[] astronauts, Camera mainCam, Camera photoCam, Light photoLight)
        {
            var panelPlain = Load<Sprite>(ToyUiRoot + "panel-plain.png");
            var btnPrimary = Load<Sprite>(ToyUiRoot + "button-primary.png");
            var btnPrimaryPressed = Load<Sprite>(ToyUiRoot + "button-primary-pressed.png");
            var btnDisabled = Load<Sprite>(ToyUiRoot + "button-disabled.png");
            var luckiestGuy = Load<Font>(ToyUiRoot + "LuckiestGuy-Regular.ttf");
            var atkinsonBold = Load<Font>(ToyUiRoot + "AtkinsonHyperlegible-Bold.ttf");
            var profile = Load<PlayerRunProfile>(ProfilePath);

            var canvasGo = new GameObject("CharacterSelectCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCam;
            canvas.planeDistance = 1.5f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // --- Minimal ToyUI Bottom Box ---
            var boxGo = new GameObject("ToyBox", typeof(RectTransform), typeof(Image), typeof(Shadow));
            boxGo.transform.SetParent(canvasGo.transform, false);
            var boxRt = boxGo.GetComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0f);
            boxRt.anchorMax = new Vector2(0.5f, 0f);
            boxRt.pivot = new Vector2(0.5f, 0f);
            boxRt.anchoredPosition = new Vector2(0f, 44f);
            boxRt.sizeDelta = new Vector2(880f, 148f);

            var boxImg = boxGo.GetComponent<Image>();
            boxImg.sprite = panelPlain;
            boxImg.type = Image.Type.Sliced;
            boxImg.pixelsPerUnitMultiplier = 2f;
            boxImg.color = Color.white;

            var boxShadow = boxGo.GetComponent<Shadow>();
            boxShadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
            boxShadow.effectDistance = new Vector2(0f, -6f);

            // 1. Character Avatar (runtime 3D face photo)
            var avatarFrameGo = new GameObject("AvatarFrame", typeof(RectTransform), typeof(Image));
            avatarFrameGo.transform.SetParent(boxRt, false);
            var avatarFrameRt = avatarFrameGo.GetComponent<RectTransform>();
            avatarFrameRt.anchorMin = new Vector2(0f, 0.5f);
            avatarFrameRt.anchorMax = new Vector2(0f, 0.5f);
            avatarFrameRt.pivot = new Vector2(0f, 0.5f);
            avatarFrameRt.anchoredPosition = new Vector2(22f, 2f);
            avatarFrameRt.sizeDelta = new Vector2(110f, 110f);
            var avatarFrameImg = avatarFrameGo.GetComponent<Image>();
            avatarFrameImg.sprite = panelPlain;
            avatarFrameImg.type = Image.Type.Sliced;
            avatarFrameImg.pixelsPerUnitMultiplier = 2f;
            avatarFrameImg.color = Color.white;

            var avatarMaskGo = new GameObject("AvatarMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            avatarMaskGo.transform.SetParent(avatarFrameRt, false);
            var avatarMaskRt = avatarMaskGo.GetComponent<RectTransform>();
            avatarMaskRt.anchorMin = Vector2.zero;
            avatarMaskRt.anchorMax = Vector2.one;
            avatarMaskRt.offsetMin = new Vector2(8f, 10f);
            avatarMaskRt.offsetMax = new Vector2(-8f, -8f);
            var avatarMaskImg = avatarMaskGo.GetComponent<Image>();
            avatarMaskImg.sprite = panelPlain;
            avatarMaskImg.type = Image.Type.Sliced;
            avatarMaskImg.pixelsPerUnitMultiplier = 2f;
            avatarMaskImg.color = new Color(0.055f, 0.105f, 0.240f, 1f);
            avatarMaskGo.GetComponent<Mask>().showMaskGraphic = true;

            var avatarPhotoGo = new GameObject("AvatarPhoto", typeof(RectTransform), typeof(Image));
            avatarPhotoGo.transform.SetParent(avatarMaskRt, false);
            var avatarPhotoRt = avatarPhotoGo.GetComponent<RectTransform>();
            avatarPhotoRt.anchorMin = Vector2.zero;
            avatarPhotoRt.anchorMax = Vector2.one;
            avatarPhotoRt.offsetMin = Vector2.zero;
            avatarPhotoRt.offsetMax = Vector2.zero;
            var avatarPhotoImg = avatarPhotoGo.GetComponent<Image>();
            avatarPhotoImg.preserveAspect = true;

            // 2. Text Field with prefilled funny name
            var inputBgGo = new GameObject("NameInputField", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputBgGo.transform.SetParent(boxRt, false);
            var inputRt = inputBgGo.GetComponent<RectTransform>();
            inputRt.anchorMin = new Vector2(0f, 0.5f);
            inputRt.anchorMax = new Vector2(1f, 0.5f);
            inputRt.pivot = new Vector2(0.5f, 0.5f);
            inputRt.offsetMin = new Vector2(150f, -34f);
            inputRt.offsetMax = new Vector2(-230f, 38f);
            var inputBg = inputBgGo.GetComponent<Image>();
            inputBg.sprite = panelPlain;
            inputBg.type = Image.Type.Sliced;
            inputBg.pixelsPerUnitMultiplier = 2f;
            inputBg.color = new Color(0.95f, 0.97f, 1.0f, 1f);

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(inputRt, false);
            var phRt = placeholderGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero;
            phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(20f, 4f);
            phRt.offsetMax = new Vector2(-20f, -2f);
            var phText = placeholderGo.GetComponent<Text>();
            phText.font = atkinsonBold;
            phText.fontSize = 26;
            phText.color = PlaceholderColor;
            phText.alignment = TextAnchor.MiddleLeft;
            phText.text = "Enter name...";

            var inputTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            inputTextGo.transform.SetParent(inputRt, false);
            var itRt = inputTextGo.GetComponent<RectTransform>();
            itRt.anchorMin = Vector2.zero;
            itRt.anchorMax = Vector2.one;
            itRt.offsetMin = new Vector2(20f, 4f);
            itRt.offsetMax = new Vector2(-20f, -2f);
            var inputText = inputTextGo.GetComponent<Text>();
            inputText.font = atkinsonBold;
            inputText.fontSize = 28;
            inputText.color = InkColor;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.supportRichText = false;

            var inputField = inputBgGo.GetComponent<InputField>();
            inputField.targetGraphic = inputBg;
            inputField.textComponent = inputText;
            inputField.placeholder = phText;
            inputField.characterLimit = PlayerRunProfile.MaxNicknameLength;

            // 3. ToyUI NEXT Button
            var nextGo = new GameObject("NextButton", typeof(RectTransform), typeof(Image), typeof(Button));
            nextGo.transform.SetParent(boxRt, false);
            var nextRt = nextGo.GetComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(1f, 0.5f);
            nextRt.anchorMax = new Vector2(1f, 0.5f);
            nextRt.pivot = new Vector2(1f, 0.5f);
            nextRt.anchoredPosition = new Vector2(-22f, 2f);
            nextRt.sizeDelta = new Vector2(188f, 64f);

            var nextImg = nextGo.GetComponent<Image>();
            nextImg.sprite = btnPrimary;
            nextImg.type = Image.Type.Sliced;
            nextImg.pixelsPerUnitMultiplier = 2f;
            nextImg.color = Color.white;

            var nextBtn = nextGo.GetComponent<Button>();
            nextBtn.targetGraphic = nextImg;
            nextBtn.transition = Selectable.Transition.SpriteSwap;
            nextBtn.spriteState = new SpriteState
            {
                highlightedSprite = btnPrimary,
                pressedSprite = btnPrimaryPressed,
                selectedSprite = btnPrimary,
                disabledSprite = btnDisabled
            };

            var nextLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Outline), typeof(Shadow));
            nextLabelGo.transform.SetParent(nextRt, false);
            var nlRt = nextLabelGo.GetComponent<RectTransform>();
            nlRt.anchorMin = Vector2.zero;
            nlRt.anchorMax = Vector2.one;
            nlRt.offsetMin = new Vector2(8f, 0f);
            nlRt.offsetMax = new Vector2(-8f, -6f); // Nudge Luckiest Guy down ~14%
            var nextLabel = nextLabelGo.GetComponent<Text>();
            nextLabel.font = luckiestGuy;
            nextLabel.fontSize = 26;
            nextLabel.color = Color.white;
            nextLabel.alignment = TextAnchor.MiddleCenter;
            nextLabel.text = "NEXT >";
            nextLabel.raycastTarget = false;

            var outline = nextLabelGo.GetComponent<Outline>();
            outline.effectColor = InkColor;
            outline.effectDistance = new Vector2(2f, -2f);
            var shadow = nextLabelGo.GetComponent<Shadow>();
            shadow.effectColor = InkColor;
            shadow.effectDistance = new Vector2(0f, -3f);

            // --- Controller Wiring ---
            var mgrGo = new GameObject("CharacterSelectManager");
            var audio = mgrGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            var ctrl = mgrGo.AddComponent<CharacterSelectController>();

            var so = new SerializedObject(ctrl);
            so.FindProperty("profile").objectReferenceValue = profile;
            so.FindProperty("gameplaySceneName").stringValue = "HandMadeBase";
            var arr = so.FindProperty("astronauts");
            arr.arraySize = astronauts.Length;
            for (int i = 0; i < astronauts.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = astronauts[i];
            so.FindProperty("facePhotoCamera").objectReferenceValue = photoCam;
            so.FindProperty("facePhotoLight").objectReferenceValue = photoLight;
            so.FindProperty("toyBoxRoot").objectReferenceValue = boxGo;
            so.FindProperty("avatarImage").objectReferenceValue = avatarPhotoImg;
            so.FindProperty("nameInputField").objectReferenceValue = inputField;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.FindProperty("selectClip").objectReferenceValue = Load<AudioClip>("Assets/GameAssets/kenney_interface-sounds/Audio/select_004.ogg");
            so.FindProperty("launchClip").objectReferenceValue = Load<AudioClip>("Assets/GameAssets/kenney_interface-sounds/Audio/click_002.ogg");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireGameplayScene()
        {
            var prevActive = EditorSceneManager.GetActiveScene().path;
            var gameplay = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);

            var legacyStart = GameObject.Find("StartScreenCanvas");
            if (legacyStart != null)
                Object.DestroyImmediate(legacyStart);

            var flow = Object.FindFirstObjectByType<GameFlowController>();
            if (flow != null)
            {
                var profile = Load<PlayerRunProfile>(ProfilePath);
                var so = new SerializedObject(flow);
                so.FindProperty("profile").objectReferenceValue = profile;
                so.FindProperty("startScreen").objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(gameplay);

            if (!string.IsNullOrEmpty(prevActive) && prevActive != GameplayScenePath)
                EditorSceneManager.OpenScene(prevActive, OpenSceneMode.Single);
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
        }

        private static void CreateDirectionalLight(string name, Vector3 euler, Color color, float intensity, LightShadows shadows)
        {
            var go = new GameObject(name);
            go.transform.rotation = Quaternion.Euler(euler);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows;
        }

        private static void PlaceProp(string assetPath, Transform parent, Vector3 pos, Quaternion rot, float scale)
        {
            var prefab = Load<GameObject>(assetPath);
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.transform.localScale = Vector3.one * scale;
        }

        private static Material GetOrCreatePadMaterial(int index, Color accent)
        {
            EnsureFolder(PadMatFolder);
            string path = $"{PadMatFolder}/M_SelectPad_{index}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) return null;
                mat = new Material(lit);
                mat.SetColor("_BaseColor", accent * 0.4f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", accent * 0.5f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) Debug.LogWarning($"[CharacterSelectSceneBuilder] Missing asset at {path}");
            return asset;
        }
    }
}
