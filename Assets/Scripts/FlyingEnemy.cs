using UnityEngine;

public class FlyingEnemy : MonoBehaviour
{
    [SerializeField] private float hoverAmplitude = 1.2f;
    [SerializeField] private float hoverFrequency = 2.5f;
    [SerializeField] private float horizontalSpeed = 2f;
    [SerializeField] private float horizontalRange = 3f;
    [SerializeField] private int damage = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Vector2 startPos;
    private int direction = 1;
    private bool isDefeated;

    private void Awake()
    {
        startPos = transform.position;
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (isDefeated) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        // Sine wave hover along Y
        float newY = startPos.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;

        // Patrol along X
        float currentX = transform.position.x + (direction * horizontalSpeed * Time.deltaTime);
        float offset = currentX - startPos.x;
        if (Mathf.Abs(offset) >= horizontalRange)
        {
            direction = (offset > 0) ? -1 : 1;
            if (spriteRenderer != null) spriteRenderer.flipX = (direction < 0);
        }

        transform.position = new Vector3(currentX, newY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDefeated) return;

        if (collision.CompareTag("Player"))
        {
            var health = collision.GetComponent<PlayerHealth>();
            var controller = collision.GetComponent<PlayerController>();

            if (controller != null && controller.IsDashing)
            {
                Defeat(controller);
                return;
            }

            float playerBottom = collision.bounds.min.y;
            float enemyTop = GetComponent<Collider2D>().bounds.max.y;

            if (playerBottom >= enemyTop - 0.25f && controller != null)
            {
                controller.Bounce(11f);
                Defeat(controller);
            }
            else if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
        }
    }

    private void Defeat(PlayerController player)
    {
        isDefeated = true;
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayEnemyDefeated(transform.position);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayImpactSound();
        }
        Destroy(gameObject);
    }
}
