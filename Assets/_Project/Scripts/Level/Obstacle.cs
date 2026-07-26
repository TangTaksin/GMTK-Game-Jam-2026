using UnityEngine;
using DG.Tweening;

public enum ObstacleType
{
    GroundSpike // Low obstacle: Must jump over
}

/// <summary>
/// Cookie Run style ground obstacle component.
/// Collides with player, applies speed slowdown, deducts bomb timer, and triggers visual juice.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    [Header("Obstacle Properties")]
    [SerializeField] private ObstacleType obstacleType = ObstacleType.GroundSpike;
    
    [Tooltip("Seconds deducted from PlayerTimer when hit.")]
    [SerializeField] private float timeDeduction = 1.0f;

    [Tooltip("Duration of player speed slowdown on hit (seconds).")]
    [SerializeField] private float slowDuration = 0.6f;

    [Tooltip("Player speed multiplier during slowdown (0.3 = 30% speed).")]
    [SerializeField] private float slowMultiplier = 0.3f;

    [Header("Juice & Feedback")]
    [SerializeField] private float cameraShakeIntensity = 0.35f;
    [SerializeField] private float cameraShakeDuration = 0.3f;
    [SerializeField] private ParticleSystem hitParticlePrefab;

    [Header("Visual Colors (Fallback procedural sprite)")]
    [SerializeField] private Color groundSpikeColor = new Color(0.9f, 0.25f, 0.2f, 1f); // Reddish ice spike

    [Header("Sorting Settings")]
    [Tooltip("Sorting layer name for obstacle sprite.")]
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Sorting order for obstacle sprite (set lower than ground, e.g. -1, to render behind ground fill).")]
    [SerializeField] private int sortingOrder = -1;

    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private bool hasBeenHit = false;

    public ObstacleType Type => obstacleType;

    public void Initialize(ObstacleType type = ObstacleType.GroundSpike)
    {
        obstacleType = type;
        SetupVisualsAndCollider();
    }

    public void SetSorting(string layerName, int order)
    {
        sortingLayerName = layerName;
        sortingOrder = order;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        SetupVisualsAndCollider();
    }

    private void SetupVisualsAndCollider()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        // If sprite is missing, generate procedural texture icon for Ground Spike
        if (spriteRenderer.sprite == null)
        {
            Texture2D tex = new Texture2D(32, 32);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    // Draw triangular spike pattern pointing UP
                    float halfWidth = (32 - y) * 0.5f;
                    bool isInsideSpike = (x >= 16 - halfWidth && x <= 16 + halfWidth);
                    tex.SetPixel(x, y, isInsideSpike ? groundSpikeColor : Color.clear);
                }
            }
            tex.Apply();
            spriteRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        if (col is BoxCollider2D boxCol)
        {
            boxCol.size = new Vector2(0.8f, 1.2f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenHit) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            OnHitPlayer(other.gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasBeenHit) return;

        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            OnHitPlayer(collision.gameObject);
        }
    }

    private void OnHitPlayer(GameObject playerObj)
    {
        hasBeenHit = true;
        if (col != null) col.enabled = false;

        Debug.Log("<color=orange>[Obstacle] Player hit GroundSpike!</color>");

        // 1. Slowdown player movement
        PlayerMovement pm = playerObj.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.TakeHit(slowDuration, slowMultiplier);
        }

        // 2. Deduct bomb timer
        PlayerTimer pt = playerObj.GetComponent<PlayerTimer>();
        if (pt == null) pt = playerObj.GetComponentInChildren<PlayerTimer>();
        if (pt != null)
        {
            pt.DeductTime(timeDeduction);
        }

        // 3. Camera Shake
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.ShakeCamera(cameraShakeIntensity, cameraShakeDuration);
        }

        // 4. Hit VFX & Juice (Break / Crumble animation)
        Vector3 hitPos = transform.position;
        if (hitParticlePrefab != null)
        {
            ParticleSystem ps = Instantiate(hitParticlePrefab, hitPos, Quaternion.identity);
            ps.Play();
            Destroy(ps.gameObject, 2.0f);
        }
        else
        {
            DustParticleEffects.PlayLandDust(hitPos);
        }

        // 5. Break tween animation (squash & fade out)
        transform.DOKill();
        transform.DOScale(new Vector3(1.3f, 0.1f, 1f), 0.2f).SetEase(Ease.OutQuad);
        if (spriteRenderer != null)
        {
            spriteRenderer.DOFade(0f, 0.2f).OnComplete(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject, 0.2f);
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (spriteRenderer != null) spriteRenderer.DOKill();
    }
}
