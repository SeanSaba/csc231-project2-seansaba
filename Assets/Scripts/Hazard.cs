using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool isInstantKill = false;
    [SerializeField] private bool isLiquidHazard = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (isLiquidHazard && ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.PlayAcidSplash(collision.transform.position);
            }

            var health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                if (isInstantKill)
                {
                    health.InstantKill();
                }
                else
                {
                    health.TakeDamage(damage, transform.position);
                }
            }
        }
    }
}
