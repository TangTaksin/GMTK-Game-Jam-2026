using UnityEngine;
using TMPro;

/// <summary>
/// Displays controls tutorial text using TextMeshPro (World Space or Canvas UI).
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
        "<b><color=#FFD700>[ CONTROLS ]</color></b>\n" +
        "• <b>D</b> or <b><color=#00FFFF>→</color></b> : Move Right\n" +
        "• <b>Spacebar</b> : Jump / Double Jump\n" +
        "• <b>Release Keys on Flat Ground</b> : Stop & Reset Timer!\n" +
        "  <i>(Come to a complete stop before you explode)</i>";

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

    private void Start()
    {
        SetText(tutorialContent);
    }

    public void SetText(string text)
    {
        if (worldText != null) worldText.text = text;
    }

    private void Update()
    {
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
