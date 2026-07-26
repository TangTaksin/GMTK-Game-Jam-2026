using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Target & References")]
    [SerializeField] private Transform playerTransform;

    [Header("Score Settings")]
    [SerializeField] private float scorePerMeter = 10f;
    [SerializeField] private float speedMultiplierThreshold = 5f;
    [SerializeField] private float speedBonusRate = 15f; // Bonus points per second while sprinting

    public int CurrentDistance { get; private set; }
    public int CurrentScore { get; private set; }
    public int HighScore { get; private set; }
    public bool IsNewHighScore { get; private set; }

    private float startX;
    private float maxDistanceReached;
    private float accumulatedSpeedBonus;
    private Rigidbody2D playerRb;

    public static event System.Action<int, int, int> OnScoreUpdated; // distance, score, highScore
    public static event System.Action OnDistance999Reached; // Event fired once when player reaches > 999 meters

    private bool hasTriggered999Event = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadHighScore();
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        if (playerTransform != null)
        {
            startX = playerTransform.position.x;
            playerRb = playerTransform.GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;
        if (playerTransform == null) return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // DEBUG KEY: Press [T] or [F9] to instantly jump to 990m for testing (Editor & Development Build only)
        if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.F9))
        {
            TeleportToDistance990();
        }
#endif

        float currentX = playerTransform.position.x - startX;
        if (currentX > maxDistanceReached)
        {
            maxDistanceReached = currentX;
        }

        CurrentDistance = Mathf.Max(0, Mathf.FloorToInt(maxDistanceReached));

        // Trigger Event when player reaches > 999 meters
        if (!hasTriggered999Event && CurrentDistance >= 999)
        {
            hasTriggered999Event = true;
            Debug.Log("<color=gold>★ MILESTONE 999 METERS REACHED! ★</color>");
            OnDistance999Reached?.Invoke();

            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.ShakeCamera(0.5f, 0.6f);
            }
        }

        // Accumulate speed bonus while sprinting
        if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.x) > speedMultiplierThreshold)
        {
            accumulatedSpeedBonus += speedBonusRate * Time.deltaTime;
        }

        int targetScore = Mathf.FloorToInt(maxDistanceReached * scorePerMeter + accumulatedSpeedBonus);
        
        // Ensure score ONLY goes up, never drops when stopping
        if (targetScore > CurrentScore)
        {
            CurrentScore = targetScore;
        }

        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            IsNewHighScore = true;
            SaveHighScore();
        }

        OnScoreUpdated?.Invoke(CurrentDistance, CurrentScore, HighScore);
    }

    private void LoadHighScore()
    {
        HighScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void SaveHighScore()
    {
        PlayerPrefs.SetInt("HighScore", HighScore);
        PlayerPrefs.Save();
    }

    public void AddBonusScore(int points)
    {
        CurrentScore += points;
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            IsNewHighScore = true;
            SaveHighScore();
        }
        OnScoreUpdated?.Invoke(CurrentDistance, CurrentScore, HighScore);
    }

    /// <summary>
    /// Debug helper method: Teleports player and threat instantly to 990m.
    /// </summary>
    public void TeleportToDistance990()
    {
        if (playerTransform == null) return;

        float targetX = startX + 990f;
        float groundY = playerTransform.position.y;
        if (DynamicTerrainGenerator.Instance != null)
        {
            groundY = DynamicTerrainGenerator.Instance.CalculateHeightAt(targetX) + 0.5f;
        }

        playerTransform.position = new Vector3(targetX, groundY, playerTransform.position.z);

        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }

        maxDistanceReached = 990f;
        CurrentDistance = 990;

        Debug.Log("<color=yellow>[DEBUG] Teleported Player to 990 meters!</color>");
    }
}

