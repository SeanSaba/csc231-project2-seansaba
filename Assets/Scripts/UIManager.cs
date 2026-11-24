using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
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
    private int playerLives = 3;
    private int crystalsCollected = 0;
    private int totalCrystals = 20;
    private float levelTime = 0f;

    private bool showTitleScreen = true;
    private bool showPauseMenu = false;
    private bool showWinScreen = false;
    private bool showGameOverScreen = false;

    private TutorialSign activeTutorialSign;
    private string tutorialTitle = "";
    private string tutorialBody = "";
    private float toastTimer = 0f;

    // UGUI References
    private Font uiFont;
    private GameObject hudRoot;
    private GameObject titlePanel;
    private GameObject pausePanel;
    private GameObject winPanel;
    private GameObject gameOverPanel;
    private GameObject tutorialBanner;
    private GameObject toastBanner;
    private GameObject bossHealthPanel;
    private Image bossHealthFill;
    private Text bossNameText;
    private GameObject doubleJumpBadge;
    private Text doubleJumpBadgeText;

    private Image[] heartImages;
    private Text livesText;
    private Text crystalText;
    private Image dashIconImg;
    private Image dashBarFill;
    private Text timerText;
    private Text tutorialTitleText;
    private Text tutorialBodyText;
    private Text toastText;
    private Text winStatsText;
    private Text titleMuteBtnText;
    private Text pauseMuteBtnText;
    private Slider titleVolSlider;
    private Slider pauseVolSlider;

    private PlayerController cachedPlayer;

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

        LoadFont();
        EnsureEventSystem();
        BuildUGUI();
        RefreshPanelVisibility();
    }

    private void Start()
    {
        Time.timeScale = showTitleScreen ? 0f : 1f;
        UpdateHealthUI(playerHealth, playerMaxHealth);
        UpdateLivesUI(playerLives, 3);
        UpdateCrystalUI(crystalsCollected, totalCrystals);
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f && toastBanner != null)
            {
                toastBanner.SetActive(false);
            }
        }

        if (hudRoot != null && hudRoot.activeSelf)
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = FindAnyObjectByType<PlayerController>();
            }
            if (cachedPlayer != null && dashBarFill != null)
            {
                float prog = cachedPlayer.DashCooldownProgress;
                dashBarFill.fillAmount = prog;
                dashBarFill.color = (prog >= 0.99f) ? new Color(0f, 1f, 0.95f) : new Color(0.2f, 0.6f, 0.85f);
                if (dashIconImg != null)
                {
                    dashIconImg.color = (prog >= 0.99f) ? Color.white : new Color(0.55f, 0.55f, 0.55f, 0.8f);
                }
            }
        }
    }

    private void LoadFont()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 20);
        }
        if (uiFont == null)
        {
            uiFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
        }
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }
    }

    private Sprite TextureToSprite(Texture2D tex)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
    }

    private void BuildUGUI()
    {
        GameObject canvasGo = new GameObject("GameUICanvas");
        canvasGo.transform.SetParent(transform);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        Transform root = canvasGo.transform;

        Color panelBg = new Color(0.05f, 0.08f, 0.14f, 0.92f);
        Color overlayBg = new Color(0.02f, 0.04f, 0.08f, 0.85f);
        Color tipBg = new Color(0.04f, 0.16f, 0.24f, 0.92f);
        Color cyanAccent = new Color(0f, 0.95f, 1f);
        Color goldAccent = new Color(0.96f, 0.86f, 0.58f);

        // ================= HUD =================
        hudRoot = CreateUIObject("HUD", root);
        StretchFull(hudRoot.GetComponent<RectTransform>());

        // Top-Left Status Panel
        GameObject tlPanel = CreatePanel("TopLeftHUD", hudRoot.transform, panelBg, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(335f, 168f), new Vector2(0f, 1f));

        CreateText("HealthLabel", tlPanel.transform, "HEALTH:", 17, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -12f), new Vector2(88f, 28f), new Vector2(0f, 1f));

        Sprite heartSpr = TextureToSprite(heartFullTex);
        heartImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject hGo = CreateUIObject($"Heart_{i}", tlPanel.transform);
            RectTransform hRt = hGo.GetComponent<RectTransform>();
            hRt.anchorMin = hRt.anchorMax = new Vector2(0f, 1f);
            hRt.pivot = new Vector2(0f, 1f);
            hRt.anchoredPosition = new Vector2(102f + i * 32f, -12f);
            hRt.sizeDelta = new Vector2(26f, 26f);
            Image hImg = hGo.AddComponent<Image>();
            if (heartSpr != null) hImg.sprite = heartSpr;
            else hImg.color = new Color(1f, 0.25f, 0.35f);
            heartImages[i] = hImg;
        }

        livesText = CreateText("LivesText", tlPanel.transform, "LIVES: x3", 17, FontStyle.Bold, goldAccent, TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -12f), new Vector2(110f, 28f), new Vector2(1f, 1f));

        // Crystals row
        Sprite crystalSpr = TextureToSprite(crystalIconTex);
        if (crystalSpr != null)
        {
            GameObject cGo = CreateUIObject("CrystalIcon", tlPanel.transform);
            RectTransform cRt = cGo.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0f, 1f);
            cRt.pivot = new Vector2(0f, 1f);
            cRt.anchoredPosition = new Vector2(14f, -50f);
            cRt.sizeDelta = new Vector2(26f, 26f);
            cGo.AddComponent<Image>().sprite = crystalSpr;
        }
        crystalText = CreateText("CrystalText", tlPanel.transform, "CRYSTALS: 0 / 20", 17, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -50f), new Vector2(260f, 26f), new Vector2(0f, 1f));

        // Dash row
        Sprite dashSpr = TextureToSprite(dashIconTex);
        if (dashSpr != null)
        {
            GameObject dGo = CreateUIObject("DashIcon", tlPanel.transform);
            RectTransform dRt = dGo.GetComponent<RectTransform>();
            dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.anchoredPosition = new Vector2(14f, -90f);
            dRt.sizeDelta = new Vector2(26f, 26f);
            dashIconImg = dGo.AddComponent<Image>();
            dashIconImg.sprite = dashSpr;
        }
        CreateText("DashLabel", tlPanel.transform, "DASH:", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -90f), new Vector2(65f, 26f), new Vector2(0f, 1f));

        GameObject barBg = CreatePanel("DashBarBg", tlPanel.transform, new Color(0.14f, 0.20f, 0.28f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(115f, -95f), new Vector2(200f, 16f), new Vector2(0f, 1f));
        GameObject barFill = CreateUIObject("DashBarFill", barBg.transform);
        StretchFull(barFill.GetComponent<RectTransform>(), 2f);
        dashBarFill = barFill.AddComponent<Image>();
        dashBarFill.sprite = MakeWhiteSprite();
        dashBarFill.type = Image.Type.Filled;
        dashBarFill.fillMethod = Image.FillMethod.Horizontal;
        dashBarFill.fillAmount = 1f;
        dashBarFill.color = cyanAccent;

        // Double Jump Ability Badge row (lights up fiery orange when Orange Gem is absorbed!)
        doubleJumpBadge = CreatePanel("DoubleJumpBadge", tlPanel.transform, new Color(0.16f, 0.14f, 0.18f, 0.9f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -126f), new Vector2(302f, 28f), new Vector2(0f, 1f));
        doubleJumpBadgeText = CreateText("DoubleJumpText", doubleJumpBadge.transform, "DOUBLE JUMP: LOCKED (FIND ORANGE GEM)", 13, FontStyle.Bold,
            new Color(0.62f, 0.66f, 0.72f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        // Top-Right Timer Panel
        GameObject trPanel = CreatePanel("TopRightHUD", hudRoot.transform, panelBg, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(215f, 76f), new Vector2(1f, 1f));
        timerText = CreateText("TimerText", trPanel.transform, "TIME: 00:00", 18, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(-24f, 28f), new Vector2(0.5f, 1f));
        CreateText("PauseHint", trPanel.transform, "[ESC / P] Pause", 15, FontStyle.Normal, new Color(0.8f, 0.85f, 0.92f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -40f), new Vector2(-24f, 26f), new Vector2(0.5f, 1f));

        // Top-Center Tutorial Banner
        tutorialBanner = CreatePanel("TutorialBanner", hudRoot.transform, tipBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(520f, 82f), new Vector2(0.5f, 1f));
        tutorialTitleText = CreateText("TutorialTitle", tutorialBanner.transform, "[TIP]", 16, FontStyle.Bold, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(-24f, 24f), new Vector2(0.5f, 1f));
        tutorialBodyText = CreateText("TutorialBody", tutorialBanner.transform, "", 15, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -34f), new Vector2(-24f, 40f), new Vector2(0.5f, 1f));
        tutorialBanner.SetActive(false);

        // Boss Health Bar Panel (appears at top-center during Final Boss battle!)
        bossHealthPanel = CreatePanel("BossHealthPanel", hudRoot.transform, new Color(0.10f, 0.04f, 0.06f, 0.94f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(540f, 66f), new Vector2(0.5f, 1f));
        bossNameText = CreateText("BossNameText", bossHealthPanel.transform, "IRONCLAD CONDUIT OVERSEER", 16, FontStyle.Bold, new Color(1f, 0.78f, 0.30f), TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(-20f, 24f), new Vector2(0.5f, 1f));
        GameObject bossBarBg = CreatePanel("BossBarBg", bossHealthPanel.transform, new Color(0.22f, 0.10f, 0.12f, 1f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(500f, 22f), new Vector2(0.5f, 0f));
        GameObject bossBarFillGo = CreateUIObject("BossBarFill", bossBarBg.transform);
        StretchFull(bossBarFillGo.GetComponent<RectTransform>(), 2f);
        bossHealthFill = bossBarFillGo.AddComponent<Image>();
        bossHealthFill.sprite = MakeWhiteSprite();
        bossHealthFill.type = Image.Type.Filled;
        bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
        bossHealthFill.fillAmount = 1f;
        bossHealthFill.color = new Color(1f, 0.62f, 0.15f);
        bossHealthPanel.SetActive(false);

        // Toast Banner
        toastBanner = CreatePanel("ToastBanner", hudRoot.transform, tipBg, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(480f, 40f), new Vector2(0.5f, 1f));
        toastText = CreateText("ToastText", toastBanner.transform, "", 16, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-16f, 0f), new Vector2(0.5f, 0.5f));
        toastBanner.SetActive(false);

        // Bottom Controls Bar
        GameObject botPanel = CreatePanel("BottomControls", hudRoot.transform, panelBg, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(620f, 34f), new Vector2(0.5f, 0f));
        CreateText("ControlsHint", botPanel.transform, "CONTROLS: [A/D] Move   |   [SPACE / W] Jump   |   [SHIFT / J] Air Dash   |   [ESC] Pause",
            15, FontStyle.Normal, new Color(0.9f, 0.95f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-16f, 0f), new Vector2(0.5f, 0.5f));

        // ================= TITLE SCREEN =================
        titlePanel = CreatePanel("TitleOverlay", root, overlayBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject tBox = CreatePanel("TitleBox", titlePanel.transform, panelBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f), new Vector2(0.5f, 0.5f));

        CreateText("Title", tBox.transform, "KIP: CAVERN CONDUIT", 34, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(560f, 46f), new Vector2(0.5f, 1f));
        CreateText("Subtitle", tBox.transform, "CSC 231 - Project 2: Platformer MVP (Level 1)", 17, FontStyle.Italic, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(560f, 28f), new Vector2(0.5f, 1f));
        CreateText("Briefing", tBox.transform,
            "Guide Kip the Clockwork Scout through the Overgrown Reactor Caves.\n" +
            "Collect Aether Crystals, stomp or dash through Scrap Crawlers & Buzz Sentries,\n" +
            "dodge spinning Sawblades & Tesla Spikes, ignite Conduit Beacons,\n" +
            "and reach the Ancient Core Reactor!\n\n" +
            "Controls: [A / D] Move   |   [SPACE / W] Jump   |   [SHIFT / J] Air Dash",
            16, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(540f, 140f), new Vector2(0.5f, 1f));

        CreateButton("StartBtn", tBox.transform, "START GAME", new Vector2(0f, -30f), new Vector2(300f, 48f), () =>
        {
            showTitleScreen = false;
            Time.timeScale = 1f;
            RefreshPanelVisibility();
        });

        titleVolSlider = CreateVolumeSlider(tBox.transform, new Vector2(0f, -92f));
        titleMuteBtnText = CreateButton("TitleMuteBtn", tBox.transform, "MUTE AUDIO", new Vector2(0f, -142f), new Vector2(300f, 44f), ToggleAudioMute);
        CreateButton("TitleQuitBtn", tBox.transform, "QUIT GAME", new Vector2(0f, -198f), new Vector2(300f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });

        // ================= PAUSE MENU =================
        pausePanel = CreatePanel("PauseOverlay", root, overlayBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject pBox = CreatePanel("PauseBox", pausePanel.transform, panelBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 440f), new Vector2(0.5f, 0.5f));

        CreateText("PauseTitle", pBox.transform, "GAME PAUSED", 30, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(360f, 42f), new Vector2(0.5f, 1f));

        CreateButton("ResumeBtn", pBox.transform, "RESUME", new Vector2(0f, 108f), new Vector2(280f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        });
        CreateButton("PauseRestartBtn", pBox.transform, "RESTART LEVEL", new Vector2(0f, 54f), new Vector2(280f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        });
        pauseVolSlider = CreateVolumeSlider(pBox.transform, new Vector2(0f, 2f));
        pauseMuteBtnText = CreateButton("PauseMuteBtn", pBox.transform, "MUTE AUDIO", new Vector2(0f, -46f), new Vector2(280f, 44f), ToggleAudioMute);
        CreateButton("PauseTitleBtn", pBox.transform, "TITLE SCREEN", new Vector2(0f, -100f), new Vector2(280f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        });
        CreateButton("PauseQuitBtn", pBox.transform, "QUIT GAME", new Vector2(0f, -154f), new Vector2(280f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });

        // ================= WIN SCREEN =================
        winPanel = CreatePanel("WinOverlay", root, overlayBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject wBox = CreatePanel("WinBox", winPanel.transform, panelBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540f, 430f), new Vector2(0.5f, 0.5f));

        CreateText("WinTitle", wBox.transform, "LEVEL COMPLETE!", 34, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(480f, 46f), new Vector2(0.5f, 1f));
        CreateText("WinSub", wBox.transform, "Ancient Core Reactor Successfully Restored!", 17, FontStyle.Italic, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(480f, 28f), new Vector2(0.5f, 1f));
        winStatsText = CreateText("WinStats", wBox.transform, "", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(460f, 70f), new Vector2(0.5f, 1f));

        CreateButton("WinPlayAgainBtn", wBox.transform, "PLAY AGAIN", new Vector2(0f, -24f), new Vector2(290f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        });
        CreateButton("WinTitleBtn", wBox.transform, "TITLE SCREEN", new Vector2(0f, -84f), new Vector2(290f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        });
        CreateButton("WinQuitBtn", wBox.transform, "QUIT GAME", new Vector2(0f, -140f), new Vector2(290f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });

        // ================= GAME OVER SCREEN =================
        gameOverPanel = CreatePanel("GameOverOverlay", root, overlayBg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject gBox = CreatePanel("GameOverBox", gameOverPanel.transform, panelBg, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 420f), new Vector2(0.5f, 0.5f));

        CreateText("GOTitle", gBox.transform, "OUT OF LIVES", 34, FontStyle.Bold, new Color(1f, 0.35f, 0.42f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(440f, 46f), new Vector2(0.5f, 1f));
        CreateText("GOSub", gBox.transform, "Kip ran out of backup power cells.", 17, FontStyle.Italic, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(440f, 28f), new Vector2(0.5f, 1f));

        CreateButton("GORetryBtn", gBox.transform, "RETRY FROM CHECKPOINT", new Vector2(0f, 20f), new Vector2(300f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RespawnPlayerAtCheckpoint();
        });
        CreateButton("GORestartBtn", gBox.transform, "RESTART LEVEL", new Vector2(0f, -38f), new Vector2(300f, 46f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        });
        CreateButton("GOTitleBtn", gBox.transform, "TITLE SCREEN", new Vector2(0f, -94f), new Vector2(300f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        });
        CreateButton("GOQuitBtn", gBox.transform, "QUIT GAME", new Vector2(0f, -148f), new Vector2(300f, 44f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });
    }

    private void ToggleAudioMute()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMute();
            string txt = AudioManager.Instance.IsMuted ? "UNMUTE AUDIO" : "MUTE AUDIO";
            if (titleMuteBtnText != null) titleMuteBtnText.text = txt;
            if (pauseMuteBtnText != null) pauseMuteBtnText.text = txt;
        }
    }

    private void RefreshPanelVisibility()
    {
        if (titlePanel != null) titlePanel.SetActive(showTitleScreen);
        if (winPanel != null) winPanel.SetActive(!showTitleScreen && showWinScreen);
        if (gameOverPanel != null) gameOverPanel.SetActive(!showTitleScreen && !showWinScreen && showGameOverScreen);
        if (pausePanel != null) pausePanel.SetActive(!showTitleScreen && !showWinScreen && !showGameOverScreen && showPauseMenu);
        if (hudRoot != null) hudRoot.SetActive(!showTitleScreen && !showWinScreen && !showGameOverScreen);
    }

    public void UpdateHealthUI(int current, int max)
    {
        playerHealth = current;
        playerMaxHealth = max;
        if (heartImages == null) return;
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                heartImages[i].color = (i < playerHealth) ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.45f);
            }
        }
    }

    public void UpdateLivesUI(int current, int max)
    {
        playerLives = current;
        if (livesText != null)
        {
            livesText.text = $"LIVES: x{playerLives}";
        }
    }

    public void UpdateCrystalUI(int collected, int total)
    {
        crystalsCollected = collected;
        totalCrystals = total;
        if (crystalText != null)
        {
            crystalText.text = $"CRYSTALS: {crystalsCollected} / {totalCrystals}";
        }
    }

    public void UpdateTimerUI(float time)
    {
        levelTime = time;
        if (timerText != null)
        {
            int mins = (int)(levelTime / 60);
            int secs = (int)(levelTime % 60);
            timerText.text = $"TIME: {mins:00}:{secs:00}";
        }
    }

    public void SetPauseMenuVisible(bool visible)
    {
        showPauseMenu = visible;
        if (visible && AudioManager.Instance != null && pauseVolSlider != null)
        {
            pauseVolSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        }
        RefreshPanelVisibility();
    }

    public void ShowWinScreen(float time, int collected, int total)
    {
        showWinScreen = true;
        levelTime = time;
        crystalsCollected = collected;
        totalCrystals = total;
        if (winStatsText != null)
        {
            int mins = (int)(levelTime / 60);
            int secs = (int)(levelTime % 60);
            winStatsText.text = $"Completion Time: {mins:00}:{secs:00}\nAether Crystals Collected: {crystalsCollected} / {totalCrystals}";
        }
        RefreshPanelVisibility();
    }

    public void ShowGameOverScreen()
    {
        showGameOverScreen = true;
        RefreshPanelVisibility();
    }

    public void HideGameOverScreen()
    {
        showGameOverScreen = false;
        RefreshPanelVisibility();
    }

    public void SetTutorialPrompt(TutorialSign source, string title, string body)
    {
        activeTutorialSign = source;
        tutorialTitle = title;
        tutorialBody = body;
        if (tutorialBanner != null)
        {
            tutorialTitleText.text = $"[TIP] {tutorialTitle}";
            tutorialBodyText.text = tutorialBody;
            tutorialBanner.SetActive(true);
        }
    }

    public void ClearTutorialPrompt(TutorialSign source)
    {
        if (activeTutorialSign == source)
        {
            activeTutorialSign = null;
            tutorialTitle = "";
            tutorialBody = "";
            if (tutorialBanner != null)
            {
                tutorialBanner.SetActive(false);
            }
        }
    }

    public void ShowToastNotification(string message, float duration = 2.5f)
    {
        toastTimer = duration;
        if (toastBanner != null && toastText != null)
        {
            toastText.text = message;
            toastBanner.SetActive(true);
        }
    }

    // ================= UGUI Helper Builders =================

    public void SetDoubleJumpUnlockedUI(bool unlocked)
    {
        if (doubleJumpBadge != null)
        {
            Image bg = doubleJumpBadge.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = unlocked ? new Color(0.55f, 0.24f, 0.04f, 0.96f) : new Color(0.16f, 0.14f, 0.18f, 0.9f);
            }
        }
        if (doubleJumpBadgeText != null)
        {
            doubleJumpBadgeText.text = unlocked ? "DOUBLE JUMP: UNLOCKED [2x SPACE]" : "DOUBLE JUMP: LOCKED (FIND ORANGE GEM)";
            doubleJumpBadgeText.color = unlocked ? new Color(1f, 0.85f, 0.35f) : new Color(0.62f, 0.66f, 0.72f);
        }
    }

    public void UpdateBossHealthUI(string name, int hp, int maxHp, bool isAngry)
    {
        if (bossHealthPanel == null) return;
        bossHealthPanel.SetActive(true);
        if (tutorialBanner != null) tutorialBanner.SetActive(false);

        if (bossNameText != null)
        {
            bossNameText.text = isAngry ? $"{name}  [ENRAGED!]" : name;
            bossNameText.color = isAngry ? new Color(1f, 0.32f, 0.24f) : new Color(1f, 0.80f, 0.32f);
        }
        if (bossHealthFill != null)
        {
            bossHealthFill.fillAmount = (maxHp > 0) ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            bossHealthFill.color = isAngry ? new Color(0.98f, 0.18f, 0.14f) : new Color(1f, 0.58f, 0.12f);
        }
    }

    public void HideBossHealthUI()
    {
        if (bossHealthPanel != null)
        {
            bossHealthPanel.SetActive(false);
        }
    }

    private Sprite MakeWhiteSprite()
    {
        Texture2D tex = new Texture2D(2, 2);
        Color[] cols = { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private void StretchFull(RectTransform rt, float padding = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }

    private GameObject CreatePanel(string name, Transform parent, Color bgColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        return go;
    }

    private Text CreateText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;

        Text t = go.AddComponent<Text>();
        t.font = uiFont;
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    private Text CreateButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.14f, 0.22f, 0.34f, 1f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, sizeDelta, new Vector2(0.5f, 0.5f));
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.14f, 0.22f, 0.34f, 1f);
        cb.highlightedColor = new Color(0.18f, 0.42f, 0.58f, 1f);
        cb.pressedColor = new Color(0.08f, 0.65f, 0.78f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        return CreateText("Label", go.transform, label, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
    }

    private Slider CreateVolumeSlider(Transform parent, Vector2 anchoredPos)
    {
        GameObject row = CreateUIObject("VolumeRow", parent);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = anchoredPos;
        rowRt.sizeDelta = new Vector2(280f, 30f);

        CreateText("VolLabel", row.transform, "VOLUME:", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(85f, 28f), new Vector2(0f, 0.5f));

        GameObject slGo = CreateUIObject("Slider", row.transform);
        RectTransform slRt = slGo.GetComponent<RectTransform>();
        slRt.anchorMin = slRt.anchorMax = new Vector2(1f, 0.5f);
        slRt.pivot = new Vector2(1f, 0.5f);
        slRt.anchoredPosition = Vector2.zero;
        slRt.sizeDelta = new Vector2(185f, 20f);

        GameObject bg = CreatePanel("Bg", slGo.transform, new Color(0.18f, 0.24f, 0.32f, 1f),
            new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject fillArea = CreateUIObject("FillArea", slGo.transform);
        StretchFull(fillArea.GetComponent<RectTransform>());
        GameObject fill = CreatePanel("Fill", fillArea.transform, new Color(0f, 0.85f, 0.95f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject handleArea = CreateUIObject("HandleArea", slGo.transform);
        StretchFull(handleArea.GetComponent<RectTransform>());
        GameObject handle = CreatePanel("Handle", handleArea.transform, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 20f), new Vector2(0.5f, 0.5f));

        Slider slider = slGo.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = (AudioManager.Instance != null) ? AudioManager.Instance.MasterVolume : 1f;
        slider.onValueChanged.AddListener((v) =>
        {
            if (AudioManager.Instance != null) AudioManager.Instance.SetVolume(v);
            if (titleVolSlider != null && titleVolSlider != slider) titleVolSlider.SetValueWithoutNotify(v);
            if (pauseVolSlider != null && pauseVolSlider != slider) pauseVolSlider.SetValueWithoutNotify(v);
        });
        return slider;
    }
}
