using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class LevelBuilder
{
    [MenuItem("Tools/Build Level 1 Scene")]
    public static void BuildScene()
    {
        try
        {
            if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
            if (!Directory.Exists("Assets/Animations")) Directory.CreateDirectory("Assets/Animations");

            AssetDatabase.Refresh();
            ConfigureSpriteImporters();
            ConfigureAudioImporters();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Shader particleShader = Shader.Find("Sprites/Default");
            if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

            Texture2D dustTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Particles/DustParticle.png");
            Texture2D sparkleTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Particles/SparkleParticle.png");

            Material dustMat = new Material(particleShader);
            if (dustTex != null) dustMat.mainTexture = dustTex;
            AssetDatabase.CreateAsset(dustMat, "Assets/Materials/ParticleDustMat.mat");

            Material sparkleMat = new Material(particleShader);
            if (sparkleTex != null) sparkleMat.mainTexture = sparkleTex;
            AssetDatabase.CreateAsset(sparkleMat, "Assets/Materials/ParticleSparkleMat.mat");

            // 1. Camera & Audio Listener (disable unused post-processing to keep player builds clean)
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.backgroundColor = new Color(0.03f, 0.05f, 0.11f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            UniversalAdditionalCameraData urpCamData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCamData.renderPostProcessing = false;
            camGo.AddComponent<AudioListener>();
            CameraController camCtrl = camGo.AddComponent<CameraController>();
            camGo.transform.position = new Vector3(0f, 1.5f, -10f);

            // 2. Global Managers
            GameObject mgrGo = new GameObject("GameManagers");
            mgrGo.AddComponent<GameManager>();
            UIManager uim = mgrGo.AddComponent<UIManager>();
            AudioManager am = mgrGo.AddComponent<AudioManager>();
            ParticleFXManager pfx = mgrGo.AddComponent<ParticleFXManager>();

            SerializedObject pfxSo = new SerializedObject(pfx);
            pfxSo.FindProperty("dustTexture").objectReferenceValue = dustTex;
            pfxSo.FindProperty("sparkleTexture").objectReferenceValue = sparkleTex;
            pfxSo.ApplyModifiedProperties();

            SerializedObject uiSo = new SerializedObject(uim);
            uiSo.FindProperty("heartFullTex").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/Heart.png");
            uiSo.FindProperty("crystalIconTex").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Collectibles/AetherCrystal.png");
            uiSo.FindProperty("dashIconTex").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/UI/DashIcon.png");
            uiSo.ApplyModifiedProperties();

            SerializedObject amSo = new SerializedObject(am);
            amSo.FindProperty("jumpClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_jump.wav");
            amSo.FindProperty("landClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_land.wav");
            amSo.FindProperty("dashClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_dash.wav");
            amSo.FindProperty("collectClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_collect.wav");
            amSo.FindProperty("hurtClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_hurt.wav");
            amSo.FindProperty("checkpointClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_checkpoint.wav");
            amSo.FindProperty("winClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_win.wav");
            amSo.FindProperty("musicLoopClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/music_loop.wav");
            amSo.ApplyModifiedProperties();

            // 3. Multi-Layer Parallax Background + Structural Support Arches
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Cavern_Background.png");
            Sprite wallArchSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Wall_AncientStone.png");

            GameObject bgParent = new GameObject("Backgrounds");
            if (bgSprite != null)
            {
                GameObject farLayer = new GameObject("Parallax_Far");
                farLayer.transform.SetParent(bgParent.transform);
                ParallaxLayer farPl = farLayer.AddComponent<ParallaxLayer>();
                SerializedObject farPlSo = new SerializedObject(farPl);
                farPlSo.FindProperty("parallaxFactor").vector2Value = new Vector2(0.70f, 0.35f);
                farPlSo.ApplyModifiedProperties();

                for (int i = 0; i < 11; i++)
                {
                    GameObject bgObj = new GameObject($"Cavern_BG_{i}");
                    bgObj.transform.SetParent(farLayer.transform);
                    bgObj.transform.position = new Vector3(-16f + (i * 27.5f), 2.8f, 20f);
                    bgObj.transform.localScale = new Vector3(2.0f, 2.0f, 1f);
                    SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                    sr.sprite = bgSprite;
                    sr.flipX = (i % 2 == 1);
                    sr.sortingOrder = -60;
                    sr.color = new Color(0.62f, 0.76f, 0.90f, 1f);
                }
            }

            if (wallArchSprite != null)
            {
                GameObject archesParent = new GameObject("StructuralSupportArches");
                archesParent.transform.SetParent(bgParent.transform);

                Vector3[] archPositions =
                {
                    new Vector3(1f, -2.2f, 0f),
                    new Vector3(76f, -0.6f, 0f),
                    new Vector3(102f, -0.6f, 0f),
                    new Vector3(148f, 0.6f, 0f),
                    new Vector3(197f, 0.4f, 0f)
                };
                for (int i = 0; i < archPositions.Length; i++)
                {
                    GameObject archObj = new GameObject($"SupportArch_{i}");
                    archObj.transform.SetParent(archesParent.transform);
                    archObj.transform.position = archPositions[i];
                    archObj.transform.localScale = new Vector3(1.8f, 1.8f, 1f);
                    SpriteRenderer sr = archObj.AddComponent<SpriteRenderer>();
                    sr.sprite = wallArchSprite;
                    sr.sortingOrder = -5;
                    sr.color = new Color(0.75f, 0.82f, 0.90f, 1f);
                }
            }

            // 4. Player Character ("Kip") + True Multi-Frame Unity AnimatorController
            Sprite[] idleFrames = LoadFrames("Assets/Sprites/Characters/Kip_Idle_{0}.png", 4);
            Sprite[] runFrames = LoadFrames("Assets/Sprites/Characters/Kip_Run_{0}.png", 6);
            Sprite[] jumpFrames = LoadFrames("Assets/Sprites/Characters/Kip_Jump_{0}.png", 3);
            Sprite[] fallFrames = LoadFrames("Assets/Sprites/Characters/Kip_Fall_{0}.png", 2);
            Sprite[] dashFrames = LoadFrames("Assets/Sprites/Characters/Kip_Dash_{0}.png", 3);
            Sprite[] hurtFrames = LoadFrames("Assets/Sprites/Characters/Kip_Hurt_{0}.png", 2);

            AnimatorController kipController = CreateKipAnimatorController(idleFrames, runFrames, jumpFrames, fallFrames, hurtFrames, dashFrames);

            GameObject playerGo = new GameObject("Player_Kip");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 0.90f, 0f);

            Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
            playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            playerRb.freezeRotation = true;
            playerRb.gravityScale = 3f;

            BoxCollider2D playerCol = playerGo.AddComponent<BoxCollider2D>();
            playerCol.size = new Vector2(0.72f, 1.56f);
            playerCol.offset = Vector2.zero;

            GameObject visualsGo = new GameObject("Visuals");
            visualsGo.transform.SetParent(playerGo.transform);
            visualsGo.transform.localScale = new Vector3(0.70f, 0.70f, 1f);
            visualsGo.transform.localPosition = new Vector3(0f, 0.06f, 0f);

            SpriteRenderer playerSr = visualsGo.AddComponent<SpriteRenderer>();
            playerSr.sprite = idleFrames[0];
            playerSr.sortingOrder = 10;

            Animator playerAnim = visualsGo.AddComponent<Animator>();
            playerAnim.runtimeAnimatorController = kipController;
            playerAnim.applyRootMotion = false;

            GameObject groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(playerGo.transform);
            groundCheck.transform.localPosition = new Vector3(0f, -0.80f, 0f);

            PlayerController pc = playerGo.AddComponent<PlayerController>();
            SerializedObject pcSo = new SerializedObject(pc);
            pcSo.FindProperty("groundCheckPoint").objectReferenceValue = groundCheck.transform;
            pcSo.FindProperty("groundLayer").intValue = ~0;
            pcSo.FindProperty("visualTransform").objectReferenceValue = visualsGo.transform;
            pcSo.FindProperty("spriteRenderer").objectReferenceValue = playerSr;
            pcSo.FindProperty("animator").objectReferenceValue = playerAnim;
            pcSo.FindProperty("idleSprite").objectReferenceValue = idleFrames[0];
            pcSo.FindProperty("runSprite").objectReferenceValue = runFrames[0];
            pcSo.FindProperty("jumpSprite").objectReferenceValue = jumpFrames[0];
            pcSo.FindProperty("fallSprite").objectReferenceValue = fallFrames[0];
            pcSo.FindProperty("hurtSprite").objectReferenceValue = hurtFrames[0];
            pcSo.FindProperty("dashSprite").objectReferenceValue = dashFrames[0];
            pcSo.ApplyModifiedProperties();

            PlayerHealth ph = playerGo.AddComponent<PlayerHealth>();
            SerializedObject phSo = new SerializedObject(ph);
            phSo.FindProperty("spriteRenderer").objectReferenceValue = playerSr;
            phSo.ApplyModifiedProperties();

            camCtrl.SetTarget(playerGo.transform);

            // 5. Level Containers & Sprites
            GameObject levelParent = new GameObject("LevelGeometry");
            GameObject platformsParent = new GameObject("Platforms");
            platformsParent.transform.SetParent(levelParent.transform);
            GameObject hazardsParent = new GameObject("Hazards");
            hazardsParent.transform.SetParent(levelParent.transform);
            GameObject enemiesParent = new GameObject("Enemies");
            enemiesParent.transform.SetParent(levelParent.transform);
            GameObject collectiblesParent = new GameObject("Collectibles");
            collectiblesParent.transform.SetParent(levelParent.transform);
            GameObject propsParent = new GameObject("DecorProps");
            propsParent.transform.SetParent(levelParent.transform);
            GameObject ambienceParent = new GameObject("Ambience");
            ambienceParent.transform.SetParent(levelParent.transform);
            GameObject tutorialsParent = new GameObject("Tutorials");
            tutorialsParent.transform.SetParent(levelParent.transform);

            Sprite mossLeft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_Moss_Left.png");
            Sprite mossMid = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_Moss_Mid.png");
            Sprite mossRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_Moss_Right.png");
            Sprite metalConduit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_MetalConduit.png");
            Sprite floatingLedge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_FloatingLedge.png");

            Sprite shroomSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Prop_GlowMushrooms.png");
            Sprite clusterSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Prop_CrystalCluster.png");
            Sprite lanternSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Prop_LanternSign.png");
            Sprite grateSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Prop_BrassGrate.png");

            Sprite crystalSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/AetherCrystal.png");
            Sprite beaconInactiveSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/ConduitBeacon_Inactive.png");
            Sprite beaconActiveSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/ConduitBeacon_Active.png");
            Sprite crawlerSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/ScrapCrawler.png");
            Sprite sentrySpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/BuzzSentry.png");
            Sprite spikesSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/TeslaSpikes.png");
            Sprite sawSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/Sawblade.png");
            Sprite acidSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/AcidPool_Surface.png");
            Sprite stalactiteSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/Acid_Stalactite.png");
            Sprite goalSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/CoreReactorGoal.png");

            // Segmented 3-piece mossy stone platform builder (Left Cap + Center Tiles + Right Cap)
            System.Func<float, float, int, string, GameObject> createStonePlatform = (x, y, tileCount, name) =>
            {
                float tileW = 1.2f;
                float totalW = tileCount * tileW;
                GameObject plat = new GameObject(name);
                plat.transform.SetParent(platformsParent.transform);
                plat.transform.position = new Vector3(x, y, 0f);
                BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
                col.size = new Vector2(totalW, tileW);

                float startX = -totalW * 0.5f + tileW * 0.5f;
                for (int i = 0; i < tileCount; i++)
                {
                    GameObject tile = new GameObject($"Tile_{i}");
                    tile.transform.SetParent(plat.transform);
                    tile.transform.localPosition = new Vector3(startX + i * tileW, 0f, 0f);
                    SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                    if (tileCount == 1) sr.sprite = mossMid;
                    else if (i == 0) sr.sprite = mossLeft;
                    else if (i == tileCount - 1) sr.sprite = mossRight;
                    else sr.sprite = mossMid;
                    sr.sortingOrder = 0;
                }
                return plat;
            };

            System.Func<float, float, float, float, Sprite, string, GameObject> createTiledPlatform = (x, y, w, h, sprite, name) =>
            {
                GameObject plat = new GameObject(name);
                plat.transform.SetParent(platformsParent.transform);
                plat.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
                sr.sprite = sprite ?? floatingLedge;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(w, h);
                sr.sortingOrder = 0;
                BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
                col.size = new Vector2(w, h);
                return plat;
            };

            System.Action<float, float, float, Vector3, float, float> createMovingPlatform = (x, y, w, moveOffset, speed, phase) =>
            {
                GameObject mpGo = createTiledPlatform(x, y, w, 1.0f, metalConduit, "MovingConduitPlatform");
                MovingPlatform mp = mpGo.AddComponent<MovingPlatform>();
                SerializedObject mpSo = new SerializedObject(mp);
                mpSo.FindProperty("moveOffset").vector3Value = moveOffset;
                mpSo.FindProperty("speed").floatValue = speed;
                mpSo.FindProperty("phaseOffset").floatValue = phase;
                mpSo.ApplyModifiedProperties();
            };

            System.Action<float, float, Vector3, float, float> createSawblade = (x, y, moveOffset, speed, phase) =>
            {
                GameObject saw = new GameObject("SawbladeTrap");
                saw.transform.SetParent(hazardsParent.transform);
                saw.transform.position = new Vector3(x, y, 0f);
                saw.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                SpriteRenderer sr = saw.AddComponent<SpriteRenderer>();
                sr.sprite = sawSpr;
                sr.sortingOrder = 6;
                CircleCollider2D col = saw.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.52f;
                MovingTrap mt = saw.AddComponent<MovingTrap>();
                SerializedObject mtSo = new SerializedObject(mt);
                mtSo.FindProperty("moveOffset").vector3Value = moveOffset;
                mtSo.FindProperty("moveSpeed").floatValue = speed;
                mtSo.FindProperty("phaseOffset").floatValue = phase;
                mtSo.ApplyModifiedProperties();
            };

            System.Action<float, float, Sprite, float, int> createProp = (x, y, spr, scale, order) =>
            {
                if (spr == null) return;
                GameObject prop = new GameObject(spr.name);
                prop.transform.SetParent(propsParent.transform);
                prop.transform.position = new Vector3(x, y, 0f);
                prop.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingOrder = order;
            };

            System.Action<float, float> createCrystal = (x, y) =>
            {
                GameObject crystal = new GameObject("AetherCrystal");
                crystal.transform.SetParent(collectiblesParent.transform);
                crystal.transform.position = new Vector3(x, y, 0f);
                crystal.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                SpriteRenderer sr = crystal.AddComponent<SpriteRenderer>();
                sr.sprite = crystalSpr;
                sr.sortingOrder = 4;
                CircleCollider2D col = crystal.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.7f;
                crystal.AddComponent<Collectible>();
            };

            System.Action<float, float, float> createSpikes = (x, y, w) =>
            {
                GameObject spike = new GameObject("TeslaSpikes");
                spike.transform.SetParent(hazardsParent.transform);
                spike.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
                sr.sprite = spikesSpr;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(w, 0.80f);
                sr.sortingOrder = 2;
                BoxCollider2D col = spike.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(w - 0.15f, 0.55f);
                col.offset = new Vector2(0f, -0.10f);
                spike.AddComponent<Hazard>();
            };

            System.Action<float, float, float> createAcidPool = (x, y, w) =>
            {
                GameObject pool = new GameObject("AcidPool");
                pool.transform.SetParent(hazardsParent.transform);
                pool.transform.position = new Vector3(x, y, 0f);
                SpriteRenderer sr = pool.AddComponent<SpriteRenderer>();
                sr.sprite = acidSpr;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(w, 1.20f);
                sr.sortingOrder = -1;
                BoxCollider2D col = pool.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(w - 0.1f, 0.85f);
                col.offset = new Vector2(0f, -0.15f);
                pool.AddComponent<Hazard>();

                CreateAmbientParticles(pool.transform, "AcidBubbles", new Vector3(0f, 0.35f, 0f), new Vector3(w * 0.8f, 0.2f, 0.1f),
                    new Color(0.15f, 1f, 0.85f, 0.85f), 8f, 1.1f, 0.22f, 1.2f, dustMat, 4);
            };

            System.Action<float, float, float, float> createCrawler = (x, y, dist, speed) =>
            {
                GameObject enemy = new GameObject("ScrapCrawler");
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.transform.position = new Vector3(x, y, 0f);
                enemy.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
                sr.sprite = crawlerSpr;
                sr.sortingOrder = 5;
                BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.7f, 1.3f);
                PatrolEnemy pe = enemy.AddComponent<PatrolEnemy>();
                SerializedObject peSo = new SerializedObject(pe);
                peSo.FindProperty("patrolDistance").floatValue = dist;
                peSo.FindProperty("moveSpeed").floatValue = speed;
                peSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
                peSo.ApplyModifiedProperties();
            };

            System.Action<float, float, float, float> createSentry = (x, y, hRange, hoverAmp) =>
            {
                GameObject enemy = new GameObject("BuzzSentry");
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.transform.position = new Vector3(x, y, 0f);
                enemy.transform.localScale = new Vector3(0.52f, 0.52f, 1f);
                SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
                sr.sprite = sentrySpr;
                sr.sortingOrder = 5;
                CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.85f;
                FlyingEnemy fe = enemy.AddComponent<FlyingEnemy>();
                SerializedObject feSo = new SerializedObject(fe);
                feSo.FindProperty("horizontalRange").floatValue = hRange;
                feSo.FindProperty("hoverAmplitude").floatValue = hoverAmp;
                feSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
                feSo.ApplyModifiedProperties();
            };

            System.Action<float, float> createStalactite = (x, y) =>
            {
                createStonePlatform(x, y + 1.05f, 3, "StalactiteCeilingRock");
                GameObject stal = new GameObject("AcidStalactite");
                stal.transform.SetParent(hazardsParent.transform);
                stal.transform.position = new Vector3(x, y, 0f);
                stal.transform.localScale = new Vector3(0.90f, 1.0f, 1f);
                SpriteRenderer sr = stal.AddComponent<SpriteRenderer>();
                sr.sprite = stalactiteSpr;
                sr.sortingOrder = 3;
                BoxCollider2D col = stal.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.85f, 1.05f);
                stal.AddComponent<FallingHazard>();
            };

            System.Action<float, float, string> createCheckpoint = (x, y, name) =>
            {
                GameObject beaconObj = new GameObject(name);
                beaconObj.transform.SetParent(levelParent.transform);
                beaconObj.transform.position = new Vector3(x, y, 0f);
                beaconObj.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                SpriteRenderer beaconSr = beaconObj.AddComponent<SpriteRenderer>();
                beaconSr.sprite = beaconInactiveSpr;
                beaconSr.sortingOrder = 2;
                BoxCollider2D beaconCol = beaconObj.AddComponent<BoxCollider2D>();
                beaconCol.isTrigger = true;
                beaconCol.size = new Vector2(1.3f, 1.9f);

                ParticleSystem auraPS = CreateAmbientParticles(beaconObj.transform, "BeaconAura", Vector3.zero, new Vector3(1.1f, 1.8f, 0.1f),
                    new Color(0f, 1f, 1f, 0.9f), 14f, 1.2f, 0.26f, 0.9f, sparkleMat, 6, false);

                Checkpoint cp = beaconObj.AddComponent<Checkpoint>();
                SerializedObject cpSo = new SerializedObject(cp);
                cpSo.FindProperty("spriteRenderer").objectReferenceValue = beaconSr;
                cpSo.FindProperty("inactiveSprite").objectReferenceValue = beaconInactiveSpr;
                cpSo.FindProperty("activeSprite").objectReferenceValue = beaconActiveSpr;
                cpSo.FindProperty("beaconAuraPS").objectReferenceValue = auraPS;
                cpSo.ApplyModifiedProperties();
            };

            System.Action<float, float, string, string> createTutorial = (x, y, title, body) =>
            {
                createProp(x - 1.4f, y - 0.36f, lanternSpr, 0.9f, -1);
                GameObject tut = new GameObject($"Tutorial_{title}");
                tut.transform.SetParent(tutorialsParent.transform);
                tut.transform.position = new Vector3(x, y, 0f);
                TutorialSign ts = tut.AddComponent<TutorialSign>();
                SerializedObject tsSo = new SerializedObject(ts);
                tsSo.FindProperty("title").stringValue = title;
                tsSo.FindProperty("instructionText").stringValue = body;
                tsSo.ApplyModifiedProperties();
            };

            // ================= 8-SECTION POLISHED LEVEL LAYOUT =================

            // SECTION 1 (X: -5 to 21): Safe Movement & Jump Intro + Glowing Cavern Flora
            createStonePlatform(1f, -0.6f, 10, "SpawnPlatform"); // topY = 0.0
            createTutorial(0f, 1.0f, "MOVEMENT & JUMP", "Use [A / D] or Arrow Keys to move, and [SPACE / W] to jump between ledges.");
            createProp(-3.8f, 0.42f, shroomSpr, 0.9f, 1);
            createProp(5.5f, 0.42f, clusterSpr, 0.9f, 1);

            createStonePlatform(11f, 0.4f, 4, "StepPlat1"); // topY = 1.0
            createProp(9.6f, 1.42f, shroomSpr, 0.75f, 1);
            createCrystal(11f, 1.9f);

            createStonePlatform(18f, 1.2f, 4, "StepPlat2"); // topY = 1.8
            createProp(19.3f, 2.22f, clusterSpr, 0.8f, 1);
            createCrystal(18f, 2.7f);

            // SECTION 2 (X: 21 to 47): Signature Move - Energy Air Dash Intro
            createTutorial(18.5f, 2.8f, "SIGNATURE MOVE: AIR DASH", "Press [SHIFT] or [J] mid-jump to Air Dash across wide chasms!");
            createTiledPlatform(33f, 1.5f, 9f, 1.0f, metalConduit, "DashLandingPlat"); // topY = 2.0
            createProp(30f, 0.45f, grateSpr, 1.0f, -2);
            createProp(36f, 0.45f, grateSpr, 1.0f, -2);
            createCrystal(25f, 3.1f);
            createCrystal(33f, 2.9f);

            createStonePlatform(44f, 2.4f, 5, "DashClimbPlat"); // topY = 3.0
            createProp(45.6f, 3.42f, shroomSpr, 0.8f, 1);
            createCrystal(44f, 3.9f);

            // SECTION 3 (X: 48 to 71): Environmental Hazards (Tesla Spikes, Sunken Acid Pool & Sawblade)
            createTutorial(44.5f, 4.0f, "CAVERN HAZARDS", "Dodge Tesla Spikes, Sawblades, and Acid Pools! Use upper mossy ledges to cross safely.");
            createStonePlatform(53.0f, -1.0f, 5, "TrenchLeft");     // topY = -0.4
            createSpikes(53.0f, 0.0f, 3.2f);
            createStonePlatform(58.5f, -1.9f, 6, "AcidPitFloor");   // topY = -1.3
            createAcidPool(58.5f, -0.95f, 5.4f);
            createStonePlatform(64.0f, -1.0f, 5, "TrenchRight");    // topY = -0.4
            createProp(65.4f, 0.02f, clusterSpr, 0.85f, 1);

            createTiledPlatform(53.5f, 2.2f, 4.0f, 1.0f, floatingLedge, "SpikeBypass1"); // topY = 2.7
            createTiledPlatform(61.5f, 3.0f, 4.0f, 1.0f, floatingLedge, "SpikeBypass2"); // topY = 3.5
            createSawblade(57.5f, 1.6f, new Vector3(0f, 2.4f, 0f), 2.1f, 0f);           // vertical saw between ledges!
            createCrystal(53.5f, 3.6f);
            createCrystal(61.5f, 4.4f);

            // SECTION 4 (X: 71 to 92): Checkpoint #1 & Dual Moving Conduit Platforms
            createStonePlatform(76f, 1.0f, 8, "CheckpointPlat1");  // topY = 1.6
            createTutorial(74f, 2.6f, "CONDUIT CHECKPOINT & MOVING FLOORS", "Ignite Conduit Beacons for auto-respawn, and ride the Moving Conduit Floors!");
            createCheckpoint(76f, 2.45f, "Checkpoint_ConduitBeacon_1");
            createProp(72.5f, 2.02f, shroomSpr, 0.85f, 1);
            createCrystal(79.5f, 2.5f);

            createMovingPlatform(83.5f, 1.6f, 3.0f, new Vector3(4.5f, 0.4f, 0f), 1.6f, 0f);
            createMovingPlatform(89.5f, 2.0f, 3.0f, new Vector3(4.0f, -0.4f, 0f), 1.6f, 1.8f);
            createCrystal(86.5f, 3.3f);
            createCrystal(91.5f, 3.4f);

            // SECTION 5 (X: 94 to 121): Scrap Crawlers & Patrolling Sawblade Bridge
            createStonePlatform(103f, 1.0f, 13, "CrawlerArena"); // topY = 1.6
            createTutorial(97f, 2.6f, "SCRAP CRAWLER DRONES", "Jump onto Scrap Crawlers or Air Dash through them to defeat them!");
            createProp(96.5f, 2.02f, clusterSpr, 0.8f, 1);
            createCrawler(100.5f, 2.04f, 3.5f, 2.3f);
            createCrawler(106.5f, 2.04f, 3.0f, 2.8f);
            createCrystal(100f, 2.9f);
            createCrystal(106.5f, 2.9f);

            createStonePlatform(116f, 1.8f, 6, "ArenaBridge"); // topY = 2.4
            createSpikes(114.5f, 2.8f, 1.6f);
            createSawblade(117.5f, 2.9f, new Vector3(0f, 2.2f, 0f), 2.4f, 0.7f);
            createCrystal(116f, 4.4f);

            // SECTION 6 (X: 121 to 145): Aerial Sentries, Moving Lift & Falling Stalactites
            createTiledPlatform(124f, 2.0f, 4.0f, 1.0f, floatingLedge, "SentryLedge1"); // topY = 2.5
            createTutorial(123.5f, 3.5f, "AERIAL SENTRIES & STALACTITES", "Watch above for falling Acid Stalactites, and stomp or dash Buzz Sentries!");
            createSentry(129f, 4.1f, 2.0f, 1.1f);
            createCrystal(129f, 5.2f);

            createMovingPlatform(133f, 2.6f, 3.0f, new Vector3(0f, 2.0f, 0f), 1.7f, 0f); // vertical elevator floor!
            createStalactite(133f, 7.8f);

            createTiledPlatform(139f, 3.2f, 4.0f, 1.0f, floatingLedge, "SentryLedge2"); // topY = 3.7
            createSentry(142.5f, 4.9f, 2.2f, 1.1f);
            createCrystal(142.5f, 6.0f);
            createStalactite(139f, 8.2f);

            // SECTION 7 (X: 145 to 179): Checkpoint #2 & Reactor Gauntlet (More Enemies & Moving Saw Traps)
            createStonePlatform(149f, 2.2f, 8, "CheckpointPlat2"); // topY = 2.8
            createCheckpoint(149f, 3.65f, "Checkpoint_ConduitBeacon_2");
            createProp(152.5f, 3.22f, shroomSpr, 0.85f, 1);

            createMovingPlatform(156.5f, 2.6f, 3.0f, new Vector3(4.5f, 1.2f, 0f), 1.7f, 0f);
            createCrystal(160f, 4.6f);

            createStonePlatform(171f, 3.2f, 12, "GauntletBridge"); // topY = 3.8
            createSpikes(166.5f, 4.2f, 3.2f);
            createSawblade(170.5f, 4.3f, new Vector3(5.0f, 0f, 0f), 2.0f, 0f); // horizontal patrolling sawblade!
            createCrawler(174.5f, 4.24f, 2.8f, 3.0f);
            createStalactite(174f, 8.8f);
            createCrystal(171.5f, 5.6f);
            createCrystal(175.5f, 4.7f);

            // SECTION 8 (X: 180 to 212): Final Climax & Ancient Core Reactor Goal
            createTiledPlatform(183f, 3.5f, 4.0f, 1.0f, floatingLedge, "FinalLedge"); // topY = 4.0
            createTutorial(182f, 5.0f, "ANCIENT CORE REACTOR", "Dash past the final Sentries and step into the Core Reactor Portal to win!");
            createSentry(187.5f, 5.4f, 2.0f, 1.0f);
            createCrystal(187.5f, 6.5f);

            createStonePlatform(197f, 2.0f, 12, "ReactorFloor"); // topY = 2.6
            createCrawler(193f, 3.04f, 2.6f, 2.8f);
            createProp(190.8f, 3.02f, clusterSpr, 0.9f, 1);
            createProp(202.8f, 3.02f, shroomSpr, 0.9f, 1);
            createCrystal(193f, 4.2f);

            GameObject goalObj = new GameObject("CoreReactorGoal");
            goalObj.transform.SetParent(levelParent.transform);
            goalObj.transform.position = new Vector3(199f, 4.325f, 0f);
            goalObj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
            SpriteRenderer goalSr = goalObj.AddComponent<SpriteRenderer>();
            goalSr.sprite = goalSpr;
            goalSr.sortingOrder = 2;
            BoxCollider2D goalCol = goalObj.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector2(1.8f, 2.8f);
            goalObj.AddComponent<GoalZone>();

            CreateAmbientParticles(goalObj.transform, "ReactorVortexParticles", Vector3.zero, new Vector3(1.6f, 2.2f, 0.1f),
                new Color(0.25f, 1f, 0.9f, 0.9f), 18f, 1.4f, 0.30f, 0.8f, sparkleMat, 6);

            // Ambient Cavern Bioluminescent Spores across the level
            for (int s = 0; s < 7; s++)
            {
                float sx = 10f + s * 30f;
                CreateAmbientParticles(ambienceParent.transform, $"CavernSpores_{s}", new Vector3(sx, 2.5f, 0f), new Vector3(26f, 8f, 1f),
                    new Color(0.2f, 0.95f, 1f, 0.45f), 8f, 3.5f, 0.18f, 0.45f, dustMat, 1, true, -0.04f);
            }

            // Abyss Kill Floor below entire level
            GameObject abyss = new GameObject("AbyssKillFloor");
            abyss.transform.SetParent(hazardsParent.transform);
            abyss.transform.position = new Vector3(100f, -7.5f, 0f);
            BoxCollider2D abyssCol = abyss.AddComponent<BoxCollider2D>();
            abyssCol.isTrigger = true;
            abyssCol.size = new Vector2(260f, 4f);
            Hazard abyssHaz = abyss.AddComponent<Hazard>();
            SerializedObject abyssSo = new SerializedObject(abyssHaz);
            abyssSo.FindProperty("isInstantKill").boolValue = true;
            abyssSo.ApplyModifiedProperties();

            string scenePath = "Assets/Scenes/Level1.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            Debug.Log($"Level 1 Scene rebuilt and saved to {scenePath}!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in BuildScene: " + ex);
            throw;
        }
    }

    private static Sprite[] LoadFrames(string formatPath, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(string.Format(formatPath, i));
        }
        return frames;
    }

    private static ParticleSystem CreateAmbientParticles(Transform parent, string name, Vector3 localPos, Vector3 boxSize,
        Color color, float rateOverTime, float lifetime, float size, float speed, Material mat, int sortingOrder,
        bool playOnAwake = true, float gravity = -0.15f)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent);
        psGo.transform.localPosition = localPos;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = playOnAwake;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.prewarm = playOnAwake;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rateOverTime;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = boxSize;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.material = mat;

        return ps;
    }

    private static AnimatorController CreateKipAnimatorController(
        Sprite[] idleFrames, Sprite[] runFrames, Sprite[] jumpFrames, Sprite[] fallFrames, Sprite[] hurtFrames, Sprite[] dashFrames)
    {
        string ctrlPath = "Assets/Animations/KipAnimator.controller";
        if (File.Exists(ctrlPath)) AssetDatabase.DeleteAsset(ctrlPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsDashing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsHurt", AnimatorControllerParameterType.Bool);

        AnimationClip idleClip = CreateSpriteClip("Assets/Animations/Kip_Idle.anim", idleFrames, 5f, true);
        AnimationClip runClip = CreateSpriteClip("Assets/Animations/Kip_Run.anim", runFrames, 14f, true);
        AnimationClip jumpClip = CreateSpriteClip("Assets/Animations/Kip_Jump.anim", jumpFrames, 12f, true);
        AnimationClip fallClip = CreateSpriteClip("Assets/Animations/Kip_Fall.anim", fallFrames, 10f, true);
        AnimationClip hurtClip = CreateSpriteClip("Assets/Animations/Kip_Hurt.anim", hurtFrames, 12f, true);
        AnimationClip dashClip = CreateSpriteClip("Assets/Animations/Kip_Dash.anim", dashFrames, 15f, true);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState sIdle = sm.AddState("Idle"); sIdle.motion = idleClip;
        AnimatorState sRun = sm.AddState("Run"); sRun.motion = runClip;
        AnimatorState sJump = sm.AddState("Jump"); sJump.motion = jumpClip;
        AnimatorState sFall = sm.AddState("Fall"); sFall.motion = fallClip;
        AnimatorState sHurt = sm.AddState("Hurt"); sHurt.motion = hurtClip;
        AnimatorState sDash = sm.AddState("Dash"); sDash.motion = dashClip;
        sm.defaultState = sIdle;

        System.Func<AnimatorState, AnimatorState, AnimatorStateTransition> addInstant = (from, to) =>
        {
            var t = from.AddTransition(to);
            t.hasExitTime = false;
            t.duration = 0f;
            return t;
        };

        var tAnyHurt = sm.AddAnyStateTransition(sHurt);
        tAnyHurt.hasExitTime = false; tAnyHurt.duration = 0f; tAnyHurt.canTransitionToSelf = false;
        tAnyHurt.AddCondition(AnimatorConditionMode.If, 0f, "IsHurt");

        var tAnyDash = sm.AddAnyStateTransition(sDash);
        tAnyDash.hasExitTime = false; tAnyDash.duration = 0f; tAnyDash.canTransitionToSelf = false;
        tAnyDash.AddCondition(AnimatorConditionMode.If, 0f, "IsDashing");
        tAnyDash.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsHurt");

        var tHurtIdle = addInstant(sHurt, sIdle);
        tHurtIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsHurt");

        var tDashIdle = addInstant(sDash, sIdle);
        tDashIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDashing");
        tDashIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");

        var tDashFall = addInstant(sDash, sFall);
        tDashFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDashing");
        tDashFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");

        var tIdleRun = addInstant(sIdle, sRun);
        tIdleRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        tIdleRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var tRunIdle = addInstant(sRun, sIdle);
        tRunIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        tRunIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var tIdleJump = addInstant(sIdle, sJump);
        tIdleJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        tIdleJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

        var tRunJump = addInstant(sRun, sJump);
        tRunJump.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        tRunJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalVelocity");

        var tIdleFall = addInstant(sIdle, sFall);
        tIdleFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        tIdleFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

        var tRunFall = addInstant(sRun, sFall);
        tRunFall.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsGrounded");
        tRunFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

        var tJumpFall = addInstant(sJump, sFall);
        tJumpFall.AddCondition(AnimatorConditionMode.Less, 0.1f, "VerticalVelocity");

        var tJumpIdle = addInstant(sJump, sIdle);
        tJumpIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        tJumpIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var tJumpRun = addInstant(sJump, sRun);
        tJumpRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        tJumpRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        var tFallIdle = addInstant(sFall, sIdle);
        tFallIdle.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        tFallIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var tFallRun = addInstant(sFall, sRun);
        tFallRun.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        tFallRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static AnimationClip CreateSpriteClip(string path, Sprite[] sprites, float frameRate, bool loop)
    {
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AnimationClip clip = new AnimationClip { frameRate = frameRate };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };

        ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void ConfigureSpriteImporters()
    {
        string[] spriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Sprites" });
        foreach (string guid in spriteGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool isParticle = path.Contains("/Particles/") || path.Contains("\\Particles\\");
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = isParticle ? FilterMode.Bilinear : FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;

                TextureImporterSettings texSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(texSettings);
                texSettings.spriteMode = (int)SpriteImportMode.Single;
                texSettings.spriteMeshType = SpriteMeshType.FullRect;
                texSettings.spritePixelsPerUnit = 100;
                texSettings.spriteExtrude = 1;
                importer.SetTextureSettings(texSettings);
                importer.spriteImportMode = SpriteImportMode.Single;

                importer.SaveAndReimport();

                Sprite loaded = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (loaded == null)
                {
                    Debug.LogError($"Failed to load Sprite sub-asset at {path} after reimport!");
                }
            }
        }
    }

    private static void ConfigureAudioImporters()
    {
        string[] audioGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        foreach (string guid in audioGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) continue;

            importer.forceToMono = false;
            importer.loadInBackground = false;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = path.Contains("music_loop") ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
            settings.quality = 1.0f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;

            // Ensure 2D audio (m_3D = false) so distance/listener orientation never silences music or SFX
            SerializedObject importerSo = new SerializedObject(importer);
            SerializedProperty prop3d = importerSo.FindProperty("m_3D");
            if (prop3d != null)
            {
                prop3d.boolValue = false;
                importerSo.ApplyModifiedProperties();
            }

            importer.SaveAndReimport();
        }
    }

    public static void CaptureVerificationScreenshots()
    {
        if (!Directory.Exists("Screenshots")) Directory.CreateDirectory("Screenshots");

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found for screenshot capture!");
            return;
        }

        Vector3 origPos = cam.transform.position;
        RenderTexture rt = new RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        Texture2D tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);

        var shots = new (string file, Vector3 pos)[]
        {
            ("Screenshots/shot_1_spawn_and_character.png", new Vector3(4f, 1.8f, -10f)),
            ("Screenshots/shot_2_air_dash_and_conduit.png", new Vector3(28f, 2.2f, -10f)),
            ("Screenshots/shot_3_spikes_and_acid_pool.png", new Vector3(57f, 1.6f, -10f)),
            ("Screenshots/shot_4_checkpoint_and_crawler.png", new Vector3(81f, 2.2f, -10f)),
            ("Screenshots/shot_4b_crawler_arena.png", new Vector3(105f, 2.2f, -10f)),
            ("Screenshots/shot_5_sentry_and_stalactites.png", new Vector3(132f, 3.6f, -10f)),
            ("Screenshots/shot_6_core_reactor_goal.png", new Vector3(195f, 3.5f, -10f)),
        };

        cam.transform.position = shots[0].pos;
        cam.Render();

        foreach (var shot in shots)
        {
            cam.transform.position = shot.pos;
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(shot.file, tex.EncodeToPNG());
            Debug.Log("Captured screenshot: " + shot.file);
        }

        RenderTexture.active = null;
        cam.targetTexture = null;
        cam.transform.position = origPos;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    public static void PerformBuild()
    {
        BuildScene();
        CaptureVerificationScreenshots();

        string buildDir = "Builds/KipCavernConduit_Windows";
        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string exePath = Path.Combine(buildDir, "KipCavernConduit.exe");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Level1.unity" },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"Build completed with result: {report.summary.result}, total size: {report.summary.totalSize} bytes");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            throw new System.Exception("Standalone build failed: " + report.summary.result);
        }
    }
}
