using UnityEngine;
using TMPro;
using DG.Tweening;

namespace UI
{
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Display Format")]
        [SerializeField] private string format = "<b>{0}m</b>  <size=85%><color=#FFCC00>SCORE: {1}</color>  <color=#888888>BEST: {2}</color></size>";

        [Header("Fade Settings")]
        [SerializeField] private float fadeInDuration = 0.35f;

        private CanvasGroup canvasGroup;
        private Tween fadeTween;

        private void Awake()
        {
            if (scoreText == null) scoreText = GetComponent<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            ScoreManager.OnScoreUpdated += UpdateScoreText;
            ScoreManager.OnDistance999Reached += HandleDistance999Reached;
            GameManager.OnGameStart += HandleGameStart;
            GameManager.OnGameOver += HideScoreText;
            CameraFollow.OnCameraPanComplete += HandleCameraPanComplete;
        }

        private void OnDisable()
        {
            ScoreManager.OnScoreUpdated -= UpdateScoreText;
            ScoreManager.OnDistance999Reached -= HandleDistance999Reached;
            GameManager.OnGameStart -= HandleGameStart;
            GameManager.OnGameOver -= HideScoreText;
            CameraFollow.OnCameraPanComplete -= HandleCameraPanComplete;
            fadeTween?.Kill();
        }

        private void HandleDistance999Reached()
        {
            if (scoreText != null)
            {
                scoreText.transform.DOKill();
                scoreText.transform.localScale = Vector3.one;
                scoreText.transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0f), 0.6f, 8, 0.5f).SetUpdate(true);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
            {
                HideScoreTextInstant();
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

        private void HandleGameStart()
        {
            // If camera is panning down, keep ScoreUI hidden until pan completes
            if (CameraFollow.Instance != null && CameraFollow.Instance.IsPanningDown)
            {
                HideScoreTextInstant();
            }
            else
            {
                ShowScoreText();
            }
        }

        private void HandleCameraPanComplete()
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameStarted && !GameManager.Instance.IsGameOver)
            {
                ShowScoreText();
            }
        }

        private void ShowScoreText()
        {
            fadeTween?.Kill();

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                if (fadeInDuration > 0f)
                {
                    fadeTween = canvasGroup.DOFade(1f, fadeInDuration).SetUpdate(true);
                }
                else
                {
                    canvasGroup.alpha = 1f;
                }
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

        private void HideScoreTextInstant()
        {
            fadeTween?.Kill();

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

        private void HideScoreText()
        {
            fadeTween?.Kill();

            if (canvasGroup != null)
            {
                if (fadeInDuration > 0f)
                {
                    fadeTween = canvasGroup.DOFade(0f, fadeInDuration)
                        .SetUpdate(true)
                        .OnComplete(() => canvasGroup.blocksRaycasts = false);
                }
                else
                {
                    canvasGroup.alpha = 0f;
                    canvasGroup.blocksRaycasts = false;
                }
            }
            else if (scoreText != null)
            {
                scoreText.gameObject.SetActive(false);
            }
        }
    }
}
