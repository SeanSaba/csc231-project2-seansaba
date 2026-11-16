using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Vector2 parallaxFactor = new Vector2(0.55f, 0.15f);

    private Transform camTransform;
    private Vector3 startPos;
    private Vector3 startCamPos;
    private bool initialized;

    private void Start()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        camTransform = cam.transform;
        startPos = transform.position;
        startCamPos = camTransform.position;
        initialized = true;
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            InitializeIfNeeded();
            if (!initialized) return;
        }

        Vector3 camDelta = camTransform.position - startCamPos;
        transform.position = new Vector3(
            startPos.x + camDelta.x * parallaxFactor.x,
            startPos.y + camDelta.y * parallaxFactor.y,
            startPos.z
        );
    }
}
