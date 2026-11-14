using UnityEngine;

public class GoalZone : MonoBehaviour
{
    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasTriggered) return;

        if (collision.CompareTag("Player"))
        {
            hasTriggered = true;

            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.PlayVictoryBurst(transform.position);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayWinSound();
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CompleteLevel();
            }
        }
    }
}
