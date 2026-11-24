using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static bool SkipTitleOnNextLoad { get; set; } = false;

    [Header("Game State")]
    [SerializeField] private int totalCrystals = 20;
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float autoRespawnDelay = 0.75f;
    [SerializeField] private Vector3 initialSpawnPoint = new Vector3(0f, 1.0f, 0f);

    private int collectedCrystals = 0;
    private int currentLives = 3;
    private float levelTimer = 0f;
    private bool isTimerRunning = false;
    private bool isPaused = false;
    private bool isGameOver = false;
    private bool isRespawning = false;
    private bool isLevelComplete = false;
    private Vector3 currentCheckpointPos;

    public bool IsGamePaused => isPaused;
    public bool IsGameOver => isGameOver || isRespawning;
    public bool IsLevelComplete => isLevelComplete;
    public int CollectedCrystals => collectedCrystals;
    public int TotalCrystals => totalCrystals;
    public int CurrentLives => currentLives;
    public int MaxLives => maxLives;
    public float ElapsedTime => levelTimer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentLives = maxLives;
        currentCheckpointPos = initialSpawnPoint;
    }

    private void Start()
    {
        isTimerRunning = true;
        totalCrystals = FindObjectsByType<Collectible>().Length;
        if (totalCrystals == 0) totalCrystals = 20;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCrystalUI(collectedCrystals, totalCrystals);
            UIManager.Instance.UpdateLivesUI(currentLives, maxLives);
        }
    }

    private void Update()
    {
        bool titleOpen = UIManager.Instance != null && UIManager.Instance.IsTitleScreenVisible;

        if (isTimerRunning && !titleOpen && !isPaused && !isGameOver && !isLevelComplete)
        {
            levelTimer += Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimerUI(levelTimer);
            }
        }

        if (!titleOpen)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame)
                {
                    TogglePause();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
        }
    }

    public void AddCollectible(int amount = 1)
    {
        collectedCrystals += amount;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateCrystalUI(collectedCrystals, totalCrystals);
        }
    }

    public void UpdateCheckpoint(Vector3 position)
    {
        currentCheckpointPos = position;
    }

    public void OnPlayerDied()
    {
        if (isRespawning || isGameOver || isLevelComplete) return;

        currentLives--;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLivesUI(Mathf.Max(0, currentLives), maxLives);
        }

        if (currentLives > 0)
        {
            StartCoroutine(AutoRespawnRoutine());
        }
        else
        {
            // Failed both lives (2 lives x 3 hearts each): automatically reset the entire level (NO checkpoint!)
            StartCoroutine(AutoResetLevelRoutine());
        }
    }

    private IEnumerator AutoResetLevelRoutine()
    {
        isGameOver = true;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToastNotification("ALL LIVES LOST (0/3)! RESETTING LEVEL FROM START...", 1.6f);
        }
        yield return new WaitForSeconds(1.45f);
        RestartLevel();
    }

    private IEnumerator AutoRespawnRoutine()
    {
        isRespawning = true;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowToastNotification($"RESPAWNING AT CHECKPOINT ({currentLives} LIVES LEFT)", 1.8f);
        }

        yield return new WaitForSeconds(autoRespawnDelay);

        isRespawning = false;
        RespawnPlayerAtCheckpoint();
    }

    public void RespawnPlayerAtCheckpoint()
    {
        isGameOver = false;
        isRespawning = false;
        if (currentLives <= 0)
        {
            currentLives = maxLives;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLivesUI(currentLives, maxLives);
            }
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.Respawn(currentCheckpointPos);
            }
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideGameOverScreen();
        }
    }

    public void CompleteLevel()
    {
        isLevelComplete = true;
        isTimerRunning = false;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowWinScreen(levelTimer, collectedCrystals, totalCrystals);
        }
    }

    public void TogglePause()
    {
        if (isGameOver || isLevelComplete) return;
        if (UIManager.Instance != null && UIManager.Instance.IsTitleScreenVisible) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetPauseMenuVisible(isPaused);
        }
    }

    public void RestartLevel()
    {
        SkipTitleOnNextLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToTitle()
    {
        SkipTitleOnNextLoad = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
