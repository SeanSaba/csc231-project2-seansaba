using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Bespoke Generated UI Artwork")]
    [SerializeField] private Texture2D titleBackdropTex;
    [SerializeField] private Texture2D panelFrameTex;
    [SerializeField] private Texture2D buttonFrameTex;
    [SerializeField] private Texture2D bossFrameTex;
    [SerializeField] private Texture2D barFrameTex;
    [SerializeField] private Texture2D ribbonFrameTex;

    [Header("Bespoke HUD Icons")]
    [SerializeField] private Texture2D heartFullTex;
    [SerializeField] private Texture2D heartEmptyTex;
    [SerializeField] private Texture2D crystalIconTex;
    [SerializeField] private Texture2D orangeGemIconTex;
    [SerializeField] private Texture2D dashIconTex;
    [SerializeField] private Texture2D livesIconTex;

    private int playerHealth = 3;
    private int playerMaxHealth = 3;
    private int playerLives = 3;
    private int crystalsCollected = 0;
    private int totalCrystals = 20;
    private int orangeGemsCollected = 0;
    private int totalOrangeGems = 1;
    private float levelTime = 0f;

    private bool showTitleScreen = true;
    private bool showPauseMenu = false;
    private bool showWinScreen = false;
    private bool showGameOverScreen = false;

    private TutorialSign activeTutorialSign;
    private string tutorialTitle = "";
    private string tutorialBody = "";
    private float toastTimer = 0f;

    // Cached bespoke 9-slice & icon Sprites
    private Font uiFont;
    private Sprite titleBackdropSpr;
    private Sprite panelFrameSpr;
    private Sprite buttonFrameSpr;
    private Sprite bossFrameSpr;
    private Sprite barFrameSpr;
    private Sprite ribbonFrameSpr;
    private Sprite heartFullSpr;
    private Sprite heartEmptySpr;
    private Sprite whiteSpr;

    // UGUI References
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
    private GameObject bossIntroBanner;
    private CanvasGroup bossIntroGroup;
    private Text bossIntroTitleText;
    private Text bossIntroSubText;
    private bool isBossIntroActive;
    private Image bossDarknessImage;
    private Image bossLightningImage;
    private Image deathFadeImage;
    private GameObject finalDeathBanner;
    private Text finalDeathTitleText;
    private Text finalDeathSubText;
    private float bossStepVignettePulse;
    private GameObject doubleJumpBadge;
    private Text doubleJumpBadgeText;

    private Image[] heartImages;
    private Text livesText;
    private Text crystalText;
    private Text orangeGemText;
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

        // Freeze physics/game immediately on Title Screen so zero startup collisions or sounds occur on frame 0!
        Time.timeScale = showTitleScreen ? 0f : 1f;

        LoadFont();
        PrepareSprites();
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
        UpdateOrangeGemUI(orangeGemsCollected, totalOrangeGems);
    }

    private void Update()
    {
        if (bossStepVignettePulse > 0f)
        {
            bossStepVignettePulse = Mathf.MoveTowards(bossStepVignettePulse, 0f, Time.unscaledDeltaTime * 0.75f);
        }

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
                dashBarFill.color = (prog >= 0.99f) ? new Color(0.12f, 0.96f, 1f) : new Color(0.18f, 0.54f, 0.80f);
                if (dashIconImg != null)
                {
                    dashIconImg.color = (prog >= 0.99f) ? Color.white : new Color(0.55f, 0.58f, 0.65f, 0.85f);
                }
            }
        }
    }

    private void LoadFont()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 20);
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
    }

    private void PrepareSprites()
    {
        whiteSpr = MakeWhiteSprite();
        titleBackdropSpr = TextureToSprite(titleBackdropTex);
        // 9-slice borders tuned precisely to our bespoke generated pixel-art frames!
        panelFrameSpr  = TextureToSlicedSprite(panelFrameTex,  new Vector4(76f, 72f, 76f, 72f));
        buttonFrameSpr = TextureToSlicedSprite(buttonFrameTex, new Vector4(72f, 26f, 72f, 26f));
        bossFrameSpr   = TextureToSlicedSprite(bossFrameTex,   new Vector4(104f, 62f, 104f, 62f));
        barFrameSpr    = TextureToSlicedSprite(barFrameTex,    new Vector4(96f, 24f, 96f, 24f));
        ribbonFrameSpr = TextureToSlicedSprite(ribbonFrameTex, new Vector4(84f, 24f, 84f, 24f));
        heartFullSpr   = TextureToSprite(heartFullTex);
        heartEmptySpr  = TextureToSprite(heartEmptyTex);
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
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite TextureToSlicedSprite(Texture2D tex, Vector4 border)
    {
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
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

        Color overlayDim = new Color(0.02f, 0.03f, 0.07f, 0.78f);
        Color cyanAccent = new Color(0.18f, 0.96f, 1f);
        Color goldAccent = new Color(1f, 0.86f, 0.44f);
        Color crimsonAccent = new Color(1f, 0.28f, 0.24f);

        // ================= BOSS DARK SCARY LIGHTING & DEATH FADE (Rendered BEHIND all HUD text!) =================
        GameObject darkGo = CreateUIObject("BossDarknessVignette", root);
        StretchFull(darkGo.GetComponent<RectTransform>());
        bossDarknessImage = darkGo.AddComponent<Image>();
        bossDarknessImage.sprite = MakeVignetteSprite(128);
        bossDarknessImage.color = new Color(0.02f, 0.01f, 0.05f, 0f);
        bossDarknessImage.raycastTarget = false;

        GameObject lightGo = CreateUIObject("BossLightningFlash", root);
        StretchFull(lightGo.GetComponent<RectTransform>());
        bossLightningImage = lightGo.AddComponent<Image>();
        bossLightningImage.sprite = whiteSpr;
        bossLightningImage.color = new Color(0.75f, 0.80f, 1f, 0f);
        bossLightningImage.raycastTarget = false;

        GameObject fadeGo = CreateUIObject("DeathFadeOverlay", root);
        StretchFull(fadeGo.GetComponent<RectTransform>());
        deathFadeImage = fadeGo.AddComponent<Image>();
        deathFadeImage.sprite = whiteSpr;
        deathFadeImage.color = new Color(0.06f, 0.0f, 0.01f, 0f);
        deathFadeImage.raycastTarget = false;

        // ================= HUD =================
        hudRoot = CreateUIObject("HUD", root);
        StretchFull(hudRoot.GetComponent<RectTransform>());

        // Top-Left Ornate Steampunk Status Panel
        GameObject tlPanel = CreateOrnatePanel("TopLeftHUD", hudRoot.transform, panelFrameSpr, Color.white, 3.3f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -14f), new Vector2(362f, 224f), new Vector2(0f, 1f));

        CreateOutlinedText("HealthLabel", tlPanel.transform, "HP:", 16, FontStyle.Bold, goldAccent, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -22f), new Vector2(44f, 28f), new Vector2(0f, 1f));

        heartImages = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject hGo = CreateUIObject($"Heart_{i}", tlPanel.transform);
            RectTransform hRt = hGo.GetComponent<RectTransform>();
            hRt.anchorMin = hRt.anchorMax = new Vector2(0f, 1f);
            hRt.pivot = new Vector2(0f, 1f);
            hRt.anchoredPosition = new Vector2(68f + i * 34f, -20f);
            hRt.sizeDelta = new Vector2(30f, 30f);
            Image hImg = hGo.AddComponent<Image>();
            hImg.preserveAspect = true;
            if (heartFullSpr != null) hImg.sprite = heartFullSpr;
            else hImg.color = new Color(1f, 0.25f, 0.35f);
            heartImages[i] = hImg;
        }

        // Bespoke Kip Scout Medallion Icon + Lives Counter
        Sprite livesSpr = TextureToSprite(livesIconTex);
        if (livesSpr != null)
        {
            GameObject lIconGo = CreateUIObject("LivesIcon", tlPanel.transform);
            RectTransform lRt = lIconGo.GetComponent<RectTransform>();
            lRt.anchorMin = lRt.anchorMax = new Vector2(1f, 1f);
            lRt.pivot = new Vector2(1f, 1f);
            lRt.anchoredPosition = new Vector2(-104f, -19f);
            lRt.sizeDelta = new Vector2(32f, 32f);
            Image lImg = lIconGo.AddComponent<Image>();
            lImg.sprite = livesSpr;
            lImg.preserveAspect = true;
        }
        livesText = CreateOutlinedText("LivesText", tlPanel.transform, "LIVES: x3", 16, FontStyle.Bold, goldAccent, TextAnchor.MiddleRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -22f), new Vector2(96f, 28f), new Vector2(1f, 1f));

        // Blue Crystals row
        Sprite crystalSpr = TextureToSprite(crystalIconTex);
        if (crystalSpr != null)
        {
            GameObject cGo = CreateUIObject("CrystalIcon", tlPanel.transform);
            RectTransform cRt = cGo.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0f, 1f);
            cRt.pivot = new Vector2(0f, 1f);
            cRt.anchoredPosition = new Vector2(26f, -58f);
            cRt.sizeDelta = new Vector2(26f, 26f);
            Image cImg = cGo.AddComponent<Image>();
            cImg.sprite = crystalSpr;
            cImg.preserveAspect = true;
        }
        crystalText = CreateOutlinedText("CrystalText", tlPanel.transform, "CRYSTALS: 0 / 20", 16, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -58f), new Vector2(270f, 26f), new Vector2(0f, 1f));

        // Orange Crystal row (directly below Blue Crystals, starts at 0 / 1)
        Sprite orangeSpr = TextureToSprite(orangeGemIconTex);
        if (orangeSpr != null)
        {
            GameObject ogGo = CreateUIObject("OrangeGemIcon", tlPanel.transform);
            RectTransform ogRt = ogGo.GetComponent<RectTransform>();
            ogRt.anchorMin = ogRt.anchorMax = new Vector2(0f, 1f);
            ogRt.pivot = new Vector2(0f, 1f);
            ogRt.anchoredPosition = new Vector2(26f, -92f);
            ogRt.sizeDelta = new Vector2(26f, 26f);
            Image ogImg = ogGo.AddComponent<Image>();
            ogImg.sprite = orangeSpr;
            ogImg.preserveAspect = true;
        }
        orangeGemText = CreateOutlinedText("OrangeGemText", tlPanel.transform, "ORANGE CRYSTAL: 0 / 1", 15, FontStyle.Bold, new Color(1f, 0.72f, 0.25f), TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -92f), new Vector2(276f, 26f), new Vector2(0f, 1f));

        // Bespoke Dash Row with Ornate Gear-Wing Lightning Icon & Clockwork Energy Tube Bar!
        Sprite dashSpr = TextureToSprite(dashIconTex);
        if (dashSpr != null)
        {
            GameObject dGo = CreateUIObject("DashIcon", tlPanel.transform);
            RectTransform dRt = dGo.GetComponent<RectTransform>();
            dRt.anchorMin = dRt.anchorMax = new Vector2(0f, 1f);
            dRt.pivot = new Vector2(0f, 1f);
            dRt.anchoredPosition = new Vector2(24f, -126f);
            dRt.sizeDelta = new Vector2(32f, 32f);
            dashIconImg = dGo.AddComponent<Image>();
            dashIconImg.sprite = dashSpr;
            dashIconImg.preserveAspect = true;
        }
        CreateOutlinedText("DashLabel", tlPanel.transform, "DASH:", 15, FontStyle.Bold, cyanAccent, TextAnchor.MiddleLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(60f, -129f), new Vector2(62f, 26f), new Vector2(0f, 1f));

        GameObject barBg = CreateOrnatePanel("DashBarFrame", tlPanel.transform, barFrameSpr, Color.white, 3.4f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(122f, -129f), new Vector2(214f, 26f), new Vector2(0f, 1f));
        GameObject barFill = CreateUIObject("DashBarFill", barBg.transform);
        RectTransform bfRt = barFill.GetComponent<RectTransform>();
        bfRt.anchorMin = Vector2.zero;
        bfRt.anchorMax = Vector2.one;
        bfRt.offsetMin = new Vector2(28f, 6f);
        bfRt.offsetMax = new Vector2(-28f, -6f);
        dashBarFill = barFill.AddComponent<Image>();
        dashBarFill.sprite = whiteSpr;
        dashBarFill.type = Image.Type.Filled;
        dashBarFill.fillMethod = Image.FillMethod.Horizontal;
        dashBarFill.fillAmount = 1f;
        dashBarFill.color = cyanAccent;

        // Bespoke Ribbon Plaque for Double Jump Status
        doubleJumpBadge = CreateOrnatePanel("DoubleJumpBadge", tlPanel.transform, ribbonFrameSpr, new Color(0.70f, 0.72f, 0.80f, 1f), 3.6f,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -168f), new Vector2(314f, 34f), new Vector2(0f, 1f));
        doubleJumpBadgeText = CreateOutlinedText("DoubleJumpText", doubleJumpBadge.transform, "DOUBLE JUMP: LOCKED (ORANGE GEM)", 12, FontStyle.Bold,
            new Color(0.75f, 0.80f, 0.88f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-42f, -4f), new Vector2(0.5f, 0.5f));
        doubleJumpBadgeText.resizeTextForBestFit = true;
        doubleJumpBadgeText.resizeTextMinSize = 10;
        doubleJumpBadgeText.resizeTextMaxSize = 13;

        // Top-Right Ornate Steampunk Timer Panel
        GameObject trPanel = CreateOrnatePanel("TopRightHUD", hudRoot.transform, panelFrameSpr, Color.white, 3.6f,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-14f, -14f), new Vector2(236f, 92f), new Vector2(1f, 1f));
        timerText = CreateOutlinedText("TimerText", trPanel.transform, "TIME: 00:00", 17, FontStyle.Bold, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(-36f, 28f), new Vector2(0.5f, 1f));
        CreateOutlinedText("PauseHint", trPanel.transform, "[ESC / P] Pause", 14, FontStyle.Normal, new Color(0.84f, 0.90f, 0.96f), TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f), new Vector2(-36f, 24f), new Vector2(0.5f, 1f));

        // Top-Center Ornate Ribbon Tutorial Banner (fits cleanly between TopLeft & TopRight HUD!)
        tutorialBanner = CreateOrnatePanel("TutorialBanner", hudRoot.transform, ribbonFrameSpr, Color.white, 2.8f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(540f, 92f), new Vector2(0.5f, 1f));
        tutorialTitleText = CreateOutlinedText("TutorialTitle", tutorialBanner.transform, "[TIP]", 15, FontStyle.Bold, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -12f), new Vector2(-78f, 22f), new Vector2(0.5f, 1f));
        tutorialBodyText = CreateOutlinedText("TutorialBody", tutorialBanner.transform, "", 14, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -36f), new Vector2(-78f, 44f), new Vector2(0.5f, 1f));
        tutorialBodyText.resizeTextForBestFit = true;
        tutorialBodyText.resizeTextMinSize = 11;
        tutorialBodyText.resizeTextMaxSize = 14;
        tutorialBanner.SetActive(false);

        // Boss Health Bar Panel — Spiked Obsidian-Magma Dread Frame + Clockwork Energy Tube!
        bossHealthPanel = CreateOrnatePanel("BossHealthPanel", hudRoot.transform, bossFrameSpr, Color.white, 2.8f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(570f, 96f), new Vector2(0.5f, 1f));
        bossNameText = CreateOutlinedText("BossNameText", bossHealthPanel.transform, "DREAD CONDUIT OVERSEER", 16, FontStyle.Bold, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(-92f, 24f), new Vector2(0.5f, 1f));
        GameObject bossBarBg = CreateOrnatePanel("BossBarBg", bossHealthPanel.transform, barFrameSpr, new Color(1f, 0.72f, 0.65f, 1f), 3.2f,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(456f, 28f), new Vector2(0.5f, 0f));
        GameObject bossBarFillGo = CreateUIObject("BossBarFill", bossBarBg.transform);
        RectTransform bbfRt = bossBarFillGo.GetComponent<RectTransform>();
        bbfRt.anchorMin = Vector2.zero;
        bbfRt.anchorMax = Vector2.one;
        bbfRt.offsetMin = new Vector2(30f, 7f);
        bbfRt.offsetMax = new Vector2(-30f, -7f);
        bossHealthFill = bossBarFillGo.AddComponent<Image>();
        bossHealthFill.sprite = whiteSpr;
        bossHealthFill.type = Image.Type.Filled;
        bossHealthFill.fillMethod = Image.FillMethod.Horizontal;
        bossHealthFill.fillAmount = 1f;
        bossHealthFill.color = new Color(1f, 0.56f, 0.14f);
        bossHealthPanel.SetActive(false);

        // Toast Banner — Ornate Steampunk Ribbon Plaque positioned cleanly below Tutorial/BossHealth!
        toastBanner = CreateOrnatePanel("ToastBanner", hudRoot.transform, ribbonFrameSpr, Color.white, 3.0f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -114f), new Vector2(620f, 52f), new Vector2(0.5f, 1f));
        toastText = CreateOutlinedText("ToastText", toastBanner.transform, "", 15, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-84f, -10f), new Vector2(0.5f, 0.5f));
        toastText.resizeTextForBestFit = true;
        toastText.resizeTextMinSize = 11;
        toastText.resizeTextMaxSize = 15;
        toastBanner.SetActive(false);

        // Big Cinematic "FINAL BOSS" + "FIGHT!!" Center Banner in Spiked Obsidian-Magma Dread Frame!
        bossIntroBanner = CreateOrnatePanel("BossIntroBanner", hudRoot.transform, bossFrameSpr, Color.white, 1.9f,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 24f), new Vector2(760f, 232f), new Vector2(0.5f, 0.5f));
        bossIntroGroup = bossIntroBanner.AddComponent<CanvasGroup>();
        bossIntroTitleText = CreateOutlinedText("BossIntroTitle", bossIntroBanner.transform, "FINAL BOSS", 58, FontStyle.Bold, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-130f, 76f), new Vector2(0.5f, 1f), 3f);
        bossIntroSubText = CreateOutlinedText("BossIntroSub", bossIntroBanner.transform, "FIGHT!!", 50, FontStyle.Bold, crimsonAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 38f), new Vector2(-130f, 64f), new Vector2(0.5f, 0f), 3f);
        bossIntroBanner.SetActive(false);

        // Dramatic Top-Center Final Death Banner (leaves center screen CLEAR so player watches Kip's 6-frame death animation!)
        finalDeathBanner = CreateOrnatePanel("FinalDeathBanner", root, bossFrameSpr, new Color(1f, 0.72f, 0.72f, 0.98f), 2.4f,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(680f, 154f), new Vector2(0.5f, 1f));
        finalDeathTitleText = CreateOutlinedText("DeathTitle", finalDeathBanner.transform, "KIP HAS FALLEN", 42, FontStyle.Bold, crimsonAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(-120f, 54f), new Vector2(0.5f, 1f), 2.5f);
        finalDeathSubText = CreateOutlinedText("DeathSub", finalDeathBanner.transform, "ALL 3 LIVES LOST — RESETTING LEVEL...", 18, FontStyle.Bold, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 28f), new Vector2(-120f, 32f), new Vector2(0.5f, 0f));
        finalDeathBanner.SetActive(false);

        // Bottom Controls Ornate Plaque
        GameObject botPanel = CreateOrnatePanel("BottomControls", hudRoot.transform, ribbonFrameSpr, Color.white, 3.2f,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(690f, 44f), new Vector2(0.5f, 0f));
        CreateOutlinedText("ControlsHint", botPanel.transform, "CONTROLS: [A/D] Move   |   [SPACE / W] Jump   |   [SHIFT / J] Air Dash   |   [ESC] Pause",
            14, FontStyle.Bold, new Color(0.92f, 0.96f, 1f), TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-92f, -6f), new Vector2(0.5f, 0.5f));

        // ================= TITLE SCREEN (Full Bespoke Generated Key-Art Backdrop + Ornate Steampunk Frame!) =================
        titlePanel = CreateUIObject("TitleOverlay", root);
        StretchFull(titlePanel.GetComponent<RectTransform>());
        Image titleBgImg = titlePanel.AddComponent<Image>();
        if (titleBackdropSpr != null)
        {
            titleBgImg.sprite = titleBackdropSpr;
            titleBgImg.color = Color.white;
        }
        else
        {
            titleBgImg.color = overlayDim;
        }
        // Subtle vignette framing over key art so center menu pops with dramatic contrast
        GameObject titleVig = CreateUIObject("TitleVignette", titlePanel.transform);
        StretchFull(titleVig.GetComponent<RectTransform>());
        Image tvImg = titleVig.AddComponent<Image>();
        tvImg.sprite = MakeVignetteSprite(128);
        tvImg.color = new Color(0.01f, 0.02f, 0.05f, 0.58f);
        tvImg.raycastTarget = false;

        GameObject tBox = CreateOrnatePanel("TitleBox", titlePanel.transform, panelFrameSpr, new Color(1f, 1f, 1f, 0.96f), 2.2f,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 560f), new Vector2(0.5f, 0.5f));

        CreateOutlinedText("Title", tBox.transform, "KIP: CAVERN CONDUIT", 36, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(580f, 48f), new Vector2(0.5f, 1f), 2.5f);
        CreateOutlinedText("Subtitle", tBox.transform, "CSC 231 - Project 2: Platformer MVP (Level 1)", 16, FontStyle.Italic, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(580f, 26f), new Vector2(0.5f, 1f));
        CreateOutlinedText("Briefing", tBox.transform,
            "Guide Kip the Clockwork Scout through the crumbling Reactor Caves.\n" +
            "Collect every Aether Crystal to unlock Conduit Beacons, absorb the\n" +
            "Special Orange Gem, defeat the Dread Overseer, and restore peace!\n\n" +
            "[A / D] Move   |   [SPACE / W] Jump   |   [SHIFT / J] Air Dash",
            15, FontStyle.Normal, new Color(0.92f, 0.96f, 1f), TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(560f, 130f), new Vector2(0.5f, 1f));

        CreateSteampunkButton("StartBtn", tBox.transform, "START GAME", new Vector2(0f, -26f), new Vector2(330f, 54f), () =>
        {
            showTitleScreen = false;
            Time.timeScale = 1f;
            RefreshPanelVisibility();
        });

        titleVolSlider = CreateVolumeSlider(tBox.transform, new Vector2(0f, -94f));
        titleMuteBtnText = CreateSteampunkButton("TitleMuteBtn", tBox.transform, "MUTE AUDIO", new Vector2(0f, -152f), new Vector2(330f, 48f), ToggleAudioMute);
        CreateSteampunkButton("TitleQuitBtn", tBox.transform, "QUIT GAME", new Vector2(0f, -210f), new Vector2(330f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });

        // ================= PAUSE MENU =================
        pausePanel = CreateFlatPanel("PauseOverlay", root, overlayDim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject pBox = CreateOrnatePanel("PauseBox", pausePanel.transform, panelFrameSpr, Color.white, 2.3f,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(470f, 490f), new Vector2(0.5f, 0.5f));

        CreateOutlinedText("PauseTitle", pBox.transform, "GAME PAUSED", 30, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(380f, 42f), new Vector2(0.5f, 1f), 2f);

        CreateSteampunkButton("ResumeBtn", pBox.transform, "RESUME", new Vector2(0f, 114f), new Vector2(310f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.TogglePause();
        });
        CreateSteampunkButton("PauseRestartBtn", pBox.transform, "RESTART LEVEL", new Vector2(0f, 56f), new Vector2(310f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        });
        pauseVolSlider = CreateVolumeSlider(pBox.transform, new Vector2(0f, -2f));
        pauseMuteBtnText = CreateSteampunkButton("PauseMuteBtn", pBox.transform, "MUTE AUDIO", new Vector2(0f, -58f), new Vector2(310f, 48f), ToggleAudioMute);
        CreateSteampunkButton("PauseTitleBtn", pBox.transform, "TITLE SCREEN", new Vector2(0f, -114f), new Vector2(310f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        });
        CreateSteampunkButton("PauseQuitBtn", pBox.transform, "QUIT GAME", new Vector2(0f, -170f), new Vector2(310f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });

        // ================= WIN SCREEN =================
        winPanel = CreateFlatPanel("WinOverlay", root, overlayDim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject wBox = CreateOrnatePanel("WinBox", winPanel.transform, panelFrameSpr, Color.white, 2.2f,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590f, 480f), new Vector2(0.5f, 0.5f));

        CreateOutlinedText("WinTitle", wBox.transform, "LEVEL COMPLETE!", 34, FontStyle.Bold, cyanAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(490f, 46f), new Vector2(0.5f, 1f), 2.5f);
        CreateOutlinedText("WinSub", wBox.transform, "Peace & Freedom Restored to the Cavern!", 17, FontStyle.Italic, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(490f, 28f), new Vector2(0.5f, 1f));
        winStatsText = CreateOutlinedText("WinStats", wBox.transform, "", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(470f, 70f), new Vector2(0.5f, 1f));

        CreateSteampunkButton("WinPlayAgainBtn", wBox.transform, "PLAY AGAIN", new Vector2(0f, -34f), new Vector2(320f, 52f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        });
        CreateSteampunkButton("WinTitleBtn", wBox.transform, "TITLE SCREEN", new Vector2(0f, -98f), new Vector2(320f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        });
        CreateSteampunkButton("WinQuitBtn", wBox.transform, "QUIT GAME", new Vector2(0f, -158f), new Vector2(320f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.QuitGame();
        });

        // ================= GAME OVER SCREEN =================
        gameOverPanel = CreateFlatPanel("GameOverOverlay", root, overlayDim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        GameObject gBox = CreateOrnatePanel("GameOverBox", gameOverPanel.transform, bossFrameSpr, Color.white, 2.1f,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 460f), new Vector2(0.5f, 0.5f));

        CreateOutlinedText("GOTitle", gBox.transform, "OUT OF LIVES", 34, FontStyle.Bold, crimsonAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(440f, 46f), new Vector2(0.5f, 1f), 2.5f);
        CreateOutlinedText("GOSub", gBox.transform, "Kip ran out of backup power cells.", 17, FontStyle.Italic, goldAccent, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(440f, 28f), new Vector2(0.5f, 1f));

        CreateSteampunkButton("GORestartBtn", gBox.transform, "RESTART LEVEL", new Vector2(0f, -10f), new Vector2(320f, 50f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.RestartLevel();
        });
        CreateSteampunkButton("GOTitleBtn", gBox.transform, "TITLE SCREEN", new Vector2(0f, -72f), new Vector2(320f, 48f), () =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ReturnToTitle();
        });
        CreateSteampunkButton("GOQuitBtn", gBox.transform, "QUIT GAME", new Vector2(0f, -132f), new Vector2(320f, 48f), () =>
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
        if (hudRoot != null) hudRoot.SetActive(!showTitleScreen && !showPauseMenu && !showWinScreen && !showGameOverScreen);
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
                bool filled = (i < playerHealth);
                if (filled && heartFullSpr != null)
                {
                    heartImages[i].sprite = heartFullSpr;
                    heartImages[i].color = Color.white;
                }
                else if (!filled && heartEmptySpr != null)
                {
                    heartImages[i].sprite = heartEmptySpr;
                    heartImages[i].color = Color.white;
                }
                else
                {
                    heartImages[i].color = filled ? Color.white : new Color(0.25f, 0.25f, 0.25f, 0.45f);
                }
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

    public void ShowFinalDeathBanner(string title, string subtitle)
    {
        if (tutorialBanner != null) tutorialBanner.SetActive(false);
        if (toastBanner != null) toastBanner.SetActive(false);
        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
        if (bossIntroBanner != null) bossIntroBanner.SetActive(false);
        if (finalDeathTitleText != null) finalDeathTitleText.text = title;
        if (finalDeathSubText != null) finalDeathSubText.text = subtitle;
        if (finalDeathBanner != null) finalDeathBanner.SetActive(true);
    }

    public void SetDeathFade(float alpha)
    {
        if (deathFadeImage != null)
        {
            deathFadeImage.color = new Color(0.06f, 0.0f, 0.01f, Mathf.Clamp01(alpha));
        }
    }

    public void SetTutorialPrompt(TutorialSign source, string title, string body)
    {
        activeTutorialSign = source;
        tutorialTitle = title;
        tutorialBody = body;
        if (bossHealthPanel != null && bossHealthPanel.activeSelf) return;
        if (isBossIntroActive) return;
        if (finalDeathBanner != null && finalDeathBanner.activeSelf) return;
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
        if (isBossIntroActive) return;
        if (finalDeathBanner != null && finalDeathBanner.activeSelf) return;
        toastTimer = duration;
        if (toastBanner != null && toastText != null)
        {
            toastText.text = message;
            toastBanner.SetActive(true);
        }
    }

    public void PlayBossIntroBanner(string title, string subtitle)
    {
        if (bossIntroBanner == null) return;
        StartCoroutine(BossIntroBannerRoutine(title, subtitle));
    }

    private IEnumerator BossIntroBannerRoutine(string title, string subtitle)
    {
        isBossIntroActive = true;
        if (tutorialBanner != null) tutorialBanner.SetActive(false);
        if (toastBanner != null) toastBanner.SetActive(false);

        if (bossIntroTitleText != null) bossIntroTitleText.text = title;
        if (bossIntroSubText != null) bossIntroSubText.text = subtitle;
        bossIntroBanner.SetActive(true);

        RectTransform rt = bossIntroBanner.GetComponent<RectTransform>();
        float inTime = 0.30f;
        float elapsed = 0f;
        while (elapsed < inTime)
        {
            float t = elapsed / inTime;
            float easeOut = 1f - (1f - t) * (1f - t);
            if (bossIntroGroup != null) bossIntroGroup.alpha = easeOut;
            float s = Mathf.Lerp(1.26f, 1.0f, easeOut);
            if (rt != null) rt.localScale = new Vector3(s, s, 1f);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (bossIntroGroup != null) bossIntroGroup.alpha = 1f;
        if (rt != null) rt.localScale = Vector3.one;

        yield return new WaitForSecondsRealtime(2.55f);

        float outTime = 0.45f;
        elapsed = 0f;
        while (elapsed < outTime)
        {
            float t = elapsed / outTime;
            if (bossIntroGroup != null) bossIntroGroup.alpha = 1f - t;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        bossIntroBanner.SetActive(false);
        isBossIntroActive = false;
    }

    public void UpdateOrangeGemUI(int collected, int total)
    {
        orangeGemsCollected = collected;
        totalOrangeGems = total;
        if (orangeGemText != null)
        {
            orangeGemText.text = $"ORANGE CRYSTAL: {collected} / {total}";
            orangeGemText.color = (collected > 0) ? new Color(1f, 0.88f, 0.32f) : new Color(1f, 0.65f, 0.24f);
        }
    }

    public void SetDoubleJumpUnlockedUI(bool unlocked, bool consumedAfterBoss = false)
    {
        if (doubleJumpBadge != null)
        {
            Image bg = doubleJumpBadge.GetComponent<Image>();
            if (bg != null)
            {
                bg.color = unlocked ? new Color(1f, 0.82f, 0.50f, 1f) : new Color(0.70f, 0.72f, 0.80f, 1f);
            }
        }
        if (doubleJumpBadgeText != null)
        {
            if (unlocked)
            {
                doubleJumpBadgeText.text = "DOUBLE JUMP: UNLOCKED [2x SPACE]";
                doubleJumpBadgeText.color = new Color(1f, 0.90f, 0.40f);
            }
            else if (consumedAfterBoss)
            {
                doubleJumpBadgeText.text = "DOUBLE JUMP: REMOVED (BOSS SLAIN)";
                doubleJumpBadgeText.color = new Color(0.75f, 0.80f, 0.88f);
            }
            else
            {
                doubleJumpBadgeText.text = "DOUBLE JUMP: LOCKED (ORANGE GEM)";
                doubleJumpBadgeText.color = new Color(0.75f, 0.80f, 0.88f);
            }
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
            bossNameText.color = isAngry ? new Color(1f, 0.34f, 0.24f) : new Color(1f, 0.84f, 0.34f);
        }
        if (bossHealthFill != null)
        {
            bossHealthFill.fillAmount = (maxHp > 0) ? Mathf.Clamp01((float)hp / maxHp) : 0f;
            bossHealthFill.color = isAngry ? new Color(0.98f, 0.16f, 0.12f) : new Color(1f, 0.56f, 0.12f);
        }
    }

    public void HideBossHealthUI()
    {
        if (bossHealthPanel != null) bossHealthPanel.SetActive(false);
    }

    public void SetBossDarkness(float darknessAlpha, bool enraged, float lightningFlash = 0f)
    {
        if (bossDarknessImage != null)
        {
            float a = Mathf.Clamp01(darknessAlpha + bossStepVignettePulse);
            bossDarknessImage.color = enraged
                ? new Color(0.08f, 0.005f, 0.02f, a)
                : new Color(0.015f, 0.008f, 0.045f, a);
        }
        if (bossLightningImage != null)
        {
            float fa = Mathf.Clamp01(lightningFlash * 0.22f);
            bossLightningImage.color = enraged
                ? new Color(1f, 0.28f, 0.22f, fa)
                : new Color(0.70f, 0.78f, 1f, fa);
        }
    }

    public void PulseBossStepVignette(float pulseStrength)
    {
        bossStepVignettePulse = Mathf.Clamp(pulseStrength, 0f, 0.22f);
    }

    public void ResetBossAtmosphere()
    {
        bossStepVignettePulse = 0f;
        if (bossDarknessImage != null) bossDarknessImage.color = new Color(0.015f, 0.008f, 0.045f, 0f);
        if (bossLightningImage != null) bossLightningImage.color = new Color(0.70f, 0.78f, 1f, 0f);
        HideBossHealthUI();
        if (bossIntroBanner != null) bossIntroBanner.SetActive(false);
        isBossIntroActive = false;
    }

    // ================= BESPOKE UGUI HELPER BUILDERS =================

    private Sprite MakeVignetteSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color[] cols = new Color[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - half) / half;
                float ny = (y - half) / half;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float v = Mathf.Clamp01(0.16f + Mathf.Pow(Mathf.Clamp01((dist - 0.25f) / 0.92f), 1.65f) * 0.84f);
                cols[y * size + x] = new Color(1f, 1f, 1f, v);
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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

    private GameObject CreateFlatPanel(string name, Transform parent, Color bgColor, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
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

    private GameObject CreateOrnatePanel(string name, Transform parent, Sprite frameSprite, Color tint, float ppuMult,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot)
    {
        GameObject go = CreateUIObject(name, parent);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = go.AddComponent<Image>();
        if (frameSprite != null)
        {
            img.sprite = frameSprite;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ppuMult;
            img.color = tint;
        }
        else
        {
            img.color = new Color(0.05f, 0.08f, 0.14f, 0.92f);
        }
        return go;
    }

    private Text CreateOutlinedText(string name, Transform parent, string content, int fontSize, FontStyle style, Color color, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot, float outlineSize = 1.5f)
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

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(0.02f, 0.03f, 0.06f, 0.95f);
        ol.effectDistance = new Vector2(outlineSize, -outlineSize);
        return t;
    }

    private Text CreateSteampunkButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 sizeDelta, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = CreateOrnatePanel(name, parent, buttonFrameSpr, Color.white, 2.5f,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, sizeDelta, new Vector2(0.5f, 0.5f));
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.95f, 0.95f, 0.98f, 1f);
        cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.65f, 0.92f, 1f, 1f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        btn.onClick.AddListener(onClick);

        return CreateOutlinedText("Label", go.transform, label, 17, FontStyle.Bold, new Color(0.96f, 0.98f, 1f), TextAnchor.MiddleCenter,
            Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-46f, -4f), new Vector2(0.5f, 0.5f), 1.6f);
    }

    private Slider CreateVolumeSlider(Transform parent, Vector2 anchoredPos)
    {
        GameObject row = CreateUIObject("VolumeRow", parent);
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.pivot = new Vector2(0.5f, 0.5f);
        rowRt.anchoredPosition = anchoredPos;
        rowRt.sizeDelta = new Vector2(320f, 34f);

        CreateOutlinedText("VolLabel", row.transform, "VOLUME:", 15, FontStyle.Bold, new Color(1f, 0.86f, 0.44f), TextAnchor.MiddleLeft,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(88f, 28f), new Vector2(0f, 0.5f));

        GameObject slGo = CreateUIObject("Slider", row.transform);
        RectTransform slRt = slGo.GetComponent<RectTransform>();
        slRt.anchorMin = slRt.anchorMax = new Vector2(1f, 0.5f);
        slRt.pivot = new Vector2(1f, 0.5f);
        slRt.anchoredPosition = Vector2.zero;
        slRt.sizeDelta = new Vector2(226f, 28f);

        // Bespoke Clockwork Energy Tube Frame for the Volume Slider!
        CreateOrnatePanel("Bg", slGo.transform, barFrameSpr, Color.white, 3.2f,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        GameObject fillArea = CreateUIObject("FillArea", slGo.transform);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(28f, 7f);
        faRt.offsetMax = new Vector2(-28f, -7f);

        GameObject fill = CreateFlatPanel("Fill", fillArea.transform, new Color(0.14f, 0.94f, 1f, 1f),
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

        GameObject handleArea = CreateUIObject("HandleArea", slGo.transform);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(30f, 0f);
        haRt.offsetMax = new Vector2(-30f, 0f);

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform hRt = handle.GetComponent<RectTransform>();
        hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.5f);
        hRt.pivot = new Vector2(0.5f, 0.5f);
        hRt.sizeDelta = new Vector2(24f, 24f);
        Image hImg = handle.AddComponent<Image>();
        Sprite knobSpr = TextureToSprite(dashIconTex);
        if (knobSpr != null)
        {
            hImg.sprite = knobSpr;
            hImg.preserveAspect = true;
            hImg.color = Color.white;
        }
        else
        {
            hImg.color = new Color(1f, 0.86f, 0.44f);
        }

        Slider slider = slGo.AddComponent<Slider>();
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = hRt;
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
