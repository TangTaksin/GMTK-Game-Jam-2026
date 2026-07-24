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

        private void OnEnable()
        {
            ScoreManager.OnScoreUpdated += UpdateScoreText;
            GameManager.OnGameOver += HideScoreText;
        }

        private void OnDisable()
        {
            ScoreManager.OnScoreUpdated -= UpdateScoreText;
            GameManager.OnGameOver -= HideScoreText;
        }

        private void UpdateScoreText(int distance, int score, int highScore)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(format, distance, score, highScore);
            }
        }

        private void HideScoreText()
        {
            if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }
        }
    }
}


