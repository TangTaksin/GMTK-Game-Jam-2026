using UnityEngine;
using TMPro;

/// <summary>
/// Displays controls tutorial text using TextMeshPro (World Space or Canvas UI).
/// Hides during menu and shows when game starts.
/// </summary>
public class ControlsTutorialUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshPro worldText;

    [Header("Settings")]
    [SerializeField] private bool fadeOutOnMove = true;
    [SerializeField] private float fadeDelay = 5f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Tutorial Text Format")]
    [TextArea(6, 12)]
    [SerializeField] private string tutorialContent =
        "<b><color=#FFD700>[ CONTROLS & BOMB ]</color></b>\n" +
        "• <b>D</b> / <b><color=#00FFFF>→</color></b> / <b>Space</b> : Move & Hold Bomb (Timer Ticks!)\n" +
        "• <b>Stop Moving & Rest (3s)</b> : Auto-Drop Bomb & Reset Timer!\n" +
        "  <i>(Stop on flat ground to charge timer before it explodes)</i>";

    private CanvasGroup canvasGroup;
    private bool playerHasMoved;
    private float timer;

    private void Awake()
    {
        // Try getting attached TextMeshPro components if not assigned
        if (worldText == null) worldText = GetComponent<TextMeshPro>();

        canvasGroup = GetComponent<CanvasGroup>();

        SetText(tutorialContent);
    }

    private void OnEnable()
    {
        GameManager.OnGameStart += ShowTutorialUI;
        GameManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= ShowTutorialUI;
        GameManager.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        HideTutorialUI();
    }

    private void Start()
    {
        SetText(tutorialContent);

        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
        {
            HideTutorialUI();
        }
        else
        {
            ShowTutorialUI();
        }
    }

    public void ShowTutorialUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        else if (worldText != null)
        {
            Color c = worldText.color;
            c.a = 1f;
            worldText.color = c;
            worldText.gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    public void HideTutorialUI()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        else if (worldText != null)
        {
            Color c = worldText.color;
            c.a = 0f;
            worldText.color = c;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void SetText(string text)
    {
        if (worldText != null) worldText.text = text;
    }

    private void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.IsGameStarted || GameManager.Instance.IsGameOver))
        {
            HideTutorialUI();
            return;
        }

        if (!fadeOutOnMove) return;

        // Detect player movement input
        if (!playerHasMoved)
        {
            if (Input.GetAxisRaw("Horizontal") > 0.1f || Input.GetButtonDown("Jump"))
            {
                playerHasMoved = true;
            }
        }

        // Handle fade out after delay
        if (playerHasMoved)
        {
            timer += Time.deltaTime;
            if (timer >= fadeDelay)
            {
                FadeOut();
            }
        }
    }

    private void FadeOut()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            if (canvasGroup.alpha <= 0f)
            {
                gameObject.SetActive(false);
            }
        }
        else if (worldText != null)
        {
            Color c = worldText.color;
            c.a = Mathf.MoveTowards(c.a, 0f, fadeSpeed * Time.deltaTime);
            worldText.color = c;
            if (c.a <= 0f) gameObject.SetActive(false);
        }
    }
}
