using System.Collections;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private ParticleSystem beaconAuraPS;
    [SerializeField] private Color activatedColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    private bool isActivated;
    private float lockedReminderCooldown;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (inactiveSprite != null) spriteRenderer.sprite = inactiveSprite;
            spriteRenderer.color = inactiveColor;
        }
        if (beaconAuraPS != null)
        {
            beaconAuraPS.Stop();
        }
    }

    private void Update()
    {
        if (lockedReminderCooldown > 0f)
        {
            lockedReminderCooldown -= Time.deltaTime;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryActivate(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryActivate(collision);
    }

    private void TryActivate(Collider2D collision)
    {
        if (isActivated) return;
        if (!collision.CompareTag("Player")) return;

        // Require ALL gems placed BEFORE this specific checkpoint to be collected first!
        int missingBefore = CountUncollectedGemsBeforeCheckpoint();
        if (missingBefore > 0)
        {
            if (lockedReminderCooldown <= 0f)
            {
                lockedReminderCooldown = 2.2f;
                if (UIManager.Instance != null)
                {
                    string plural = (missingBefore == 1) ? "GEM" : "GEMS";
                    UIManager.Instance.ShowToastNotification(
                        $"CHECKPOINT LOCKED! {missingBefore} {plural} MISSED BEFORE THIS BEACON — COLLECT ALL GEMS FIRST!", 2.8f);
                }
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayImpactSound();
                }
                StartCoroutine(LockedFlashRoutine());
            }
            return;
        }

        ActivateCheckpoint();
    }

    private int CountUncollectedGemsBeforeCheckpoint()
    {
        int missing = 0;
        float cutoffX = transform.position.x - 0.25f;
        Collectible[] allGems = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
        for (int i = 0; i < allGems.Length; i++)
        {
            Collectible c = allGems[i];
            if (c != null && !c.IsCollected && c.transform.position.x < cutoffX)
            {
                missing++;
            }
        }
        return missing;
    }

    private IEnumerator LockedFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = new Color(1f, 0.34f, 0.26f, 1f);
        yield return new WaitForSeconds(0.28f);
        if (!isActivated && spriteRenderer != null)
        {
            spriteRenderer.color = inactiveColor;
        }
    }

    public void ActivateCheckpoint()
    {
        isActivated = true;
        if (spriteRenderer != null)
        {
            if (activeSprite != null) spriteRenderer.sprite = activeSprite;
            spriteRenderer.color = activatedColor;
        }

        if (beaconAuraPS != null)
        {
            beaconAuraPS.Play();
        }

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlaySparklePickup(transform.position + Vector3.up * 0.5f);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCheckpointSound();
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCheckpoint(transform.position + Vector3.up * 0.4f);
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToastNotification("CHECKPOINT ACTIVATED!");
        }
    }
}
