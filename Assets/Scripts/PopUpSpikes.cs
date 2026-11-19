using System.Collections;
using UnityEngine;

/// <summary>
/// Embedded ground spikes that sit concealed inside a recessed floor slot and
/// snap upward unpredictably when the player approaches or on a staggered wave cycle.
/// </summary>
public class PopUpSpikes : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform spikesVisual;
    [SerializeField] private Collider2D damageCollider;

    [Header("Motion Settings")]
    [SerializeField] private float retractedLocalY = -0.68f;
    [SerializeField] private float peekLocalY = -0.44f;
    [SerializeField] private float extendedLocalY = 0.04f;
    [SerializeField] private float telegraphDuration = 0.22f;
    [SerializeField] private float popUpDuration = 0.07f;
    [SerializeField] private float holdDuration = 0.90f;
    [SerializeField] private float retractDuration = 0.26f;
    [SerializeField] private float cooldownDuration = 0.85f;

    [Header("Behavior Mode")]
    [SerializeField] private bool proximityTriggered = true;
    [SerializeField] private float triggerRadiusX = 2.1f;
    [SerializeField] private float triggerRadiusY = 2.6f;
    [SerializeField] private float autoCycleInterval = 2.4f;
    [SerializeField] private float phaseOffset = 0f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;

    private Transform playerTransform;
    private bool isCycling;
    private bool isDangerous;
    private float autoTimer;

    private void Awake()
    {
        if (spikesVisual == null && transform.childCount > 0)
        {
            spikesVisual = transform.GetChild(0);
        }
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }

        SetVisualY(retractedLocalY);
        SetDangerous(false);
        autoTimer = phaseOffset;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;
        if (isCycling) return;

        if (proximityTriggered)
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) playerTransform = player.transform;
                return;
            }

            float dx = Mathf.Abs(playerTransform.position.x - transform.position.x);
            float dy = playerTransform.position.y - transform.position.y;

            // Trigger as Kip runs up to or steps over the recessed floor seam
            if (dx <= triggerRadiusX && dy >= -0.8f && dy <= triggerRadiusY)
            {
                StartCoroutine(SpikeCycleRoutine());
            }
        }
        else
        {
            autoTimer += Time.deltaTime;
            if (autoTimer >= autoCycleInterval)
            {
                autoTimer = 0f;
                StartCoroutine(SpikeCycleRoutine());
            }
        }
    }

    private IEnumerator SpikeCycleRoutine()
    {
        isCycling = true;

        // 1. Telegraph: rapid rattle + warning sparks + tips peeking out of the floor slot
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlaySpikeTelegraph(transform.position + Vector3.up * 0.10f);
        }

        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            float jitterX = Mathf.Sin(elapsed * 90f) * 0.045f;
            float y = Mathf.Lerp(retractedLocalY, peekLocalY, elapsed / telegraphDuration);
            SetVisualPos(jitterX, y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Erupt straight up out of the floor!
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlaySpikeErupt(transform.position + Vector3.up * 0.20f);
        }

        elapsed = 0f;
        while (elapsed < popUpDuration)
        {
            float t = elapsed / popUpDuration;
            float easeOut = 1f - (1f - t) * (1f - t);
            float y = Mathf.Lerp(peekLocalY, extendedLocalY, easeOut);
            SetVisualPos(0f, y);
            if (t >= 0.35f) SetDangerous(true);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetVisualPos(0f, extendedLocalY);
        SetDangerous(true);

        // 3. Hold extended
        yield return new WaitForSeconds(holdDuration);

        // 4. Retract smoothly back inside the floor
        elapsed = 0f;
        while (elapsed < retractDuration)
        {
            float t = elapsed / retractDuration;
            float y = Mathf.Lerp(extendedLocalY, retractedLocalY, t * t);
            SetVisualPos(0f, y);
            if (t >= 0.45f) SetDangerous(false);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetVisualPos(0f, retractedLocalY);
        SetDangerous(false);

        yield return new WaitForSeconds(cooldownDuration);
        isCycling = false;
    }

    private void SetVisualY(float localY)
    {
        SetVisualPos(0f, localY);
    }

    private void SetVisualPos(float localX, float localY)
    {
        if (spikesVisual != null)
        {
            spikesVisual.localPosition = new Vector3(localX, localY, 0f);
        }
    }

    private void SetDangerous(bool active)
    {
        isDangerous = active;
        if (damageCollider != null)
        {
            damageCollider.enabled = active;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryDamage(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryDamage(collision);
    }

    private void TryDamage(Collider2D collision)
    {
        if (!isDangerous) return;
        if (collision.CompareTag("Player"))
        {
            var health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
        }
    }
}
