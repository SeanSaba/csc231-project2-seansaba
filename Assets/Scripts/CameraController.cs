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
    [SerializeField] private Vector2 maxBounds = new Vector2(241.5f, 22f);

    private float shakeDuration;
    private float shakeMaxDuration;
    private float shakeMagnitude;
    private float shakeRollDegrees;
    private bool shakeHeavyDrop;
    private Vector3 shakeOffset;
    private Vector3 baseFollowPos;
    private bool hasInitializedPos;
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
        baseFollowPos = transform.position;
        hasInitializedPos = true;
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

        if (!hasInitializedPos)
        {
            baseFollowPos = transform.position;
            hasInitializedPos = true;
        }

        float desiredLookAhead = 0f;
        if (targetRb != null && Mathf.Abs(targetRb.linearVelocity.x) > 0.4f)
        {
            desiredLookAhead = Mathf.Sign(targetRb.linearVelocity.x) * lookAheadDistance;
        }
        float lookT = 1f - Mathf.Exp(-lookAheadSpeed * Time.deltaTime);
        currentLookAhead = Mathf.Lerp(currentLookAhead, desiredLookAhead, lookT);

        Vector3 desiredPosition = target.position + offset + new Vector3(currentLookAhead, 0f, 0f);
        desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
        desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        desiredPosition.z = offset.z;

        float followT = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        baseFollowPos = Vector3.Lerp(baseFollowPos, desiredPosition, followT);

        float currentRoll = 0f;
        if (shakeDuration > 0f)
        {
            float decay = (shakeMaxDuration > 0.001f) ? Mathf.Clamp01(shakeDuration / shakeMaxDuration) : 1f;
            float mag = shakeMagnitude * decay;
            Vector2 rnd = Random.insideUnitCircle * mag;
            if (shakeHeavyDrop && decay > 0.65f)
            {
                // Heavy downward seismic floor-slam jolt on the initial frame of a boss footstep!
                rnd.y -= mag * 0.75f;
            }
            shakeOffset = new Vector3(rnd.x, rnd.y, 0f);
            currentRoll = Random.Range(-shakeRollDegrees, shakeRollDegrees) * decay;
            shakeDuration -= Time.deltaTime;
        }
        else
        {
            shakeOffset = Vector3.zero;
            shakeRollDegrees = 0f;
            shakeHeavyDrop = false;
        }

        // Apply shake offset AFTER smoothing so seismic shakes feel punchy and immediate!
        transform.position = baseFollowPos + shakeOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, currentRoll);
    }

    public void Shake(float duration, float magnitude)
    {
        if (magnitude >= shakeMagnitude || shakeDuration <= 0.02f)
        {
            shakeDuration = duration;
            shakeMaxDuration = Mathf.Max(0.01f, duration);
            shakeMagnitude = magnitude;
            shakeRollDegrees = magnitude * 1.4f;
            shakeHeavyDrop = false;
        }
    }

    public void ShakeSeismic(float duration, float magnitude, float rollDegrees = 0.85f)
    {
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeMaxDuration = Mathf.Max(0.01f, shakeDuration);
        shakeMagnitude = Mathf.Max(shakeMagnitude, magnitude);
        shakeRollDegrees = Mathf.Max(shakeRollDegrees, rollDegrees);
        shakeHeavyDrop = true;
    }

    public void StopShake()
    {
        shakeDuration = 0f;
        shakeMaxDuration = 0f;
        shakeMagnitude = 0f;
        shakeRollDegrees = 0f;
        shakeHeavyDrop = false;
        shakeOffset = Vector3.zero;
        transform.rotation = Quaternion.identity;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
