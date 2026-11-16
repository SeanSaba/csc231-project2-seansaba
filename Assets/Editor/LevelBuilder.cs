using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelBuilder
{
    private static Material spriteMat;

    [MenuItem("Tools/Build Level 1 Scene")]
    public static void BuildScene()
    {
        try
        {
            if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            ConfigureSpriteImporters();

        // Create new 2D scene
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // URP 2D Unlit Sprite Material
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        spriteMat = new Material(shader);
        AssetDatabase.CreateAsset(spriteMat, "Assets/Materials/SpriteUnlitMat.mat");

        // 1. Camera & Audio Listener
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.5f;
        cam.backgroundColor = new Color(0.04f, 0.05f, 0.1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.AddComponent<AudioListener>();
        CameraController camCtrl = camGo.AddComponent<CameraController>();
        camGo.transform.position = new Vector3(0f, 1.5f, -10f);

        // 2. Global Managers
        GameObject mgrGo = new GameObject("GameManagers");
        GameManager gm = mgrGo.AddComponent<GameManager>();
        UIManager uim = mgrGo.AddComponent<UIManager>();
        AudioManager am = mgrGo.AddComponent<AudioManager>();
        ParticleFXManager pfx = mgrGo.AddComponent<ParticleFXManager>();

        // Wire Audio clips to AudioManager
        AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_jump.wav");
        AudioClip landClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_land.wav");
        AudioClip dashClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_dash.wav");
        AudioClip collectClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_collect.wav");
        AudioClip hurtClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_hurt.wav");
        AudioClip checkpointClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_checkpoint.wav");
        AudioClip winClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_win.wav");
        AudioClip musicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/music_loop.wav");

        SerializedObject amSo = new SerializedObject(am);
        amSo.FindProperty("jumpClip").objectReferenceValue = jumpClip;
        amSo.FindProperty("landClip").objectReferenceValue = landClip;
        amSo.FindProperty("dashClip").objectReferenceValue = dashClip;
        amSo.FindProperty("collectClip").objectReferenceValue = collectClip;
        amSo.FindProperty("hurtClip").objectReferenceValue = hurtClip;
        amSo.FindProperty("checkpointClip").objectReferenceValue = checkpointClip;
        amSo.FindProperty("winClip").objectReferenceValue = winClip;
        amSo.FindProperty("musicLoopClip").objectReferenceValue = musicClip;
        amSo.ApplyModifiedProperties();

        // 3. Parallax Background Layers
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Cavern_Background.png");
        if (bgSprite != null)
        {
            GameObject bgParent = new GameObject("Backgrounds");
            // Tile 8 background segments to cover 160 units of level
            for (int i = 0; i < 9; i++)
            {
                GameObject bgObj = new GameObject($"Cavern_BG_{i}");
                bgObj.transform.SetParent(bgParent.transform);
                float bgWidth = 13.76f * 1.5f; // ~20.64 units
                bgObj.transform.position = new Vector3(-10f + (i * 20.6f), 5f, 15f);
                bgObj.transform.localScale = new Vector3(1.5f, 1.5f, 1f);
                SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                sr.sprite = bgSprite;
                sr.sortingOrder = -50;
                sr.material = spriteMat;
            }
        }

        // 4. Player Character ("Kip")
        GameObject playerGo = new GameObject("Player_Kip");
        playerGo.tag = "Player";
        playerGo.transform.position = new Vector3(0f, 1f, 0f);

        Sprite idleSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Kip_Idle.png");
        Sprite runSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Kip_Run.png");
        Sprite jumpSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Kip_Jump.png");
        Sprite fallSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Kip_Fall.png");
        Sprite hurtSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Kip_Hurt.png");
        Sprite dashSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Characters/Kip_Dash.png");

        SpriteRenderer playerSr = playerGo.AddComponent<SpriteRenderer>();
        playerSr.sprite = idleSpr;
        playerSr.sortingOrder = 5;
        playerSr.material = spriteMat;
        playerGo.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        Rigidbody2D playerRb = playerGo.AddComponent<Rigidbody2D>();
        playerRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerRb.freezeRotation = true;
        playerRb.gravityScale = 3f;

        BoxCollider2D playerCol = playerGo.AddComponent<BoxCollider2D>();
        playerCol.size = new Vector2(1.1f, 1.5f);
        playerCol.offset = new Vector2(0f, 0f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(playerGo.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.78f, 0f);

        PlayerController pc = playerGo.AddComponent<PlayerController>();
        SerializedObject pcSo = new SerializedObject(pc);
        pcSo.FindProperty("groundCheckPoint").objectReferenceValue = groundCheck.transform;
        pcSo.FindProperty("groundLayer").intValue = ~0; // all layers
        pcSo.FindProperty("spriteRenderer").objectReferenceValue = playerSr;
        pcSo.FindProperty("idleSprite").objectReferenceValue = idleSpr;
        pcSo.FindProperty("runSprite").objectReferenceValue = runSpr;
        pcSo.FindProperty("jumpSprite").objectReferenceValue = jumpSpr;
        pcSo.FindProperty("fallSprite").objectReferenceValue = fallSpr;
        pcSo.FindProperty("hurtSprite").objectReferenceValue = hurtSpr;
        pcSo.FindProperty("dashSprite").objectReferenceValue = dashSpr;
        pcSo.ApplyModifiedProperties();

        PlayerHealth ph = playerGo.AddComponent<PlayerHealth>();
        camCtrl.SetTarget(playerGo.transform);

        // 5. Build Level Geometry & Layout (130+ units length, 2-4 minutes gameplay)
        GameObject levelParent = new GameObject("LevelGeometry");
        GameObject platformsParent = new GameObject("Platforms");
        platformsParent.transform.SetParent(levelParent.transform);
        GameObject hazardsParent = new GameObject("Hazards");
        hazardsParent.transform.SetParent(levelParent.transform);
        GameObject enemiesParent = new GameObject("Enemies");
        enemiesParent.transform.SetParent(levelParent.transform);
        GameObject collectiblesParent = new GameObject("Collectibles");
        collectiblesParent.transform.SetParent(levelParent.transform);

        Sprite stoneSlab = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_StoneSlab.png");
        Sprite mossBrick = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_MossBrick.png");
        Sprite metalConduit = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_MetalConduit.png");
        Sprite floatingLedge = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Platform_FloatingLedge.png");
        Sprite wallStone = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Wall_AncientStone.png");

        Sprite crystalSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/AetherCrystal.png");
        Sprite beaconSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/ConduitBeacon.png");
        Sprite crawlerSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/ScrapCrawler.png");
        Sprite sentrySpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Enemies/BuzzSentry.png");
        Sprite spikesSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/TeslaSpikes.png");
        Sprite acidSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/AcidPool_Surface.png");
        Sprite stalactiteSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/Acid_Stalactite.png");
        Sprite goalSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/CoreReactorGoal.png");

        // Helper lambdas
        System.Action<float, float, float, float, Sprite, string> createPlatform = (x, y, w, h, sprite, name) =>
        {
            GameObject plat = new GameObject(name);
            plat.transform.SetParent(platformsParent.transform);
            plat.transform.position = new Vector3(x, y, 0f);
            SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
            sr.sprite = sprite ?? stoneSlab;
            sr.material = spriteMat;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, h);
            BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, h);
        };

        System.Action<float, float> createCrystal = (x, y) =>
        {
            GameObject crystal = new GameObject("AetherCrystal");
            crystal.transform.SetParent(collectiblesParent.transform);
            crystal.transform.position = new Vector3(x, y, 0f);
            crystal.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            SpriteRenderer sr = crystal.AddComponent<SpriteRenderer>();
            sr.sprite = crystalSpr;
            sr.material = spriteMat;
            CircleCollider2D col = crystal.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.8f;
            crystal.AddComponent<Collectible>();
        };

        System.Action<float, float, float> createSpikes = (x, y, w) =>
        {
            GameObject spike = new GameObject("TeslaSpikes");
            spike.transform.SetParent(hazardsParent.transform);
            spike.transform.position = new Vector3(x, y, 0f);
            SpriteRenderer sr = spike.AddComponent<SpriteRenderer>();
            sr.sprite = spikesSpr;
            sr.material = spriteMat;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = new Vector2(w, 0.8f);
            BoxCollider2D col = spike.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(w, 0.6f);
            col.offset = new Vector2(0f, -0.1f);
            spike.AddComponent<Hazard>();
        };

        System.Action<float, float, float, float> createCrawler = (x, y, dist, speed) =>
        {
            GameObject enemy = new GameObject("ScrapCrawler");
            enemy.transform.SetParent(enemiesParent.transform);
            enemy.transform.position = new Vector3(x, y, 0f);
            enemy.transform.localScale = new Vector3(0.45f, 0.45f, 1f);
            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = crawlerSpr;
            sr.material = spriteMat;
            BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.8f, 1.2f);
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
            enemy.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
            SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
            sr.sprite = sentrySpr;
            sr.material = spriteMat;
            CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.9f;
            FlyingEnemy fe = enemy.AddComponent<FlyingEnemy>();
            SerializedObject feSo = new SerializedObject(fe);
            feSo.FindProperty("horizontalRange").floatValue = hRange;
            feSo.FindProperty("hoverAmplitude").floatValue = hoverAmp;
            feSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
            feSo.ApplyModifiedProperties();
        };

        System.Action<float, float> createStalactite = (x, y) =>
        {
            GameObject stal = new GameObject("AcidStalactite");
            stal.transform.SetParent(hazardsParent.transform);
            stal.transform.position = new Vector3(x, y, 0f);
            stal.transform.localScale = new Vector3(0.4f, 0.5f, 1f);
            SpriteRenderer sr = stal.AddComponent<SpriteRenderer>();
            sr.sprite = stalactiteSpr;
            sr.material = spriteMat;
            BoxCollider2D col = stal.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);
            stal.AddComponent<FallingHazard>();
        };

        // --- LEVEL SECTIONS ---

        // SECTION 1: Spawn & Basic Jumps (X: -4 to 22)
        // Spawn ground
        createPlatform(0f, -0.6f, 10f, 1.2f, stoneSlab, "SpawnPlatform");
        // Stepping stones
        createPlatform(8f, 0.4f, 4f, 1f, mossBrick, "StepPlat1");
        createPlatform(15f, 1.2f, 4.5f, 1f, stoneSlab, "StepPlat2");
        createCrystal(8f, 1.5f);
        createCrystal(15f, 2.3f);

        // SECTION 2: Introducing AIR DASH (X: 20 to 45)
        // Large gap between X=17 and X=26 (9 units gap, impossible with jump alone!)
        createPlatform(29f, 1.5f, 7f, 1f, metalConduit, "DashLandingPlat");
        createCrystal(23f, 2.5f); // Rewarding air-dash in mid-air!
        createPlatform(40f, 0.5f, 8f, 1.2f, stoneSlab, "Section2Plat2");
        createCrystal(39f, 1.8f);

        // SECTION 3: Introducing STATIC HAZARDS (Tesla Spikes & Pit) (X: 45 to 65)
        createPlatform(50f, -1.0f, 10f, 1.2f, mossBrick, "TrenchGround");
        createSpikes(50f, -0.1f, 6f);
        createPlatform(48f, 2.0f, 3.5f, 0.8f, floatingLedge, "SpikeBypassLedge1");
        createPlatform(54f, 2.8f, 3.5f, 0.8f, floatingLedge, "SpikeBypassLedge2");
        createCrystal(51f, 3.8f);

        // SECTION 4: MID-POINT CHECKPOINT BEACON (X: 62 to 70)
        createPlatform(64f, 1.0f, 8f, 1.2f, stoneSlab, "CheckpointPlat");
        GameObject beaconObj = new GameObject("Checkpoint_ConduitBeacon");
        beaconObj.transform.SetParent(levelParent.transform);
        beaconObj.transform.position = new Vector3(64f, 2.2f, 0f);
        beaconObj.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        SpriteRenderer beaconSr = beaconObj.AddComponent<SpriteRenderer>();
        beaconSr.sprite = beaconSpr;
        beaconSr.material = spriteMat;
        BoxCollider2D beaconCol = beaconObj.AddComponent<BoxCollider2D>();
        beaconCol.isTrigger = true;
        beaconCol.size = new Vector2(1.5f, 2f);
        beaconObj.AddComponent<Checkpoint>();
        createCrystal(64f, 3.8f);

        // SECTION 5: Introducing PATROL ENEMY (Scrap Crawler) (X: 72 to 92)
        createPlatform(78f, 0.5f, 14f, 1.2f, mossBrick, "CrawlerPatrolArena");
        createCrawler(77f, 1.5f, 4.5f, 2.5f);
        createCrystal(75f, 1.8f);
        createCrystal(81f, 1.8f);

        // SECTION 6: Introducing FLYING ENEMY & FALLING STALACTITES (X: 92 to 118)
        createPlatform(93f, 1.2f, 4f, 0.8f, floatingLedge, "SentryGap1");
        createPlatform(101f, 2.0f, 4.5f, 0.8f, floatingLedge, "SentryGap2");
        createPlatform(109f, 2.8f, 4.5f, 0.8f, floatingLedge, "SentryGap3");
        createSentry(97f, 3.2f, 2.5f, 1.2f);
        createSentry(105f, 4.0f, 2.5f, 1.2f);
        createStalactite(101f, 7.5f);
        createCrystal(97f, 4.2f);
        createCrystal(105f, 5.0f);

        // SECTION 7: FINAL CLIMAX & CORE REACTOR GOAL (X: 118 to 142)
        createPlatform(122f, 1.5f, 6f, 1.0f, metalConduit, "PreGoalBridge");
        createSpikes(122f, 2.3f, 3.5f);
        createPlatform(122f, 4.2f, 4f, 0.8f, floatingLedge, "HighBridgeBypass");
        createPlatform(133f, 1.5f, 14f, 1.4f, stoneSlab, "ReactorChamberFloor");
        createCrystal(122f, 5.2f);
        createCrystal(130f, 2.8f);

        // Ancient Core Reactor Gateway (Level Goal)
        GameObject goalObj = new GameObject("CoreReactorGoal");
        goalObj.transform.SetParent(levelParent.transform);
        goalObj.transform.position = new Vector3(135f, 3.2f, 0f);
        goalObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
        SpriteRenderer goalSr = goalObj.AddComponent<SpriteRenderer>();
        goalSr.sprite = goalSpr;
        goalSr.material = spriteMat;
        BoxCollider2D goalCol = goalObj.AddComponent<BoxCollider2D>();
        goalCol.isTrigger = true;
        goalCol.size = new Vector2(2f, 3.5f);
        goalObj.AddComponent<GoalZone>();

        // Deep Pit Hazard (Catch-all bottom abyss)
        GameObject abyss = new GameObject("AbyssKillFloor");
        abyss.transform.SetParent(hazardsParent.transform);
        abyss.transform.position = new Vector3(70f, -8f, 0f);
        BoxCollider2D abyssCol = abyss.AddComponent<BoxCollider2D>();
        abyssCol.isTrigger = true;
        abyssCol.size = new Vector2(200f, 4f);
        Hazard abyssHaz = abyss.AddComponent<Hazard>();
        SerializedObject abyssSo = new SerializedObject(abyssHaz);
        abyssSo.FindProperty("isInstantKill").boolValue = true;
        abyssSo.ApplyModifiedProperties();

        // Save scene
        string scenePath = "Assets/Scenes/Level1.unity";
        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"Level 1 Scene built and saved successfully to {scenePath}!");

        // Add to Editor Build Settings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in BuildScene: " + ex);
            throw;
        }
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
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }

    public static void PerformBuild()
    {
        BuildScene();

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
    }
}
