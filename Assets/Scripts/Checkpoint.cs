using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color activatedColor = new Color(0f, 1f, 1f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 0.9f);

    private bool isActivated;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.color = inactiveColor;
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
            spriteRenderer.color = activatedColor;
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
            GameManager.Instance.UpdateCheckpoint(transform.position + Vector3.up * 0.2f);
        }
    }
}
