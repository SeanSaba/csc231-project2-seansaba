using System.Collections;
using UnityEngine;

/// <summary>
/// Eye-catching animated Core Reactor Portal:
/// - Starts dormant/closed (sealed stone archway).
/// - Opens dramatically with sparks, camera shake, vortex particles, and multi-frame animation as Kip approaches.
/// - Loops a blazing cyan-gold swirling energy vortex while open.
/// - Draws Kip smoothly into the vortex on entry, then implodes and seals itself shut in reverse before showing the Win Screen!
/// </summary>
public class GoalZone : MonoBehaviour
{
    private enum PortalState
    {
        Closed,
        Opening,
        Open,
        ClosingForWin,
        Finished
    }

    [Header("Portal Visuals & Animation")]
    [SerializeField] private SpriteRenderer portalRenderer;
    [SerializeField] private Sprite[] openingFrames;
    [SerializeField] private Sprite[] openLoopFrames;
    [SerializeField] private float openFrameDuration = 0.12f;
    [SerializeField] private float openLoopFrameRate = 10f;

    [Header("Portal FX & Proximity")]
    [SerializeField] private ParticleSystem vortexSwirlPS;
    [SerializeField] private ParticleSystem goldenEmbersPS;
    [SerializeField] private float openTriggerDistance = 8.5f;
    [SerializeField] private float recloseDistance = 12.5f;
    [SerializeField] private bool lockedByBoss = false;

    private PortalState state = PortalState.Closed;
    private Transform playerTransform;
    private Vector3 baseScale;
    private float loopTimer;
    private int loopIndex;
    private bool playerEnteredDuringOpen;
    private float lockedToastCooldown;

    public void SetLockedByBoss(bool locked)
    {
        lockedByBoss = locked;
    }

    public void UnlockFromBoss()
    {
        lockedByBoss = false;
        // Keep the unlocked portal invitingly open across the entire boss arena once all gems are collected!
        recloseDistance = 60f;
        bool allGems = (GameManager.Instance == null || GameManager.Instance.HasCollectedAllGems());
        if (state == PortalState.Closed && allGems)
        {
            StartCoroutine(OpenPortalRoutine());
        }
    }

    private void Awake()
    {
        if (portalRenderer == null) portalRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        // Start sealed shut until Kip approaches!
        if (portalRenderer != null && openingFrames != null && openingFrames.Length > 0 && openingFrames[0] != null)
        {
            portalRenderer.sprite = openingFrames[0];
        }
        StopPortalParticles();
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;
        if (state == PortalState.ClosingForWin || state == PortalState.Finished) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (lockedToastCooldown > 0f) lockedToastCooldown -= Time.deltaTime;

        if (lockedByBoss)
        {
            return;
        }
        if (GameManager.Instance != null && !GameManager.Instance.HasCollectedAllGems())
        {
            return;
        }

        if (state == PortalState.Closed && dist <= openTriggerDistance)
        {
            StartCoroutine(OpenPortalRoutine());
        }
        else if (state == PortalState.Open)
        {
            if (dist >= recloseDistance)
            {
                StartCoroutine(CloseIdleRoutine());
                return;
            }

            // Animate blazing swirling vortex frames + subtle energetic breathing pulse
            if (openLoopFrames != null && openLoopFrames.Length > 0 && portalRenderer != null)
            {
                loopTimer += Time.deltaTime;
                float frameTime = 1f / Mathf.Max(1f, openLoopFrameRate);
                if (loopTimer >= frameTime)
                {
                    loopTimer -= frameTime;
                    loopIndex = (loopIndex + 1) % openLoopFrames.Length;
                    if (openLoopFrames[loopIndex] != null)
                    {
                        portalRenderer.sprite = openLoopFrames[loopIndex];
                    }
                }
            }

            float pulse = 1f + Mathf.Sin(Time.time * 5.2f) * 0.025f;
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
        }
    }

    private IEnumerator OpenPortalRoutine()
    {
        state = PortalState.Opening;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCheckpointSound();
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.32f, 0.13f);
        }
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayPortalOpenBurst(transform.position);
        }

        int count = (openingFrames != null) ? openingFrames.Length : 0;
        for (int i = 0; i < count; i++)
        {
            if (portalRenderer != null && openingFrames[i] != null)
            {
                portalRenderer.sprite = openingFrames[i];
            }

            // Ignite swirling vortex particles as the rift tears open
            if (i == Mathf.Max(1, count / 2))
            {
                StartPortalParticles();
            }

            // Eye-catching energy surge punch on each opening keyframe
            float progress = (count > 1) ? (float)i / (count - 1) : 1f;
            float punch = 1f + Mathf.Sin(progress * Mathf.PI) * 0.08f;
            transform.localScale = new Vector3(baseScale.x * punch, baseScale.y * punch, baseScale.z);

            if (portalRenderer != null)
            {
                portalRenderer.color = Color.Lerp(new Color(0.85f, 1f, 1f, 1f), Color.white, 0.5f);
            }

            yield return new WaitForSeconds(openFrameDuration);
        }

        if (portalRenderer != null) portalRenderer.color = Color.white;
        transform.localScale = baseScale;
        StartPortalParticles();
        state = PortalState.Open;

        // If Kip dashed straight into the portal while it was finishing its opening sequence, trigger entry immediately
        if (playerEnteredDuringOpen && playerTransform != null)
        {
            StartCoroutine(EnterAndClosePortalRoutine(playerTransform.gameObject));
        }
    }

    private IEnumerator CloseIdleRoutine()
    {
        state = PortalState.Opening; // lock transitions while sealing
        StopPortalParticles();

        int count = (openingFrames != null) ? openingFrames.Length : 0;
        for (int i = count - 1; i >= 0; i--)
        {
            if (portalRenderer != null && openingFrames[i] != null)
            {
                portalRenderer.sprite = openingFrames[i];
            }
            yield return new WaitForSeconds(openFrameDuration * 0.85f);
        }

        transform.localScale = baseScale;
        state = PortalState.Closed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryEnterPortal(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryEnterPortal(collision);
    }

    private void TryEnterPortal(Collider2D collision)
    {
        if (state == PortalState.ClosingForWin || state == PortalState.Finished) return;
        if (!collision.CompareTag("Player")) return;

        if (lockedByBoss)
        {
            if (lockedToastCooldown <= 0f && UIManager.Instance != null)
            {
                lockedToastCooldown = 2.0f;
                UIManager.Instance.ShowToastNotification("PORTAL SEALED! TAKE THE ORANGE GEM & DEFEAT THE FINAL BOSS!", 2.4f);
            }
            return;
        }

        // Do NOT let the player finish the level unless ALL gems (all blue crystals + orange gem) have been collected!
        if (GameManager.Instance != null && !GameManager.Instance.HasCollectedAllGems())
        {
            if (lockedToastCooldown <= 0f)
            {
                lockedToastCooldown = 2.4f;
                int collected = GameManager.Instance.CollectedCrystals;
                int total = GameManager.Instance.TotalCrystals;
                int missing = GameManager.Instance.MissingCrystalsCount;
                if (UIManager.Instance != null)
                {
                    string msg = (missing > 0)
                        ? $"PORTAL LOCKED! COLLECT ALL GEMS TO FINISH ({collected}/{total} CRYSTALS) — {missing} LEFT!"
                        : "PORTAL LOCKED! TAKE THE SPECIAL ORANGE GEM TO FINISH!";
                    UIManager.Instance.ShowToastNotification(msg, 3.2f);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayImpactSound();
                }
            }
            return;
        }

        if (state == PortalState.Closed)
        {
            playerEnteredDuringOpen = true;
            StartCoroutine(OpenPortalRoutine());
            return;
        }

        if (state == PortalState.Opening)
        {
            playerEnteredDuringOpen = true;
            return;
        }

        if (state == PortalState.Open)
        {
            StartCoroutine(EnterAndClosePortalRoutine(collision.gameObject));
        }
    }

    private IEnumerator EnterAndClosePortalRoutine(GameObject playerObj)
    {
        state = PortalState.ClosingForWin;

        // Freeze Kip's controls & physics so we can smoothly draw Kip into the swirling vortex
        var pc = playerObj.GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;
        var rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWinSound();
        }
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlaySparklePickup(transform.position);
        }

        // Phase 1: Pull Kip smoothly into the swirling vortex center with spin & dissolve!
        Vector3 startPos = playerObj.transform.position;
        Vector3 portalCenter = transform.position + new Vector3(0f, -0.15f, 0f);
        Vector3 startPlayerScale = playerObj.transform.localScale;
        SpriteRenderer playerSr = playerObj.GetComponentInChildren<SpriteRenderer>();

        float pullDuration = 0.42f;
        float elapsed = 0f;
        while (elapsed < pullDuration)
        {
            float t = elapsed / pullDuration;
            float easeIn = t * t;
            playerObj.transform.position = Vector3.Lerp(startPos, portalCenter, easeIn);
            playerObj.transform.Rotate(0f, 0f, -540f * Time.deltaTime);
            playerObj.transform.localScale = Vector3.Lerp(startPlayerScale, Vector3.zero, easeIn);
            if (playerSr != null)
            {
                playerSr.color = Color.Lerp(Color.white, new Color(0.25f, 1f, 1f, 0f), easeIn);
            }

            // Keep swirling open portal bright while absorbing Kip
            if (openLoopFrames != null && openLoopFrames.Length > 0 && portalRenderer != null)
            {
                int idx = Mathf.FloorToInt(elapsed * 16f) % openLoopFrames.Length;
                if (openLoopFrames[idx] != null) portalRenderer.sprite = openLoopFrames[idx];
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        playerObj.SetActive(false);

        // Phase 2: Eye-catching self-closing implosion sequence!
        StopPortalParticles();
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayPortalCloseImplosion(transform.position);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.30f, 0.16f);
        }

        int count = (openingFrames != null) ? openingFrames.Length : 0;
        for (int i = count - 1; i >= 0; i--)
        {
            if (portalRenderer != null && openingFrames[i] != null)
            {
                portalRenderer.sprite = openingFrames[i];
            }

            float p = (count > 1) ? (float)i / (count - 1) : 0f;
            // Dramatic inward squeeze before snapping firmly shut!
            float scaleMult = (i == 0) ? 0.96f : (1.0f + p * 0.06f);
            transform.localScale = new Vector3(baseScale.x * scaleMult, baseScale.y * scaleMult, baseScale.z);

            yield return new WaitForSeconds(openFrameDuration * 1.15f);
        }

        // Settle solidly shut
        transform.localScale = baseScale;
        if (portalRenderer != null && openingFrames != null && openingFrames.Length > 0 && openingFrames[0] != null)
        {
            portalRenderer.sprite = openingFrames[0];
        }

        // Phase 3: Celebratory Victory Fireworks above the newly sealed ancient portal!
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayVictoryBurst(transform.position + Vector3.up * 0.5f);
        }

        yield return new WaitForSeconds(0.50f);

        state = PortalState.Finished;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompleteLevel();
        }
    }

    private void StartPortalParticles()
    {
        if (vortexSwirlPS != null && !vortexSwirlPS.isPlaying) vortexSwirlPS.Play();
        if (goldenEmbersPS != null && !goldenEmbersPS.isPlaying) goldenEmbersPS.Play();
    }

    private void StopPortalParticles()
    {
        if (vortexSwirlPS != null) vortexSwirlPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (goldenEmbersPS != null) goldenEmbersPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
}
