using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
    [SerializeField] private float smoothSpeed = 6f;
    [SerializeField] private Vector2 minBounds = new Vector2(-10f, -2f);
    [SerializeField] private Vector2 maxBounds = new Vector2(150f, 20f);

    private float shakeDuration;
    private float shakeMagnitude;
    private Vector3 shakeOffset;

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
            if (player != null) target = player.transform;
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;
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
