using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Text Format")]
    [TextArea(3, 6)]
    [SerializeField] private string textFormat =
        "<b><color=#FF3333>GAME OVER!</color></b>\n" +
        "<size=70%><color=#FFFFFF>Press <b>[R]</b> to Restart</color></size>";

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
            gameOverText.text = textFormat;
        }

        if (container != null)
        {
            container.SetActive(true);
        }
    }
}
