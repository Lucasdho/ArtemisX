using System.Collections.Generic;
using ArtemisX.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtemisX.EditorTools
{
    /// <summary>
    /// Creates stylized WebGL-safe URP particle materials, FX prefabs, and wires lander thrust,
    /// power plant steam, walking dust, and rover wheel dust in HandMadeBase.unity.
    /// </summary>
    public static class VfxBuilder
    {
        private const string FxMatDir = "Assets/_Materials/FX";
        private const string FxPrefabDir = "Assets/_Prefabs/FX";
        private const string HandMadeScenePath = "Assets/Scenes/HandMadeBase.unity";
        private const string MoonWalkerPrefabPath = "Assets/_Prefabs/Characters/MoonWalker.prefab";
        private const string VisualsConfigPath = "Assets/_Config/Visuals/VisualsConfig.asset";

        private struct FxMaterials
        {
            public Material steam;
            public Material dust;
            public Material grit;
            public Material flame;
            public Material spark;
        }

        [MenuItem("Artemis X/Build Game VFX")]
        public static void BuildAll()
        {
            EnsureFolder(FxMatDir);
            EnsureFolder(FxPrefabDir);

            Texture2D puffTex = ConfigureParticleTexture(FxMatDir + "/T_SoftPuff.png");
            Texture2D coreTex = ConfigureParticleTexture(FxMatDir + "/T_ThrusterCore.png");
            Texture2D sparkTex = ConfigureParticleTexture(FxMatDir + "/T_SparkStreak.png");

            FxMaterials mats = new FxMaterials
            {
                steam = CreateOrUpdateParticleMat(FxMatDir + "/M_Steam.mat", puffTex, additive: false, new Color(0.94f, 0.97f, 1f, 0.78f)),
                dust = CreateOrUpdateParticleMat(FxMatDir + "/M_LunarDust.mat", puffTex, additive: false, new Color(0.88f, 0.88f, 0.92f, 0.78f)),
                grit = CreateOrUpdateParticleMat(FxMatDir + "/M_LunarGrit.mat", puffTex, additive: false, new Color(0.35f, 0.34f, 0.39f, 0.92f)),
                flame = CreateOrUpdateParticleMat(FxMatDir + "/M_ThrusterFlame.mat", coreTex, additive: true, new Color(1.6f, 1.6f, 1.6f, 1f)),
                spark = CreateOrUpdateParticleMat(FxMatDir + "/M_ThrusterSpark.mat", sparkTex, additive: true, new Color(1.9f, 1.9f, 1.9f, 1f)),
            };

            GameObject landerThrustPrefab = BuildLanderThrustPrefab(mats);
            GameObject landerVentPrefab = BuildLanderIdleVentPrefab(mats);
            GameObject steamPrefab = BuildPowerPlantSteamPrefab(mats);
            GameObject genExhaustPrefab = BuildGeneratorExhaustPrefab(mats);
            GameObject wheelDustPrefab = BuildWheelDustPrefab(mats);
            GameObject walkingDustPrefab = BuildWalkingDustPrefab(mats);

            UpdateVisualsConfig();
            UpdateMoonWalkerPrefab(mats);

            Scene active = SceneManager.GetActiveScene();
            if (active.path == HandMadeScenePath)
            {
                WireHandMadeBase(landerThrustPrefab, landerVentPrefab, steamPrefab, genExhaustPrefab, wheelDustPrefab, walkingDustPrefab, mats);
                EditorSceneManager.MarkSceneDirty(active);
                EditorSceneManager.SaveScene(active);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VfxBuilder] Built FX materials, prefabs, and wired HandMadeBase VFX.");
        }

        private static Texture2D ConfigureParticleTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
                if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (importer.filterMode != FilterMode.Bilinear) { importer.filterMode = FilterMode.Bilinear; dirty = true; }
                if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material CreateOrUpdateParticleMat(string path, Texture2D tex, bool additive, Color baseColor)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }

            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", additive ? 2f : 0f); // 0=Alpha, 2=Additive
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 2f);
            mat.SetFloat("_SoftParticlesEnabled", 0f);
            mat.SetFloat("_CameraFadingEnabled", 0f);
            mat.SetFloat("_FlipbookBlending", 0f);
            mat.SetFloat("_ColorMode", 0f); // Multiply
            mat.SetColor("_BaseColor", baseColor);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.mainTexture = tex;
            }
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static GameObject BuildLanderThrustPrefab(FxMaterials mats)
        {
            var root = new GameObject("LanderThrust");

            // Two downward VTOL thruster pods (-0.67, 0.08, -0.18) and (0.67, 0.08, -0.18)
            CreateThrusterNozzle(root.transform, "VTOL_Left", new Vector3(-0.67f, 0.08f, -0.18f), Quaternion.Euler(90f, 0f, 0f), mats, isRear: false);
            CreateThrusterNozzle(root.transform, "VTOL_Right", new Vector3(0.67f, 0.08f, -0.18f), Quaternion.Euler(90f, 0f, 0f), mats, isRear: false);

            // Rear main cruise engine (0, 0.38, -0.94) pointing backward (-Z)
            CreateThrusterNozzle(root.transform, "Rear_Main", new Vector3(0f, 0.38f, -0.94f), Quaternion.Euler(0f, 180f, 0f), mats, isRear: true);

            // Ground-wash radial dust ring (repositioned dynamically onto terrain below dropship by LanderThrustFX)
            var washGo = new GameObject("GroundWashDust");
            washGo.transform.SetParent(root.transform, false);
            washGo.transform.localPosition = new Vector3(0f, -2f, 0f);
            var washPs = ConfigureBasePS(washGo, mats.dust, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 45);
            var wMain = washPs.main;
            wMain.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.95f);
            wMain.startSpeed = new ParticleSystem.MinMaxCurve(1.1f, 2.0f);
            wMain.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.50f);
            wMain.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            wMain.gravityModifier = new ParticleSystem.MinMaxCurve(-0.08f);
            wMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.88f, 0.87f, 0.92f, 0.45f), new Color(0.76f, 0.74f, 0.80f, 0.58f));

            var wEm = washPs.emission;
            wEm.rateOverTime = new ParticleSystem.MinMaxCurve(16f);

            var wShape = washPs.shape;
            wShape.enabled = true;
            wShape.shapeType = ParticleSystemShapeType.Circle;
            wShape.radius = 0.65f;
            wShape.radiusThickness = 0.35f;
            wShape.rotation = new Vector3(-90f, 0f, 0f);

            SetSizeOverLifetime(washPs, 0.45f, 1.4f);
            SetAlphaFade(washPs, 0.12f, 0.82f);
            SetNoise(washPs, 0.25f, 1.2f);

            return SaveAsPrefab(root, FxPrefabDir + "/LanderThrust.prefab");
        }

        private static void CreateThrusterNozzle(Transform parent, string name, Vector3 localPos, Quaternion localRot, FxMaterials mats, bool isRear)
        {
            var nozzle = new GameObject(name);
            nozzle.transform.SetParent(parent, false);
            nozzle.transform.localPosition = localPos;
            nozzle.transform.localRotation = localRot;

            // Flame plume
            var ps = ConfigureBasePS(nozzle, mats.flame, loop: true, playOnAwake: true, worldSpace: false, maxParticles: 45);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(isRear ? 0.16f : 0.15f, isRear ? 0.24f : 0.22f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(isRear ? 2.4f : 2.0f, isRear ? 3.3f : 2.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(isRear ? 0.24f : 0.22f, isRear ? 0.36f : 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.90f, 1.25f, 0.85f), new Color(1.20f, 0.65f, 0.25f, 0.85f));

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(isRear ? 38f : 34f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = isRear ? 7f : 8f;
            shape.radius = isRear ? 0.08f : 0.06f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.65f, 0.98f, 1f), 0f),
                    new GradientColorKey(new Color(1f, 0.65f, 0.2f), 0.45f),
                    new GradientColorKey(new Color(1f, 0.3f, 0.08f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0.60f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            SetSizeOverLifetime(ps, 1.0f, 0.15f);

            // Nozzle soft glow halo
            var glowGo = new GameObject(name + "_Glow");
            glowGo.transform.SetParent(nozzle.transform, false);
            var glowPs = ConfigureBasePS(glowGo, mats.flame, loop: true, playOnAwake: true, worldSpace: false, maxParticles: 12);
            var gm = glowPs.main;
            gm.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
            gm.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            gm.startSize = new ParticleSystem.MinMaxCurve(isRear ? 0.32f : 0.28f, isRear ? 0.42f : 0.38f);
            gm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.88f, 1.2f, 0.36f));
            var ge = glowPs.emission;
            ge.rateOverTime = new ParticleSystem.MinMaxCurve(14f);
            var gs = glowPs.shape;
            gs.enabled = true;
            gs.shapeType = ParticleSystemShapeType.Sphere;
            gs.radius = 0.02f;
            SetAlphaFade(glowPs, 0.25f, 0.6f);

            // Hot exhaust sparks
            CreateSparkEmitter(
                nozzle.transform,
                name + "_Sparks",
                Vector3.zero,
                Vector3.zero,
                mats.spark,
                rate: isRear ? 10f : 8f,
                maxParticles: 16,
                minLife: 0.16f,
                maxLife: 0.28f,
                minSpeed: 2.2f,
                maxSpeed: 3.4f,
                minSize: 0.035f,
                maxSize: 0.065f,
                shapeType: ParticleSystemShapeType.Cone,
                angle: 14f,
                radius: 0.05f,
                velocityScale: 0.045f,
                lengthScale: 1.6f);
        }

        private static void CreateSparkEmitter(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 shapeRotation,
            Material mat,
            float rate,
            int maxParticles,
            float minLife,
            float maxLife,
            float minSpeed,
            float maxSpeed,
            float minSize,
            float maxSize,
            ParticleSystemShapeType shapeType,
            float angle,
            float radius,
            float velocityScale,
            float lengthScale)
        {
            var sparkGo = new GameObject(name);
            sparkGo.transform.SetParent(parent, false);
            sparkGo.transform.localPosition = localPos;
            var sparkPs = ConfigureBasePS(sparkGo, mat, loop: true, playOnAwake: true, worldSpace: true, maxParticles: maxParticles);
            var sm = sparkPs.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
            sm.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1.3f, 0.75f, 0.3f, 0.92f), new Color(0.55f, 0.95f, 1.35f, 0.92f));
            var se = sparkPs.emission;
            se.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
            var ss = sparkPs.shape;
            ss.enabled = true;
            ss.shapeType = shapeType;
            ss.angle = angle;
            ss.radius = radius;
            ss.rotation = shapeRotation;
            var sr = sparkPs.GetComponent<ParticleSystemRenderer>();
            sr.renderMode = ParticleSystemRenderMode.Stretch;
            sr.velocityScale = velocityScale;
            sr.lengthScale = lengthScale;
            SetAlphaFade(sparkPs, 0.1f, 0.9f);
        }

        private static GameObject BuildLanderIdleVentPrefab(FxMaterials mats)
        {
            var root = new GameObject("LanderIdleVent");
            var ps = ConfigureBasePS(root, mats.steam, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 32);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.94f, 1f, 0.36f), new Color(0.95f, 0.98f, 1f, 0.48f));

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(5.5f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.14f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            SetSizeOverLifetime(ps, 0.45f, 1.6f);
            SetAlphaFade(ps, 0.18f, 0.8f);
            SetNoise(ps, 0.2f, 0.9f);

            // Sparkles from landed lander's vent and lower skirt rim matching Dropship thruster sparks
            CreateSparkEmitter(
                root.transform,
                "VentSparks",
                Vector3.zero,
                new Vector3(-90f, 0f, 0f),
                mats.spark,
                rate: 9f,
                maxParticles: 20,
                minLife: 0.32f,
                maxLife: 0.56f,
                minSpeed: 0.9f,
                maxSpeed: 1.8f,
                minSize: 0.05f,
                maxSize: 0.09f,
                shapeType: ParticleSystemShapeType.Cone,
                angle: 28f,
                radius: 0.10f,
                velocityScale: 0.055f,
                lengthScale: 1.9f);

            CreateSparkEmitter(
                root.transform,
                "PadRimSparks",
                new Vector3(-0.28f, -0.18f, 0.32f),
                new Vector3(-90f, 0f, 0f),
                mats.spark,
                rate: 12f,
                maxParticles: 24,
                minLife: 0.32f,
                maxLife: 0.58f,
                minSpeed: 1.0f,
                maxSpeed: 2.0f,
                minSize: 0.05f,
                maxSize: 0.095f,
                shapeType: ParticleSystemShapeType.Cone,
                angle: 70f,
                radius: 0.58f,
                velocityScale: 0.055f,
                lengthScale: 2.0f);

            return SaveAsPrefab(root, FxPrefabDir + "/LanderIdleVent.prefab");
        }

        private static GameObject BuildPowerPlantSteamPrefab(FxMaterials mats)
        {
            var root = new GameObject("PowerPlantSteam");
            var ps = ConfigureBasePS(root, mats.steam, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 80);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.1f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.20f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.32f, 0.50f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.90f, 0.96f, 1f, 0.62f), new Color(1f, 1f, 1f, 0.76f));

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(16f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 13f;
            shape.radius = 0.10f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
            vol.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            vol.z = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);

            SetSizeOverLifetime(ps, 0.38f, 1.85f);
            SetAlphaFade(ps, 0.12f, 0.85f);
            SetNoise(ps, 0.28f, 1.1f);

            // Inner denser core at chimney rim
            var coreGo = new GameObject("SteamCore");
            coreGo.transform.SetParent(root.transform, false);
            var cps = ConfigureBasePS(coreGo, mats.steam, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 28);
            var cm = cps.main;
            cm.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.2f);
            cm.startSpeed = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
            cm.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.28f);
            cm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            cm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.98f, 1f, 0.72f));
            var ce = cps.emission;
            ce.rateOverTime = new ParticleSystem.MinMaxCurve(10f);
            var cs = cps.shape;
            cs.enabled = true;
            cs.shapeType = ParticleSystemShapeType.Cone;
            cs.angle = 8f;
            cs.radius = 0.06f;
            cs.rotation = new Vector3(-90f, 0f, 0f);
            SetSizeOverLifetime(cps, 0.55f, 1.35f);
            SetAlphaFade(cps, 0.1f, 0.9f);

            return SaveAsPrefab(root, FxPrefabDir + "/PowerPlantSteam.prefab");
        }

        private static GameObject BuildGeneratorExhaustPrefab(FxMaterials mats)
        {
            var root = new GameObject("GeneratorExhaustSteam");
            var ps = ConfigureBasePS(root, mats.steam, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 80);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.9f, 2.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.30f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.10f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.96f, 0.92f, 0.88f, 0.66f), new Color(1f, 0.96f, 0.92f, 0.78f));

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(17f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = 0.09f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var vol = ps.velocityOverLifetime;
            vol.enabled = true;
            vol.space = ParticleSystemSimulationSpace.World;
            vol.x = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            vol.y = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            vol.z = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);

            SetSizeOverLifetime(ps, 0.38f, 1.8f);
            SetAlphaFade(ps, 0.12f, 0.85f);
            SetNoise(ps, 0.30f, 1.2f);

            // Subtle warm hydrogen-generator sparks
            var sparkGo = new GameObject("GeneratorSparks");
            sparkGo.transform.SetParent(root.transform, false);
            var sps = ConfigureBasePS(sparkGo, mats.spark, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 18);
            var sm = sps.main;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.95f, 1.65f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.065f);
            sm.startColor = new ParticleSystem.MinMaxGradient(new Color(1.4f, 0.78f, 0.25f, 0.95f), new Color(0.65f, 1.15f, 1.6f, 0.95f));
            var se = sps.emission;
            se.rateOverTime = new ParticleSystem.MinMaxCurve(4.5f);
            var ss = sps.shape;
            ss.enabled = true;
            ss.shapeType = ParticleSystemShapeType.Cone;
            ss.angle = 16f;
            ss.radius = 0.06f;
            ss.rotation = new Vector3(-90f, 0f, 0f);
            var sr = sps.GetComponent<ParticleSystemRenderer>();
            sr.renderMode = ParticleSystemRenderMode.Stretch;
            sr.velocityScale = 0.04f;
            sr.lengthScale = 1.6f;
            SetAlphaFade(sps, 0.1f, 0.85f);
            SetNoise(sps, 0.4f, 1.6f);

            return SaveAsPrefab(root, FxPrefabDir + "/GeneratorExhaustSteam.prefab");
        }

        private static GameObject BuildWheelDustPrefab(FxMaterials mats)
        {
            var root = new GameObject("WheelDust");

            // Primary dust under the rover's 12 wheels (kept named "Dust" for compatibility)
            var dustGo = new GameObject("Dust");
            dustGo.transform.SetParent(root.transform, false);
            var ps = ConfigureBasePS(dustGo, mats.dust, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 110);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.75f, 1.25f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.32f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.06f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.86f, 0.85f, 0.89f, 0.66f), new Color(0.70f, 0.68f, 0.74f, 0.80f));

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(28f);

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.55f, 0.08f, 2.3f);

            SetSizeOverLifetime(ps, 0.48f, 1.75f);
            SetAlphaFade(ps, 0.12f, 0.82f);
            SetNoise(ps, 0.25f, 1.2f);

            // Trailing rear wake plume
            var trailGo = new GameObject("DustWake");
            trailGo.transform.SetParent(root.transform, false);
            trailGo.transform.localPosition = new Vector3(0f, 0.14f, -1.55f);
            var tps = ConfigureBasePS(trailGo, mats.dust, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 75);
            var tm = tps.main;
            tm.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.65f);
            tm.startSpeed = new ParticleSystem.MinMaxCurve(0.45f, 0.88f);
            tm.startSize = new ParticleSystem.MinMaxCurve(0.34f, 0.56f);
            tm.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            tm.gravityModifier = new ParticleSystem.MinMaxCurve(-0.05f);
            tm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.84f, 0.83f, 0.88f, 0.58f));
            var te = tps.emission;
            te.rateOverTime = new ParticleSystem.MinMaxCurve(18f);
            var ts = tps.shape;
            ts.enabled = true;
            ts.shapeType = ParticleSystemShapeType.Cone;
            ts.angle = 30f;
            ts.radius = 0.55f;
            ts.rotation = new Vector3(-22f, 180f, 0f);
            SetSizeOverLifetime(tps, 0.55f, 1.9f);
            SetAlphaFade(tps, 0.15f, 0.82f);
            SetNoise(tps, 0.3f, 1.0f);

            // Regolith grit kicked up by tires
            var gritGo = new GameObject("WheelGrit");
            gritGo.transform.SetParent(root.transform, false);
            gritGo.transform.localPosition = new Vector3(0f, 0.06f, -1.25f);
            var gps = ConfigureBasePS(gritGo, mats.grit, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 45);
            var gm = gps.main;
            gm.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            gm.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.45f);
            gm.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.085f);
            gm.gravityModifier = new ParticleSystem.MinMaxCurve(0.35f); // low lunar gravity arc
            gm.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.53f, 0.58f, 0.9f), new Color(0.38f, 0.37f, 0.42f, 0.95f));
            var ge = gps.emission;
            ge.rateOverTime = new ParticleSystem.MinMaxCurve(18f);
            var gs = gps.shape;
            gs.enabled = true;
            gs.shapeType = ParticleSystemShapeType.Cone;
            gs.angle = 32f;
            gs.radius = 0.5f;
            gs.rotation = new Vector3(-35f, 180f, 0f);
            SetAlphaFade(gps, 0.05f, 0.85f);

            return SaveAsPrefab(root, FxPrefabDir + "/WheelDust.prefab");
        }

        private static GameObject BuildWalkingDustPrefab(FxMaterials mats)
        {
            var root = new GameObject("LandingDust");
            ConfigureWalkingDustPS(root, mats.dust);
            return SaveAsPrefab(root, FxPrefabDir + "/WalkingDust.prefab");
        }

        private static void ConfigureWalkingDustPS(GameObject go, Material dustMat)
        {
            var ps = ConfigureBasePS(go, dustMat, loop: true, playOnAwake: true, worldSpace: true, maxParticles: 48);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.50f, 0.90f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.22f, 0.52f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.24f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = new ParticleSystem.MinMaxCurve(-0.04f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.86f, 0.85f, 0.89f, 0.70f), new Color(0.70f, 0.68f, 0.74f, 0.82f));

            var em = ps.emission;
            em.rateOverTime = new ParticleSystem.MinMaxCurve(0f); // Emitted on hop landing & push-off by MoonWalker

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 65f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            SetSizeOverLifetime(ps, 0.5f, 1.55f);
            SetAlphaFade(ps, 0.08f, 0.82f);
            SetNoise(ps, 0.16f, 1.4f);
        }

        private static void UpdateVisualsConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<VisualsConfig>(VisualsConfigPath);
            if (cfg == null) return;
            var so = new SerializedObject(cfg);
            var prop = so.FindProperty("landingDustCount");
            if (prop != null && prop.intValue < 8)
            {
                prop.intValue = 8;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cfg);
            }
        }

        private static void UpdateMoonWalkerPrefab(FxMaterials mats)
        {
            var root = PrefabUtility.LoadPrefabContents(MoonWalkerPrefabPath);
            if (root == null) return;
            try
            {
                Transform ld = root.transform.Find("LandingDust");
                if (ld == null)
                {
                    var go = new GameObject("LandingDust");
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    ld = go.transform;
                }
                ConfigureWalkingDustPS(ld.gameObject, mats.dust);
                var mw = root.GetComponent<MoonWalker>();
                if (mw != null)
                {
                    var so = new SerializedObject(mw);
                    so.FindProperty("landingDust").objectReferenceValue = ld.GetComponent<ParticleSystem>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, MoonWalkerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void WireHandMadeBase(
            GameObject landerThrustPrefab,
            GameObject landerVentPrefab,
            GameObject steamPrefab,
            GameObject genExhaustPrefab,
            GameObject wheelDustPrefab,
            GameObject walkingDustPrefab,
            FxMaterials mats)
        {
            // 1. Dropship Lander Thrust
            var dropship = GameObject.Find("dropship");
            if (dropship != null)
            {
                RemoveChildIfExists(dropship.transform, "LanderThrust");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(landerThrustPrefab, dropship.transform);
                instance.name = "LanderThrust";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                var fx = dropship.GetComponent<LanderThrustFX>();
                if (fx == null) fx = dropship.AddComponent<LanderThrustFX>();

                var vtolList = new List<ParticleSystem>();
                var rearList = new List<ParticleSystem>();
                ParticleSystem wash = null;

                foreach (var ps in instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps.name == "GroundWashDust") wash = ps;
                    else if (ps.name.StartsWith("Rear_")) rearList.Add(ps);
                    else if (ps.name.StartsWith("VTOL_")) vtolList.Add(ps);
                }

                var so = new SerializedObject(fx);
                SetPSArray(so.FindProperty("vtolThrusters"), vtolList);
                SetPSArray(so.FindProperty("rearThrusters"), rearList);
                so.FindProperty("groundWashDust").objectReferenceValue = wash;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // 2. Parked lander_A subtle idle coolant vent
            var landerA = GameObject.Find("lander_A");
            if (landerA != null)
            {
                RemoveChildIfExists(landerA.transform, "LanderIdleVent");
                var vent = (GameObject)PrefabUtility.InstantiatePrefab(landerVentPrefab, landerA.transform);
                vent.name = "LanderIdleVent";
                vent.transform.localPosition = new Vector3(0.28f, 0.42f, -0.32f);
                vent.transform.localRotation = Quaternion.identity;
            }

            // 3. ProcessingPlant SteamStack steam
            var plant = GameObject.Find("ProcessingPlant");
            if (plant != null)
            {
                Transform stack = plant.transform.Find("Visuals/SteamStack");
                if (stack != null)
                {
                    AttachAtTopOfMesh(stack, steamPrefab, "PowerPlantSteam", new Vector3(0f, 0.04f, 0f));
                }
            }

            // 4. Electrolyser_H2Generator GeneratorExhaust steam + sparks
            var gen = GameObject.Find("Electrolyser_H2Generator");
            if (gen != null)
            {
                Transform exhaust = gen.transform.Find("Visuals/GeneratorExhaust");
                if (exhaust != null)
                {
                    AttachAtTopOfMesh(exhaust, genExhaustPrefab, "GeneratorExhaustSteam", new Vector3(0f, 0.04f, 0f));
                }
            }

            // 5. ExplorationHover WheelDust (repositioned under actual wheels)
            var hover = GameObject.Find("ExplorationHover");
            if (hover != null)
            {
                RemoveChildIfExists(hover.transform, "WheelDust");
                var wd = (GameObject)PrefabUtility.InstantiatePrefab(wheelDustPrefab, hover.transform);
                wd.name = "WheelDust";
                wd.transform.localPosition = new Vector3(0.01f, 0.08f, -0.05f);
                wd.transform.localRotation = Quaternion.identity;

                ParticleSystem primaryDust = wd.transform.Find("Dust").GetComponent<ParticleSystem>();
                var rm = hover.GetComponent<RoverMissionDriver>();
                if (rm != null)
                {
                    var so = new SerializedObject(rm);
                    so.FindProperty("wheelDust").objectReferenceValue = primaryDust;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                var rd = hover.GetComponent<RoverDriver>();
                if (rd != null)
                {
                    var so = new SerializedObject(rd);
                    so.FindProperty("wheelDust").objectReferenceValue = primaryDust;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // 6. Walking astronauts (MoonWalker) LandingDust
            foreach (var mw in Object.FindObjectsByType<MoonWalker>(FindObjectsSortMode.None))
            {
                Transform existing = mw.transform.Find("LandingDust");
                if (existing != null)
                {
                    ConfigureWalkingDustPS(existing.gameObject, mats.dust);
                }
                else
                {
                    var ld = (GameObject)PrefabUtility.InstantiatePrefab(walkingDustPrefab, mw.transform);
                    ld.name = "LandingDust";
                    ld.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    existing = ld.transform;
                }
                var so = new SerializedObject(mw);
                so.FindProperty("landingDust").objectReferenceValue = existing.GetComponent<ParticleSystem>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AttachAtTopOfMesh(Transform target, GameObject prefab, string childName, Vector3 extraLocalOffset)
        {
            RemoveChildIfExists(target, childName);
            Vector3 localTop = Vector3.up * 1.0f;
            var mf = target.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Bounds b = mf.sharedMesh.bounds;
                localTop = new Vector3(b.center.x, b.max.y, b.center.z);
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target);
            go.name = childName;
            go.transform.localPosition = localTop + extraLocalOffset;
            go.transform.localRotation = Quaternion.identity;
        }

        private static void RemoveChildIfExists(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }

        private static void SetPSArray(SerializedProperty prop, List<ParticleSystem> list)
        {
            prop.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        private static GameObject SaveAsPrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static ParticleSystem ConfigureBasePS(GameObject go, Material mat, bool loop, bool playOnAwake, bool worldSpace, int maxParticles)
        {
            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = loop;
            main.playOnAwake = playOnAwake;
            main.simulationSpace = worldSpace ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = maxParticles;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        private static void SetSizeOverLifetime(ParticleSystem ps, float startScale, float endScale)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0f, startScale), new Keyframe(1f, endScale));
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }

        private static void SetAlphaFade(ParticleSystem ps, float fadeInEnd, float fadeOutStart)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, fadeInEnd), new GradientAlphaKey(1f, fadeOutStart), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        private static void SetNoise(ParticleSystem ps, float strength, float frequency)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(strength);
            noise.frequency = frequency;
            noise.scrollSpeed = 0.4f;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
