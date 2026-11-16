using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingHazard : MonoBehaviour
{
    [SerializeField] private float triggerDistance = 3.5f;
    [SerializeField] private float fallDelay = 0.4f;
    [SerializeField] private int damage = 1;

    private Rigidbody2D rb;
    private bool hasTriggered;
    private Transform playerTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        float dx = Mathf.Abs(playerTransform.position.x - transform.position.x);
        float dy = transform.position.y - playerTransform.position.y;

        if (dx <= triggerDistance && dy > 0f && dy < 12f)
        {
            StartCoroutine(DropRoutine());
        }
    }

    private IEnumerator DropRoutine()
    {
        hasTriggered = true;
        Vector3 initialPos = transform.position;
        float elapsed = 0f;

        // Warning shake
        while (elapsed < fallDelay)
        {
            transform.position = initialPos + (Vector3)Random.insideUnitCircle * 0.08f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = initialPos;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 3f;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered) return;

        if (collision.CompareTag("Player"))
        {
            var health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
            Shatter();
        }
        else if (!collision.isTrigger)
        {
            Shatter();
        }
    }

    private void Shatter()
    {
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayLandingDust(transform.position);
        }
        Destroy(gameObject);
    }
}
