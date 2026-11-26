using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8.5f;
    [SerializeField] private float acceleration = 65f;
    [SerializeField] private float deceleration = 75f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 14.5f;
    [SerializeField] private float doubleJumpForce = 13.8f;
    [SerializeField] private float fallMultiplier = 3.2f;
    [SerializeField] private float lowJumpMultiplier = 2.2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.55f, 0.16f);

    [Header("Abilities")]
    [SerializeField] private bool canDoubleJump = false;

    [Header("Signature Move: Air Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dashCooldown = 0.75f;
    [SerializeField] private bool canAirDash = true;

    [Header("Visuals & Animation")]
    [SerializeField] private Transform visualTransform;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite idleSprite2;
    [SerializeField] private Sprite runSprite;
    [SerializeField] private Sprite runSprite2;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite fallSprite;
    [SerializeField] private Sprite hurtSprite;
    [SerializeField] private Sprite dashSprite;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float squashReturnSpeed = 14f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded = true;
    private float spawnSilenceTimer = 0.55f;
    private float airTime = 0f;
    private float horizontalInput;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isDashing;
    private float dashTimer;
    private float dashTrailTimer;
    private float dashCooldownCounter;
    private bool hasAirDashed;
    private bool hasDoubleJumped;
    private float jumpLockoutTimer;
    private float bounceLockTimer;
    private int facingDirection = 1;
    private Vector2 dashDirection = Vector2.right;
    private bool isHurt;
    private bool isPlayingFinalDeath;
    private bool useDemoInput;
    private float demoMoveX;
    private bool demoJumpQueued;
    private bool demoJumpHeld;
    private bool demoDashQueued;
    private Vector3 baseVisualScale = Vector3.one;

    public void SetDemoInput(bool active, float moveX = 0f, bool jumpPressed = false, bool jumpHeld = false, bool dashPressed = false)
    {
        useDemoInput = active;
        demoMoveX = moveX;
        if (jumpPressed) demoJumpQueued = true;
        demoJumpHeld = jumpHeld;
        if (dashPressed) demoDashQueued = true;
    }
    private float animTimer;
    private float steamPuffTimer;
    private float footstepDustTimer;

    private static readonly int AnimSpeedHash = Animator.StringToHash("Speed");
    private static readonly int AnimGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int AnimVelYHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int AnimDashingHash = Animator.StringToHash("IsDashing");
    private static readonly int AnimHurtHash = Animator.StringToHash("IsHurt");

    public bool IsDashing => isDashing;
    public bool IsGrounded => isGrounded;
    public bool HasDoubleJump => canDoubleJump;
    public float DashCooldownProgress => Mathf.Clamp01(1f - (dashCooldownCounter / dashCooldown));

    public void UnlockDoubleJump()
    {
        canDoubleJump = true;
        hasDoubleJumped = false;
    }

    public void LockDoubleJump()
    {
        canDoubleJump = false;
        hasDoubleJumped = false;
    }

    private void Awake()
    {
        // Always start with Double Jump strictly disabled until the Special Orange Gem is collected
        canDoubleJump = false;
        hasDoubleJumped = false;
        wasGrounded = true;
        spawnSilenceTimer = 0.55f;
        airTime = 0f;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Interpolate physics transforms so high-refresh camera tracking never double-blurs Kip while running
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        if (visualTransform == null && spriteRenderer != null)
        {
            visualTransform = spriteRenderer.transform;
        }
        if (visualTransform != null)
        {
            baseVisualScale = visualTransform.localScale;
        }
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        if (isPlayingFinalDeath) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsGamePaused || GameManager.Instance.IsLevelComplete || GameManager.Instance.IsGameOver)) return;
        if (isHurt)
        {
            UpdateAnimatorParameters();
            return;
        }

        ReadInput();
        UpdateTimers();
        HandleJumpBuffer();
        HandleDash();
        UpdateSquashAndStretch();
        UpdateSpriteVisuals();
        UpdateAnimatorParameters();
    }

    private void FixedUpdate()
    {
        if (isPlayingFinalDeath) return;
        if (GameManager.Instance != null && (GameManager.Instance.IsGamePaused || GameManager.Instance.IsLevelComplete || GameManager.Instance.IsGameOver))
        {
            if (GameManager.Instance.IsLevelComplete || GameManager.Instance.IsGameOver)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            return;
        }

        CheckGround();

        if (isDashing)
        {
            PerformDashMovement();
            return;
        }

        if (isHurt) return;

        ApplyHorizontalMovement();
        ApplyCustomGravity();
    }

    private void ReadInput()
    {
        if (useDemoInput)
        {
            horizontalInput = demoMoveX;
            if (demoJumpQueued)
            {
                jumpBufferCounter = jumpBufferTime;
                demoJumpQueued = false;
            }
            if (demoDashQueued)
            {
                TriggerDash();
                demoDashQueued = false;
            }
            if (horizontalInput > 0.05f) facingDirection = 1;
            else if (horizontalInput < -0.05f) facingDirection = -1;
            return;
        }

        horizontalInput = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (Keyboard.current.leftShiftKey.wasPressedThisFrame || Keyboard.current.rightShiftKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame)
            {
                TriggerDash();
            }
        }
        else
        {
            horizontalInput = Input.GetAxisRaw("Horizontal");
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                jumpBufferCounter = jumpBufferTime;
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.J))
            {
                TriggerDash();
            }
        }

        if (horizontalInput > 0.05f) facingDirection = 1;
        else if (horizontalInput < -0.05f) facingDirection = -1;
    }

    private void UpdateTimers()
    {
        if (jumpLockoutTimer > 0f)
        {
            jumpLockoutTimer -= Time.deltaTime;
        }
        if (bounceLockTimer > 0f)
        {
            bounceLockTimer -= Time.deltaTime;
        }
        if (spawnSilenceTimer > 0f)
        {
            spawnSilenceTimer -= Time.deltaTime;
        }

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            hasAirDashed = false;
            hasDoubleJumped = false;
            airTime = 0f;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
            airTime += Time.deltaTime;
        }

        jumpBufferCounter -= Time.deltaTime;
        dashCooldownCounter -= Time.deltaTime;
        animTimer += Time.deltaTime;

        steamPuffTimer -= Time.deltaTime;
        if (steamPuffTimer <= 0f && !isDashing && ParticleFXManager.Instance != null)
        {
            steamPuffTimer = Mathf.Abs(horizontalInput) > 0.1f ? 0.38f : 0.75f;
            Vector3 packPos = transform.position + new Vector3(-facingDirection * 0.38f, 0.26f, 0f);
            ParticleFXManager.Instance.EmitSteamPackPuff(packPos);
        }

        if (footstepDustTimer > 0f)
        {
            footstepDustTimer -= Time.deltaTime;
        }
    }

    private void CheckGround()
    {
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position + Vector2.down * 0.84f;
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkPos, groundCheckSize, 0f, groundLayer);

        bool groundedNow = false;
        if (jumpLockoutTimer <= 0f && rb.linearVelocity.y <= 0.5f)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];
                if (col != null && col.gameObject != gameObject && !col.isTrigger)
                {
                    groundedNow = true;
                    break;
                }
            }
        }
        isGrounded = groundedNow;

        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0.5f && spawnSilenceTimer <= 0f && airTime >= 0.12f)
        {
            ApplySquash(1.15f, 0.85f);
            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.PlayLandingDust(checkPos);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayLandSound();
            }
        }

        wasGrounded = isGrounded;
    }

    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0f && !isDashing)
        {
            if (coyoteCounter > 0f)
            {
                ExecuteJump();
            }
            else if (canDoubleJump && !hasDoubleJumped)
            {
                ExecuteDoubleJump();
            }
        }
    }

    private void ExecuteDoubleJump()
    {
        hasDoubleJumped = true;
        jumpBufferCounter = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, doubleJumpForce);
        ApplySquash(0.86f, 1.18f);

        if (ParticleFXManager.Instance != null)
        {
            Vector3 feetPos = groundCheckPoint != null ? groundCheckPoint.position : transform.position + Vector3.down * 0.75f;
            ParticleFXManager.Instance.PlayDoubleJumpBurst(feetPos);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJumpSound();
        }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        jumpLockoutTimer = 0.15f;
        isGrounded = false;
        ApplySquash(0.90f, 1.12f);

        if (ParticleFXManager.Instance != null)
        {
            Vector3 dustPos = groundCheckPoint != null ? groundCheckPoint.position : transform.position + Vector3.down * 0.8f;
            ParticleFXManager.Instance.PlayJumpDust(dustPos);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJumpSound();
        }
    }

    private void TriggerDash()
    {
        if (dashCooldownCounter > 0f || isDashing) return;
        if (!isGrounded && hasAirDashed && !canAirDash) return;

        isDashing = true;
        dashTimer = dashDuration;
        dashTrailTimer = 0f;
        dashCooldownCounter = dashCooldown;
        dashDirection = new Vector2(facingDirection, 0f);
        ApplySquash(1.20f, 0.82f);

        if (!isGrounded)
        {
            hasAirDashed = true;
        }

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayDashEffect(transform.position, facingDirection);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDashSound();
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.12f, 0.14f);
        }
    }

    private void HandleDash()
    {
        if (!isDashing) return;

        dashTimer -= Time.deltaTime;
        dashTrailTimer -= Time.deltaTime;
        if (dashTrailTimer <= 0f)
        {
            dashTrailTimer = 0.025f;
            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.EmitDashTrail(transform.position);
            }
        }

        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.linearVelocity = new Vector2(dashDirection.x * moveSpeed, 0f);
        }
    }

    private void PerformDashMovement()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void ApplyHorizontalMovement()
    {
        // Preserve knock-away launch momentum briefly after bouncing off the Final Boss's head
        if (bounceLockTimer > 0f) return;

        float targetSpeed = horizontalInput * moveSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f && footstepDustTimer <= 0f && ParticleFXManager.Instance != null)
        {
            footstepDustTimer = 0.17f;
            Vector3 dustPos = (groundCheckPoint != null ? groundCheckPoint.position : transform.position + Vector3.down * 0.8f)
                + new Vector3(-facingDirection * 0.25f, 0.04f, 0f);
            ParticleFXManager.Instance.EmitFootstepDust(dustPos);
        }
    }

    private void ApplyCustomGravity()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !IsJumpHeld())
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private bool IsJumpHeld()
    {
        if (useDemoInput) return demoJumpHeld;
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
        }
        return Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    private void ApplySquash(float xMult, float yMult)
    {
        if (visualTransform != null && visualTransform != transform)
        {
            visualTransform.localScale = new Vector3(baseVisualScale.x * xMult, baseVisualScale.y * yMult, baseVisualScale.z);
        }
    }

    private void UpdateSquashAndStretch()
    {
        if (visualTransform != null && visualTransform != transform)
        {
            visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, baseVisualScale, squashReturnSpeed * Time.deltaTime);
        }
    }

    private void UpdateSpriteVisuals()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = (facingDirection < 0);

        // If an Animator controller is actively driving sprites, let it handle frame swapping
        if (animator != null && animator.runtimeAnimatorController != null) return;

        if (isDashing)
        {
            if (dashSprite != null) spriteRenderer.sprite = dashSprite;
            return;
        }

        if (isGrounded)
        {
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                bool useSecond = ((int)(animTimer * 10f) % 2) == 1;
                spriteRenderer.sprite = (useSecond && runSprite2 != null) ? runSprite2 : runSprite;
            }
            else
            {
                bool useSecond = ((int)(animTimer * 2.5f) % 2) == 1;
                spriteRenderer.sprite = (useSecond && idleSprite2 != null) ? idleSprite2 : idleSprite;
            }
        }
        else
        {
            if (rb.linearVelocity.y > 0.5f)
            {
                if (jumpSprite != null) spriteRenderer.sprite = jumpSprite;
            }
            else
            {
                if (fallSprite != null) spriteRenderer.sprite = fallSprite;
            }
        }
    }

    private void UpdateAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        animator.SetFloat(AnimSpeedHash, Mathf.Abs(horizontalInput));
        animator.SetBool(AnimGroundedHash, isGrounded);
        animator.SetFloat(AnimVelYHash, rb.linearVelocity.y);
        animator.SetBool(AnimDashingHash, isDashing);
        animator.SetBool(AnimHurtHash, isHurt);
    }

    public void OnTakeDamage(Vector2 knockbackDir, float duration = 0.35f)
    {
        if (isHurt) return;
        StartCoroutine(HurtRoutine(knockbackDir, duration));
    }

    private IEnumerator HurtRoutine(Vector2 knockbackDir, float duration)
    {
        isHurt = true;
        isDashing = false;
        rb.linearVelocity = knockbackDir;
        ApplySquash(1.18f, 0.82f);

        if (hurtSprite != null && spriteRenderer != null && (animator == null || animator.runtimeAnimatorController == null))
        {
            spriteRenderer.sprite = hurtSprite;
        }

        yield return new WaitForSeconds(duration);
        isHurt = false;
    }

    /// <summary>
    /// Dramatic 6-frame final defeat animation played when Kip loses all 3 lives before level reset.
    /// </summary>
    public IEnumerator PlayFinalDeathAnimation()
    {
        isPlayingFinalDeath = true;
        isDashing = false;
        isHurt = true;

        // Disable Animator so Idle/Hurt transitions never override our 6-frame collapse sequence!
        if (animator != null) animator.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            // If Kip died by falling into the deep abyss, bring Kip cleanly into lower-center camera view so the player sees the entire death animation!
            Camera cam = Camera.main;
            if (cam != null && transform.position.y < cam.transform.position.y - 4.2f)
            {
                transform.position = new Vector3(transform.position.x, cam.transform.position.y - 2.1f, transform.position.z);
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
            else
            {
                rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        if (visualTransform != null && visualTransform != transform)
        {
            visualTransform.localScale = baseVisualScale;
        }

        float[] frameDurations = { 0.30f, 0.32f, 0.36f, 0.38f, 0.44f, 0.72f };
        int count = (deathFrames != null && deathFrames.Length > 0) ? deathFrames.Length : 6;

        for (int i = 0; i < count; i++)
        {
            if (spriteRenderer != null && deathFrames != null && i < deathFrames.Length && deathFrames[i] != null)
            {
                spriteRenderer.sprite = deathFrames[i];
            }

            // Dramatic synced FX on each beat of Kip's collapse!
            if (i == 0)
            {
                ApplySquash(1.16f, 0.86f);
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.PlayDeathBurst(transform.position);
                    ParticleFXManager.Instance.EmitBossEmber(transform.position + Vector3.up * 0.25f);
                }
            }
            else if (i == 1)
            {
                if (ParticleFXManager.Instance != null)
                {
                    Vector3 pack = transform.position + new Vector3(-facingDirection * 0.35f, 0.35f, 0f);
                    ParticleFXManager.Instance.EmitSteamPackPuff(pack);
                    ParticleFXManager.Instance.EmitSteamPackPuff(pack + Vector3.up * 0.15f);
                }
            }
            else if (i == 2)
            {
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.PlayLandingDust(transform.position + Vector3.down * 0.65f);
                }
            }
            else if (i == 3)
            {
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.EmitRubbleDust(transform.position + Vector3.down * 0.55f);
                    ParticleFXManager.Instance.PlaySparklePickup(transform.position + Vector3.up * 0.35f);
                }
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.Shake(0.18f, 0.16f);
                }
            }
            else if (i == 4)
            {
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.PlaySparklePickup(transform.position + Vector3.up * 0.15f);
                }
            }

            float wait = (i < frameDurations.Length) ? frameDurations[i] : 0.35f;
            float elapsed = 0f;
            while (elapsed < wait)
            {
                if (visualTransform != null && visualTransform != transform)
                {
                    visualTransform.localScale = Vector3.Lerp(visualTransform.localScale, baseVisualScale, 10f * Time.deltaTime);
                }
                if (i == count - 1 && spriteRenderer != null)
                {
                    float fadeT = Mathf.Clamp01(elapsed / wait);
                    spriteRenderer.color = Color.Lerp(Color.white, new Color(0.65f, 0.78f, 0.92f, 0.30f), fadeT);
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.SetDeathFade(fadeT * 0.85f);
                    }
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    public void Bounce(float bounceForce = 11f)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
        hasAirDashed = false;
        ApplySquash(0.88f, 1.15f);
    }

    /// <summary>
    /// Vaults Kip cleanly up AND away in another direction (used when stomping the Final Boss's head
    /// so Kip cannot repeatedly chain head-stomps in place).
    /// </summary>
    public void BounceAway(Vector2 launchVelocity, float controlLockDuration = 0.30f)
    {
        isDashing = false;
        rb.linearVelocity = launchVelocity;
        bounceLockTimer = controlLockDuration;
        jumpLockoutTimer = 0.18f;
        isGrounded = false;
        hasAirDashed = false;
        hasDoubleJumped = false;
        if (Mathf.Abs(launchVelocity.x) > 0.1f)
        {
            facingDirection = launchVelocity.x > 0f ? 1 : -1;
        }
        ApplySquash(0.85f, 1.18f);
    }

    public void ResetState(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        isDashing = false;
        isHurt = false;
        dashCooldownCounter = 0f;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        wasGrounded = true;
        spawnSilenceTimer = 0.45f;
        airTime = 0f;
        if (visualTransform != null && visualTransform != transform)
        {
            visualTransform.localScale = baseVisualScale;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position + Vector2.down * 0.84f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}
