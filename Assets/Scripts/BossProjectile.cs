using UnityEngine;

/// <summary>
/// Spinning molten gear-orb thrown by the Final Boss. Travels along a crisp ballistic
/// or straight trajectory with fiery ember particles and damages Kip on contact.
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float maxLifetime = 5.5f;
    [SerializeField] private float spinSpeed = 540f;

    private Vector2 velocity;
    private float gravity;
    private float lifeTimer;
    private float trailTimer;
    private SpriteRenderer sr;
    private Sprite[] animFrames;
    private float animTimer;
    private int animIdx;
    private bool hasImpacted;

    public void Launch(Vector2 initialVelocity, float gravityScale, Sprite[] frames, Color tint)
    {
        velocity = initialVelocity;
        gravity = gravityScale;
        animFrames = frames;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = tint;
            if (animFrames != null && animFrames.Length > 0 && animFrames[0] != null)
            {
                sr.sprite = animFrames[0];
            }
        }
    }

    private void Update()
    {
        if (hasImpacted) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        float dt = Time.deltaTime;
        lifeTimer += dt;
        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        transform.Rotate(0f, 0f, spinSpeed * dt);

        if (animFrames != null && animFrames.Length > 1 && sr != null)
        {
            animTimer += dt;
            if (animTimer >= 0.08f)
            {
                animTimer = 0f;
                animIdx = (animIdx + 1) % animFrames.Length;
                if (animFrames[animIdx] != null) sr.sprite = animFrames[animIdx];
            }
        }

        trailTimer += dt;
        if (trailTimer >= 0.045f)
        {
            trailTimer = 0f;
            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.EmitBossEmber(transform.position);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasImpacted) return;

        if (collision.CompareTag("Player"))
        {
            var pc = collision.GetComponent<PlayerController>();
            // Allow Kip to narrowly phase-dash cleanly if dashing through, otherwise take damage
            var health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
            Explode();
        }
        else if (!collision.isTrigger)
        {
            // Ignore boss body colliders; shatter cleanly on arena floor/walls
            if (collision.GetComponentInParent<FinalBoss>() == null)
            {
                Explode();
            }
        }
    }

    private void Explode()
    {
        hasImpacted = true;
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayBossProjectileHit(transform.position);
        }
        Destroy(gameObject);
    }
}
