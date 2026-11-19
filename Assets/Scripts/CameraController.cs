using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float smoothSpeed = 7f;
    [SerializeField] private float lookAheadDistance = 1.6f;
    [SerializeField] private float lookAheadSpeed = 4.5f;
    [SerializeField] private Vector2 minBounds = new Vector2(-5f, -1f);
    [SerializeField] private Vector2 maxBounds = new Vector2(222f, 22f);

    private float shakeDuration;
    private float shakeMagnitude;
    private Vector3 shakeOffset;
    private float currentLookAhead;
    private Rigidbody2D targetRb;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                targetRb = player.GetComponent<Rigidbody2D>();
            }
            if (target == null) return;
        }
        else if (targetRb == null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
        }

        float desiredLookAhead = 0f;
        if (targetRb != null && Mathf.Abs(targetRb.linearVelocity.x) > 0.4f)
        {
            desiredLookAhead = Mathf.Sign(targetRb.linearVelocity.x) * lookAheadDistance;
        }
        currentLookAhead = Mathf.Lerp(currentLookAhead, desiredLookAhead, lookAheadSpeed * Time.deltaTime);

        Vector3 desiredPosition = target.position + offset + new Vector3(currentLookAhead, 0f, 0f);
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        desiredPosition.z = offset.z;

        if (shakeDuration > 0f)
        {
            shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition + shakeOffset, smoothSpeed * Time.deltaTime);
        transform.position = smoothed;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
