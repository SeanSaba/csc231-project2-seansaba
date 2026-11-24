using System.Collections;
using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private int scoreValue = 1;

    private Vector3 startPos;
    private bool isCollected;

    public bool IsCollected => isCollected;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (isCollected) return;

        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(startPos.x, newY, startPos.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCollectible(scoreValue);
            }

            StartCoroutine(PlayUnifiedGemPickupAnimation(gameObject));
        }
    }

    /// <summary>
    /// Shared unified pickup animation & sound used by ALL gems (Blue Aether Crystals & Special Orange Gem).
    /// </summary>
    public static IEnumerator PlayUnifiedGemPickupAnimation(GameObject gemObj)
    {
        if (gemObj == null) yield break;

        Collider2D col = gemObj.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        SpriteFrameAnimator frameAnim = gemObj.GetComponent<SpriteFrameAnimator>();
        if (frameAnim != null) frameAnim.enabled = false;

        ParticleSystem[] childParticles = gemObj.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < childParticles.Length; i++)
        {
            if (childParticles[i] != null) childParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlaySparklePickup(gemObj.transform.position);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCollectSound();
        }

        SpriteRenderer sr = gemObj.GetComponentInChildren<SpriteRenderer>();
        Color origColor = (sr != null) ? sr.color : Color.white;
        Vector3 originPos = gemObj.transform.position;
        Vector3 originScale = gemObj.transform.localScale;

        float duration = 0.30f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (gemObj == null) yield break;
            float t = elapsed / duration;
            float easeOut = 1f - (1f - t) * (1f - t);

            // Smooth upward pop + energetic spin + expand-then-vanish sparkle flash!
            gemObj.transform.position = originPos + Vector3.up * (0.95f * easeOut);
            gemObj.transform.Rotate(0f, 0f, 720f * Time.deltaTime);

            float scaleMult = (t < 0.35f)
                ? Mathf.Lerp(1f, 1.48f, t / 0.35f)
                : Mathf.Lerp(1.48f, 0f, (t - 0.35f) / 0.65f);
            gemObj.transform.localScale = originScale * scaleMult;

            if (sr != null)
            {
                Color c = Color.Lerp(Color.white, origColor, t);
                c.a = (t < 0.45f) ? 1f : Mathf.Clamp01(1f - ((t - 0.45f) / 0.55f));
                sr.color = c;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (gemObj != null)
        {
            Destroy(gemObj);
        }
    }
}
