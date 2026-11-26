using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Opt-in live-engine gameplay demo video recorder.
/// Completely dormant during normal play; activates ONLY when launched with `-record-demo <outputFolder>`.
/// Captures crisp 1280x720 30 FPS frames (including full bespoke UGUI screens & HUD) of:
///   1. Title Screen -> Start Game
///   2. Live Movement, Jumps, Air Dash, Crystals, Pop-Up Spikes & Acid Pool
///   3. Checkpoint Beacon #1 activation + Real Death & Checkpoint Respawn
///   4. Scrap Crawler combat & Orange Double-Jump Gem summoning the Final Boss ("FINAL BOSS, FIGHT!!")
///   5. Final Boss seismic walk, laser targeting, dark lighting, head-stomp defeat -> Peace & Freedom restore
///   6. Animated Core Reactor Portal entry & self-closing implosion -> "LEVEL COMPLETE!" Win Screen
///   7. 3-Lives-Lost 6-frame Kip Final Death Animation -> "OUT OF LIVES" Game Over Screen
/// </summary>
public class DemoVideoRecorder : MonoBehaviour
{
    private string outputDir;
    private bool isRecording;
    private int frameCounter;
    private Texture2D readTex;
    private string audioLogPath;

    private void Awake()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-record-demo")
            {
                outputDir = args[i + 1];
                break;
            }
        }
        if (string.IsNullOrEmpty(outputDir))
        {
            enabled = false;
            return;
        }

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
        audioLogPath = Path.Combine(outputDir, "audio_events.txt");
        File.WriteAllText(audioLogPath, "");
        Time.captureFramerate = 30;
        Application.runInBackground = true;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(outputDir)) return;
        StartCoroutine(MasterDemoRoutine());
        StartCoroutine(FrameCaptureLoop());
    }

    private void LogAudioEvent(string evt)
    {
        float sec = frameCounter / 30f;
        File.AppendAllText(audioLogPath, string.Format("{0:0.000}|{1}\n", sec, evt));
    }

    private IEnumerator FrameCaptureLoop()
    {
        isRecording = true;
        yield return null;

        while (isRecording)
        {
            yield return new WaitForEndOfFrame();
            int w = Screen.width;
            int h = Screen.height;
            if (readTex == null || readTex.width != w || readTex.height != h)
            {
                if (readTex != null) Destroy(readTex);
                readTex = new Texture2D(w, h, TextureFormat.RGB24, false);
            }
            readTex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readTex.Apply(false);
            byte[] jpg = readTex.EncodeToJPG(92);
            File.WriteAllBytes(Path.Combine(outputDir, string.Format("frame_{0:D5}.jpg", frameCounter)), jpg);
            frameCounter++;
        }
    }

    private IEnumerator WaitFrames(int frames)
    {
        for (int i = 0; i < frames; i++) yield return null;
    }

    private IEnumerator MasterDemoRoutine()
    {
        yield return WaitFrames(4);
        LogAudioEvent("START_APOCALYPSE_MUSIC");

        // ========== SCENE 1: TITLE SCREEN (4.5s = 135 frames) ==========
        // Title Screen is visible with Time.timeScale == 0; WaitForEndOfFrame still ticks at 30 FPS!
        yield return WaitFrames(135);

        if (UIManager.Instance != null) UIManager.Instance.DismissTitleScreen();
        yield return WaitFrames(20);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        PlayerController pc = playerObj != null ? playerObj.GetComponent<PlayerController>() : null;
        PlayerHealth ph = playerObj != null ? playerObj.GetComponent<PlayerHealth>() : null;
        Rigidbody2D prb = playerObj != null ? playerObj.GetComponent<Rigidbody2D>() : null;
        if (pc == null || ph == null || prb == null)
        {
            FinishRecording();
            yield break;
        }

        // ========== SCENE 2: SECTION 1 & 2 — RUN, JUMP, CRYSTALS & SIGNATURE AIR DASH (12s) ==========
        // Run right across SpawnPlatform & jump onto StepPlat1 -> collect Crystal #1
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 5.2f, 90);
        pc.SetDemoInput(true, 1f, true, true, false);
        LogAudioEvent("JUMP");
        yield return WaitFrames(18);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 12.6f, 90);

        // Jump onto StepPlat2 -> collect Crystal #2
        pc.SetDemoInput(true, 1f, true, true, false);
        LogAudioEvent("JUMP");
        yield return WaitFrames(18);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 20.1f, 90);

        // Signature Move: Jump + Mid-Air DASH across the Abyssal Chasm collecting Crystal #3!
        pc.SetDemoInput(true, 1f, true, true, false);
        LogAudioEvent("JUMP");
        yield return WaitFrames(14);
        pc.SetDemoInput(true, 1f, false, false, true);
        LogAudioEvent("DASH");
        yield return WaitFrames(26);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 34.5f, 90);
        pc.SetDemoInput(true, 0f, false, false, false);
        yield return WaitFrames(15);

        // ========== SCENE 3: SECTION 3 & 4 — POP-UP SPIKES, ACID POOL, CHECKPOINT #1 (9s) ==========
        // Collect crystals up to Checkpoint 1 cleanly so Checkpoint 1 ignites smoothly!
        CollectAllCrystalsBeforeX(75.0f);
        TeleportKip(playerObj, prb, new Vector3(49.5f, 1.0f, 0f));
        yield return WaitFrames(10);

        // Run toward Pop-Up Spikes (they telegraph & erupt!), jump cleanly onto elevated bypass over Acid Pool!
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitFrames(26);
        pc.SetDemoInput(true, 1f, true, true, false);
        LogAudioEvent("JUMP");
        yield return WaitFrames(16);
        pc.SetDemoInput(true, 1f, false, false, true);
        LogAudioEvent("DASH");
        yield return WaitFrames(30);
        pc.SetDemoInput(true, 0f, false, false, false);

        // Advance onto Checkpoint #1 Temple and ignite Conduit Beacon #1!
        TeleportKip(playerObj, prb, new Vector3(71.2f, 2.6f, 0f));
        yield return WaitFrames(8);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 76.2f, 90);
        LogAudioEvent("CHECKPOINT");
        pc.SetDemoInput(true, 0f, false, false, false);
        yield return WaitFrames(45);

        // ========== SCENE 4: REAL DEATH & CHECKPOINT RESPAWN (9s) ==========
        // Move to Crawler Arena, show Kip getting hit -> losing hearts -> dying -> auto-respawning at Checkpoint #1!
        TeleportKip(playerObj, prb, new Vector3(100.2f, 2.6f, 0f));
        yield return WaitFrames(18);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitFrames(20);
        pc.SetDemoInput(true, 0f, false, false, false);

        // Take 3 clear hits (or 2 hits + lethal hit) showing ruby hearts breaking to empty sockets, then death & auto-respawn!
        ph.TakeDamage(1, playerObj.transform.position + Vector3.right);
        LogAudioEvent("HURT");
        yield return WaitFrames(42);
        ph.TakeDamage(1, playerObj.transform.position + Vector3.right);
        LogAudioEvent("HURT");
        yield return WaitFrames(42);
        ph.TakeDamage(1, playerObj.transform.position + Vector3.right);
        LogAudioEvent("HURT");
        // Wait cleanly for auto-respawn back at Checkpoint #1 (shows LIVES: x2 + respawn flash!)
        yield return WaitFrames(70);

        // ========== SCENE 5: COMBAT SHOWCASE — AIR DASH SHATTERING CRAWLERS (4s) ==========
        TeleportKip(playerObj, prb, new Vector3(100.2f, 2.6f, 0f));
        yield return WaitFrames(10);
        pc.SetDemoInput(true, 1f, false, false, true);
        LogAudioEvent("DASH");
        yield return WaitFrames(35);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitFrames(40);
        pc.SetDemoInput(true, 0f, false, false, false);

        // ========== SCENE 6: ORANGE GEM & FINAL BOSS BATTLE ("FINAL BOSS, FIGHT!!") (22s) ==========
        // Collect remaining blue crystals cleanly so Kip enters Final Boss with 20/20 Crystals!
        CollectAllCrystalsBeforeX(216.0f);
        TeleportKip(playerObj, prb, new Vector3(216.2f, 3.6f, 0f));
        yield return WaitFrames(18);

        // Run right into the Special Orange Gem -> triggers Double Jump + Boss sky-drop + dark lighting + "FINAL BOSS, FIGHT!!"
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 220.4f, 80);
        LogAudioEvent("COLLECT");
        LogAudioEvent("BOSS_INTRO");
        pc.SetDemoInput(true, 0f, false, false, false);
        yield return WaitFrames(55); // Let banner & boss drop-slam shine!

        // Jump onto the Boss Arena block to confront the stomping Dread Conduit Overseer!
        pc.SetDemoInput(true, 1f, true, true, false);
        LogAudioEvent("JUMP");
        yield return WaitFrames(18);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 229.5f, 90);
        pc.SetDemoInput(true, 0f, false, false, false);
        yield return WaitFrames(95); // Showcase dark boss lighting, 4-frame stomping walk, seismic ground shake, and laser target fire!

        // Showcase Double Jump + Head Stomp -> Boss Angry Phase -> Final Lethal Head Stomp!
        FinalBoss boss = FindAnyObjectByType<FinalBoss>();
        if (boss != null)
        {
            // Jump & Double-Jump cleanly above the boss's head to stomp him!
            pc.SetDemoInput(true, 1f, true, true, false);
            LogAudioEvent("JUMP");
            yield return WaitFrames(14);
            pc.SetDemoInput(true, 1f, true, true, false);
            LogAudioEvent("JUMP");
            yield return WaitFrames(16);
            pc.SetDemoInput(true, 0f, false, false, false);

            // Dramatically weaken boss via head stomps to showcase Enraged Phase & Final Fury explosions!
            var takeDmgMethod = typeof(FinalBoss).GetMethod("TakeBossDamage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (takeDmgMethod != null)
            {
                // Drop boss to half health -> Angry Roar + crimson dark lighting + lightning flash!
                takeDmgMethod.Invoke(boss, new object[] { 6, true });
                LogAudioEvent("BOSS_ROAR");
                yield return WaitFrames(78);

                // Final lethal head-stomp -> boss explodes -> lighting & Peace & Freedom music restore -> Portal opens!
                TeleportKip(playerObj, prb, new Vector3(boss.transform.position.x - 1.2f, 5.8f, 0f));
                prb.linearVelocity = new Vector2(2f, -4f);
                yield return WaitFrames(8);
                takeDmgMethod.Invoke(boss, new object[] { 6, true });
                LogAudioEvent("BOSS_DEFEAT_PEACE");
                yield return WaitFrames(90);
            }
        }

        // ========== SCENE 7: PEACEFUL PORTAL ENTRY -> ANIMATED CLOSE -> WIN SCREEN (11s) ==========
        TeleportKip(playerObj, prb, new Vector3(243.0f, 3.6f, 0f));
        yield return WaitFrames(24);
        pc.SetDemoInput(true, 1f, false, false, false);
        yield return WaitUntilX(pc, 248.3f, 90);
        LogAudioEvent("WIN");
        pc.SetDemoInput(true, 0f, false, false, false);
        // Watch Kip get pulled into the swirling portal, portal implode shut, fireworks, and LEVEL COMPLETE! Win Screen!
        yield return WaitFrames(155);

        // ========== SCENE 8: BONUS — 3-LIVES FINAL DEATH ANIMATION -> GAME OVER SCREEN (7s) ==========
        // Briefly reactivate Kip on SpawnPlatform to showcase the 6-frame Final Death Animation -> OUT OF LIVES Game Over screen!
        if (UIManager.Instance != null)
        {
            var winFlag = typeof(UIManager).GetField("showWinScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var refreshM = typeof(UIManager).GetMethod("RefreshPanelVisibility", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (winFlag != null) winFlag.SetValue(UIManager.Instance, false);
            if (refreshM != null) refreshM.Invoke(UIManager.Instance, null);
        }
        playerObj.SetActive(true);
        playerObj.transform.rotation = Quaternion.identity;
        playerObj.transform.localScale = Vector3.one;
        SpriteRenderer psr = playerObj.GetComponentInChildren<SpriteRenderer>();
        if (psr != null) psr.color = Color.white;
        prb.simulated = true;
        prb.bodyType = RigidbodyType2D.Dynamic;
        prb.gravityScale = 3f;
        pc.enabled = true;
        pc.ResetState(new Vector3(1.5f, 1.0f, 0f));
        if (Camera.main != null) Camera.main.transform.position = new Vector3(1.5f, 2.3f, -10f);
        yield return WaitFrames(15);

        // Trigger the authentic 3rd-life loss -> 6-frame Kip Final Death Animation -> OUT OF LIVES Game Over Screen!
        if (GameManager.Instance != null)
        {
            var livesF = typeof(GameManager).GetField("currentLives", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var compF  = typeof(GameManager).GetField("isLevelComplete", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (livesF != null) livesF.SetValue(GameManager.Instance, 1);
            if (compF != null) compF.SetValue(GameManager.Instance, false);
            LogAudioEvent("PLAYER_DEATH");
            GameManager.Instance.OnPlayerDied();
        }
        yield return WaitFrames(175); // Full 6-frame death animation + 2.5s holding on OUT OF LIVES Game Over screen!

        FinishRecording();
    }

    private void TeleportKip(GameObject playerObj, Rigidbody2D prb, Vector3 pos)
    {
        playerObj.transform.position = pos;
        prb.linearVelocity = Vector2.zero;
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(pos.x + 1.5f, Mathf.Clamp(pos.y + 1.4f, -1f, 22f), -10f);
        }
    }

    private void CollectAllCrystalsBeforeX(float maxX)
    {
        Collectible[] all = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && !all[i].IsCollected && all[i].transform.position.x < maxX)
            {
                if (GameManager.Instance != null) GameManager.Instance.AddCollectible(1);
                Destroy(all[i].gameObject);
            }
        }
    }

    private IEnumerator WaitUntilX(PlayerController pc, float targetX, int maxFrames)
    {
        int waited = 0;
        while (pc != null && pc.transform.position.x < targetX && waited < maxFrames)
        {
            waited++;
            yield return null;
        }
    }

    private void FinishRecording()
    {
        isRecording = false;
        File.WriteAllText(Path.Combine(outputDir, "done.txt"), frameCounter.ToString());
        Application.Quit();
    }
}
