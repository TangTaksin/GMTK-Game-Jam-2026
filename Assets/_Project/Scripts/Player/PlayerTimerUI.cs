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

    private int lastTime = -1;
    private Vector3 initialScale = Vector3.one;

    private void Awake()
    {
        if (timerText != null)
        {
            initialScale = timerText.transform.localScale;
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
        if (playerTimer == null || timerText == null) return;

        // Follow player position & lock rotation (prevent spinning with player)
        timerText.transform.position = playerTimer.transform.position + offset;
        timerText.transform.rotation = Quaternion.identity;

        // Format time display
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
