using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8.5f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 70f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float fallMultiplier = 3.2f;
    [SerializeField] private float lowJumpMultiplier = 2.2f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.15f);

    [Header("Signature Move: Air Dash")]
    [SerializeField] private float dashSpeed = 19f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private bool canAirDash = true;

    [Header("Sprite References")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite runSprite;
    [SerializeField] private Sprite jumpSprite;
    [SerializeField] private Sprite fallSprite;
    [SerializeField] private Sprite hurtSprite;
    [SerializeField] private Sprite dashSprite;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool wasGrounded;
    private float horizontalInput;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownCounter;
    private bool hasAirDashed;
    private int facingDirection = 1;
    private bool isHurt;

    public bool IsDashing => isDashing;
    public float DashCooldownProgress => Mathf.Clamp01(1f - (dashCooldownCounter / dashCooldown));

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;
        if (isHurt) return;

        ReadInput();
        UpdateTimers();
        HandleJumpBuffer();
        HandleDash();
        UpdateSpriteVisuals();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

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
        horizontalInput = 0f;

        // Support New Input System
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
            // Fallback to legacy input
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
        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
            hasAirDashed = false;
        }
        else
        {
            coyoteCounter -= Time.deltaTime;
        }

        jumpBufferCounter -= Time.deltaTime;
        dashCooldownCounter -= Time.deltaTime;
    }

    private void CheckGround()
    {
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position + Vector2.down * 0.5f;
        Collider2D hit = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer);
        isGrounded = hit != null;

        if (isGrounded && !wasGrounded && rb.linearVelocity.y <= 0.1f)
        {
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
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isDashing)
        {
            ExecuteJump();
        }
    }

    private void ExecuteJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayJumpDust(transform.position + Vector3.down * 0.4f);
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
        dashCooldownCounter = dashCooldown;

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
            CameraController.Instance.Shake(0.12f, 0.15f);
        }
    }

    private void HandleDash()
    {
        if (!isDashing) return;

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.linearVelocity = new Vector2(facingDirection * moveSpeed, 0f);
        }
    }

    private void PerformDashMovement()
    {
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0f);
    }

    private void ApplyHorizontalMovement()
    {
        float targetSpeed = horizontalInput * moveSpeed;
        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float newSpeed = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);

        if (isGrounded && Mathf.Abs(horizontalInput) > 0.1f && ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.EmitFootstepDust(transform.position + Vector3.down * 0.45f);
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
        if (Keyboard.current != null)
        {
            return Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed;
        }
        return Input.GetButton("Jump") || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    private void UpdateSpriteVisuals()
    {
        if (spriteRenderer == null) return;

        spriteRenderer.flipX = (facingDirection < 0);

        if (isDashing)
        {
            if (dashSprite != null) spriteRenderer.sprite = dashSprite;
            return;
        }

        if (isGrounded)
        {
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                if (runSprite != null) spriteRenderer.sprite = runSprite;
            }
            else
            {
                if (idleSprite != null) spriteRenderer.sprite = idleSprite;
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

        if (hurtSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = hurtSprite;
        }

        yield return new WaitForSeconds(duration);
        isHurt = false;
    }

    public void Bounce(float bounceForce = 11f)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
        hasAirDashed = false;
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
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 checkPos = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position + Vector2.down * 0.5f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(checkPos, groundCheckSize);
    }
}
