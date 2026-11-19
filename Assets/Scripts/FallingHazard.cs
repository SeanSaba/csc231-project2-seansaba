using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingHazard : MonoBehaviour
{
    [SerializeField] private float triggerDistance = 3.6f;
    [SerializeField] private float fallDelay = 0.36f;
    [SerializeField] private float respawnDelay = 6.0f;
    [SerializeField] private int damage = 1;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Collider2D col;
    private Vector3 originPos;
    private bool hasTriggered;
    private bool isFalling;
    private Transform playerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originPos = transform.position;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (hasTriggered) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        float dx = Mathf.Abs(playerTransform.position.x - originPos.x);
        float dy = originPos.y - playerTransform.position.y;

        if (dx <= triggerDistance && dy > 0f && dy < 12f)
        {
            StartCoroutine(DropRoutine());
        }
    }

    private IEnumerator DropRoutine()
    {
        hasTriggered = true;
        float elapsed = 0f;

        // Warning shake + falling ceiling pebbles
        while (elapsed < fallDelay)
        {
            transform.position = originPos + (Vector3)Random.insideUnitCircle * 0.09f;
            if (ParticleFXManager.Instance != null && Random.value < 0.35f)
            {
                ParticleFXManager.Instance.EmitRubbleDust(originPos + Vector3.up * 0.35f);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originPos;
        isFalling = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3.2f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isFalling) return;

        if (collision.CompareTag("Player"))
        {
            var health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
            StartCoroutine(ShatterAndRespawnRoutine());
        }
        else if (!collision.isTrigger)
        {
            StartCoroutine(ShatterAndRespawnRoutine());
        }
    }

    private IEnumerator ShatterAndRespawnRoutine()
    {
        isFalling = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayStalactiteShatter(transform.position);
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.12f, 0.14f);
        }

        yield return new WaitForSeconds(respawnDelay);

        transform.position = originPos;
        if (sr != null) sr.enabled = true;
        if (col != null) col.enabled = true;
        hasTriggered = false;
    }
}
