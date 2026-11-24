using System.Collections;
using UnityEngine;

/// <summary>
/// Final Boss: Ironclad Conduit Overseer.
/// - Throws glowing molten gear-orbs one by one in Phase 1.
/// - Enters Angry Enraged mode at half health (<= 3 HP) throwing 3-object multi-arc volleys.
/// - Enters Final Fury right before dying (1 HP) throwing tight 4-object multi-arc volleys.
/// - Weaknesses: Dashing right through weakens/staggers him (down to 1 HP minimum) and jumping
///   on his head damages him AND is the ONLY way to deliver the killing blow!
/// - Defeating him unlocks the final Core Reactor Portal.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class FinalBoss : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private string bossName = "IRONCLAD CONDUIT OVERSEER";
    [SerializeField] private int maxHealth = 6;
    [SerializeField] private int contactDamage = 1;

    [Header("Arena & Movement")]
    [SerializeField] private float activationDistance = 15.5f;
    [SerializeField] private float arenaMinX = 232.0f;
    [SerializeField] private float arenaMaxX = 242.5f;
    [SerializeField] private float moveSpeed = 2.0f;
    [SerializeField] private float angryMoveSpeed = 3.1f;

    [Header("Sprites (8-Frame Boss Sheet)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] bossFrames; // 0:Idle 1:Windup 2:Throw 3:Hurt 4:AngryRoar 5:AngryThrow 6:WeakenedKneel 7:Defeat
    [SerializeField] private Sprite[] projectileFrames;

    [Header("Linked Goal Portal")]
    [SerializeField] private GoalZone linkedPortal;

    private int currentHealth;
    private bool isFightActive;
    private bool isDefeated;
    private bool isStaggered;
    private bool isRoaring;
    private bool hasTriggeredHalfAngry;
    private bool hasTriggeredFinalFury;
    private float hitCooldownTimer;
    private float attackTimer = 1.2f;
    private int moveDir = -1;
    private int attackCounter = 0;

    private Transform playerTransform;
    private BoxCollider2D bodyCol;
    private Vector3 baseScale;
    private Vector3 startPos;

    private bool IsAngry => hasTriggeredHalfAngry || hasTriggeredFinalFury;

    private void Awake()
    {
        currentHealth = maxHealth;
        bodyCol = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
        startPos = transform.position;
        SetFrame(0);
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

        float dx = Mathf.Abs(playerTransform.position.x - transform.position.x);
        if (!isFightActive)
        {
            if (dx <= activationDistance && playerTransform.position.x >= arenaMinX - 11.5f)
            {
                BeginBossFight();
            }
            return;
        }

        // Face Kip (sprite naturally faces LEFT; flipX = true faces RIGHT)
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (playerTransform.position.x > transform.position.x);
        }

        if (isRoaring || isStaggered) return;

        // Pace menacingly within the boss arena
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

        // Subtle menacing breathing scale
        float bob = 1f + Mathf.Sin(Time.time * (IsAngry ? 7f : 3.5f)) * 0.025f;
        transform.localScale = new Vector3(baseScale.x, baseScale.y * bob, baseScale.z);

        // Attack timer
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            StartCoroutine(PerformThrowAttackRoutine());
        }
    }

    private void BeginBossFight()
    {
        isFightActive = true;
        attackTimer = 1.15f;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI(bossName, currentHealth, maxHealth, false);
            UIManager.Instance.ShowToastNotification("BOSS BATTLE: IRONCLAD CONDUIT OVERSEER! STOMP HIS HEAD!", 3.2f);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.32f, 0.16f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCheckpointSound();
        }
    }

    private IEnumerator PerformThrowAttackRoutine()
    {
        isRoaring = true; // Pause pacing cleanly during throw wind-up
        attackCounter++;

        // Wind-up telegraph
        SetFrame(IsAngry ? 4 : 1);
        if (IsAngry && ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.EmitBossEmber(transform.position + Vector3.up * 0.8f);
        }
        yield return new WaitForSeconds(IsAngry ? 0.34f : 0.44f);

        if (isDefeated || isStaggered)
        {
            isRoaring = false;
            yield break;
        }

        // Launch pose!
        SetFrame(IsAngry ? 5 : 2);
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDashSound();
        }

        float dirToPlayer = (playerTransform != null && playerTransform.position.x > transform.position.x) ? 1f : -1f;
        Vector3 spawnPos = transform.position + new Vector3(dirToPlayer * 1.15f, 0.45f, 0f);

        if (hasTriggeredFinalFury)
        {
            // FINAL FURY (1 HP, right before dying): Throws 4 objects at once in a tight, hard-but-realistic dodgeable multi-arc barrage!
            SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 10.6f, 0.4f), 0.2f, new Color(1f, 0.38f, 0.2f));
            SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 8.4f, 5.4f), 9.2f, new Color(1f, 0.52f, 0.2f));
            SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 6.5f, 8.2f), 9.5f, new Color(1f, 0.68f, 0.25f));
            yield return new WaitForSeconds(0.18f);
            if (!isDefeated)
            {
                SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 9.8f, 2.7f), 5.2f, new Color(1f, 0.35f, 0.15f));
            }
            attackTimer = 1.95f;
        }
        else if (hasTriggeredHalfAngry)
        {
            // HALF-HEALTH ANGRY (<= 3 HP): Throws 3 objects at once in a realistic spread (low, mid-arc, high-arc)
            SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 9.8f, 0.3f), 0.15f, new Color(1f, 0.48f, 0.22f));
            SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 7.8f, 5.6f), 9.0f, new Color(1f, 0.65f, 0.25f));
            SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 5.8f, 8.4f), 9.4f, new Color(1f, 0.80f, 0.35f));
            attackTimer = 2.05f;
        }
        else
        {
            // PHASE 1 (Normal): Throws 1 little object at a time (alternating low runner & medium lob)
            if (attackCounter % 2 == 1)
            {
                SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 9.2f, 0.2f), 0.1f, Color.white);
            }
            else
            {
                SpawnProjectile(spawnPos, new Vector2(dirToPlayer * 7.6f, 5.8f), 9.0f, Color.white);
            }
            attackTimer = 1.60f;
        }

        yield return new WaitForSeconds(0.28f);

        if (!isDefeated && !isStaggered)
        {
            SetFrame(IsAngry ? 4 : 0);
        }
        isRoaring = false;
    }

    private void SpawnProjectile(Vector3 pos, Vector2 velocity, float gravityScale, Color tint)
    {
        GameObject projObj = new GameObject("BossProjectile");
        projObj.transform.position = pos;
        projObj.transform.localScale = new Vector3(0.62f, 0.62f, 1f);

        SpriteRenderer psr = projObj.AddComponent<SpriteRenderer>();
        psr.sortingOrder = 11;

        CircleCollider2D col = projObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        BossProjectile bp = projObj.AddComponent<BossProjectile>();
        bp.Launch(velocity, gravityScale, projectileFrames, tint);
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
        if (isDefeated) return;
        if (!collision.CompareTag("Player")) return;

        var pc = collision.GetComponent<PlayerController>();
        var ph = collision.GetComponent<PlayerHealth>();
        var prb = collision.GetComponent<Rigidbody2D>();

        // Check Weakness #1: HEAD STOMP (can damage AND is the ONLY way to KILL the boss!)
        Bounds bossBounds = bodyCol.bounds;
        float playerBottom = collision.bounds.min.y;
        float headZoneY = bossBounds.center.y + bossBounds.extents.y * 0.22f;
        bool isFallingOrApex = (prb == null || prb.linearVelocity.y <= 1.5f);

        if (playerBottom >= headZoneY && isFallingOrApex && pc != null)
        {
            if (hitCooldownTimer > 0f) return;
            pc.Bounce(13.8f);
            TakeBossDamage(1, true);
            return;
        }

        // Check Weakness #2: DASHING RIGHT THROUGH (easier to do — weakens & staggers him, but CANNOT kill!)
        if (pc != null && pc.IsDashing)
        {
            if (hitCooldownTimer > 0f) return;
            TakeBossDamage(1, false);
            return;
        }

        // Otherwise, if boss is not currently stunned/invulnerable from just being hit, Kip takes contact damage
        if (!isStaggered && hitCooldownTimer <= 0f && ph != null)
        {
            ph.TakeDamage(contactDamage, transform.position);
        }
    }

    private void TakeBossDamage(int amount, bool fromHeadStomp)
    {
        hitCooldownTimer = 0.48f;

        // STRICT KILL CONDITION: ONLY a head stomp can kill the boss!
        if (!fromHeadStomp && currentHealth <= 1)
        {
            // Boss is already at 1 HP — dashing through weakens/kneels him so Kip can finish him on his head!
            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.PlaySparklePickup(transform.position + Vector3.up * 0.8f);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLandSound();
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
            // Dash-through weakens him down to a minimum of 1 HP (cannot deal the killing blow!)
            currentHealth = Mathf.Max(1, currentHealth - amount);
        }

        if (currentHealth < 0) currentHealth = 0;

        if (ParticleFXManager.Instance != null)
        {
            Vector3 fxPos = fromHeadStomp ? (transform.position + Vector3.up * 1.45f) : transform.position;
            ParticleFXManager.Instance.PlayEnemyDefeated(fxPos);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLandSound();
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.24f, 0.22f);
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

        // Check Angry Phase transitions!
        if (currentHealth == 1 && !hasTriggeredFinalFury)
        {
            hasTriggeredFinalFury = true;
            hasTriggeredHalfAngry = true;
            StartCoroutine(AngryPhaseTransitionRoutine("FINAL FURY! DODGE THE BARRAGE & STOMP HIS HEAD!"));
        }
        else if (currentHealth <= maxHealth / 2 && !hasTriggeredHalfAngry)
        {
            hasTriggeredHalfAngry = true;
            StartCoroutine(AngryPhaseTransitionRoutine("THE OVERSEER IS ANGRY! MULTI-THROW BARRAGE!"));
        }
        else
        {
            // Brief stagger/kneel (longer when dashed through so head is exposed!)
            StartCoroutine(StaggerRoutine(fromHeadStomp ? 0.45f : 0.95f));
        }
    }

    private IEnumerator StaggerRoutine(float duration)
    {
        isStaggered = true;
        SetFrame(duration >= 0.8f ? 6 : 3); // Frame 6: Weakened Kneel exposing head; Frame 3: Hurt flash
        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.7f, 0.7f, 1f);

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
        isRoaring = true;
        isStaggered = true;
        SetFrame(4); // Angry Roar!

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealthUI(bossName, currentHealth, maxHealth, true);
            UIManager.Instance.ShowToastNotification(message, 3.0f);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.55f, 0.28f);
        }
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayBossAngryRoar(transform.position + Vector3.up * 0.6f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHurtSound();
        }

        float elapsed = 0f;
        while (elapsed < 0.85f)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (Mathf.FloorToInt(elapsed * 18f) % 2 == 0)
                    ? new Color(1f, 0.42f, 0.35f, 1f)
                    : new Color(1f, 0.90f, 0.75f, 1f);
            }
            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x + Random.Range(-0.08f, 0.08f), arenaMinX, arenaMaxX),
                startPos.y, transform.position.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.84f, 0.80f, 1f);
        isStaggered = false;
        isRoaring = false;
        attackTimer = 0.35f; // Immediately unleash angry barrage!
    }

    private IEnumerator DefeatRoutine()
    {
        isDefeated = true;
        bodyCol.enabled = false;
        SetFrame(7); // Defeat meltdown!

        // Clear all active projectiles cleanly
        BossProjectile[] activeProjs = FindObjectsByType<BossProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < activeProjs.Length; i++)
        {
            if (activeProjs[i] != null) Destroy(activeProjs[i].gameObject);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideBossHealthUI();
            UIManager.Instance.ShowToastNotification("OVERSEER DEFEATED! THE PORTAL IS UNLOCKED!", 3.5f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayWinSound();
        }

        for (int burst = 0; burst < 4; burst++)
        {
            if (ParticleFXManager.Instance != null)
            {
                Vector3 offset = (Vector3)Random.insideUnitCircle * 1.1f;
                ParticleFXManager.Instance.PlayEnemyDefeated(transform.position + offset);
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.Shake(0.25f, 0.25f);
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = (burst % 2 == 0) ? new Color(1f, 0.95f, 0.6f, 1f) : new Color(1f, 0.35f, 0.25f, 1f);
            }
            yield return new WaitForSeconds(0.24f);
        }

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayPortalOpenBurst(transform.position);
        }

        // Unlock the final Core Reactor Portal!
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
