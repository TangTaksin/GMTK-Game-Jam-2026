using UnityEngine;
using TMPro;
using DG.Tweening;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Animation Settings")]
    [SerializeField] private float animDuration = 0.4f;
    [SerializeField] private Ease easeType = Ease.OutBack;

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

    private void OnDestroy()
    {
        if (container != null)
        {
            container.transform.DOKill();
        }
    }

    private void ShowGameOverUI()
    {
        bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;

        // 🔊 เล่นเสียง GameOver หรือ Win เมื่อขึ้น Panel ผลลัพธ์
        if (AudioManager.Instance != null)
        {
            if (isVictory)
            {
                AudioManager.Instance.PlaySFX("Win");
            }
            else
            {
                AudioManager.Instance.PlaySFX("GameOver");
            }
        }

        if (gameOverText != null)
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            int distance = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentDistance : 0;
            int highScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;
            bool isNewHigh = ScoreManager.Instance != null && ScoreManager.Instance.IsNewHighScore;

            string headerTitle = isVictory
                ? "<b><color=#FFD700>PENGUIN KABOOM! </color></b>\n<size=85%><color=#00FFCC><b>CONGRATULATIONS!</b></color></size>"
                : "<b><color=#FF3333>NOOT NOOT...! </color></b>\n<size=75%><color=#FFAA00>Game Over</color></size>";

            string highMsg = isNewHigh ? "<color=#00FFCC><b>NEW HIGH SCORE!</b></color>\n" : "";

            gameOverText.text =
                $"{headerTitle}\n\n" +
                highMsg +
                $"<size=80%>Distance: <b>{distance}m</b>\n" +
                $"Final Score: <b><color=#FFCC00>{score}</color></b>\n" +
                $"Best Score: <b>{highScore}</b></size>\n\n" +
                "<size=70%><color=#FFFFFF>Press <b>[R]</b> to Play Again</color></size>";
        }

        if (container != null)
        {
            container.transform.DOKill();
            container.transform.localScale = Vector3.zero;
            container.SetActive(true);

            container.transform.DOScale(Vector3.one, animDuration)
                .SetEase(easeType)
                .SetUpdate(true)
                .SetLink(container);
        }
    }
}
