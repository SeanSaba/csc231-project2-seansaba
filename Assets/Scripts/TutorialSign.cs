using UnityEngine;

public class TutorialSign : MonoBehaviour
{
    [SerializeField] private string title = "MOVEMENT";
    [SerializeField] private string instructionText = "Use [A / D] or Arrow Keys to move, [SPACE] to jump.";
    [SerializeField] private float triggerRadius = 4.5f;

    private Transform playerTransform;
    private bool isShowing;

    private void Update()
    {
        if (playerTransform == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= triggerRadius)
        {
            if (!isShowing)
            {
                isShowing = true;
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetTutorialPrompt(this, title, instructionText);
            }
        }
        else if (isShowing)
        {
            isShowing = false;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClearTutorialPrompt(this);
            }
        }
    }
}
