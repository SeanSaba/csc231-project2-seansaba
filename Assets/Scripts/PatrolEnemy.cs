using UnityEngine;

public class PatrolEnemy : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2.4f;
    [SerializeField] private float patrolDistance = 4f;
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

        transform.Translate(Vector2.right * (direction * moveSpeed * Time.deltaTime));

        float currentOffset = transform.position.x - startPos.x;
        if (Mathf.Abs(currentOffset) >= patrolDistance)
        {
            direction = (currentOffset > 0) ? -1 : 1;
            if (spriteRenderer != null) spriteRenderer.flipX = (direction < 0);
        }
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
                // Dashing directly through defeats the enemy
                Defeat(controller);
                return;
            }

            // Check if player landed on top
            float playerBottom = collision.bounds.min.y;
            float enemyTop = GetComponent<Collider2D>().bounds.max.y;

            if (playerBottom >= enemyTop - 0.25f && controller != null)
            {
                controller.Bounce(10f);
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
            AudioManager.Instance.PlayLandSound();
        }
        Destroy(gameObject);
    }
}
