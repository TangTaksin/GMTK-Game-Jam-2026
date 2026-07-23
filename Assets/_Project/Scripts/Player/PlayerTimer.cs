using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    [SerializeField] private int maxTime = 5;
    [SerializeField] private float currentTime;

    [Header("Safe Stop Conditions")]
    [SerializeField] private float stopSpeedThreshold = 0.1f;
    [SerializeField] private float stopAngularSpeedThreshold = 1.0f;
    [SerializeField] private float maxFlatAngle = 15.0f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.6f;

    private Rigidbody2D rb;
    private bool isGrounded;
    private Vector2 groundNormal = Vector2.up;
    private bool isExploded;

    public float CurrentTime => currentTime;
    public int CurrentTimeDisplay => Mathf.CeilToInt(currentTime);
    public int MaxTime => maxTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentTime = maxTime;
    }

    private void Update()
    {
        if (isExploded) return;

        CheckGrounded();
        bool isCompletelySafe = IsSafeAndStopped();

        if (isCompletelySafe)
        {
            // Reset timer & freeze position
            currentTime = maxTime;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        else
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                Explode();
            }
        }
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        if (hit.collider != null)
        {
            isGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector2.up;
        }
    }

    private bool IsSafeAndStopped()
    {
        if (!isGrounded) return false;

        // Must NOT press active move input (A key / Left Arrow is disabled, so ignore moveX < 0)
        float moveX = Input.GetAxisRaw("Horizontal");
        if (moveX < 0f) moveX = 0f;
        bool hasNoInput = Mathf.Approximately(moveX, 0f);

        // Must stop both linear moving & angular spinning
        bool isLinearStopped = rb.linearVelocity.magnitude <= stopSpeedThreshold;
        bool isAngularStopped = Mathf.Abs(rb.angularVelocity) <= stopAngularSpeedThreshold;

        // Must be flat surface
        float surfaceAngle = Vector2.Angle(groundNormal, Vector2.up);
        bool isFlat = surfaceAngle <= maxFlatAngle;

        return hasNoInput && isLinearStopped && isAngularStopped && isFlat;
    }

    private void Explode()
    {
        isExploded = true;
        Debug.Log("<color=red>BOOM! Player Exploded!</color>");
        gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerGameOver();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, Vector2.down * groundCheckDistance);
    }
}
