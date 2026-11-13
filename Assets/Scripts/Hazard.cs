using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool isInstantKill = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
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
