using UnityEngine;
using TMPro;

namespace UI
{
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Display Format")]
        [SerializeField] private string format = "<b>{0}m</b>  <size=85%><color=#FFCC00>SCORE: {1}</color>  <color=#888888>BEST: {2}</color></size>";

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (scoreText == null) scoreText = GetComponent<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            ScoreManager.OnScoreUpdated += UpdateScoreText;
            GameManager.OnGameStart += ShowScoreText;
            GameManager.OnGameOver += HideScoreText;
        }

        private void OnDisable()
        {
            ScoreManager.OnScoreUpdated -= UpdateScoreText;
            GameManager.OnGameStart -= ShowScoreText;
            GameManager.OnGameOver -= HideScoreText;
        }

        private void Start()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
            {
                HideScoreText();
            }
            else
            {
                ShowScoreText();
            }
        }

        private void UpdateScoreText(int distance, int score, int highScore)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(format, distance, score, highScore);
            }
        }

        private void ShowScoreText()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            else if (scoreText != null)
            {
                scoreText.gameObject.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        private void HideScoreText()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
            else if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }
        }
    }
}
