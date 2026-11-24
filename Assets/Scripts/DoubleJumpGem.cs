using UnityEngine;

/// <summary>
/// Special Orange Gem placed before the Final Boss arena that permanently unlocks
/// Kip's Double Jump ability for the run.
/// </summary>
public class DoubleJumpGem : MonoBehaviour
{
    [SerializeField] private float bobSpeed = 3.2f;
    [SerializeField] private float bobHeight = 0.22f;

    private Vector3 startPos;
    private bool isCollected;

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
        if (!collision.CompareTag("Player")) return;

        isCollected = true;

        var pc = collision.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.UnlockDoubleJump();
        }

        if (ParticleFXManager.Instance != null)
        {
            ParticleFXManager.Instance.PlayDoubleJumpBurst(transform.position);
            ParticleFXManager.Instance.PlaySparklePickup(transform.position);
        }
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCheckpointSound();
        }
        if (CameraController.Instance != null)
        {
            CameraController.Instance.Shake(0.22f, 0.12f);
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetDoubleJumpUnlockedUI(true);
            UIManager.Instance.ShowToastNotification("ORANGE GEM ABSORBED! DOUBLE JUMP UNLOCKED!", 3.5f);
        }

        Destroy(gameObject);
    }
}
