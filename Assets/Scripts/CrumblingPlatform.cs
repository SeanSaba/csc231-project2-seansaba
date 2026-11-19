using System.Collections;
using UnityEngine;

/// <summary>
/// Unpredictable ancient stone ledge that trembles and sheds rubble when Kip steps on it,
/// collapses into the abyss, and reassembles with conduit energy after a delay.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class CrumblingPlatform : MonoBehaviour
{
    [SerializeField] private float shakeDuration = 0.55f;
    [SerializeField] private float respawnDelay = 3.2f;
    [SerializeField] private float shakeMagnitude = 0.08f;

    private BoxCollider2D boxCol;
    private SpriteRenderer[] renderers;
    private Vector3 originPos;
    private bool isTriggered;

    private void Awake()
    {
        boxCol = GetComponent<BoxCollider2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
        originPos = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isTriggered) return;
        if (collision.collider.CompareTag("Player"))
        {
            // Trigger when player lands on top of the ledge
            if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.45f)
            {
                StartCoroutine(CrumbleRoutine());
            }
        }
    }

    private IEnumerator CrumbleRoutine()
    {
        isTriggered = true;
        float elapsed = 0f;
        float dustTick = 0f;

        // 1. Shake and shed falling stone crumbs
        while (elapsed < shakeDuration)
        {
            float progress = elapsed / shakeDuration;
            Vector2 jitter = Random.insideUnitCircle * (shakeMagnitude * (0.5f + progress));
            transform.position = originPos + new Vector3(jitter.x, jitter.y, 0f);

            dustTick += Time.deltaTime;
            if (dustTick >= 0.09f)
            {
                dustTick = 0f;
                if (ParticleFXManager.Instance != null)
                {
                    ParticleFXManager.Instance.EmitRubbleDust(originPos + Vector3.down * 0.35f);
                }
            }

            // Slight warning amber/crack tint
            SetColor(Color.Lerp(Color.white, new Color(1f, 0.72f, 0.55f, 1f), progress));

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2. Collapse!
        transform.position = originPos;
        boxCol.enabled = false;
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayPlatformCollapse(originPos);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLandSound();
        }

        // Brief falling fade-out
        float fallTime = 0.25f;
        elapsed = 0f;
        while (elapsed < fallTime)
        {
            float t = elapsed / fallTime;
            transform.position = originPos + Vector3.down * (t * t * 1.4f);
            SetColor(new Color(0.8f, 0.7f, 0.7f, 1f - t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetRenderersEnabled(false);

        // 3. Wait and reassemble with cyan conduit sparkle
        yield return new WaitForSeconds(respawnDelay);

        transform.position = originPos;
        SetRenderersEnabled(true);
        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlaySparklePickup(originPos);
        }

        float fadeIn = 0.25f;
        elapsed = 0f;
        while (elapsed < fadeIn)
        {
            float t = elapsed / fadeIn;
            SetColor(new Color(0.75f + 0.25f * t, 1f, 1f, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        SetColor(Color.white);
        boxCol.enabled = true;
        isTriggered = false;
    }

    private void SetColor(Color c)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].color = c;
        }
    }

    private void SetRenderersEnabled(bool enabled)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = enabled;
        }
    }
}
