using UnityEngine;

/// <summary>
/// Special Orange Gem placed before the Final Boss arena:
/// - Unlocks Kip's Double Jump ability.
/// - Plays the EXACT same pickup animation & sound as every other gem.
/// - Awakens and summons the Final Boss (who stays hidden and inactive until this gem is taken!).
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.MarkOrangeGemCollected();
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateOrangeGemUI(1, 1);
            UIManager.Instance.SetDoubleJumpUnlockedUI(true, false);
        }

        // ONLY when the Orange Gem is taken does the Final Boss appear and initiate his attack!
        FinalBoss boss = FindAnyObjectByType<FinalBoss>();
        if (boss != null)
        {
            boss.SummonFromOrangeGem();
        }

        // Unified pickup animation & sound identical to all Blue Aether Crystals!
        StartCoroutine(Collectible.PlayUnifiedGemPickupAnimation(gameObject));
    }
}
