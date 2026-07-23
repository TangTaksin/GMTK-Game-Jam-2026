using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Restart Settings")]
    [SerializeField] private KeyCode restartKey = KeyCode.R;
    [SerializeField] private bool allowRestartAnytime = false;

    [Header("Game Over UI (Optional)")]
    [SerializeField] private GameObject gameOverUI;

    public bool IsGameOver { get; private set; }

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
    /// Call this when the player dies / explodes.
    /// </summary>
    public void TriggerGameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        Debug.Log("<color=red>[GameManager] Game Over! Press R to Restart.</color>");

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
