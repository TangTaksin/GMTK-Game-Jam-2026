using UnityEngine;
using TMPro;

public class PlayerTimerUI : MonoBehaviour
{
    [SerializeField] private PlayerTimer playerTimer;
    [SerializeField] private TextMeshPro timerText; // World space TextMeshPro
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, 0f);

    private void Update()
    {
        if (playerTimer == null || timerText == null) return;

        // Follow player position & lock rotation (prevent spinning with player)
        timerText.transform.position = playerTimer.transform.position + offset;
        timerText.transform.rotation = Quaternion.identity;

        // Format time display
        int time = playerTimer.CurrentTimeDisplay;
        timerText.text = time.ToString(); // e.g. "5", "4"

        // Color warning: red when low time
        timerText.color = (time <= 1) ? Color.red : Color.black;
    }
}
