using UnityEngine;
using DG.Tweening;

/// <summary>
/// Permanent Super Bomb dropped by player when pressing [S] at 999m.
/// Placed permanently on terrain until ChasingThreat collides with it, triggering Victory!
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PermanentSuperBomb : MonoBehaviour
{
    [Header("Visual & Audio Effects")]
    [SerializeField] private ParticleSystem explosionPrefab;
    [SerializeField] private Color glowColor = new Color(1f, 0.3f, 0f, 1f); // Fiery orange/gold glow

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private bool hasTriggered = false;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Tint existing bomb sprite with fiery glow color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = glowColor;
        }

        // Fiery pulsing tween animation
        transform.DOKill();
        transform.DOScale(new Vector3(1.3f, 1.3f, 1f), 0.35f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckThreatCollision(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckThreatCollision(collision.gameObject);
    }

    private void CheckThreatCollision(GameObject target)
    {
        if (hasTriggered) return;

        bool isThreat = target.GetComponent<ChasingThreat>() != null 
                     || target.GetComponentInParent<ChasingThreat>() != null 
                     || target.name.Contains("Threat") 
                     || target.name.Contains("Wall") 
                     || target.name.Contains("Boundary");

        if (isThreat)
        {
            hasTriggered = true;
            Debug.Log("<color=gold>★ PERMANENT SUPER BOMB BLASTED THREAT! VICTORY! ★</color>");

            Vector3 hitPos = transform.position;

            // 1. Play massive explosion VFX
            if (explosionPrefab != null)
            {
                ParticleSystem exp = Instantiate(explosionPrefab, hitPos, Quaternion.identity);
                exp.gameObject.SetActive(true);
                exp.Play(true);
                Destroy(exp.gameObject, 2.5f);
            }
            else
            {
                DustParticleEffects.PlayExplosion(hitPos);
                DustParticleEffects.PlayBloodSplatter(hitPos);
            }

            // 2. Camera Shake
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.ShakeCamera(0.6f, 0.8f);
            }

            // 3. Disable / Retreat ChasingThreat
            ChasingThreat threatComponent = target.GetComponent<ChasingThreat>();
            if (threatComponent == null) threatComponent = target.GetComponentInParent<ChasingThreat>();
            if (threatComponent != null)
            {
                threatComponent.TriggerGameOverRetreat();
            }

            // 4. Trigger Victory End Game in GameManager!
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TriggerVictory();
            }

            // Fade out and destroy bomb object
            transform.DOKill();
            if (spriteRenderer != null)
            {
                spriteRenderer.DOFade(0f, 0.2f).OnComplete(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject, 0.2f);
            }
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
