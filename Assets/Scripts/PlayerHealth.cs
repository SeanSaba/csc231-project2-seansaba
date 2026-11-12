using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float invincibilityDuration = 1.2f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentHealth;
    private bool isInvincible;
    private PlayerController playerController;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }

    public void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (isInvincible || currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHurtSound();
        }

        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.25f, 0.35f);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }

        Vector2 knockbackDir = ((Vector2)transform.position - sourcePosition).normalized * 8f;
        knockbackDir.y = Mathf.Max(knockbackDir.y, 5f);
        if (playerController != null)
        {
            playerController.OnTakeDamage(knockbackDir);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFlashRoutine());
        }
    }

    public void InstantKill()
    {
        if (currentHealth <= 0) return;
        currentHealth = 0;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayHurtSound();
        }
        Die();
    }

    private void Die()
    {
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayDeathBurst(transform.position);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }

    public void Respawn(Vector3 checkpointPos)
    {
        currentHealth = maxHealth;
        isInvincible = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        if (playerController != null)
        {
            playerController.ResetState(checkpointPos);
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealthUI(currentHealth, maxHealth);
        }
    }

    private IEnumerator InvincibilityFlashRoutine()
    {
        isInvincible = true;
        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < invincibilityDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.4f, 0.4f, 0.5f);
            }
            yield return new WaitForSeconds(flashInterval);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval * 2f;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        isInvincible = false;
    }
}
