using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class LevelBuilder
{
    private struct PlatformSweepBounds
    {
        public string Name;
        public bool IsMoving;
        public Rect Bounds;
    }

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

            // 1. Camera & Audio Listener
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
            uiSo.FindProperty("crystalIconTex").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Collectibles/AetherCrystal_Frame_0.png");
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

            // 3. Multi-Layer Parallax Cavern Background
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Cavern_Background.png");
            Sprite wallArchSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Wall_AncientStone.png");
            Sprite foundationSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Environment/Foundation_DarkStone.png");

            GameObject bgParent = new GameObject("Backgrounds");
            if (bgSprite != null)
            {
                GameObject farLayer = new GameObject("Parallax_Far");
                farLayer.transform.SetParent(bgParent.transform);
                ParallaxLayer farPl = farLayer.AddComponent<ParallaxLayer>();
                SerializedObject farPlSo = new SerializedObject(farPl);
                farPlSo.FindProperty("parallaxFactor").vector2Value = new Vector2(0.70f, 0.35f);
                farPlSo.ApplyModifiedProperties();

                for (int i = 0; i < 12; i++)
                {
                    GameObject bgObj = new GameObject($"Cavern_BG_{i}");
                    bgObj.transform.SetParent(farLayer.transform);
                    bgObj.transform.position = new Vector3(-16f + (i * 27.5f), 2.8f, 20f);
                    bgObj.transform.localScale = new Vector3(2.0f, 2.0f, 1f);
                    SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
                    sr.sprite = bgSprite;
                    sr.flipX = (i % 2 == 1);
                    sr.sortingOrder = -60;
                    sr.color = new Color(0.60f, 0.74f, 0.88f, 1f);
                }
            }

            // 4. Player Character ("Kip") + Full Multi-Frame Unity AnimatorController
            Sprite[] idleRaw = LoadFrames("Assets/Sprites/Characters/Kip_Idle_{0}.png", 4);
            // Smooth 6-keyframe breathing/blink rhythm: alert -> inhale -> blink -> exhale -> alert -> inhale
            Sprite[] idleFrames = new Sprite[] { idleRaw[0], idleRaw[1], idleRaw[2], idleRaw[3], idleRaw[0], idleRaw[1] };
            Sprite[] runFrames = LoadFrames("Assets/Sprites/Characters/Kip_Run_{0}.png", 8);
            Sprite[] jumpFrames = LoadFrames("Assets/Sprites/Characters/Kip_Jump_{0}.png", 3);
            Sprite[] fallFrames = LoadFrames("Assets/Sprites/Characters/Kip_Fall_{0}.png", 3);
            Sprite[] dashFrames = LoadFrames("Assets/Sprites/Characters/Kip_Dash_{0}.png", 3);
            Sprite[] hurtFrames = LoadFrames("Assets/Sprites/Characters/Kip_Hurt_{0}.png", 3);

            AnimatorController kipController = CreateKipAnimatorController(idleFrames, runFrames, jumpFrames, fallFrames, hurtFrames, dashFrames);

            GameObject playerGo = new GameObject("Player_Kip");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(0f, 0.84f, 0f);

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
            visualsGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);

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

            // 5. Level Containers & Multi-Frame Sprites
            GameObject levelParent = new GameObject("LevelGeometry");
            GameObject platformsParent = new GameObject("Platforms");
            platformsParent.transform.SetParent(levelParent.transform);
            GameObject foundationsParent = new GameObject("Foundations");
            foundationsParent.transform.SetParent(levelParent.transform);
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

            Sprite[] crystalFrames = LoadFrames("Assets/Sprites/Collectibles/AetherCrystal_Frame_{0}.png", 4);
            Sprite beaconInactiveSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/ConduitBeacon_Inactive.png");
            Sprite beaconActiveSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Collectibles/ConduitBeacon_Active.png");
            Sprite[] crawlerFrames = LoadFrames("Assets/Sprites/Enemies/ScrapCrawler_Frame_{0}.png", 4);
            Sprite[] sentryFrames = LoadFrames("Assets/Sprites/Enemies/BuzzSentry_Frame_{0}.png", 4);
            Sprite spikesSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/TeslaSpikes.png");
            Sprite spikeSlotSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/SpikeFloorSlot.png");
            Sprite sawSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/Sawblade.png");
            Sprite sawRailSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/SawbladeRail.png");
            Sprite[] acidFrames = LoadFrames("Assets/Sprites/Hazards/AcidPool_Frame_{0}.png", 6);
            Sprite stalactiteSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Hazards/Acid_Stalactite.png");
            Sprite[] goalFrames = LoadFrames("Assets/Sprites/Environment/CoreReactorGoal_Frame_{0}.png", 4);

            List<PlatformSweepBounds> platformSweeps = new List<PlatformSweepBounds>();

            // Helper: Attach SpriteFrameAnimator cleanly via SerializedObject
            System.Action<GameObject, SpriteRenderer, Sprite[], float, bool, bool, bool> addFrameAnimator =
                (go, sr, frames, fps, randomStart, sway, pulse) =>
            {
                SpriteFrameAnimator anim = go.AddComponent<SpriteFrameAnimator>();
                SerializedObject so = new SerializedObject(anim);
                so.FindProperty("targetRenderer").objectReferenceValue = sr;
                SerializedProperty framesProp = so.FindProperty("frames");
                framesProp.arraySize = frames != null ? frames.Length : 0;
                for (int i = 0; i < framesProp.arraySize; i++)
                {
                    framesProp.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
                }
                so.FindProperty("frameRate").floatValue = fps;
                so.FindProperty("randomizeStartFrame").boolValue = randomStart;
                so.FindProperty("gentleSway").boolValue = sway;
                so.FindProperty("gentlePulse").boolValue = pulse;
                so.ApplyModifiedProperties();
            };

            // Helper: Grounded dark foundation pier descending all the way off-screen to y = -9.5f
            System.Action<float, float, float, bool> createFoundation = (centerX, topOfFoundationY, width, includeArch) =>
            {
                float bottomY = -9.5f;
                float height = Mathf.Max(1.0f, topOfFoundationY - bottomY);

                if (includeArch && wallArchSprite != null && width >= 8.0f)
                {
                    // Place stone arch flush under the stone platform, flanked by deep piers going to cavern floor
                    float archH = 2.4f;
                    float archW = 6.4f;
                    GameObject archObj = new GameObject("FoundationArch");
                    archObj.transform.SetParent(foundationsParent.transform);
                    archObj.transform.position = new Vector3(centerX, topOfFoundationY - archH * 0.5f + 0.08f, 0f);
                    archObj.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
                    SpriteRenderer asr = archObj.AddComponent<SpriteRenderer>();
                    asr.sprite = wallArchSprite;
                    asr.sortingOrder = -5;
                    asr.color = new Color(0.72f, 0.80f, 0.90f, 1f);

                    // Left and right solid stone legs running down from arch piers to cavern bottom
                    float legTop = topOfFoundationY - archH + 0.35f;
                    float legH = Mathf.Max(1f, legTop - bottomY);
                    float[] legOffsets = { -2.28f, 2.28f };
                    foreach (float off in legOffsets)
                    {
                        GameObject leg = new GameObject("ArchPierLeg");
                        leg.transform.SetParent(foundationsParent.transform);
                        leg.transform.position = new Vector3(centerX + off, bottomY + legH * 0.5f, 0f);
                        SpriteRenderer lsr = leg.AddComponent<SpriteRenderer>();
                        lsr.sprite = foundationSprite;
                        lsr.drawMode = SpriteDrawMode.Tiled;
                        lsr.size = new Vector2(1.56f, legH);
                        lsr.sortingOrder = -6;
                        lsr.color = new Color(0.88f, 0.92f, 0.96f, 1f);
                    }
                }
                else
                {
                    // Solid grounded masonry column under the platform extending down into cavern floor
                    float pierW = Mathf.Max(1.6f, width - 0.8f);
                    GameObject pier = new GameObject("FoundationPier");
                    pier.transform.SetParent(foundationsParent.transform);
                    pier.transform.position = new Vector3(centerX, bottomY + height * 0.5f, 0f);
                    SpriteRenderer psr = pier.AddComponent<SpriteRenderer>();
                    psr.sprite = foundationSprite;
                    psr.drawMode = SpriteDrawMode.Tiled;
                    psr.size = new Vector2(pierW, height);
                    psr.sortingOrder = -6;
                    psr.color = new Color(0.88f, 0.92f, 0.96f, 1f);
                }
            };

            // Helper: Segmented 3-piece mossy stone platform defined by exact WALKABLE topY
            System.Func<float, float, int, string, bool, bool, GameObject> createStonePlatform =
                (centerX, topY, tileCount, name, withFoundation, withArch) =>
            {
                float tileW = 1.2f;
                float totalW = tileCount * tileW;
                float centerY = topY - tileW * 0.5f;

                GameObject plat = new GameObject(name);
                plat.transform.SetParent(platformsParent.transform);
                plat.transform.position = new Vector3(centerX, centerY, 0f);
                BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
                col.size = new Vector2(totalW, tileW);

                float startX = -totalW * 0.5f + tileW * 0.5f;
                for (int i = 0; i < tileCount; i++)
                {
                    GameObject tile = new GameObject($"Tile_{i}");
                    tile.transform.SetParent(plat.transform);
                    tile.transform.localPosition = new Vector3(startX + i * tileW, 0f, 0f);
                    // Slight horizontal overlap prevents 1px sub-pixel seams on Point-filtered sprites
                    tile.transform.localScale = new Vector3(1.015f, 1.0f, 1f);
                    SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                    if (tileCount == 1) sr.sprite = mossMid;
                    else if (i == 0) sr.sprite = mossLeft;
                    else if (i == tileCount - 1) sr.sprite = mossRight;
                    else sr.sprite = mossMid;
                    sr.sortingOrder = 2; // Opaque foreground stone naturally hides retracted PopUpSpikes (order 1)
                }

                if (withFoundation)
                {
                    createFoundation(centerX, topY - tileW + 0.06f, totalW, withArch);
                }

                platformSweeps.Add(new PlatformSweepBounds
                {
                    Name = name,
                    IsMoving = false,
                    Bounds = new Rect(centerX - totalW * 0.5f, topY - tileW, totalW, tileW)
                });
                return plat;
            };

            // Helper: Tiled platform (metal conduit or floating mossy ledge) defined by exact topY
            System.Func<float, float, float, float, Sprite, string, bool, GameObject> createTiledPlatform =
                (centerX, topY, w, h, sprite, name, crumbling) =>
            {
                float centerY = topY - h * 0.5f;
                GameObject plat = new GameObject(name);
                plat.transform.SetParent(platformsParent.transform);
                plat.transform.position = new Vector3(centerX, centerY, 0f);
                SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
                sr.sprite = sprite ?? floatingLedge;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(w, h);
                sr.sortingOrder = 2;
                if (crumbling)
                {
                    // Subtle warm weathered tint hints at ancient unstable stonework
                    sr.color = new Color(0.95f, 0.88f, 0.84f, 1f);
                }
                BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
                col.size = new Vector2(w, h);
                if (crumbling)
                {
                    plat.AddComponent<CrumblingPlatform>();
                }

                platformSweeps.Add(new PlatformSweepBounds
                {
                    Name = name,
                    IsMoving = false,
                    Bounds = new Rect(centerX - w * 0.5f, topY - h, w, h)
                });
                return plat;
            };

            // Helper: Continuous dark cavern ceiling extending off the top of the screen for stalactites
            System.Action<float, float, float, string> createCavernCeiling = (centerX, ceilingBottomY, width, name) =>
            {
                float ceilingH = 3.8f;
                GameObject ceil = new GameObject(name);
                ceil.transform.SetParent(foundationsParent.transform);
                ceil.transform.position = new Vector3(centerX, ceilingBottomY + ceilingH * 0.5f, 0f);
                SpriteRenderer sr = ceil.AddComponent<SpriteRenderer>();
                sr.sprite = foundationSprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, ceilingH);
                sr.sortingOrder = 2;
                sr.color = new Color(0.70f, 0.78f, 0.90f, 1f);
            };

            // Helper: Moving Conduit Platform with Thruster Jet Particles + Full Sweep Registration
            System.Action<float, float, float, Vector3, float, float, string> createMovingPlatform =
                (startX, topY, w, moveOffset, speed, phase, name) =>
            {
                float h = 1.0f;
                float startCenterY = topY - h * 0.5f;
                GameObject mpGo = new GameObject(name);
                mpGo.transform.SetParent(platformsParent.transform);
                mpGo.transform.position = new Vector3(startX, startCenterY, 0f);
                SpriteRenderer sr = mpGo.AddComponent<SpriteRenderer>();
                sr.sprite = metalConduit;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(w, h);
                sr.sortingOrder = 2;
                BoxCollider2D col = mpGo.AddComponent<BoxCollider2D>();
                col.size = new Vector2(w, h);

                MovingPlatform mp = mpGo.AddComponent<MovingPlatform>();
                SerializedObject mpSo = new SerializedObject(mp);
                mpSo.FindProperty("moveOffset").vector3Value = moveOffset;
                mpSo.FindProperty("speed").floatValue = speed;
                mpSo.FindProperty("phaseOffset").floatValue = phase;
                mpSo.ApplyModifiedProperties();

                // Hover Conduit Plasma Thruster Jets underneath the moving floor!
                ParticleSystem thrustPS = CreateThrusterParticles(mpGo.transform, "ConduitThrusters",
                    new Vector3(0f, -0.45f, 0f), w * 0.55f, new Color(0.18f, 0.95f, 1f, 0.85f), sparkleMat, 1);

                // Register FULL swept AABB for build-time anti-collision check
                float minX = Mathf.Min(startX, startX + moveOffset.x) - w * 0.5f;
                float maxX = Mathf.Max(startX, startX + moveOffset.x) + w * 0.5f;
                float minY = Mathf.Min(startCenterY, startCenterY + moveOffset.y) - h * 0.5f;
                float maxY = Mathf.Max(startCenterY, startCenterY + moveOffset.y) + h * 0.5f;
                platformSweeps.Add(new PlatformSweepBounds
                {
                    Name = name,
                    IsMoving = true,
                    Bounds = Rect.MinMaxRect(minX, minY, maxX, maxY)
                });
            };

            // Helper: Embedded Ground Pop-Up Retractable Spikes (hidden INSIDE stone platform, erupts on proximity or wave!)
            System.Action<float, float, float, bool, float> createPopUpSpikes =
                (x, surfaceTopY, width, proximityTriggered, phaseOffset) =>
            {
                GameObject trap = new GameObject(proximityTriggered ? "PopUpSpikes_Ambush" : "PopUpSpikes_Wave");
                trap.transform.SetParent(hazardsParent.transform);
                trap.transform.position = new Vector3(x, surfaceTopY, 0f);

                // 1. Retractable Spikes visual (sortingOrder = 1, BEHIND sortingOrder = 2 stone floor!)
                GameObject vis = new GameObject("SpikesVisual");
                vis.transform.SetParent(trap.transform);
                vis.transform.localPosition = new Vector3(0f, -0.46f, 0f);
                SpriteRenderer sr = vis.AddComponent<SpriteRenderer>();
                sr.sprite = spikesSpr;
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, 0.76f);
                sr.sortingOrder = 1;

                // 2. Recessed Metallic Floor-Slot Bezel flush on the stone surface
                GameObject slot = new GameObject("FloorSlotBezel");
                slot.transform.SetParent(trap.transform);
                slot.transform.localPosition = new Vector3(0f, -0.07f, 0f);
                SpriteRenderer slotSr = slot.AddComponent<SpriteRenderer>();
                slotSr.sprite = spikeSlotSpr;
                slotSr.drawMode = SpriteDrawMode.Tiled;
                slotSr.size = new Vector2(width + 0.24f, 0.24f);
                slotSr.sortingOrder = 3;

                // 3. Damage trigger collider (enabled ONLY while erupted!)
                BoxCollider2D col = trap.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(width - 0.14f, 0.62f);
                col.offset = new Vector2(0f, 0.32f);

                PopUpSpikes pus = trap.AddComponent<PopUpSpikes>();
                SerializedObject so = new SerializedObject(pus);
                so.FindProperty("spikesVisual").objectReferenceValue = vis.transform;
                so.FindProperty("damageCollider").objectReferenceValue = col;
                so.FindProperty("retractedLocalY").floatValue = -0.46f;
                so.FindProperty("peekLocalY").floatValue = -0.20f;
                so.FindProperty("extendedLocalY").floatValue = 0.34f;
                so.FindProperty("proximityTriggered").boolValue = proximityTriggered;
                so.FindProperty("triggerRadiusX").floatValue = Mathf.Max(2.0f, width * 0.65f + 1.1f);
                so.FindProperty("phaseOffset").floatValue = phaseOffset;
                so.ApplyModifiedProperties();
            };

            // Helper: Tracked Sawblade mounted on a real metallic rail + grinding sparks particle system!
            System.Action<float, float, Vector3, float, float> createSawblade = (x, y, moveOffset, speed, phase) =>
            {
                // Visible mechanical rail behind the sawblade's exact travel line
                if (sawRailSpr != null && moveOffset.sqrMagnitude > 0.1f)
                {
                    GameObject rail = new GameObject("SawbladeTrackRail");
                    rail.transform.SetParent(hazardsParent.transform);
                    Vector3 mid = new Vector3(x, y, 0f) + moveOffset * 0.5f;
                    rail.transform.position = mid;
                    float angle = Mathf.Atan2(moveOffset.y, moveOffset.x) * Mathf.Rad2Deg;
                    rail.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    SpriteRenderer rsr = rail.AddComponent<SpriteRenderer>();
                    rsr.sprite = sawRailSpr;
                    rsr.drawMode = SpriteDrawMode.Tiled;
                    rsr.size = new Vector2(moveOffset.magnitude + 0.6f, 0.38f);
                    rsr.sortingOrder = -1;
                }

                GameObject saw = new GameObject("SawbladeTrap");
                saw.transform.SetParent(hazardsParent.transform);
                saw.transform.position = new Vector3(x, y, 0f);
                saw.transform.localScale = new Vector3(0.82f, 0.82f, 1f);
                SpriteRenderer sr = saw.AddComponent<SpriteRenderer>();
                sr.sprite = sawSpr;
                sr.sortingOrder = 6;
                CircleCollider2D col = saw.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.50f;
                MovingTrap mt = saw.AddComponent<MovingTrap>();
                SerializedObject mtSo = new SerializedObject(mt);
                mtSo.FindProperty("moveOffset").vector3Value = moveOffset;
                mtSo.FindProperty("moveSpeed").floatValue = speed;
                mtSo.FindProperty("phaseOffset").floatValue = phase;
                mtSo.ApplyModifiedProperties();

                // Grinding friction sparks spraying continuously off the spinning blade!
                CreateSparkShowerParticles(saw.transform, "SawGrindSparks", Vector3.zero,
                    new Color(1f, 0.78f, 0.25f, 0.95f), 14f, sparkleMat, 7);
            };

            // Helper: Grounded Prop placed mathematically flush onto surfaceTopY
            System.Action<float, float, Sprite, float, int, bool, bool> createGroundedProp =
                (x, surfaceTopY, spr, scale, order, sway, pulse) =>
            {
                if (spr == null) return;
                float worldHalfH = (spr.rect.height / 100f) * 0.5f * scale;
                float y = surfaceTopY + worldHalfH - 0.05f; // Tucked 5cm into moss — NEVER floating!
                GameObject prop = new GameObject(spr.name);
                prop.transform.SetParent(propsParent.transform);
                prop.transform.position = new Vector3(x, y, 0f);
                prop.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer sr = prop.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingOrder = order;
                if (sway || pulse)
                {
                    addFrameAnimator(prop, sr, null, 0f, false, sway, pulse);
                }
            };

            // Helper: Animated 4-Frame Rotating Aether Crystal + Shimmer Particle Aura
            System.Action<float, float> createCrystal = (x, y) =>
            {
                GameObject crystal = new GameObject("AetherCrystal");
                crystal.transform.SetParent(collectiblesParent.transform);
                crystal.transform.position = new Vector3(x, y, 0f);
                crystal.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                SpriteRenderer sr = crystal.AddComponent<SpriteRenderer>();
                sr.sprite = crystalFrames[0];
                sr.sortingOrder = 5;
                addFrameAnimator(crystal, sr, crystalFrames, 7f, true, false, false);
                CircleCollider2D col = crystal.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.68f;
                crystal.AddComponent<Collectible>();

                CreateAmbientParticles(crystal.transform, "CrystalAura", Vector3.zero, new Vector3(0.75f, 0.9f, 0.1f),
                    new Color(0.35f, 1f, 1f, 0.75f), 4f, 0.9f, 0.16f, 0.55f, sparkleMat, 6, true, -0.08f, true);
            };

            // Helper: Animated 6-Frame Bioluminescent Acid Water Pool tucked cleanly inside stone trench!
            System.Action<float, float, float> createAcidPool = (centerX, rimTopY, width) =>
            {
                float poolH = 1.24f;
                // Water crest sits 0.12 units BELOW the trench rim so it fills the basin naturally!
                float centerY = (rimTopY - 0.12f) - poolH * 0.5f;

                GameObject pool = new GameObject("AcidPool");
                pool.transform.SetParent(hazardsParent.transform);
                pool.transform.position = new Vector3(centerX, centerY, 0f);
                SpriteRenderer sr = pool.AddComponent<SpriteRenderer>();
                sr.sprite = acidFrames[0];
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(width, poolH);
                sr.sortingOrder = 1; // Tucked neatly between stone trench edges (sortingOrder 2)
                addFrameAnimator(pool, sr, acidFrames, 7.5f, false, false, false);

                BoxCollider2D col = pool.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(width - 0.15f, poolH * 0.72f);
                col.offset = new Vector2(0f, -0.14f);
                Hazard haz = pool.AddComponent<Hazard>();
                SerializedObject hSo = new SerializedObject(haz);
                hSo.FindProperty("isLiquidHazard").boolValue = true;
                hSo.ApplyModifiedProperties();

                // Layer 1: Rising glowing acid bubbles with Noise wobble
                CreateAmbientParticles(pool.transform, "AcidBubbles", new Vector3(0f, 0.36f, 0f), new Vector3(width * 0.86f, 0.22f, 0.1f),
                    new Color(0.20f, 1f, 0.88f, 0.92f), 14f, 1.15f, 0.22f, 1.15f, sparkleMat, 4, true, -0.25f, true);
                // Layer 2: Drifting translucent toxic surface mist
                CreateAmbientParticles(pool.transform, "AcidMist", new Vector3(0f, 0.48f, 0f), new Vector3(width * 0.90f, 0.18f, 0.1f),
                    new Color(0.15f, 0.92f, 0.85f, 0.34f), 9f, 1.9f, 0.55f, 0.45f, dustMat, 3, true, -0.08f, true);
            };

            // Helper: Animated 4-Frame Scrap Crawler placed flush on surfaceTopY
            System.Action<float, float, float, float> createCrawler = (x, surfaceTopY, dist, speed) =>
            {
                float scale = 0.62f;
                float worldY = surfaceTopY + 0.50f; // Exact foot alignment for 220x176 footY=172 @ scale 0.62
                GameObject enemy = new GameObject("ScrapCrawler");
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.transform.position = new Vector3(x, worldY, 0f);
                enemy.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
                sr.sprite = crawlerFrames[0];
                sr.sortingOrder = 5;
                addFrameAnimator(enemy, sr, crawlerFrames, 9f, true, false, false);

                BoxCollider2D col = enemy.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1.75f, 1.35f);
                PatrolEnemy pe = enemy.AddComponent<PatrolEnemy>();
                SerializedObject peSo = new SerializedObject(pe);
                peSo.FindProperty("patrolDistance").floatValue = dist;
                peSo.FindProperty("moveSpeed").floatValue = speed;
                peSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
                peSo.ApplyModifiedProperties();
            };

            // Helper: Animated 4-Frame Buzz Sentry with Rotor Spin & Pulsing Red Eye
            System.Action<float, float, float, float> createSentry = (x, y, hRange, hoverAmp) =>
            {
                GameObject enemy = new GameObject("BuzzSentry");
                enemy.transform.SetParent(enemiesParent.transform);
                enemy.transform.position = new Vector3(x, y, 0f);
                enemy.transform.localScale = new Vector3(0.60f, 0.60f, 1f);
                SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
                sr.sprite = sentryFrames[0];
                sr.sortingOrder = 5;
                addFrameAnimator(enemy, sr, sentryFrames, 14f, true, false, false);

                CircleCollider2D col = enemy.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.82f;
                FlyingEnemy fe = enemy.AddComponent<FlyingEnemy>();
                SerializedObject feSo = new SerializedObject(fe);
                feSo.FindProperty("horizontalRange").floatValue = hRange;
                feSo.FindProperty("hoverAmplitude").floatValue = hoverAmp;
                feSo.FindProperty("spriteRenderer").objectReferenceValue = sr;
                feSo.ApplyModifiedProperties();

                // Subtle rotor-wash mist particles beneath hovering drone
                CreateThrusterParticles(enemy.transform, "RotorWash", new Vector3(0f, -0.55f, 0f), 0.5f,
                    new Color(1f, 0.42f, 0.28f, 0.55f), dustMat, 4);
            };

            // Helper: Acid Stalactite anchored flush onto a real dark cavern ceiling + periodic acid drips!
            System.Action<float, float> createStalactite = (x, ceilingBottomY) =>
            {
                float y = ceilingBottomY - 0.50f; // Tucked 10cm up into dark ceiling stone
                GameObject stal = new GameObject("AcidStalactite");
                stal.transform.SetParent(hazardsParent.transform);
                stal.transform.position = new Vector3(x, y, 0f);
                stal.transform.localScale = new Vector3(0.95f, 1.0f, 1f);
                SpriteRenderer sr = stal.AddComponent<SpriteRenderer>();
                sr.sprite = stalactiteSpr;
                sr.sortingOrder = 3;
                BoxCollider2D col = stal.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.85f, 1.05f);
                stal.AddComponent<FallingHazard>();

                // Atmospheric glowing acid droplets dripping off stalactite tip!
                CreateAmbientParticles(stal.transform, "AcidDrips", new Vector3(0f, -0.52f, 0f), new Vector3(0.25f, 0.08f, 0.1f),
                    new Color(0.25f, 1f, 0.85f, 0.90f), 2.5f, 1.1f, 0.15f, 0.35f, sparkleMat, 4, true, 0.95f, false);
            };

            // Helper: Checkpoint Conduit Beacon placed flush on surfaceTopY + Orbital Energy Spire!
            System.Action<float, float, string> createCheckpoint = (x, surfaceTopY, name) =>
            {
                float scale = 0.85f;
                float worldY = surfaceTopY + 0.82f; // 200px * 0.5 * 0.85 - 0.03 sink
                GameObject beaconObj = new GameObject(name);
                beaconObj.transform.SetParent(levelParent.transform);
                beaconObj.transform.position = new Vector3(x, worldY, 0f);
                beaconObj.transform.localScale = new Vector3(scale, scale, 1f);
                SpriteRenderer beaconSr = beaconObj.AddComponent<SpriteRenderer>();
                beaconSr.sprite = beaconInactiveSpr;
                beaconSr.sortingOrder = 3;
                BoxCollider2D beaconCol = beaconObj.AddComponent<BoxCollider2D>();
                beaconCol.isTrigger = true;
                beaconCol.size = new Vector2(1.3f, 1.9f);

                ParticleSystem auraPS = CreateOrbitalVortexParticles(beaconObj.transform, "BeaconAura",
                    Vector3.zero, new Color(0.1f, 1f, 1f, 0.92f), 22f, 1.3f, 0.24f, 0.85f, 3.2f, sparkleMat, 6, false);

                Checkpoint cp = beaconObj.AddComponent<Checkpoint>();
                SerializedObject cpSo = new SerializedObject(cp);
                cpSo.FindProperty("spriteRenderer").objectReferenceValue = beaconSr;
                cpSo.FindProperty("inactiveSprite").objectReferenceValue = beaconInactiveSpr;
                cpSo.FindProperty("activeSprite").objectReferenceValue = beaconActiveSpr;
                cpSo.FindProperty("beaconAuraPS").objectReferenceValue = auraPS;
                cpSo.ApplyModifiedProperties();
            };

            // Helper: Tutorial Signpost with Lantern planted FLUSH on surfaceTopY!
            System.Action<float, float, string, string> createTutorial = (x, surfaceTopY, title, body) =>
            {
                createGroundedProp(x, surfaceTopY, lanternSpr, 0.90f, 1, false, false);
                GameObject tut = new GameObject($"Tutorial_{title}");
                tut.transform.SetParent(tutorialsParent.transform);
                tut.transform.position = new Vector3(x + 1.0f, surfaceTopY + 1.0f, 0f);
                TutorialSign ts = tut.AddComponent<TutorialSign>();
                SerializedObject tsSo = new SerializedObject(ts);
                tsSo.FindProperty("title").stringValue = title;
                tsSo.FindProperty("instructionText").stringValue = body;
                tsSo.ApplyModifiedProperties();
            };

            // ================= 8-SECTION INTENTIONAL, GROUNDED, UNPREDICTABLE LEVEL 1 =================

            // SECTION 1 (X: -5 to 21): Spawn, Grounded Lanterns & Baseline Jumps
            createStonePlatform(1.0f, 0.0f, 10, "SpawnPlatform", true, true);             // X=[-5.0 .. 7.0], topY=0.0
            createTutorial(-1.8f, 0.0f, "MOVEMENT & JUMP", "Use [A / D] or Arrows to move, and [SPACE / W] to jump across the cavern.");
            createGroundedProp(-3.8f, 0.0f, shroomSpr, 0.90f, 3, true, false);
            createGroundedProp(5.4f, 0.0f, clusterSpr, 0.90f, 3, false, true);

            createStonePlatform(11.2f, 1.0f, 4, "StepPlat1", true, false);                // X=[8.8 .. 13.6], topY=1.0
            createGroundedProp(9.8f, 1.0f, shroomSpr, 0.78f, 3, true, false);
            createCrystal(11.2f, 2.0f);

            createStonePlatform(18.6f, 1.8f, 4, "StepPlat2", true, false);                // X=[16.2 .. 21.0], topY=1.8
            createTutorial(17.2f, 1.8f, "SIGNATURE MOVE: AIR DASH", "Press [SHIFT] or [J] mid-jump to Air Dash across wide chasms!");
            createGroundedProp(20.0f, 1.8f, clusterSpr, 0.80f, 3, false, true);
            createCrystal(18.6f, 2.8f);

            // SECTION 2 (X: 21 to 47): Air Dash Chasm + Grounded Bridge Piers + First Ambush Spike!
            createTiledPlatform(33.0f, 2.0f, 9.0f, 1.0f, metalConduit, "DashLandingPlat", false); // X=[28.5 .. 37.5], topY=2.0
            // Real stone support piers anchored all the way into cavern floor (NO floating grates!)
            createFoundation(30.2f, 1.08f, 2.2f, false);
            createFoundation(35.8f, 1.08f, 2.2f, false);
            createCrystal(24.8f, 3.2f);
            createCrystal(33.0f, 3.1f);

            createStonePlatform(43.2f, 2.8f, 6, "DashClimbPlat", true, false);            // X=[39.6 .. 46.8], topY=2.8
            createPopUpSpikes(43.2f, 2.8f, 1.6f, true, 0f);                               // First surprise floor ambush spike!
            createGroundedProp(40.6f, 2.8f, shroomSpr, 0.82f, 3, true, false);
            createCrystal(43.2f, 4.3f);

            // SECTION 3 (X: 48 to 69): Embedded Ambush Spikes, Animated 6-Frame Acid Pool & Tracked Sawblade
            createStonePlatform(52.2f, 0.0f, 6, "TrenchLeft", true, false);               // X=[48.6 .. 55.8], topY=0.0
            createTutorial(49.8f, 0.0f, "UNPREDICTABLE TRAPS", "Watch for recessed floor slots! Hidden spikes erupt when you approach—use upper ledges!");
            createPopUpSpikes(52.6f, 0.0f, 2.6f, true, 0f);

            createStonePlatform(58.8f, -1.35f, 6, "AcidPitFloor", true, false);           // X=[55.2 .. 62.4], topY=-1.35
            createAcidPool(58.8f, 0.0f, 6.0f);                                            // X=[55.8 .. 61.8], animated 6-frame water!

            createStonePlatform(65.4f, 0.0f, 6, "TrenchRight", true, false);              // X=[61.8 .. 69.0], topY=0.0
            createPopUpSpikes(64.8f, 0.0f, 2.0f, true, 0f);
            createGroundedProp(67.8f, 0.0f, clusterSpr, 0.85f, 3, false, true);

            // Upper bypass route: Crumbling ancient ledge + Tracked Sawblade + Safe ledge
            createTiledPlatform(53.5f, 2.8f, 3.8f, 1.0f, floatingLedge, "SpikeBypass1_Crumble", true); // X=[51.6 .. 55.4]
            createSawblade(58.8f, 1.4f, new Vector3(0f, 2.5f, 0f), 2.1f, 0f);
            createTiledPlatform(63.8f, 3.4f, 3.8f, 1.0f, floatingLedge, "SpikeBypass2", false);        // X=[61.9 .. 65.7]
            createCrystal(53.5f, 3.9f);
            createCrystal(63.8f, 4.5f);

            // SECTION 4 (X: 70 to 97): Checkpoint #1 & Non-Colliding Dual Moving Conduit Floors
            createStonePlatform(75.0f, 1.6f, 8, "CheckpointPlat1", true, true);           // X=[70.2 .. 79.8], topY=1.6
            createTutorial(71.8f, 1.6f, "CONDUIT CHECKPOINT & MOVING FLOORS", "Ignite Conduit Beacons for auto-respawn and ride the timed Moving Conduit Floors!");
            createGroundedProp(73.3f, 1.6f, shroomSpr, 0.85f, 3, true, false);
            createCheckpoint(76.0f, 1.6f, "Checkpoint_ConduitBeacon_1");
            createCrystal(78.6f, 2.7f);

            // Mathematically verified NON-COLLIDING dual moving floors (1.8-unit clear buffer between sweeps!)
            createMovingPlatform(82.8f, 1.6f, 3.0f, new Vector3(3.6f, 0.5f, 0f), 1.5f, 0f, "MovingConduit_A");     // X sweep [81.3 .. 87.9]
            createMovingPlatform(91.2f, 2.2f, 3.0f, new Vector3(3.6f, -0.4f, 0f), 1.5f, 3.14f, "MovingConduit_B"); // X sweep [89.7 .. 96.3]
            createCrystal(84.6f, 3.4f);
            createCrystal(93.0f, 3.6f);

            // SECTION 5 (X: 98 to 125): Animated Scrap Crawler Arena & Pop-Up Spike Sawblade Bridge
            createStonePlatform(105.8f, 1.6f, 13, "CrawlerArena", true, true);            // X=[98.0 .. 113.6], topY=1.6
            createTutorial(99.4f, 1.6f, "SCRAP CRAWLER DRONES", "Jump onto Scrap Crawlers or Air Dash right through them to shatter them!");
            createGroundedProp(100.9f, 1.6f, clusterSpr, 0.82f, 3, false, true);
            createCrawler(103.0f, 1.6f, 2.4f, 2.3f);
            createCrawler(109.6f, 1.6f, 2.4f, 2.7f);
            createCrystal(103.0f, 3.0f);
            createCrystal(109.6f, 3.0f);

            createStonePlatform(120.2f, 2.4f, 7, "ArenaBridge", true, false);             // X=[116.0 .. 124.4], topY=2.4
            createPopUpSpikes(118.2f, 2.4f, 2.0f, true, 0f);
            createSawblade(121.6f, 3.1f, new Vector3(0f, 2.3f, 0f), 2.3f, 0.7f);
            createCrystal(120.2f, 4.7f);

            // SECTION 6 (X: 126 to 150): Animated Buzz Sentries, Vertical Lift, Crumbling Ledge & Anchored Ceiling Stalactites
            createCavernCeiling(139.5f, 7.6f, 22.0f, "CavernCeiling_Sec6");               // Continuous dark cave ceiling (NO floating grass!)
            createTiledPlatform(128.4f, 2.6f, 4.2f, 1.0f, floatingLedge, "SentryLedge1", false); // X=[126.3 .. 130.5]
            createTutorial(127.6f, 2.6f, "AERIAL SENTRIES & STALACTITES", "Watch the ceiling for dripping Acid Stalactites, and watch out for crumbling ledges!");
            createSentry(133.0f, 4.3f, 1.6f, 0.9f);
            createCrystal(133.0f, 5.5f);

            createMovingPlatform(136.8f, 2.6f, 3.0f, new Vector3(0f, 2.2f, 0f), 1.6f, 0f, "MovingConduit_Lift"); // X=[135.3 .. 138.3]
            createStalactite(136.8f, 7.6f);

            createTiledPlatform(143.5f, 3.8f, 4.0f, 1.0f, floatingLedge, "SentryLedge2_Crumble", true); // X=[141.5 .. 145.5]
            createStalactite(143.5f, 7.6f);
            createSentry(147.8f, 5.0f, 1.6f, 0.9f);
            createCrystal(147.8f, 6.2f);

            // SECTION 7 (X: 150 to 187): Checkpoint #2, Diagonal Lift & Unpredictable Reactor Gauntlet
            createStonePlatform(155.2f, 2.8f, 8, "CheckpointPlat2", true, true);          // X=[150.4 .. 160.0], topY=2.8
            createCheckpoint(154.5f, 2.8f, "Checkpoint_ConduitBeacon_2");
            createGroundedProp(158.0f, 2.8f, shroomSpr, 0.85f, 3, true, false);

            createMovingPlatform(163.0f, 2.8f, 3.0f, new Vector3(4.2f, 1.1f, 0f), 1.6f, 0f, "MovingConduit_Ascend"); // X sweep [161.5 .. 168.7]
            createCrystal(165.5f, 4.9f);

            createCavernCeiling(179.0f, 8.4f, 16.0f, "CavernCeiling_Sec7");
            createStonePlatform(178.3f, 3.8f, 13, "GauntletBridge", true, false);         // X=[170.5 .. 186.1], topY=3.8
            createPopUpSpikes(173.4f, 3.8f, 2.0f, false, 0.0f);                           // Rhythmic wave spikes!
            createPopUpSpikes(177.2f, 3.8f, 2.0f, true, 0.0f);                            // Surprise proximity ambush spikes!
            createSawblade(176.0f, 5.5f, new Vector3(5.0f, 0f, 0f), 2.0f, 0f);            // Overhead tracked horizontal sawblade!
            createCrawler(182.6f, 3.8f, 2.2f, 2.9f);
            createStalactite(181.5f, 8.4f);
            createCrystal(175.3f, 5.2f);
            createCrystal(182.6f, 5.1f);

            // SECTION 8 (X: 188 to 214): Final Crumbling Leap & Animated Core Reactor Portal
            createTiledPlatform(190.4f, 4.0f, 4.0f, 1.0f, floatingLedge, "FinalLedge_Crumble", true); // X=[188.4 .. 192.4]
            createTutorial(189.4f, 4.0f, "ANCIENT CORE REACTOR", "Dash past the final Sentry before the ledge collapses and enter the Core Reactor Portal!");
            createSentry(195.2f, 5.4f, 1.6f, 0.85f);
            createCrystal(195.2f, 6.6f);

            createStonePlatform(205.2f, 2.6f, 13, "ReactorFloor", true, true);            // X=[197.4 .. 213.0], topY=2.6
            createCrawler(200.6f, 2.6f, 2.2f, 2.7f);
            createGroundedProp(198.4f, 2.6f, clusterSpr, 0.90f, 3, false, true);
            createGroundedProp(211.2f, 2.6f, shroomSpr, 0.90f, 3, true, false);
            createCrystal(200.6f, 4.2f);

            // Animated 4-Frame Swirling Core Reactor Goal Portal (flush on topY=2.6)
            GameObject goalObj = new GameObject("CoreReactorGoal");
            goalObj.transform.SetParent(levelParent.transform);
            goalObj.transform.position = new Vector3(207.2f, 2.6f + 1.64f, 0f);
            goalObj.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
            SpriteRenderer goalSr = goalObj.AddComponent<SpriteRenderer>();
            goalSr.sprite = goalFrames[0];
            goalSr.sortingOrder = 3;
            addFrameAnimator(goalObj, goalSr, goalFrames, 8f, false, false, false);
            BoxCollider2D goalCol = goalObj.AddComponent<BoxCollider2D>();
            goalCol.isTrigger = true;
            goalCol.size = new Vector2(1.8f, 2.8f);
            goalObj.AddComponent<GoalZone>();

            // Multi-layered Unity Particle System Vortex on Goal Portal!
            CreateOrbitalVortexParticles(goalObj.transform, "ReactorVortexSwirl", Vector3.zero,
                new Color(0.25f, 1f, 0.92f, 0.95f), 24f, 1.4f, 0.28f, 0.6f, 4.2f, sparkleMat, 6, true);
            CreateAmbientParticles(goalObj.transform, "ReactorGoldenEmbers", Vector3.zero, new Vector3(2.0f, 2.4f, 0.1f),
                new Color(1f, 0.88f, 0.35f, 0.85f), 14f, 1.8f, 0.22f, 0.7f, sparkleMat, 7, true, -0.22f, true);

            // Ambient Cavern Bioluminescent Spores (with Noise turbulence!) & Depth Mist across entire 220-unit level
            for (int s = 0; s < 8; s++)
            {
                float sx = 8f + s * 29f;
                CreateAmbientParticles(ambienceParent.transform, $"CavernSpores_{s}", new Vector3(sx, 2.8f, 0f), new Vector3(28f, 8.5f, 1f),
                    new Color(0.25f, 0.98f, 1f, 0.55f), 12f, 4.0f, 0.18f, 0.45f, sparkleMat, 1, true, -0.03f, true);
                CreateAmbientParticles(ambienceParent.transform, $"CavernDepthMist_{s}", new Vector3(sx, -1.8f, 0f), new Vector3(28f, 3.0f, 1f),
                    new Color(0.18f, 0.55f, 0.72f, 0.18f), 5f, 5.2f, 1.25f, 0.25f, dustMat, -2, true, -0.01f, true);
            }

            // Abyss Kill Floor below entire level
            GameObject abyss = new GameObject("AbyssKillFloor");
            abyss.transform.SetParent(hazardsParent.transform);
            abyss.transform.position = new Vector3(105f, -7.8f, 0f);
            BoxCollider2D abyssCol = abyss.AddComponent<BoxCollider2D>();
            abyssCol.isTrigger = true;
            abyssCol.size = new Vector2(280f, 4f);
            Hazard abyssHaz = abyss.AddComponent<Hazard>();
            SerializedObject abyssSo = new SerializedObject(abyssHaz);
            abyssSo.FindProperty("isInstantKill").boolValue = true;
            abyssSo.ApplyModifiedProperties();

            // Build-time mathematical validation: Guarantee ZERO collisions between moving & static platforms!
            ValidateNoPlatformCollisions(platformSweeps);

            string scenePath = "Assets/Scenes/Level1.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(scenePath, true)
            };

            Debug.Log($"Level 1 Scene rebuilt cleanly with zero platform collisions and saved to {scenePath}!");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error in BuildScene: " + ex);
            throw;
        }
    }

    private static void ValidateNoPlatformCollisions(List<PlatformSweepBounds> sweeps)
    {
        float margin = 0.25f;
        for (int i = 0; i < sweeps.Count; i++)
        {
            if (!sweeps[i].IsMoving) continue;
            Rect a = sweeps[i].Bounds;
            Rect paddedA = Rect.MinMaxRect(a.xMin - margin, a.yMin - margin, a.xMax + margin, a.yMax + margin);

            for (int j = 0; j < sweeps.Count; j++)
            {
                if (i == j) continue;
                Rect b = sweeps[j].Bounds;
                if (paddedA.Overlaps(b))
                {
                    throw new System.Exception(
                        $"PLATFORM COLLISION DETECTED! Moving platform '{sweeps[i].Name}' sweep {a} overlaps '{sweeps[j].Name}' {b}!");
                }
            }
        }
        Debug.Log($"Validated {sweeps.Count} platforms: ZERO moving platform collisions or overlaps.");
    }

    private static Sprite[] LoadFrames(string formatPath, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string p = string.Format(formatPath, i);
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (frames[i] == null)
            {
                Debug.LogWarning($"Missing frame sprite at {p}");
            }
        }
        return frames;
    }

    private static ParticleSystem CreateAmbientParticles(Transform parent, string name, Vector3 localPos, Vector3 boxSize,
        Color color, float rateOverTime, float lifetime, float size, float speed, Material mat, int sortingOrder,
        bool playOnAwake = true, float gravity = -0.15f, bool enableNoise = true)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent);
        psGo.transform.localPosition = localPos;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = playOnAwake;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
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
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(color.a, 0.28f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        if (enableNoise)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.55f;
            noise.frequency = 0.75f;
            noise.scrollSpeed = 0.45f;
            noise.quality = ParticleSystemNoiseQuality.Medium;
        }

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.material = mat;

        return ps;
    }

    private static ParticleSystem CreateOrbitalVortexParticles(Transform parent, string name, Vector3 localPos,
        Color color, float rateOverTime, float lifetime, float size, float radius, float orbitalSpeed, Material mat,
        int sortingOrder, bool playOnAwake)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent);
        psGo.transform.localPosition = localPos;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = playOnAwake;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
        main.startSpeed = 0.25f;
        main.startColor = color;
        main.prewarm = playOnAwake;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rateOverTime;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.65f;

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.orbitalZ = new ParticleSystem.MinMaxCurve(orbitalSpeed);
        vel.radial = new ParticleSystem.MinMaxCurve(-0.35f);
        vel.y = new ParticleSystem.MinMaxCurve(0.45f);

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.material = mat;
        return ps;
    }

    private static ParticleSystem CreateThrusterParticles(Transform parent, string name, Vector3 localPos,
        float width, Color color, Material mat, int sortingOrder)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent);
        psGo.transform.localPosition = localPos;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 2.5f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 16f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(width, 0.08f, 0.1f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colOverLife.color = new ParticleSystem.MinMaxGradient(grad);

        var renderer = psGo.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.material = mat;
        return ps;
    }

    private static ParticleSystem CreateSparkShowerParticles(Transform parent, string name, Vector3 localPos,
        Color color, float rate, Material mat, int sortingOrder)
    {
        GameObject psGo = new GameObject(name);
        psGo.transform.SetParent(parent);
        psGo.transform.localPosition = localPos;
        ParticleSystem ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.48f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.10f, 0.20f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.6f);
        main.startColor = color;
        main.gravityModifier = 0.85f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.42f;
        shape.radiusThickness = 0.15f;

        var colOverLife = ps.colorOverLifetime;
        colOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.4f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
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

        AnimationClip idleClip = CreateSpriteClip("Assets/Animations/Kip_Idle.anim", idleFrames, 5.5f, true);
        AnimationClip runClip = CreateSpriteClip("Assets/Animations/Kip_Run.anim", runFrames, 15f, true);
        AnimationClip jumpClip = CreateSpriteClip("Assets/Animations/Kip_Jump.anim", jumpFrames, 12f, false);
        AnimationClip fallClip = CreateSpriteClip("Assets/Animations/Kip_Fall.anim", fallFrames, 10f, true);
        AnimationClip hurtClip = CreateSpriteClip("Assets/Animations/Kip_Hurt.anim", hurtFrames, 12f, false);
        AnimationClip dashClip = CreateSpriteClip("Assets/Animations/Kip_Dash.anim", dashFrames, 16f, false);

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
                bool isAcid = path.Contains("AcidPool_Frame");
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = (isParticle || isAcid) ? FilterMode.Bilinear : FilterMode.Point;
                importer.wrapMode = isAcid ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
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
            ("Screenshots/shot_1_spawn_and_character.png", new Vector3(3.5f, 1.8f, -10f)),
            ("Screenshots/shot_2_air_dash_and_conduit.png", new Vector3(30.5f, 2.2f, -10f)),
            ("Screenshots/shot_3_spikes_and_acid_pool.png", new Vector3(58.5f, 1.8f, -10f)),
            ("Screenshots/shot_4_checkpoint_and_moving.png", new Vector3(83.5f, 2.2f, -10f)),
            ("Screenshots/shot_4b_crawler_arena.png", new Vector3(106.5f, 2.2f, -10f)),
            ("Screenshots/shot_5_sentry_and_stalactites.png", new Vector3(138.0f, 3.6f, -10f)),
            ("Screenshots/shot_5b_gauntlet_bridge.png", new Vector3(177.0f, 4.4f, -10f)),
            ("Screenshots/shot_6_core_reactor_goal.png", new Vector3(203.0f, 3.6f, -10f)),
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
