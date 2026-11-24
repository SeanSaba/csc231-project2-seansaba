using System.Collections;
using UnityEngine;

/// <summary>
/// Final Boss: Dread Ironclad Conduit Overseer (12 HP — tougher, longer lifespan).
/// - Uses a real-time visible Laser Target Scanner (LineRenderer) that scans Kip, locks onto
///   his exact coordinates, and shoots molten projectiles DIRECTLY at Kip!
/// - Phase 1 (12..7 HP): Scans -> locks -> fires fast single direct aimed shots one by one.
/// - Phase 2 (6..3 HP, Half Health Angry): Enraged crimson roar -> scans & locks -> fires 3-shot direct spread fan at Kip.
/// - Phase 3 (2..1 HP, Final Fury right before dying): Intense meltdown roar -> rapid scan & lock -> 5-shot two-wave aimed barrage.
/// - Weaknesses: Dashing through weakens/staggers him (min 1 HP); jumping on his head damages him
///   AND is the ONLY condition that can kill him.
/// - On death: Removes Kip's Double Jump ability and unlocks the Core Reactor Portal!
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class FinalBoss : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private string bossName = "DREAD CONDUIT OVERSEER";
    [SerializeField] private int maxHealth = 12;
    [SerializeField] private int contactDamage = 1;

    [Header("Arena & Movement")]
    [SerializeField] private float activationDistance = 16.5f;
    [SerializeField] private float arenaMinX = 232.0f;
    [SerializeField] private float arenaMaxX = 243.0f;
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float angryMoveSpeed = 3.9f;

    [Header("Sprites (8-Frame Boss Sheet + 4-Frame Walk Cycles)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] bossFrames; // 0:Idle 1:Scan/Windup 2:Shoot 3:Hurt 4:AngryRoar 5:AngryShoot 6:WeakenedKneel 7:Defeat
    [SerializeField] private Sprite[] walkFrames; // 0:LeftLift 1:LeftSlam 2:RightLift 3:RightSlam
    [SerializeField] private Sprite[] angryWalkFrames; // 0..3 Enraged heavy stomping cycle
    [SerializeField] private Sprite[] projectileFrames;

    [Header("Arena Environment & Linked Portal")]
    [SerializeField] private Transform arenaGroundTransform;
    [SerializeField] private GoalZone linkedPortal;

    private int currentHealth;
    private bool isSummonedByGem;
    private bool isSpawningIn;
    private bool isFightActive;
    private bool isDefeated;
    private bool isStaggered;
    private bool isAttacking;
    private bool hasTriggeredHalfAngry;
    private bool hasTriggeredFinalFury;
    private float hitCooldownTimer;
    private float attackTimer = 1.0f;
    private float distractionNoiseTimer = 3.5f;
    private float walkAnimTimer = 0f;
    private int walkFrameIndex = 0;
    private float lightingBlend = 0f;
    private float stormFlashTimer = 5.5f;
    private float activeLightningFlash = 0f;
    private int moveDir = -1;

    private Transform playerTransform;
    private BoxCollider2D bodyCol;
    private LineRenderer scannerLaser;
    private Vector3 baseScale;
    private Vector3 startPos;
    private Vector3 arenaGroundOriginPos;
    private bool hasArenaGroundOrigin;
    private Coroutine groundShudderRoutine;

    private Camera mainCam;
    private Color originalCamBgColor;
    private SpriteRenderer[] bgRenderers;
    private Color[] bgOriginalColors;
    private SpriteRenderer[] arenaGroundRenderers;
    private Color[] arenaGroundOriginalColors;

    private bool IsAngry => hasTriggeredHalfAngry || hasTriggeredFinalFury;

    private void Awake()
    {
        currentHealth = maxHealth;
        bodyCol = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
        startPos = transform.position;
        SetFrame(0);
        SetupScannerLaser();

        // Remain completely HIDDEN and inactive until Kip collects the Special Orange Gem!
        SetBossVisible(false);
    }

    private void SetBossVisible(bool visible)
    {
        if (spriteRenderer != null) spriteRenderer.enabled = visible;
        if (bodyCol != null) bodyCol.enabled = visible;
    }

    /// <summary>
    /// Called by DoubleJumpGem ONLY when the Special Orange Gem is collected.
    /// Dramatically reveals the Final Boss, plunges the arena into dark scary lighting,
    /// announces "FINAL BOSS, FIGHT!!", and initiates his attack!
    /// </summary>
    public void SummonFromOrangeGem()
    {
        if (isSummonedByGem || isDefeated) return;
        isSummonedByGem = true;
        StartCoroutine(SummonEntranceRoutine());
    }

    private IEnumerator SummonEntranceRoutine()
    {
        isSpawningIn = true;
        isFightActive = true;
        attackTimer = 1.65f;
        distractionNoiseTimer = 4.5f;
        activeLightningFlash = 0.85f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI(bossName, currentHealth, maxHealth, false);
            UIManager.Instance.PlayBossIntroBanner("FINAL BOSS", "FIGHT!!");
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossIntroSound();
        }

        // Dramatic sky-drop appearance onto the Boss Arena!
        Vector3 dropStart = startPos + Vector3.up * 7.2f;
        transform.position = dropStart;
        SetFrame(4);
        SetBossVisible(true);
        if (bodyCol != null) bodyCol.enabled = false; // Enable collision on landing

        float dropDur = 0.52f;
        float elapsed = 0f;
        while (elapsed < dropDur)
        {
            float t = elapsed / dropDur;
            float easeIn = t * t * t;
            transform.position = Vector3.Lerp(dropStart, startPos, easeIn);
            lightingBlend = Mathf.Clamp01(t * 0.85f);
            ApplyAtmosphereBlend(lightingBlend, activeLightningFlash * (1f - t * 0.4f));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = startPos;
        if (bodyCol != null) bodyCol.enabled = true;

        // Massive seismic landing slam when the Boss touches down!
        OnBossFootstep(true);
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayBossAngryRoar(startPos + Vector3.up * 0.4f);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeSeismic(0.55f, 0.34f, 1.15f);
        }

        // Brief menacing stance beat before starting full walk + attack cycle
        float stanceDur = 0.65f;
        elapsed = 0f;
        while (elapsed < stanceDur)
        {
            UpdateBossDarkLighting();
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 10f * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetFrame(0);
        isSpawningIn = false;
    }

    private void SetupScannerLaser()
    {
        GameObject laserGo = new GameObject("BossScannerLaser");
        laserGo.transform.SetParent(transform, false);
        scannerLaser = laserGo.AddComponent<LineRenderer>();
        scannerLaser.positionCount = 2;
        scannerLaser.useWorldSpace = true;
        scannerLaser.sortingOrder = 14;
        scannerLaser.startWidth = 0.08f;
        scannerLaser.endWidth = 0.04f;
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        scannerLaser.material = new Material(sh);
        scannerLaser.enabled = false;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;

        if (linkedPortal == null)
        {
            linkedPortal = FindAnyObjectByType<GoalZone>();
        }
        if (linkedPortal != null)
        {
            linkedPortal.SetLockedByBoss(true);
        }

        // Cache environment visuals for dark scary boss lighting & ground shudder!
        mainCam = Camera.main;
        if (mainCam != null) originalCamBgColor = mainCam.backgroundColor;

        GameObject bgRoot = GameObject.Find("Backgrounds");
        if (bgRoot != null)
        {
            bgRenderers = bgRoot.GetComponentsInChildren<SpriteRenderer>();
            bgOriginalColors = new Color[bgRenderers.Length];
            for (int i = 0; i < bgRenderers.Length; i++)
            {
                bgOriginalColors[i] = bgRenderers[i] != null ? bgRenderers[i].color : Color.white;
            }
        }

        if (arenaGroundTransform == null)
        {
            GameObject groundGo = GameObject.Find("BossArenaBlock");
            if (groundGo != null) arenaGroundTransform = groundGo.transform;
        }
        if (arenaGroundTransform != null)
        {
            arenaGroundOriginPos = arenaGroundTransform.position;
            hasArenaGroundOrigin = true;
            arenaGroundRenderers = arenaGroundTransform.GetComponentsInChildren<SpriteRenderer>();
            arenaGroundOriginalColors = new Color[arenaGroundRenderers.Length];
            for (int i = 0; i < arenaGroundRenderers.Length; i++)
            {
                arenaGroundOriginalColors[i] = arenaGroundRenderers[i] != null ? arenaGroundRenderers[i].color : Color.white;
            }
        }
    }

    private void Update()
    {
        if (isDefeated) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsGamePaused || GameManager.Instance.IsGameOver)) return;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        if (hitCooldownTimer > 0f) hitCooldownTimer -= Time.deltaTime;

        // Stay completely hidden and dormant until the Orange Gem is taken!
        if (!isSummonedByGem || !isFightActive || isSpawningIn) return;

        // Drive dark, scary Final Boss atmosphere & occasional ominous lightning flashes!
        UpdateBossDarkLighting();

        // Always face Kip (sprite naturally faces LEFT; flipX = true faces RIGHT)
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (playerTransform.position.x > transform.position.x);
        }

        if (isAttacking || isStaggered) return;

        // Heavy stomping stalk across the boss arena with proper 4-frame walk cycle!
        float speed = IsAngry ? angryMoveSpeed : moveSpeed;
        float nextX = transform.position.x + moveDir * speed * Time.deltaTime;
        if (nextX <= arenaMinX)
        {
            nextX = arenaMinX;
            moveDir = 1;
        }
        else if (nextX >= arenaMaxX)
        {
            nextX = arenaMaxX;
            moveDir = -1;
        }
        transform.position = new Vector3(nextX, startPos.y, transform.position.z);

        // Advance proper 4-frame stomping walk animation (0:LeftLift, 1:LeftSlam, 2:RightLift, 3:RightSlam)
        float frameDuration = IsAngry ? 0.165f : 0.245f;
        walkAnimTimer += Time.deltaTime;
        if (walkAnimTimer >= frameDuration)
        {
            walkAnimTimer -= frameDuration;
            walkFrameIndex = (walkFrameIndex + 1) % 4;

            if (walkFrameIndex == 1 || walkFrameIndex == 3)
            {
                // EVERY single boss footstep slams the iron boot down and shakes the ground & whole game!
                OnBossFootstep(walkFrameIndex == 1);
            }
        }
        ApplyWalkFrame(walkFrameIndex);

        // Smooth scale spring return from step squash
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 12f * Time.deltaTime);

        // Periodic menacing boss noises (deep bellows, hydraulic clanks, steam hisses) to intimidate & distract!
        distractionNoiseTimer -= Time.deltaTime;
        if (distractionNoiseTimer <= 0f)
        {
            distractionNoiseTimer = IsAngry ? Random.Range(2.4f, 4.0f) : Random.Range(3.4f, 5.4f);
            if (AudioManager.Instance != null)
            {
                if (IsAngry && Random.value < 0.55f) AudioManager.Instance.PlayBossRoarSound();
                else AudioManager.Instance.PlayBossTauntSound();
            }
            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.EmitBossEmber(transform.position + Vector3.up * 1.1f);
            }
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            StartCoroutine(ScanTargetAndShootRoutine());
        }
    }

    private void ApplyWalkFrame(int idx)
    {
        if (spriteRenderer == null) return;
        Sprite[] activeWalk = (IsAngry && angryWalkFrames != null && angryWalkFrames.Length >= 4)
            ? angryWalkFrames
            : walkFrames;
        if (activeWalk != null && activeWalk.Length > idx && activeWalk[idx] != null)
        {
            spriteRenderer.sprite = activeWalk[idx];
        }
        else
        {
            SetFrame(IsAngry ? 4 : 0);
        }
    }

    /// <summary>
    /// Triggered on EVERY single heavy foot-plant step of the Final Boss:
    /// shakes the entire camera + physically jolts the arena ground + blasts dust/rubble + plays heavy iron stomp SFX!
    /// </summary>
    private void OnBossFootstep(bool isLeftBoot)
    {
        // 1. Heavy impact squash on boss body
        transform.localScale = new Vector3(baseScale.x * 1.07f, baseScale.y * 0.93f, baseScale.z);

        // 2. Whole-game seismic camera shake + slight rotational rattle!
        if (CameraController.Instance != null)
        {
            float dur  = IsAngry ? 0.25f : 0.21f;
            float mag  = IsAngry ? 0.34f : 0.24f;
            float roll = IsAngry ? 1.15f : 0.72f;
            CameraController.Instance.ShakeSeismic(dur, mag, roll);
        }

        // 3. Physically shudder the BossArenaBlock ground platform underneath Kip & the Boss!
        if (hasArenaGroundOrigin && arenaGroundTransform != null)
        {
            if (groundShudderRoutine != null) StopCoroutine(groundShudderRoutine);
            groundShudderRoutine = StartCoroutine(GroundShudderRoutine(IsAngry ? 0.14f : 0.10f));
        }

        // 4. Pulse the dark scary boss screen edges with the stomp shockwave!
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PulseBossStepVignette(IsAngry ? 0.16f : 0.09f);
        }

        // 5. Burst dust, stone rubble, and ceiling pebbles across the arena!
        if (ParticleFXManager.Instance != null)
        {
            float footOffsetX = isLeftBoot ? -0.55f : 0.55f;
            if (spriteRenderer != null && spriteRenderer.flipX) footOffsetX = -footOffsetX;
            Vector3 footPos = new Vector3(transform.position.x + footOffsetX, 2.65f, 0f);
            ParticleFXManager.Instance.PlayLandingDust(footPos);
            ParticleFXManager.Instance.EmitRubbleDust(footPos);
            if (IsAngry)
            {
                ParticleFXManager.Instance.EmitBossEmber(footPos + Vector3.up * 0.2f);
            }
            // Falling cavern ceiling dust shaken loose by the boss's massive footsteps!
            Vector3 ceilingDustPos = new Vector3(transform.position.x + Random.Range(-7.5f, 5.5f), 8.2f, 0f);
            ParticleFXManager.Instance.EmitRubbleDust(ceilingDustPos);
        }

        // 6. Deep earth-shaking iron boot stomp sound!
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossStepSound();
        }
    }

    private IEnumerator GroundShudderRoutine(float dropAmp)
    {
        float dur = 0.18f;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            float t = elapsed / dur;
            // Sharp downward slam -> oscillating damped spring tremor -> zero
            float yOff = -Mathf.Sin(t * Mathf.PI * 2.5f) * dropAmp * (1f - t);
            float xOff = Mathf.Sin(t * Mathf.PI * 5f) * (dropAmp * 0.35f) * (1f - t);
            if (arenaGroundTransform != null)
            {
                arenaGroundTransform.position = arenaGroundOriginPos + new Vector3(xOff, yOff, 0f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (arenaGroundTransform != null)
        {
            arenaGroundTransform.position = arenaGroundOriginPos;
        }
        groundShudderRoutine = null;
    }

    private void UpdateBossDarkLighting()
    {
        lightingBlend = Mathf.MoveTowards(lightingBlend, 1f, Time.deltaTime * 1.1f);

        // Occasional eerie scary storm lightning flash in the dark cavern!
        stormFlashTimer -= Time.deltaTime;
        if (stormFlashTimer <= 0f)
        {
            stormFlashTimer = IsAngry ? Random.Range(3.2f, 5.8f) : Random.Range(5.5f, 9.0f);
            activeLightningFlash = IsAngry ? 0.65f : 0.42f;
        }
        if (activeLightningFlash > 0f)
        {
            activeLightningFlash = Mathf.MoveTowards(activeLightningFlash, 0f, Time.deltaTime * 2.8f);
        }

        ApplyAtmosphereBlend(lightingBlend, activeLightningFlash);
    }

    private void ApplyAtmosphereBlend(float darkFactor, float lightningFlash)
    {
        Color darkBgTarget = IsAngry
            ? new Color(0.012f, 0.002f, 0.008f)
            : new Color(0.004f, 0.003f, 0.012f);
        Color flashSkyColor = IsAngry
            ? new Color(0.32f, 0.08f, 0.10f)
            : new Color(0.16f, 0.18f, 0.28f);

        if (mainCam != null)
        {
            Color c = Color.Lerp(originalCamBgColor, darkBgTarget, darkFactor);
            mainCam.backgroundColor = Color.Lerp(c, flashSkyColor, lightningFlash);
        }

        if (bgRenderers != null && bgOriginalColors != null)
        {
            Color bgDarkTint = IsAngry
                ? new Color(0.20f, 0.07f, 0.10f, 1f)
                : new Color(0.14f, 0.10f, 0.20f, 1f);
            Color bgFlashTint = IsAngry
                ? new Color(0.55f, 0.22f, 0.24f, 1f)
                : new Color(0.42f, 0.48f, 0.62f, 1f);

            for (int i = 0; i < bgRenderers.Length; i++)
            {
                if (bgRenderers[i] != null)
                {
                    Color c = Color.Lerp(bgOriginalColors[i], bgDarkTint, darkFactor);
                    bgRenderers[i].color = Color.Lerp(c, bgFlashTint, lightningFlash);
                }
            }
        }

        if (arenaGroundRenderers != null && arenaGroundOriginalColors != null)
        {
            Color stoneDarkTint = IsAngry
                ? new Color(0.68f, 0.56f, 0.58f, 1f)
                : new Color(0.64f, 0.62f, 0.72f, 1f);
            for (int i = 0; i < arenaGroundRenderers.Length; i++)
            {
                if (arenaGroundRenderers[i] != null)
                {
                    arenaGroundRenderers[i].color = Color.Lerp(arenaGroundOriginalColors[i], stoneDarkTint, darkFactor);
                }
            }
        }

        if (UIManager.Instance != null)
        {
            float targetDarkAlpha = (IsAngry ? 0.70f : 0.58f) * darkFactor;
            UIManager.Instance.SetBossDarkness(targetDarkAlpha, IsAngry, lightningFlash);
        }
    }

    private void BeginBossFight()
    {
        isFightActive = true;
        attackTimer = 2.0f; // Dramatic opening for "FINAL BOSS / KILL" banner & loud spoken audio!
        distractionNoiseTimer = 4.4f;
        activeLightningFlash = 0.75f; // Scary opening lightning flash as the arena plunges into darkness!
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI(bossName, currentHealth, maxHealth, false);
            UIManager.Instance.PlayBossIntroBanner("FINAL BOSS", "FIGHT!!");
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeSeismic(0.55f, 0.30f, 1.0f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossIntroSound();
        }
    }

    private Vector3 GetEyeOrigin()
    {
        float facingSign = (playerTransform != null && playerTransform.position.x > transform.position.x) ? 1f : -1f;
        return transform.position + new Vector3(facingSign * 0.35f, 0.85f, 0f);
    }

    private Vector3 GetCannonOrigin()
    {
        float facingSign = (playerTransform != null && playerTransform.position.x > transform.position.x) ? 1f : -1f;
        return transform.position + new Vector3(facingSign * 1.15f, 0.25f, 0f);
    }

    private IEnumerator ScanTargetAndShootRoutine()
    {
        isAttacking = true;
        transform.localScale = baseScale;
        SetFrame(IsAngry ? 4 : 1);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossChargeSound();
        }

        // 1. ACTIVE LASER SCANNING PHASE: Track Kip live with visible flickering amber/orange beam
        float scanTime = hasTriggeredFinalFury ? 0.30f : (hasTriggeredHalfAngry ? 0.36f : 0.44f);
        float lockTime = hasTriggeredFinalFury ? 0.16f : (hasTriggeredHalfAngry ? 0.19f : 0.22f);

        Vector3 lockedTargetPos = (playerTransform != null) ? playerTransform.position : (transform.position + Vector3.left * 6f);

        if (scannerLaser != null)
        {
            scannerLaser.enabled = true;
            scannerLaser.startWidth = 0.06f;
            scannerLaser.endWidth = 0.04f;
        }

        float elapsed = 0f;
        while (elapsed < scanTime)
        {
            if (isDefeated || isStaggered)
            {
                DisableLaser();
                isAttacking = false;
                yield break;
            }
            if (playerTransform != null)
            {
                lockedTargetPos = playerTransform.position + new Vector3(0f, 0.05f, 0f);
            }
            if (scannerLaser != null)
            {
                Color scanCol = (Mathf.FloorToInt(elapsed * 24f) % 2 == 0)
                    ? new Color(1f, 0.65f, 0.12f, 0.85f)
                    : new Color(1f, 0.30f, 0.10f, 0.55f);
                scannerLaser.startColor = scanCol;
                scannerLaser.endColor = scanCol;
                scannerLaser.SetPosition(0, GetEyeOrigin());
                scannerLaser.SetPosition(1, lockedTargetPos);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. TARGET LOCK PHASE: Freeze beam in blazing crimson right on Kip's exact locked coordinates!
        if (scannerLaser != null)
        {
            Color lockCol = new Color(1f, 0.10f, 0.12f, 1f);
            scannerLaser.startWidth = 0.12f;
            scannerLaser.endWidth = 0.07f;
            scannerLaser.startColor = lockCol;
            scannerLaser.endColor = lockCol;
            scannerLaser.SetPosition(0, GetEyeOrigin());
            scannerLaser.SetPosition(1, lockedTargetPos);
        }
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.EmitBossEmber(GetEyeOrigin());
        }

        elapsed = 0f;
        while (elapsed < lockTime)
        {
            if (isDefeated || isStaggered)
            {
                DisableLaser();
                isAttacking = false;
                yield break;
            }
            if (scannerLaser != null)
            {
                scannerLaser.SetPosition(0, GetEyeOrigin());
                scannerLaser.SetPosition(1, lockedTargetPos);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DisableLaser();

        // 3. DIRECT TARGETED FIRE!
        SetFrame(IsAngry ? 5 : 2);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDashSound();
        }

        Vector3 muzzle = GetCannonOrigin();
        Vector2 aimDir = ((Vector2)(lockedTargetPos - muzzle)).normalized;
        if (aimDir.sqrMagnitude < 0.01f) aimDir = Vector2.left;

        if (hasTriggeredFinalFury)
        {
            // FINAL FURY (<= 2 HP): 5-shot two-wave direct aimed barrage (3-shot aimed fan + 2-shot follow-up) — intense, hard-but-realistic dodge!
            float speed = 14.2f;
            FireAimedShot(muzzle, aimDir, 0f, speed, new Color(1f, 0.30f, 0.18f));
            FireAimedShot(muzzle, aimDir, 14f, speed * 0.94f, new Color(1f, 0.45f, 0.20f));
            FireAimedShot(muzzle, aimDir, -14f, speed * 0.94f, new Color(1f, 0.45f, 0.20f));

            yield return new WaitForSeconds(0.24f);
            if (!isDefeated && !isStaggered)
            {
                Vector3 updatedTarget = (playerTransform != null) ? playerTransform.position : lockedTargetPos;
                Vector2 aimDir2 = ((Vector2)(updatedTarget - muzzle)).normalized;
                if (aimDir2.sqrMagnitude < 0.01f) aimDir2 = aimDir;
                FireAimedShot(muzzle, aimDir2, 8f, speed * 0.98f, new Color(1f, 0.25f, 0.12f));
                FireAimedShot(muzzle, aimDir2, -8f, speed * 0.98f, new Color(1f, 0.25f, 0.12f));
            }
            attackTimer = 1.45f;
        }
        else if (hasTriggeredHalfAngry)
        {
            // HALF-HEALTH ANGRY (<= 6 HP): 3-shot direct aimed spread fan straight toward Kip!
            float speed = 13.4f;
            FireAimedShot(muzzle, aimDir, 0f, speed, new Color(1f, 0.42f, 0.18f));
            FireAimedShot(muzzle, aimDir, 15f, speed * 0.93f, new Color(1f, 0.62f, 0.22f));
            FireAimedShot(muzzle, aimDir, -15f, speed * 0.93f, new Color(1f, 0.62f, 0.22f));
            attackTimer = 1.55f;
        }
        else
        {
            // PHASE 1 (12..7 HP): Fast single direct targeted shot straight at Kip one by one!
            FireAimedShot(muzzle, aimDir, 0f, 12.6f, Color.white);
            attackTimer = 1.25f;
        }

        yield return new WaitForSeconds(0.24f);

        if (!isDefeated && !isStaggered)
        {
            SetFrame(IsAngry ? 4 : 0);
        }
        isAttacking = false;
    }

    private void FireAimedShot(Vector3 origin, Vector2 baseDir, float angleOffsetDeg, float speed, Color tint)
    {
        float rad = angleOffsetDeg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 dir = new Vector2(baseDir.x * cos - baseDir.y * sin, baseDir.x * sin + baseDir.y * cos).normalized;

        GameObject projObj = new GameObject("BossProjectile");
        projObj.transform.position = origin;
        projObj.transform.localScale = new Vector3(0.64f, 0.64f, 1f);

        SpriteRenderer psr = projObj.AddComponent<SpriteRenderer>();
        psr.sortingOrder = 11;

        CircleCollider2D col = projObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.40f;

        BossProjectile bp = projObj.AddComponent<BossProjectile>();
        // Direct laser-targeted shot (zero gravity drop so it flies straight at Kip's locked position!)
        bp.Launch(dir * speed, 0f, projectileFrames, tint);
    }

    private void DisableLaser()
    {
        if (scannerLaser != null) scannerLaser.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandlePlayerCollision(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandlePlayerCollision(collision);
    }

    private void HandlePlayerCollision(Collider2D collision)
    {
        if (isDefeated || !isSummonedByGem || isSpawningIn) return;
        if (!collision.CompareTag("Player")) return;

        var pc = collision.GetComponent<PlayerController>();
        var ph = collision.GetComponent<PlayerHealth>();
        var prb = collision.GetComponent<Rigidbody2D>();

        // Weakness #1: HEAD STOMP (damages him AND is the ONLY way to KILL him!)
        Bounds bossBounds = bodyCol.bounds;
        float playerBottom = collision.bounds.min.y;
        float headZoneY = bossBounds.center.y + bossBounds.extents.y * 0.22f;
        bool isFallingOrApex = (prb == null || prb.linearVelocity.y <= 1.5f);

        if (playerBottom >= headZoneY && isFallingOrApex && pc != null)
        {
            if (hitCooldownTimer > 0f) return;
            // Launch Kip cleanly OFF and AWAY in another direction so Kip cannot chain multiple head-stomps in a row!
            // If Kip is on the left or center, vault cleanly left into the open arena; if clearly right, vault right (clamped safely by the arena barrier).
            float awayDir = (collision.transform.position.x > transform.position.x + 0.55f && transform.position.x < arenaMaxX - 1.5f) ? 1f : -1f;
            pc.BounceAway(new Vector2(awayDir * 10.8f, 13.5f), 0.30f);
            TakeBossDamage(1, true);
            return;
        }

        // Weakness #2: DASHING RIGHT THROUGH (easier to pull off — weakens & staggers him, CANNOT kill!)
        if (pc != null && pc.IsDashing)
        {
            if (hitCooldownTimer > 0f) return;
            TakeBossDamage(1, false);
            return;
        }

        if (!isStaggered && hitCooldownTimer <= 0f && ph != null)
        {
            ph.TakeDamage(contactDamage, transform.position);
        }
    }

    private void TakeBossDamage(int amount, bool fromHeadStomp)
    {
        hitCooldownTimer = fromHeadStomp ? 0.72f : 0.48f;
        DisableLaser();

        // STRICT KILL CONDITION: ONLY stomping his head can deal the lethal blow!
        if (!fromHeadStomp && currentHealth <= 1)
        {
            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.PlaySparklePickup(transform.position + Vector3.up * 0.9f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayImpactSound();
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowToastNotification("WEAKENED! JUMP ON HIS HEAD TO KILL HIM!", 2.4f);
            }
            StartCoroutine(StaggerRoutine(1.35f));
            return;
        }

        if (fromHeadStomp)
        {
            currentHealth -= amount;
        }
        else
        {
            currentHealth = Mathf.Max(1, currentHealth - amount);
        }

        if (currentHealth < 0) currentHealth = 0;

        if (ParticleFXManager.Instance != null)
        {
            Vector3 fxPos = fromHeadStomp ? (transform.position + Vector3.up * 1.65f) : transform.position;
            ParticleFXManager.Instance.PlayEnemyDefeated(fxPos);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayImpactSound();
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.26f, 0.24f);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI(bossName, currentHealth, maxHealth, IsAngry || currentHealth <= maxHealth / 2);
        }

        if (currentHealth <= 0)
        {
            StartCoroutine(DefeatRoutine());
            return;
        }

        // Angry Phase transitions: Half health (<= 6 HP) and Right Before Dying (<= 2 HP)
        if (currentHealth <= 2 && !hasTriggeredFinalFury)
        {
            hasTriggeredFinalFury = true;
            hasTriggeredHalfAngry = true;
            StartCoroutine(AngryPhaseTransitionRoutine("FINAL FURY! RAPID TARGETED BARRAGE — STOMP HIS HEAD!"));
        }
        else if (currentHealth <= maxHealth / 2 && !hasTriggeredHalfAngry)
        {
            hasTriggeredHalfAngry = true;
            StartCoroutine(AngryPhaseTransitionRoutine("THE OVERSEER IS ENRAGED! MULTI-SHOT TARGETING!"));
        }
        else
        {
            StartCoroutine(StaggerRoutine(fromHeadStomp ? 0.42f : 0.95f));
        }
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        isStaggered = true;
        DisableLaser();
        SetFrame(duration >= 0.8f ? 6 : 3);
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.68f, 0.68f, 1f);

        yield return new WaitForSeconds(0.14f);
        if (spriteRenderer != null) spriteRenderer.color = IsAngry ? new Color(1f, 0.82f, 0.78f, 1f) : Color.white;

        yield return new WaitForSeconds(Mathf.Max(0.1f, duration - 0.14f));
        if (!isDefeated)
        {
            SetFrame(IsAngry ? 4 : 0);
        }
        isStaggered = false;
    }

    private IEnumerator AngryPhaseTransitionRoutine(string message)
    {
        isAttacking = true;
        isStaggered = true;
        DisableLaser();
        SetFrame(4);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI(bossName, currentHealth, maxHealth, true);
            UIManager.Instance.ShowToastNotification(message, 3.2f);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ShakeSeismic(0.65f, 0.36f, 1.35f);
        }
        activeLightningFlash = 0.90f;
        ApplyAtmosphereBlend(1f, activeLightningFlash);
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayBossAngryRoar(transform.position + Vector3.up * 0.7f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBossRoarSound();
        }

        float elapsed = 0f;
        while (elapsed < 0.85f)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (Mathf.FloorToInt(elapsed * 18f) % 2 == 0)
                    ? new Color(1f, 0.36f, 0.28f, 1f)
                    : new Color(1f, 0.88f, 0.72f, 1f);
            }
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x + Random.Range(-0.09f, 0.09f), arenaMinX, arenaMaxX),
                startPos.y, transform.position.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.82f, 0.78f, 1f);
        isStaggered = false;
        isAttacking = false;
        attackTimer = 0.28f;
    }

    private IEnumerator DefeatRoutine()
    {
        isDefeated = true;
        DisableLaser();
        bodyCol.enabled = false;
        SetFrame(7);

        // Clear all active projectiles cleanly
        BossProjectile[] activeProjs = FindObjectsByType<BossProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < activeProjs.Length; i++)
        {
            if (activeProjs[i] != null) Destroy(activeProjs[i].gameObject);
        }

        // REMOVE DOUBLE JUMP ABILITY ON BOSS KILL!
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.LockDoubleJump();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBossHealthUI();
            UIManager.Instance.SetDoubleJumpUnlockedUI(false, true);
            UIManager.Instance.ShowToastNotification("OVERSEER SLAIN! DOUBLE JUMP CONSUMED — ENTER THE PORTAL!", 3.8f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWinSound();
        }

        for (int burst = 0; burst < 5; burst++)
        {
            if (ParticleFXManager.Instance != null)
            {
                Vector3 offset = (Vector3)Random.insideUnitCircle * 1.25f;
                ParticleFXManager.Instance.PlayEnemyDefeated(transform.position + offset);
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.ShakeSeismic(0.28f, 0.28f, 0.9f);
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (burst % 2 == 0) ? new Color(1f, 0.95f, 0.55f, 1f) : new Color(1f, 0.30f, 0.20f, 1f);
            }
            // Gradually lift the scary boss darkness as the Overseer explodes!
            ApplyAtmosphereBlend(Mathf.Clamp01(1f - ((burst + 1) / 5f)), burst % 2 == 0 ? 0.35f : 0f);
            yield return new WaitForSeconds(0.22f);
        }

        // Restore normal bright cavern lighting, steady camera, original ground position & UI cleanly!
        if (groundShudderRoutine != null)
        {
            StopCoroutine(groundShudderRoutine);
            groundShudderRoutine = null;
        }
        if (hasArenaGroundOrigin && arenaGroundTransform != null)
        {
            arenaGroundTransform.position = arenaGroundOriginPos;
        }
        ApplyAtmosphereBlend(0f, 0f);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ResetBossAtmosphere();
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.StopShake();
        }

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayPortalOpenBurst(transform.position);
        }

        if (linkedPortal != null)
        {
            linkedPortal.UnlockFromBoss();
        }

        Destroy(gameObject);
    }

    private void SetFrame(int index)
    {
        if (spriteRenderer == null || bossFrames == null || bossFrames.Length == 0) return;
        int clamped = Mathf.Clamp(index, 0, bossFrames.Length - 1);
        if (bossFrames[clamped] != null)
        {
            spriteRenderer.sprite = bossFrames[clamped];
        }
    }
}
