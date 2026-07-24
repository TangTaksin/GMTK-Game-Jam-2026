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

        float currentX = playerTransform.position.x - startX;
        if (currentX > maxDistanceReached)
        {
            maxDistanceReached = currentX;
        }

        CurrentDistance = Mathf.Max(0, Mathf.FloorToInt(maxDistanceReached));

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
}

