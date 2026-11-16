using UnityEngine;

public class MovingTrap : MonoBehaviour
{
    [Header("Motion Settings")]
    [SerializeField] private Vector3 moveOffset = new Vector3(4f, 0f, 0f);
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float phaseOffset = 0f;
    [SerializeField] private float rotationSpeed = 420f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;

    private Vector3 startPos;

    private void Awake()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        if (moveOffset.sqrMagnitude > 0.001f)
        {
            float t = 0.5f * (1f - Mathf.Cos((Time.time * moveSpeed) + phaseOffset));
            transform.position = startPos + moveOffset * t;
        }

        if (Mathf.Abs(rotationSpeed) > 0.1f)
        {
            transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            var health = collision.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position);
            }
        }
    }
}
