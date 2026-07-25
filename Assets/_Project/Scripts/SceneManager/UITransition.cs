using UnityEngine;
using DG.Tweening;

public class UITransition : MonoBehaviour
{
    [SerializeField] private float transitionTime = 0.5f; // The duration of the UI transition.
    [SerializeField] private RectTransform rectTransform; // The RectTransform of the UI element.
    [SerializeField] private Ease easeType = Ease.OutBack;

    private Vector2 startPosition;

    private void Awake()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if (rectTransform != null)
        {
            startPosition = rectTransform.anchoredPosition;
        }
    }

    private void OnDestroy()
    {
        if (rectTransform != null)
        {
            rectTransform.DOKill();
        }
    }

    public void ShowUI()
    {
        if (rectTransform == null) return;

        rectTransform.DOKill();
        rectTransform.anchoredPosition = startPosition;
        rectTransform.DOAnchorPos(Vector2.zero, transitionTime)
            .SetEase(easeType)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
