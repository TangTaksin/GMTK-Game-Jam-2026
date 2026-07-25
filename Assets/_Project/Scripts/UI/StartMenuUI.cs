using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controls the in-game Start Menu UI overlay.
/// Displays the game logo and "CLICK TO START" prompt.
/// Hides the logo & UI and starts the player/threat intro animation when clicked anywhere on screen.
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("Canvas & Component References")]
    [Tooltip("Main CanvasGroup for smooth fade-out animation.")]
    [SerializeField] private CanvasGroup mainCanvasGroup;
    [Tooltip("Transform of the Game Logo or Title panel.")]
    [SerializeField] private RectTransform logoTransform;
    [Tooltip("TextMeshPro text for 'Click to Start' prompt.")]
    [SerializeField] private TextMeshProUGUI clickToStartText;

    [Header("Text Settings")]
    [SerializeField] private string defaultPromptText = "CLICK TO START";

    [Header("Animation Settings")]
    [Tooltip("Duration of fade-out transition when starting the game.")]
    [SerializeField] private float fadeOutDuration = 0.45f;
    [Tooltip("Enable smooth pulsing alpha/scale on 'Click to Start' text.")]
    [SerializeField] private bool enablePulsingText = true;
    [SerializeField] private float pulseSpeed = 3.2f;
    [SerializeField] private float pulseMinAlpha = 0.3f;
    [SerializeField] private float pulseMaxAlpha = 1.0f;
    [SerializeField] private float pulseMinScale = 0.95f;
    [SerializeField] private float pulseMaxScale = 1.05f;

    [Header("Floating Logo Settings")]
    [Tooltip("Enable subtle floating animation for logo.")]
    [SerializeField] private bool enableFloatingLogo = true;
    [SerializeField] private float floatSpeed = 1.8f;
    [SerializeField] private float floatAmount = 10.0f;

    private bool isStarting = false;
    private Vector3 initialLogoPos;
    private Vector3 initialTextScale = Vector3.one;
    private CanvasGroup clickTextCanvasGroup;

    private void Awake()
    {
        // Auto-assign CanvasGroup
        if (mainCanvasGroup == null) mainCanvasGroup = GetComponent<CanvasGroup>();
        if (mainCanvasGroup == null) mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Auto-assign logo transform if unassigned
        if (logoTransform == null)
        {
            Transform logoChild = transform.Find("Logo") ?? transform.Find("GameLogo") ?? transform.Find("Title");
            if (logoChild != null) logoTransform = logoChild.GetComponent<RectTransform>();
        }

        if (logoTransform != null)
        {
            initialLogoPos = logoTransform.anchoredPosition;
        }

        // Auto-assign click to start text
        if (clickToStartText == null)
        {
            clickToStartText = GetComponentInChildren<TextMeshProUGUI>();
        }

        if (clickToStartText != null)
        {
            initialTextScale = clickToStartText.transform.localScale;
            if (clickToStartText.text == "Button" || string.IsNullOrWhiteSpace(clickToStartText.text))
            {
                clickToStartText.text = defaultPromptText;
            }

            clickTextCanvasGroup = clickToStartText.GetComponent<CanvasGroup>();
            if (clickTextCanvasGroup == null)
            {
                clickTextCanvasGroup = clickToStartText.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void Start()
    {
        // If game is already marked as started, hide menu immediately
        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted)
        {
            gameObject.SetActive(false);
            return;
        }

        mainCanvasGroup.alpha = 1f;
        mainCanvasGroup.blocksRaycasts = true;
        mainCanvasGroup.interactable = true;
        isStarting = false;

        // Ensure text says CLICK TO START
        if (clickToStartText != null && (clickToStartText.text == "Button" || string.IsNullOrWhiteSpace(clickToStartText.text)))
        {
            clickToStartText.text = defaultPromptText;
        }
    }

    private void Update()
    {
        if (isStarting) return;

        // If game is started elsewhere, initiate hide sequence
        if (GameManager.Instance != null && GameManager.Instance.IsGameStarted)
        {
            StartGameSequence();
            return;
        }

        // Animate Click to Start pulse (alpha + scale)
        if (enablePulsingText)
        {
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1

            if (clickTextCanvasGroup != null)
            {
                clickTextCanvasGroup.alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, wave);
            }

            if (clickToStartText != null)
            {
                float scaleFactor = Mathf.Lerp(pulseMinScale, pulseMaxScale, wave);
                clickToStartText.transform.localScale = initialTextScale * scaleFactor;
            }
        }

        // Animate floating logo
        if (enableFloatingLogo && logoTransform != null)
        {
            float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            logoTransform.anchoredPosition = initialLogoPos + new Vector3(0f, yOffset, 0f);
        }

        // Detect click anywhere on screen or touch or space/enter key
        if (Input.GetMouseButtonDown(0) || 
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || 
            Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return))
        {
            StartGameSequence();
        }
    }

    /// <summary>
    /// Triggered when the player clicks anywhere to start the game.
    /// Fades out the logo & menu UI and starts the game intro sequence.
    /// </summary>
    public void StartGameSequence()
    {
        if (isStarting) return;
        isStarting = true;

        // Quick punch scale effect on prompt text before fade
        if (clickToStartText != null)
        {
            clickToStartText.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.2f, 1, 0.5f);
        }

        // Trigger Game Start event on GameManager
        if (GameManager.Instance != null && !GameManager.Instance.IsGameStarted)
        {
            GameManager.Instance.StartGame();
        }

        // Fade out UI panel cleanly (hides logo and click to start prompt)
        mainCanvasGroup.blocksRaycasts = false;
        mainCanvasGroup.interactable = false;

        mainCanvasGroup.DOFade(0f, fadeOutDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }
}
