using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI gameOverText;

    private void Awake()
    {
        if (container == null) container = gameObject;
        if (gameOverText == null) gameOverText = GetComponentInChildren<TextMeshProUGUI>();

        // Hide UI by default on start
        if (container != null) container.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.OnGameOver += ShowGameOverUI;
    }

    private void OnDisable()
    {
        GameManager.OnGameOver -= ShowGameOverUI;
    }

    private void ShowGameOverUI()
    {
        if (gameOverText != null)
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            int distance = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentDistance : 0;
            int highScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;
            bool isNewHigh = ScoreManager.Instance != null && ScoreManager.Instance.IsNewHighScore;

            string highMsg = isNewHigh ? "<color=#00FFCC><b>NEW HIGH SCORE!</b></color>\n" : "";

            gameOverText.text =
                "<b><color=#FF3333>GAME OVER!</color></b>\n\n" +
                highMsg +
                $"<size=80%>Distance: <b>{distance}m</b>\n" +
                $"Final Score: <b><color=#FFCC00>{score}</color></b>\n" +
                $"Best Score: <b>{highScore}</b></size>\n\n" +
                "<size=70%><color=#FFFFFF>Press <b>[R]</b> to Restart</color></size>";
        }

        if (container != null)
        {
            container.SetActive(true);
        }
    }
}

