using UnityEngine;

/// <summary>
/// Clean, lightweight multi-frame sprite animator that works with both Simple and Tiled
/// SpriteRenderers (ideal for animated water/acid surfaces, enemies, portals, and props).
/// </summary>
public class SpriteFrameAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameRate = 8f;
    [SerializeField] private bool randomizeStartFrame = false;

    [Header("Optional Ambient Motion")]
    [SerializeField] private bool gentleSway = false;
    [SerializeField] private float swayAngle = 3.5f;
    [SerializeField] private float swaySpeed = 2.2f;
    [SerializeField] private bool gentlePulse = false;
    [SerializeField] private float pulseAmount = 0.06f;
    [SerializeField] private float pulseSpeed = 2.8f;

    private float timer;
    private int currentFrame;
    private float phaseOffset;
    private Vector3 baseScale;

    private void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);

        if (frames != null && frames.Length > 0)
        {
            currentFrame = randomizeStartFrame ? Random.Range(0, frames.Length) : 0;
            if (targetRenderer != null && frames[currentFrame] != null)
            {
                targetRenderer.sprite = frames[currentFrame];
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused) return;

        if (frames != null && frames.Length > 1 && frameRate > 0f && targetRenderer != null)
        {
            timer += Time.deltaTime;
            float frameDuration = 1f / frameRate;
            if (timer >= frameDuration)
            {
                timer -= frameDuration;
                currentFrame = (currentFrame + 1) % frames.Length;
                if (frames[currentFrame] != null)
                {
                    targetRenderer.sprite = frames[currentFrame];
                }
            }
        }

        if (gentleSway)
        {
            float z = Mathf.Sin(Time.time * swaySpeed + phaseOffset) * swayAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, z);
        }

        if (gentlePulse)
        {
            float s = 1f + Mathf.Sin(Time.time * pulseSpeed + phaseOffset) * pulseAmount;
            transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
        }
    }
}
