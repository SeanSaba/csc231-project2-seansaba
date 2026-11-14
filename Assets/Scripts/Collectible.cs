using UnityEngine;

public class Collectible : MonoBehaviour
{
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private int scoreValue = 1;

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

        if (collision.CompareTag("Player"))
        {
            isCollected = true;

            if (ParticleFXManager.Instance != null)
            {
                ParticleFXManager.Instance.PlaySparklePickup(transform.position);
            }
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCollectSound();
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCollectible(scoreValue);
            }

            Destroy(gameObject);
        }
    }
}
