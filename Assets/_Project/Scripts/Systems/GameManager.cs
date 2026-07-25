using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Restart Settings")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private bool allowRestartAnytime = false;

    [Header("Start Menu Settings")]
    [SerializeField] private bool useStartMenu = true;

    [Header("Game Over UI (Optional)")]
    [SerializeField] private GameObject gameOverUI;
    [Tooltip("Delay in seconds before showing the Game Over UI screen, allowing explosion & blood particles to play out.")]
    [SerializeField] private float gameOverDelay = 2.0f;

    public bool IsGameOver { get; private set; }
    public bool IsGameStarted { get; private set; }

    public static event System.Action OnGameStart;
    public static event System.Action OnGameOver;
    public static event System.Action OnGameRestart;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!useStartMenu)
        {
            IsGameStarted = true;
        }
    }

    private void Update()
    {
        // Press R to restart when dead (or anytime if enabled)
        if ((IsGameOver || allowRestartAnytime) && Input.GetKeyDown(restartKey))
        {
            RestartLevel();
        }
    }

    /// <summary>
    /// Call this to start the game when user clicks on the start menu.
    /// </summary>
    public void StartGame()
    {
        if (IsGameStarted) return;

        IsGameStarted = true;
        Debug.Log("<color=green>[GameManager] Game Started!</color>");
        OnGameStart?.Invoke();
    }

    /// <summary>
    /// Call this when the player dies / explodes.
    /// </summary>
    public void TriggerGameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log("<color=red>[GameManager] Game Over! Press R to Restart.</color>");

        StartCoroutine(RoutineGameOver());
    }

    private System.Collections.IEnumerator RoutineGameOver()
    {
        // 1. Immediately hide Player character sprite and freeze physics on Frame 0!
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player != null)
            {
                SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in renderers)
                {
                    sr.enabled = false;
                }

                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.simulated = false;
                }
                player.enabled = false;
            }
        }

        PlayerTimer[] bombTimers = FindObjectsByType<PlayerTimer>(FindObjectsSortMode.None);
        foreach (var bomb in bombTimers)
        {
            if (bomb != null)
            {
                bomb.gameObject.SetActive(false);
            }
        }

        // 2. Delay 2 seconds for explosion particle, blood splatter, and camera shake to finish
        if (gameOverDelay > 0f)
        {
            yield return new WaitForSeconds(gameOverDelay);
        }

        // 3. Display Game Over UI screen after 2 seconds
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        OnGameOver?.Invoke();
    }

    /// <summary>
    /// Reloads the currently active scene.
    /// </summary>
    public void RestartLevel()
    {
        OnGameRestart?.Invoke();
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }
}
