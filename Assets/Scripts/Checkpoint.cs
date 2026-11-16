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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActivated) return;

        if (collision.CompareTag("Player"))
        {
            ActivateCheckpoint();
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
