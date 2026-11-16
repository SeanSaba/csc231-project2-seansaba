using UnityEngine;
using UnityEngine.UI;

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
    private int totalCrystals = 10;
    private float levelTime = 0f;

    private bool showTitleScreen = true;
    private bool showPauseMenu = false;
    private bool showWinScreen = false;
    private bool showGameOverScreen = false;

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle headerStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle boxStyle;
    private bool stylesInitialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (showTitleScreen)
        {
            Time.timeScale = 0f;
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

    private void InitStyles()
    {
        if (stylesInitialized) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0f, 0.95f, 1f) }
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Italic,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.9f, 0.85f, 0.7f) }
        };

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(0f, 1f, 1f) }
        };

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeSolidTexture(2, 2, new Color(0.08f, 0.08f, 0.14f, 0.92f)) }
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
        GUI.Box(new Rect(15, 15, 290, 115), "", boxStyle);

        // Health Hearts
        GUI.Label(new Rect(25, 22, 100, 25), "HEALTH:", headerStyle);
        for (int i = 0; i < playerMaxHealth; i++)
        {
            Color heartColor = (i < playerHealth) ? new Color(1f, 0.25f, 0.35f) : new Color(0.35f, 0.35f, 0.35f, 0.6f);
            var prev = GUI.color;
            GUI.color = heartColor;
            GUI.Label(new Rect(115 + (i * 28), 22, 26, 26), "♥", headerStyle);
            GUI.color = prev;
        }

        // Crystals Counter
        GUI.Label(new Rect(25, 52, 260, 25), $"CRYSTALS: {crystalsCollected} / {totalCrystals}", bodyStyle);

        // Dash Cooldown
        var player = FindFirstObjectByType<PlayerController>();
        float dashCooldown = (player != null) ? player.DashCooldownProgress : 1f;
        string dashStatus = (dashCooldown >= 0.99f) ? "READY" : $"{Mathf.RoundToInt(dashCooldown * 100)}%";
        GUI.Label(new Rect(25, 78, 260, 25), $"AIR DASH: {dashStatus}", bodyStyle);

        // Top-right Timer & Controls hint
        GUI.Box(new Rect(Screen.width - 240, 15, 225, 80), "", boxStyle);
        int mins = (int)(levelTime / 60);
        int secs = (int)(levelTime % 60);
        GUI.Label(new Rect(Screen.width - 225, 22, 200, 25), $"TIME: {mins:00}:{secs:00}", headerStyle);
        GUI.Label(new Rect(Screen.width - 225, 52, 200, 35), "[ESC / P] Pause", bodyStyle);

        // Bottom Controls Banner
        GUI.Box(new Rect(Screen.width / 2 - 260, Screen.height - 45, 520, 35), "", boxStyle);
        GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height - 40, 500, 25), "CONTROLS: [A/D] Move | [SPACE] Jump | [SHIFT/J] Air Dash", bodyStyle);
    }

    private void DrawTitleScreen()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 280, cy - 240, 560, 480), "", boxStyle);

        GUI.Label(new Rect(cx - 270, cy - 200, 540, 50), "KIP: CAVERN CONDUIT", titleStyle);
        GUI.Label(new Rect(cx - 270, cy - 150, 540, 30), "CSC 231 - Project 2: Platformer MVP", subtitleStyle);

        GUI.Label(new Rect(cx - 240, cy - 100, 480, 120), 
            "MISSION BRIEFING:\n" +
            "Guide Kip the Clockwork Scout through the overgrown reactor caverns.\n" +
            "Collect Aether Crystals, overcome Patrol Drones and Tesla Spikes,\n" +
            "activate the mid-station Checkpoint Beacon, and restore the Core Reactor!", bodyStyle);

        if (GUI.Button(new Rect(cx - 150, cy + 50, 300, 48), "START GAME", buttonStyle))
        {
            showTitleScreen = false;
            Time.timeScale = 1f;
        }

        if (GUI.Button(new Rect(cx - 150, cy + 110, 300, 45), (AudioManager.Instance != null && AudioManager.Instance.IsMuted) ? "UNMUTE AUDIO" : "MUTE AUDIO", buttonStyle))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 165, 300, 45), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }

    private void DrawPauseMenu()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 190, cy - 170, 380, 340), "", boxStyle);

        GUI.Label(new Rect(cx - 180, cy - 140, 360, 40), "GAME PAUSED", titleStyle);

        if (GUI.Button(new Rect(cx - 130, cy - 80, 260, 45), "RESUME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        }

        if (GUI.Button(new Rect(cx - 130, cy - 25, 260, 45), "RESTART LEVEL", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        if (GUI.Button(new Rect(cx - 130, cy + 30, 260, 45), (AudioManager.Instance != null && AudioManager.Instance.IsMuted) ? "UNMUTE AUDIO" : "MUTE AUDIO", buttonStyle))
        {
            if (AudioManager.Instance != null) AudioManager.Instance.ToggleMute();
        }

        if (GUI.Button(new Rect(cx - 130, cy + 85, 260, 45), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }

    private void DrawWinScreen()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 260, cy - 200, 520, 400), "", boxStyle);

        GUI.Label(new Rect(cx - 250, cy - 170, 500, 45), "LEVEL COMPLETE!", titleStyle);
        GUI.Label(new Rect(cx - 250, cy - 120, 500, 30), "Core Reactor Successfully Activated!", subtitleStyle);

        int mins = (int)(levelTime / 60);
        int secs = (int)(levelTime % 60);
        GUI.Label(new Rect(cx - 200, cy - 70, 400, 28), $"Total Time: {mins:00}:{secs:00}", headerStyle);
        GUI.Label(new Rect(cx - 200, cy - 35, 400, 28), $"Aether Crystals: {crystalsCollected} / {totalCrystals}", headerStyle);

        if (GUI.Button(new Rect(cx - 140, cy + 40, 280, 50), "PLAY AGAIN", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        if (GUI.Button(new Rect(cx - 140, cy + 105, 280, 45), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }

    private void DrawGameOverScreen()
    {
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "", boxStyle);

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.Box(new Rect(cx - 240, cy - 180, 480, 360), "", boxStyle);

        GUI.Label(new Rect(cx - 230, cy - 140, 460, 45), "ENERGY DEPLETED", titleStyle);
        GUI.Label(new Rect(cx - 230, cy - 90, 460, 30), "Kip was neutralized by cavern hazards.", subtitleStyle);

        if (GUI.Button(new Rect(cx - 150, cy - 20, 300, 50), "RESPAWN AT CHECKPOINT", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RespawnPlayerAtCheckpoint();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 45, 300, 48), "RESTART LEVEL", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        }

        if (GUI.Button(new Rect(cx - 150, cy + 105, 300, 45), "QUIT GAME", buttonStyle))
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        }
    }
}
