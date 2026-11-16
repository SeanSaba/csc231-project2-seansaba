using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Visuals")]
    [SerializeField] private Texture2D heartFullTex;
    [SerializeField] private Texture2D crystalIconTex;
    [SerializeField] private Texture2D dashIconTex;

    private int playerHealth = 3;
    private int playerMaxHealth = 3;
    private int crystalsCollected = 0;
    private int totalCrystals = 18;
    private float levelTime = 0f;

    private bool showTitleScreen = true;
    private bool showPauseMenu = false;
    private bool showWinScreen = false;
    private bool showGameOverScreen = false;

    private TutorialSign activeTutorialSign;
    private string tutorialTitle = "";
    private string tutorialBody = "";

    private string toastMessage = "";
    private float toastTimer = 0f;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private GUIStyle centerBodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private GUIStyle tutorialBoxStyle;
    private Texture2D barBgTex;
    private Texture2D barFillTex;
    private Texture2D solidPanelTex;
    private Texture2D tutorialPanelTex;
    private bool stylesInitialized = false;

    public bool IsTitleScreenVisible => showTitleScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (GameManager.SkipTitleOnNextLoad)
        {
            showTitleScreen = false;
            GameManager.SkipTitleOnNextLoad = false;
        }
    }

    private void Start()
    {
        Time.timeScale = showTitleScreen ? 0f : 1f;
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
        }
    }

    public void UpdateHealthUI(int current, int max)
    {
        playerHealth = current;
        playerMaxHealth = max;
    }

    public void UpdateCrystalUI(int collected, int total)
    {
        crystalsCollected = collected;
        totalCrystals = total;
    }

    public void UpdateTimerUI(float time)
    {
        levelTime = time;
    }

    public void SetPauseMenuVisible(bool visible)
    {
        showPauseMenu = visible;
    }

    public void ShowWinScreen(float time, int collected, int total)
    {
        showWinScreen = true;
        levelTime = time;
        crystalsCollected = collected;
        totalCrystals = total;
    }

    public void ShowGameOverScreen()
    {
        showGameOverScreen = true;
    }

    public void HideGameOverScreen()
    {
        showGameOverScreen = false;
    }

    public void SetTutorialPrompt(TutorialSign source, string title, string body)
    {
        activeTutorialSign = source;
        tutorialTitle = title;
        tutorialBody = body;
    }

    public void ClearTutorialPrompt(TutorialSign source)
    {
        if (activeTutorialSign == source)
        {
            activeTutorialSign = null;
            tutorialTitle = "";
            tutorialBody = "";
        }
    }

    public void ShowToastNotification(string message, float duration = 2.5f)
    {
        toastMessage = message;
        toastTimer = duration;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        solidPanelTex = MakeSolidTexture(2, 2, new Color(0.06f, 0.08f, 0.14f, 0.92f));
        tutorialPanelTex = MakeSolidTexture(2, 2, new Color(0.04f, 0.14f, 0.22f, 0.90f));
        barBgTex = MakeSolidTexture(2, 2, new Color(0.15f, 0.20f, 0.28f, 0.95f));
        barFillTex = MakeSolidTexture(2, 2, new Color(0f, 0.90f, 1f, 1f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0f, 0.95f, 1f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.88f, 0.68f) }
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.94f, 0.98f) }
        };

        centerBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.92f, 0.97f, 1f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 17,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(0f, 1f, 1f) }
        };

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = solidPanelTex }
        };

        tutorialBoxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = tutorialPanelTex }
        };

        stylesInitialized = true;
    }

    private Texture2D MakeSolidTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        InitStyles();

        if (showTitleScreen)
        {
            DrawTitleScreen();
            return;
        }

        if (showWinScreen)
        {
            DrawWinScreen();
            return;
        }

        if (showGameOverScreen)
        {
            DrawGameOverScreen();
            return;
        }

        if (showPauseMenu)
        {
            DrawPauseMenu();
        }

        DrawHUD();
    }

    private void DrawHUD()
    {
        // Top-left HUD panel
        GUI.Box(new Rect(15, 15, 310, 128), "", boxStyle);

        // Health Hearts
        GUI.Label(new Rect(28, 22, 95, 28), "HEALTH:", headerStyle);
        for (int i = 0; i < playerMaxHealth; i++)
        {
            Rect hRect = new Rect(126 + (i * 34), 22, 28, 28);
            Color prev = GUI.color;
            GUI.color = (i < playerHealth) ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.45f);
            if (heartFullTex != null)
            {
                GUI.DrawTexture(hRect, heartFullTex, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.color = (i < playerHealth) ? new Color(1f, 0.25f, 0.35f) : new Color(0.35f, 0.35f, 0.35f, 0.6f);
                GUI.Label(hRect, "♥", headerStyle);
            }
            GUI.color = prev;
        }

        // Crystals Counter with icon
        if (crystalIconTex != null)
        {
            GUI.DrawTexture(new Rect(28, 56, 26, 26), crystalIconTex, ScaleMode.ScaleToFit);
        }
        GUI.Label(new Rect(60, 56, 250, 26), $"CRYSTALS: {crystalsCollected} / {totalCrystals}", headerStyle);

        // Dash Cooldown Icon + Bar
        var player = FindAnyObjectByType<PlayerController>();
        float dashCooldown = (player != null) ? player.DashCooldownProgress : 1f;
        if (dashIconTex != null)
        {
            Color prev = GUI.color;
            GUI.color = (dashCooldown >= 0.99f) ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.7f);
            GUI.DrawTexture(new Rect(28, 92, 26, 26), dashIconTex, ScaleMode.ScaleToFit);
            GUI.color = prev;
        }
        GUI.Label(new Rect(60, 92, 75, 26), "DASH:", bodyStyle);
        Rect barBack = new Rect(122, 97, 175, 16);
        GUI.DrawTexture(barBack, barBgTex);
        Rect barFill = new Rect(124, 99, 171 * dashCooldown, 12);
        Color prevC = GUI.color;
        GUI.color = (dashCooldown >= 0.99f) ? new Color(0f, 1f, 0.95f) : new Color(0.2f, 0.65f, 0.85f);
        GUI.DrawTexture(barFill, barFillTex);
        GUI.color = prevC;

        // Top-right Timer & Pause hint
        GUI.Box(new Rect(Screen.width - 240, 15, 225, 76), "", boxStyle);
        int mins = (int)(levelTime / 60);
        int secs = (int)(levelTime % 60);
        GUI.Label(new Rect(Screen.width - 224, 22, 200, 28), $"TIME: {mins:00}:{secs:00}", headerStyle);
        GUI.Label(new Rect(Screen.width - 224, 52, 200, 26), "[ESC / P] Pause", bodyStyle);

        // Active Tutorial Prompt Banner (top-center)
        if (activeTutorialSign != null && !string.IsNullOrEmpty(tutorialBody))
        {
            float bw = Mathf.Min(560f, Screen.width - 680f);
            if (bw < 360f) bw = 360f;
            float bx = (Screen.width - bw) / 2f;
            GUI.Box(new Rect(bx, 15, bw, 76), "", tutorialBoxStyle);
            GUI.Label(new Rect(bx + 12, 20, bw - 24, 24), $"[TIP] {tutorialTitle}", subtitleStyle);
            GUI.Label(new Rect(bx + 12, 44, bw - 24, 40), tutorialBody, centerBodyStyle);
        }

        // Toast Notification (e.g. Checkpoint Activated)
        if (toastTimer > 0f && !string.IsNullOrEmpty(toastMessage))
        {
            GUI.Box(new Rect(Screen.width / 2f - 180f, 102f, 360f, 40f), "", tutorialBoxStyle);
            GUI.Label(new Rect(Screen.width / 2f - 170f, 108f, 340f, 28f), toastMessage, subtitleStyle);
        }

        // Bottom Controls Banner
        GUI.Box(new Rect(Screen.width / 2f - 280f, Screen.height - 46f, 560f, 34f), "", boxStyle);
        GUI.Label(new Rect(Screen.width / 2f - 270f, Screen.height - 42f, 540f, 26f),
            "CONTROLS: [A/D] Move  |  [SPACE / W] Jump  |  [SHIFT / J] Air Dash  |  [ESC] Pause", centerBodyStyle);
    }

    private void DrawAudioControls(float cx, float yStart)
    {
        if (AudioManager.Instance == null) return;

        GUI.Label(new Rect(cx - 150, yStart, 110, 26), "VOLUME:", bodyStyle);
        float vol = AudioManager.Instance.MasterVolume;
        float newVol = GUI.HorizontalSlider(new Rect(cx - 40, yStart + 7, 190, 20), vol, 0f, 1f);
        if (!Mathf.Approximately(newVol, vol))
        {
            AudioManager.Instance.SetVolume(newVol);
        }
    }

    private void DrawTitleScreen()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 300, cy - 260, 600, 520), "", boxStyle);

        GUI.Label(new Rect(cx - 280, cy - 230, 560, 48), "KIP: CAVERN CONDUIT", titleStyle);
        GUI.Label(new Rect(cx - 280, cy - 182, 560, 28), "CSC 231 - Project 2: Platformer MVP (Level 1)", subtitleStyle);

        GUI.Label(new Rect(cx - 255, cy - 135, 510, 125),
            "MISSION BRIEFING:\n" +
            "Guide Kip the Clockwork Fox-Rabbit Scout through the Overgrown Reactor Caves.\n" +
            "Collect Aether Crystals, outmaneuver Scrap Crawlers & Buzz Sentries,\n" +
            "ignite the Conduit Beacons, and reach the Core Reactor Gateway!\n\n" +
            "Controls: [A/D] Move  |  [SPACE] Jump  |  [SHIFT / J] Energy Air Dash",
            centerBodyStyle);

        if (GUI.Button(new Rect(cx - 150, cy + 15, 300, 48), "START GAME", buttonStyle))
        {
            showTitleScreen = false;
            Time.timeScale = 1f;
        }

        DrawAudioControls(cx, cy + 78);

        string muteLabel = (AudioManager.Instance != null && AudioManager.Instance.IsMuted) ? "UNMUTE AUDIO" : "MUTE AUDIO";
        if (GUI.Button(new Rect(cx - 150, cy + 115, 300, 44), muteLabel, buttonStyle))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 170, 300, 44), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }

    private void DrawPauseMenu()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 210, cy - 210, 420, 420), "", boxStyle);

        GUI.Label(new Rect(cx - 190, cy - 188, 380, 42), "GAME PAUSED", titleStyle);

        if (GUI.Button(new Rect(cx - 140, cy - 130, 280, 44), "RESUME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        if (GUI.Button(new Rect(cx - 140, cy - 76, 280, 44), "RESTART LEVEL", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        DrawAudioControls(cx, cy - 20);

        string muteLabel = (AudioManager.Instance != null && AudioManager.Instance.IsMuted) ? "UNMUTE AUDIO" : "MUTE AUDIO";
        if (GUI.Button(new Rect(cx - 140, cy + 18, 280, 44), muteLabel, buttonStyle))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        if (GUI.Button(new Rect(cx - 140, cy + 72, 280, 44), "TITLE SCREEN", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        }

        if (GUI.Button(new Rect(cx - 140, cy + 126, 280, 44), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }

    private void DrawWinScreen()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 270, cy - 220, 540, 440), "", boxStyle);

        GUI.Label(new Rect(cx - 250, cy - 190, 500, 46), "LEVEL COMPLETE!", titleStyle);
        GUI.Label(new Rect(cx - 250, cy - 140, 500, 28), "Core Reactor Successfully Activated!", subtitleStyle);

        int mins = (int)(levelTime / 60);
        int secs = (int)(levelTime % 60);
        GUI.Label(new Rect(cx - 200, cy - 88, 400, 28), $"Completion Time: {mins:00}:{secs:00}", headerStyle);
        GUI.Label(new Rect(cx - 200, cy - 52, 400, 28), $"Aether Crystals Collected: {crystalsCollected} / {totalCrystals}", headerStyle);

        if (GUI.Button(new Rect(cx - 145, cy + 10, 290, 48), "PLAY AGAIN", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        if (GUI.Button(new Rect(cx - 145, cy + 70, 290, 44), "TITLE SCREEN", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        }

        if (GUI.Button(new Rect(cx - 145, cy + 126, 290, 44), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }

    private void DrawGameOverScreen()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 250, cy - 210, 500, 420), "", boxStyle);

        GUI.Label(new Rect(cx - 230, cy - 175, 460, 46), "ENERGY DEPLETED", titleStyle);
        GUI.Label(new Rect(cx - 230, cy - 125, 460, 28), "Kip was neutralized by cavern hazards.", subtitleStyle);

        if (GUI.Button(new Rect(cx - 150, cy - 60, 300, 48), "RESPAWN AT CHECKPOINT", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RespawnPlayerAtCheckpoint();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 2, 300, 46), "RESTART LEVEL", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 60, 300, 44), "TITLE SCREEN", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 116, 300, 44), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }
}
