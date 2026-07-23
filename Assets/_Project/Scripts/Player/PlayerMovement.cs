using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveForce = 15f;
    [SerializeField] private float maxVelocity = 10f;
    [SerializeField] private float jumpForce = 8f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    [Header("Jump Settings")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.6f;

    private int jumpsRemaining;
    private bool isGrounded;
    private Vector2 groundNormal = Vector2.up;

    private void Update()
    {
        CheckGrounded();

        if (Input.GetButtonDown("Jump") && jumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset Y velocity for consistent jump height
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpsRemaining--;
        }
    }

    private void CheckGrounded()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;

        if (isGrounded)
        {
            groundNormal = hit.normal;
            if (rb.linearVelocity.y <= 0.1f)
            {
                jumpsRemaining = maxJumps;
            }
        }
        else
        {
            groundNormal = Vector2.up;
        }
    }

    [Header("Brake / Self-Stop System")]
    [SerializeField] private float brakeForce = 12f;
    [SerializeField] private float angularBrakeDamping = 10f;
    [SerializeField] private float maxFlatAngle = 15f;
    [SerializeField] private float slopeGravityAssist = 8f;

    private void FixedUpdate()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        if (moveX < 0f) moveX = 0f; // Disable A / Left Arrow key input

        float surfaceAngle = Vector2.Angle(groundNormal, Vector2.up);
        bool isOnSlope = isGrounded && surfaceAngle > 5f;

        if (!Mathf.Approximately(moveX, 0f))
        {
            Vector2 forceDir = new Vector2(moveX, 0f);

            if (isOnSlope)
            {
                // Calculate slope tangent vectors (downhillTangent always points downward)
                Vector2 downhillTangent = new Vector2(groundNormal.y, -groundNormal.x);
                if (downhillTangent.y > 0f) downhillTangent = -downhillTangent;

                Vector2 desiredHorizontalDir = new Vector2(Mathf.Sign(moveX), 0f);
                if (Vector2.Dot(downhillTangent, desiredHorizontalDir) > 0f)
                {
                    forceDir = downhillTangent;
                }
                else
                {
                    forceDir = -downhillTangent;
                }
            }

            // Active player input
            rb.AddForce(forceDir * moveForce, ForceMode2D.Force);

            if (Mathf.Abs(rb.linearVelocity.x) > maxVelocity)
            {
                rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * maxVelocity, rb.linearVelocity.y);
            }
        }
        else
        {
            bool isFlatGround = isGrounded && surfaceAngle <= maxFlatAngle;

            if (isFlatGround)
            {
                // Self-stop system: Apply active counter-braking force & angular damping on flat surface
                Vector2 currentVel = rb.linearVelocity;
                if (Mathf.Abs(currentVel.x) > 0.05f)
                {
                    float targetVelX = Mathf.MoveTowards(currentVel.x, 0f, brakeForce * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector2(targetVelX, currentVel.y);
                }

                // Slow down rotation spinning
                rb.angularVelocity = Mathf.MoveTowards(rb.angularVelocity, 0f, angularBrakeDamping * Time.fixedDeltaTime * 100f);
            }
            else if (isOnSlope)
            {
                // On slopes without input: apply extra downhill force along slope tangent to overcome 2D static friction
                Vector2 downhillTangent = new Vector2(groundNormal.y, -groundNormal.x);
                if (downhillTangent.y > 0f) downhillTangent = -downhillTangent;

                rb.AddForce(downhillTangent * slopeGravityAssist, ForceMode2D.Force);
            }
        }
    }
}
