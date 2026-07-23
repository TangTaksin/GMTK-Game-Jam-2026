using UnityEngine;
using TMPro;

/// <summary>
/// Fetches the Game Version from Project Settings (Application.version) and displays it via TextMeshPro.
/// </summary>
public class GameVersionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI uiText;

    [Header("Display Format")]
    [Tooltip("Use {0} as placeholder for version string. E.g. 'v{0}' -> 'v1.0.0' or 'Version {0}'")]
    [SerializeField] private string versionFormat = "v{0}";

    private void Awake()
    {
        // Auto-get component if not assigned
        if (uiText == null) uiText = GetComponent<TextMeshProUGUI>();
        UpdateVersionDisplay();
    }

    private void Start()
    {
        UpdateVersionDisplay();
    }

    public void UpdateVersionDisplay()
    {
        string currentVersion = Application.version;
        string displayText = string.Format(versionFormat, currentVersion);

        if (uiText != null)
        {
            uiText.text = displayText;
        }
    }
}
