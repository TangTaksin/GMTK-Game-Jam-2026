using UnityEngine;
using TMPro;
using DG.Tweening;

public class PlayerTimerUI : MonoBehaviour
{
    [SerializeField] private PlayerTimer playerTimer;
    [SerializeField] private TextMeshPro timerText; // World space TextMeshPro
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);
    [SerializeField] private Color normalColor = Color.black;
    [SerializeField] private Color warningColor = Color.red;
    [SerializeField] private Color resetColor = Color.green;

    private int lastTime = -1;
    private Vector3 initialScale = Vector3.one;

    private void Awake()
    {
        if (timerText != null)
        {
            initialScale = timerText.transform.localScale;
        }

        if (playerTimer == null)
        {
            playerTimer = FindAnyObjectByType<PlayerTimer>();
        }
    }

    private void OnDestroy()
    {
        if (timerText != null)
        {
            timerText.transform.DOKill();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            if (timerText != null && timerText.gameObject.activeSelf)
            {
                timerText.gameObject.SetActive(false);
            }
            return;
        }

        if (playerTimer == null || timerText == null)
        {
            playerTimer = FindAnyObjectByType<PlayerTimer>();
            if (playerTimer == null) return;
        }

        // Follow bomb position & lock rotation
        timerText.transform.position = playerTimer.transform.position + offset;
        timerText.transform.rotation = Quaternion.identity;

        if (playerTimer.IsDefusing)
        {
            // Reset charging state on ground while defusing
            float remainingRest = Mathf.Max(0f, playerTimer.ResetGroundDuration - playerTimer.GroundRestTimer);
            if (remainingRest > 0f)
            {
                timerText.color = resetColor;
                timerText.text = $"{remainingRest:F1}";
            }
            else
            {
                timerText.color = resetColor;
                timerText.text = "R!";
            }
            lastTime = -1;
        }
        else
        {
            // Normal countdown display while carrying / in air
            int time = playerTimer.CurrentTimeDisplay;

            if (time != lastTime)
            {
                lastTime = time;
                timerText.text = time.ToString();

                // Color warning and pulse animation on countdown
                if (time <= 3 && time > 0)
                {
                    timerText.color = warningColor;

                    // Pulse scale animation on tick
                    timerText.transform.DOKill();
                    timerText.transform.localScale = initialScale;
                    timerText.transform.DOPunchScale(Vector3.one * 0.4f, 0.25f, 6, 0.5f).SetLink(timerText.gameObject);
                }
                else
                {
                    timerText.color = normalColor;
                }
            }
        }
    }
}
