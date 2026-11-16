using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 moveOffset = new Vector3(5f, 0f, 0f);
    [SerializeField] private float speed = 1.6f;
    [SerializeField] private float phaseOffset = 0f;

    private Vector3 startPos;
    private Vector3 lastPos;
    private Transform carriedPlayer;

    private void Awake()
    {
        startPos = transform.position;
        lastPos = startPos;
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        float t = 0.5f * (1f - Mathf.Cos((Time.time * speed) + phaseOffset));
        Vector3 nextPos = startPos + moveOffset * t;
        Vector3 delta = nextPos - lastPos;

        transform.position = nextPos;
        if (carriedPlayer != null)
        {
            carriedPlayer.position += delta;
        }

        lastPos = nextPos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
            {
                carriedPlayer = collision.transform;
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (carriedPlayer == null && collision.collider.CompareTag("Player"))
        {
            if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
            {
                carriedPlayer = collision.transform;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player") && carriedPlayer == collision.transform)
        {
            carriedPlayer = null;
        }
    }
}
